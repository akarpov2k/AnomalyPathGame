#nullable enable
using System;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Часть класса <see cref="AutoPlayDriver"/>: ассист при опасности. Лечит, кормит,
    /// восстанавливает силы, докладывает патроны и аптечки — каждое такое действие
    /// попадает в журнал, чтобы прогон оставался прозрачным.
    /// </summary>
    internal sealed partial class AutoPlayDriver
    {
        /// <summary>Подготовка к вылазке: здоровье, еда, вода, силы, патроны и аптечки.</summary>
        private void PrepareForExpedition(GameScreen screen, GameSession session)
        {
            HealOutsideCombat(screen, session);
            RestoreNeeds(session);
            if (WorldStaminaLow(session))
            {
                RestoreStamina(session);
            }

            var weapon = session.State.Player?.Equipment.Get(
                AnomalyPath.Core.Equipment.EquipmentSlot.Main);
            if (weapon != null)
            {
                EnsureAmmo(screen, session, weapon);
            }

            EnsureMedkits(screen, session);
            TryPutMedkitInQuickSlot(screen, session);
        }

        /// <summary>Подготовка к бою: лечение и ресурсы до нажатия фигуры противника.</summary>
        /// <returns><c>true</c>, если что-то изменилось и бой лучше начать следующим шагом.</returns>
        private bool PrepareForFight(GameScreen screen, GameSession session)
        {
            var changed = HealOutsideCombat(screen, session);
            changed |= RestoreNeeds(session);
            var player = session.State.Player;
            var weapon = player?.Equipment.Get(AnomalyPath.Core.Equipment.EquipmentSlot.Main);
            if (weapon != null)
            {
                changed |= EnsureAmmo(screen, session, weapon);
            }

            changed |= EnsureMedkits(screen, session);
            changed |= TryPutMedkitInQuickSlot(screen, session);
            return changed;
        }

        /// <summary>Лечит до максимума вне боя, если здоровье неполное.</summary>
        private bool HealOutsideCombat(GameScreen screen, GameSession session)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return false;
            }

            var max = screen.Damage.MaxHealth(session.State);
            if (max <= 0 || player.Health >= max)
            {
                return false;
            }

            var before = player.Health;
            player.SetHealth(max);
            _log.Assist("лечение вне боя: " + before + " → " + max);
            return true;
        }

        /// <summary>Восстанавливает еду и воду, если они ниже половины шкалы.</summary>
        private bool RestoreNeeds(GameSession session)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return false;
            }

            var changed = false;
            if (player.Needs.Hunger < player.Needs.FoodMax / 2)
            {
                _log.Assist("еда: " + player.Needs.Hunger + " → " + player.Needs.FoodMax);
                player.Needs.SetHunger(player.Needs.FoodMax);
                changed = true;
            }

            if (player.Needs.Thirst < player.Needs.WaterMax / 2)
            {
                _log.Assist("вода: " + player.Needs.Thirst + " → " + player.Needs.WaterMax);
                player.Needs.SetThirst(player.Needs.WaterMax);
                changed = true;
            }

            return changed;
        }

        /// <summary>Мало ли выносливости: меньше половины запаса.</summary>
        private static bool WorldStaminaLow(GameSession session)
        {
            var world = session.State.World;
            return world.StaminaMax > 0 && world.Stamina < world.StaminaMax / 2;
        }

        /// <summary>Восстанавливает выносливость до максимума.</summary>
        private void RestoreStamina(GameSession session)
        {
            var world = session.State.World;
            if (world.Stamina >= world.StaminaMax)
            {
                return;
            }

            var before = world.Stamina;
            world.RestoreStamina(world.StaminaMax);
            _log.Assist("выносливость: " + before + " → " + world.Stamina);
        }

        /// <summary>Докладывает патроны, если их осталось меньше 60.</summary>
        private bool EnsureAmmo(GameScreen screen, GameSession session, ItemInstance weapon)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return false;
            }

            if (!screen.Items.TryGet(weapon.ItemId, out var weaponTemplate) ||
                weaponTemplate.Weapon == null)
            {
                return false;
            }

            var caliber = weaponTemplate.Weapon.AmmoCaliber;
            var ammoId = FindAmmoId(screen, caliber);
            if (ammoId == null)
            {
                return false;
            }

            var have = CountAmmo(screen, session, caliber);
            if (have >= 60)
            {
                return false;
            }

            if (!screen.Inventory.TryAdd(player.Inventory, ammoId, 120, out _))
            {
                _log.Warn("патроны " + caliber + " не поместились в инвентарь");
                return false;
            }

            _log.Assist("патроны " + caliber + ": " + have + " → " + CountAmmo(screen, session, caliber));
            return true;
        }

        /// <summary>Ищет патрон калибра оружия в данных.</summary>
        private static string? FindAmmoId(GameScreen screen, string caliber)
        {
            foreach (var item in screen.Items.All)
            {
                if (item.Category == ItemCategory.Ammo &&
                    string.Equals(item.Caliber, caliber, StringComparison.Ordinal))
                {
                    return item.Id;
                }
            }

            return null;
        }

        /// <summary>Считает патроны калибра в инвентаре.</summary>
        private static int CountAmmo(GameScreen screen, GameSession session, string caliber)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return 0;
            }

            var total = 0;
            foreach (var item in player.Inventory.Items)
            {
                if (screen.Items.TryGet(item.ItemId, out var template) &&
                    template.Category == ItemCategory.Ammo &&
                    string.Equals(template.Caliber, caliber, StringComparison.Ordinal))
                {
                    total += item.Stack;
                }
            }

            return total;
        }

        /// <summary>Держит в инвентаре две боевые аптечки.</summary>
        private bool EnsureMedkits(GameScreen screen, GameSession session)
        {
            var player = session.State.Player;
            var medkitId = FindBattleMedkitId(screen);
            if (player == null || medkitId == null)
            {
                return false;
            }

            var have = screen.Inventory.Count(player.Inventory, medkitId);
            if (have >= 2)
            {
                return false;
            }

            if (!screen.Inventory.TryAdd(player.Inventory, medkitId, 2 - have, out _))
            {
                _log.Warn("аптечки не поместились в инвентарь");
                return false;
            }

            _log.Assist("аптечки: " + have + " → " + (have + 2 - have));
            return true;
        }

        /// <summary>Ищет боевую медицину с лечением в данных предметов.</summary>
        private static string? FindBattleMedkitId(GameScreen screen)
        {
            foreach (var item in screen.Items.All)
            {
                if (item.Category != ItemCategory.Consumable || item.Consumable == null)
                {
                    continue;
                }

                if (item.Consumable.UsableInCombat && item.Consumable.TotalHeal() > 0)
                {
                    return item.Id;
                }
            }

            return null;
        }

        /// <summary>Кладёт боевую аптечку из инвентаря в первый быстрый слот.</summary>
        private bool TryPutMedkitInQuickSlot(GameScreen screen, GameSession session)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return false;
            }

            var equipment = player.Equipment;
            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                var quick = equipment.GetQuick(index);
                if (quick != null && IsBattleMedkit(screen, quick))
                {
                    return false;
                }
            }

            foreach (var item in player.Inventory.Items)
            {
                if (!IsBattleMedkit(screen, item))
                {
                    continue;
                }

                if (screen.Equipment.TryPutQuick(equipment, 0, item, out _))
                {
                    _log.Assist("аптечка в быстром слоте");
                    return true;
                }
            }

            return false;
        }

        /// <summary>Боевая ли это медицина с лечением.</summary>
        private static bool IsBattleMedkit(GameScreen screen, ItemInstance item)
        {
            return screen.Items.TryGet(item.ItemId, out var template) &&
                   template.Category == ItemCategory.Consumable &&
                   template.Consumable != null &&
                   template.Consumable.UsableInCombat &&
                   template.Consumable.TotalHeal() > 0;
        }
    }
}
