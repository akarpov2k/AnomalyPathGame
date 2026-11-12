#nullable enable
using System;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="CombatService"/>: оружие, меткость, шаги и ходы.</summary>
    public sealed partial class CombatService
    {
        /// <inheritdoc />
        public ItemInstance? PlayerWeapon(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return state.Player.Equipment?.Get(state.PlayerWeaponSlot);
        }

        /// <summary>Бронебойность заряженного патрона; пустой или неизвестный магазин — 0.</summary>
        /// <param name="weapon">Оружие атакующего; <c>null</c> — естественная атака.</param>
        /// <returns>Пробитие из данных патрона, 0..1.</returns>
        private double AmmoPenetration(ItemInstance? weapon)
        {
            if (weapon?.AmmoItemId == null || !_items.TryGet(weapon.AmmoItemId, out var ammo))
            {
                return 0;
            }

            return ammo.Penetration;
        }

        /// <inheritdoc />
        public bool TrySwapWeapon(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished || !ReferenceEquals(state.Current, state.Player))
            {
                return false;
            }

            var equipment = state.Player.Equipment;
            if (equipment == null)
            {
                return false;
            }

            var next = state.PlayerWeaponSlot == EquipmentSlot.Main
                ? EquipmentSlot.Secondary
                : EquipmentSlot.Main;
            var cost = _settings.WeaponSwapApCost + PlayerApPenalty(state, state.Player);
            if (equipment.Get(next) == null || state.Player.Ap < cost)
            {
                return false;
            }

            state.Player.Ap -= cost;
            state.PlayerWeaponSlot = next;
            return true;
        }

        /// <inheritdoc />
        public bool TryUseQuickWeapon(CombatState state, int quickIndex)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished || !ReferenceEquals(state.Current, state.Player))
            {
                return false;
            }

            var equipment = state.Player.Equipment;
            if (equipment == null || quickIndex < 0 || quickIndex >= equipment.QuickSlotCount)
            {
                return false;
            }

            var item = equipment.GetQuick(quickIndex);
            if (item == null)
            {
                return false;
            }

            var template = _items.Get(item.ItemId);
            if (template.Weapon == null)
            {
                return false;
            }

            var active = state.PlayerWeaponSlot;
            var equipped = equipment.Get(active);
            if (equipped != null && ReferenceEquals(equipped, item))
            {
                return false;
            }

            var cost = _settings.WeaponSwapApCost + PlayerApPenalty(state, state.Player);
            if (state.Player.Ap < cost)
            {
                return false;
            }

            equipment.Set(active, item);
            equipment.SetQuick(quickIndex, equipped);
            state.Player.Ap -= cost;
            return true;
        }

        /// <inheritdoc />
        public bool CanEnemyReload(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (state.IsFinished || !ReferenceEquals(state.Current, state.Enemy) || state.EnemyWeapon == null)
            {
                return false;
            }

            var block = _items.Get(state.EnemyWeapon.ItemId).Weapon;
            if (block == null || state.EnemyWeapon.MagazineRounds >= block.Magazine)
            {
                return false;
            }

            return state.Enemy.Ap >= _settings.ReloadApCost;
        }

        /// <inheritdoc />
        public bool TryEnemyReload(CombatState state)
        {
            if (!CanEnemyReload(state))
            {
                return false;
            }

            var block = _items.Get(state.EnemyWeapon!.ItemId).Weapon!;
            state.EnemyWeapon.Magazine = block.Magazine;
            state.Enemy.Ap -= _settings.ReloadApCost;
            return true;
        }

        /// <inheritdoc />
        public bool TryReload(CombatState state, ItemInstance weapon, GridContainer inventory)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (weapon == null)
            {
                throw new ArgumentNullException(nameof(weapon));
            }

            if (inventory == null)
            {
                throw new ArgumentNullException(nameof(inventory));
            }

            var attacker = state.Current;
            var template = _items.Get(weapon.ItemId);
            var cost = _settings.ReloadApCost + PlayerApPenalty(state, attacker);
            if (state.IsFinished || template.Weapon == null || attacker.Ap < cost)
            {
                return false;
            }

            var size = template.Weapon.Magazine;
            var current = weapon.MagazineRounds;
            var need = template.Weapon.ReloadPerShell ? 1 : size - current;
            if (need <= 0 || (template.Weapon.ReloadPerShell && current >= size))
            {
                return false;
            }

            var taken = _inventory.TakeAmmo(inventory, template.Weapon.AmmoCaliber, need);
            if (taken <= 0)
            {
                return false;
            }

            weapon.Magazine = current + taken;
            attacker.Ap -= cost;
            return true;
        }

        /// <inheritdoc />
        public double HitChance(CombatState state, ItemInstance weapon) => HitChance(state, weapon, null);

        private double HitChance(CombatState state, ItemInstance? weapon, SkillTemplate? skill) =>
            Chance(state, weapon, skill, includePity: true);

        /// <summary>
        /// Шанс попадания с учётом позиции, оружия и скилла. «Пожалей»-бонус включается флагом,
        /// а дополнительная его часть передаётся отдельно — так предпросмотр считает серию выстрелов.
        /// </summary>
        private double Chance(
            CombatState state,
            ItemInstance? weapon,
            SkillTemplate? skill,
            bool includePity,
            double extraPity = 0)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var template = weapon == null ? null : _items.Get(weapon.ItemId);
            var weaponBlock = template?.Weapon;
            if (weapon != null && weaponBlock == null)
            {
                return 0;
            }

            var attacker = state.Current;
            var defender = state.Opponent;
            var ratio = defender.Level > 0 ? attacker.Level / (double)defender.Level : attacker.Level;
            if (Math.Floor(ratio) > 5)
            {
                return Math.Clamp(ApplyNightPenalty(state, attacker, weaponBlock, 0.98), 0, 1);
            }

            var combat = attacker.Template.Combat;
            var accuracy = (combat?.Accuracy ?? 0.8) + (combat?.AccuracyBonus ?? 0) +
                           attacker.RarityAccuracyBonus + attacker.AccuracyBonus + attacker.Modifiers.AccuracyBonus;
            accuracy = ApplyNightPenalty(state, attacker, weaponBlock, accuracy);
            accuracy += extraPity;
            if (includePity)
            {
                accuracy += attacker.PityBonus;
            }

            var skillAccuracy = skill?.Accuracy ?? 1.0;
            var coefficient = (weaponBlock?.AccuracyCoefficient ?? 1.0) *
                              WeaponStats.Multiplier(weapon, template, _rarities);
            var chance = accuracy * coefficient * skillAccuracy;
            return Math.Clamp(chance, 0, 1);
        }

        private double BaseChance(
            CombatState state,
            Combatant attacker,
            WeaponBlock? weaponBlock,
            SkillTemplate? skill,
            double weaponMultiplier = 1)
        {
            var combat = attacker.Template.Combat;
            var accuracy = (combat?.Accuracy ?? 0.8) + (combat?.AccuracyBonus ?? 0) +
                           attacker.RarityAccuracyBonus + attacker.AccuracyBonus + attacker.Modifiers.AccuracyBonus;
            accuracy = ApplyNightPenalty(state, attacker, weaponBlock, accuracy);
            var skillAccuracy = skill?.Accuracy ?? 1.0;
            var coefficient = (weaponBlock?.AccuracyCoefficient ?? 1.0) * weaponMultiplier;
            return Math.Clamp(accuracy * coefficient * skillAccuracy, 0, 1);
        }

        /// <summary>
        /// Вычитает ночной штраф из базовой меткости атакующего до множителей позиции и скилла.
        /// Штраф действует на игрока и противника одинаково; 0 — ночь не влияет.
        /// </summary>
        private double ApplyNightPenalty(
            CombatState state,
            Combatant attacker,
            WeaponBlock? weaponBlock,
            double accuracy)
        {
            if (state.Phase != TimePhase.Night ||
                state.NightAccuracyPenalty <= 0 ||
                IgnoresNightPenalty(attacker, weaponBlock))
            {
                return accuracy;
            }

            return Math.Max(0, accuracy - state.NightAccuracyPenalty);
        }

        /// <summary>
        /// Заглушка будущих ночного прицела и ПНВ: сейчас не освобождает от штрафа никого.
        /// Когда появятся данные оружия и актёра, здесь будут проверяться их флаги.
        /// </summary>
        private static bool IgnoresNightPenalty(Combatant attacker, WeaponBlock? weaponBlock) => false;

        /// <inheritdoc />
        public void EndTurn(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            state.Current.TickStatuses(InjuryDurationKind.Turns, 1);
            state.Current = state.Opponent;
            state.Current.Ap = RestoreAp(state.Current);
            if (ReferenceEquals(state.Current, state.Player))
            {
                state.Round++;
                if (state.PlayerTurnDamage > 0)
                {
                    state.Player.Health = Math.Max(0, state.Player.Health - state.PlayerTurnDamage);
                }
            }
        }

    }
}
