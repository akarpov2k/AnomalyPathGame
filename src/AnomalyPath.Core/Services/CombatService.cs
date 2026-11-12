#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Talents;
using AnomalyPath.Core.World;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Боевой расчёт: урон по формуле из данных, защита, износ оружия и брони, ходы.
    /// </summary>
    public sealed partial class CombatService : ICombatService
    {
        private readonly IItemRepository _items;
        private readonly IDamageTypeRepository _damageTypes;
        private readonly IEquipmentService _equipment;
        private readonly IArmorWearService _wear;
        private readonly IFormulaRepository _formulas;
        private readonly CombatSettingsTemplate _settings;
        private readonly BalanceTemplate _balance;
        private readonly ICombatRandom _random;
        private readonly IInventoryService _inventory;
        private readonly ISkillRepository _skills;
        private readonly IRarityRepository _rarities;
        private readonly IInjuryRepository _injuries;

        /// <summary>Создаёт боевой сервис.</summary>
        /// <param name="items">Репозиторий предметов.</param>
        /// <param name="damageTypes">Репозиторий типов урона.</param>
        /// <param name="equipment">Экипировка.</param>
        /// <param name="wear">Износ.</param>
        /// <param name="formulas">Формулы.</param>
        /// <param name="settings">Настройки боя.</param>
        /// <param name="balance">Константы баланса.</param>
        /// <param name="random">Источник бросков.</param>
        /// <param name="inventory">Инвентарь для патронов.</param>
        /// <param name="skills">Репозиторий скиллов.</param>
        /// <param name="rarities">Репозиторий редкостей: поправка к меткости актёра.</param>
        /// <param name="injuries">Репозиторий травм: выбор и наложение при попадании.</param>
        public CombatService(
            IItemRepository items,
            IDamageTypeRepository damageTypes,
            IEquipmentService equipment,
            IArmorWearService wear,
            IFormulaRepository formulas,
            CombatSettingsTemplate settings,
            BalanceTemplate balance,
            ICombatRandom random,
            IInventoryService inventory,
            ISkillRepository skills,
            IRarityRepository rarities,
            IInjuryRepository injuries)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _damageTypes = damageTypes ?? throw new ArgumentNullException(nameof(damageTypes));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _wear = wear ?? throw new ArgumentNullException(nameof(wear));
            _formulas = formulas ?? throw new ArgumentNullException(nameof(formulas));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _balance = balance ?? throw new ArgumentNullException(nameof(balance));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _skills = skills ?? throw new ArgumentNullException(nameof(skills));
            _rarities = rarities ?? throw new ArgumentNullException(nameof(rarities));
            _injuries = injuries ?? throw new ArgumentNullException(nameof(injuries));
        }

        /// <inheritdoc />
        public CombatState Start(
            ActorTemplate playerActor,
            EquipmentState playerEquipment,
            ActorTemplate enemy,
            CombatScenario? scenario = null,
            TalentModifiers? playerModifiers = null,
            int playerHealthBonus = 0,
            IReadOnlyList<InjuryState>? playerInjuries = null,
            AnomalyPath.Core.Artifacts.ArtifactModifiers? playerArtifacts = null,
            TimePhase phase = TimePhase.Day,
            double nightAccuracyPenalty = 0,
            CombatEncounterContext? context = null,
            int playerApCostBonus = 0,
            int playerTurnDamage = 0,
            int enemyWeaponSeed = 0)
        {
            if (playerActor == null)
            {
                throw new ArgumentNullException(nameof(playerActor));
            }

            if (playerEquipment == null)
            {
                throw new ArgumentNullException(nameof(playerEquipment));
            }

            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            var player = new Combatant(
                playerActor,
                playerEquipment,
                playerModifiers,
                playerHealthBonus,
                RarityAccuracy(playerActor),
                playerArtifacts);
            ApplyInjuries(player, playerInjuries);
            var opponent = new Combatant(enemy, rarityAccuracyBonus: RarityAccuracy(enemy));
            var state = new CombatState(
                player,
                opponent,
                scenario ?? CombatScenario.Normal,
                CreateEnemyWeapon(enemy, enemyWeaponSeed),
                enemy.Combat?.MedkitCharges ?? 0,
                phase,
                nightAccuracyPenalty,
                context,
                playerApCostBonus,
                playerTurnDamage);
            if (playerTurnDamage > 0)
            {
                player.Health = Math.Max(0, player.Health - playerTurnDamage);
            }

            return state;
        }

        /// <summary>Переносит активные травмы игрока в бой: модификаторы берутся из данных, остаток — из состояния.</summary>
        private void ApplyInjuries(Combatant player, IReadOnlyList<InjuryState>? injuries)
        {
            if (injuries == null)
            {
                return;
            }

            for (var i = 0; i < injuries.Count; i++)
            {
                var injury = injuries[i];
                var template = _injuries.Get(injury.Id);
                player.ApplyStatus(new CombatStatus(template, injury.Remaining));
            }
        }

        /// <summary>Прибавка к цене действия в ОД только для игрока: голод, зафиксированный на бой.</summary>
        /// <param name="state">Состояние боя.</param>
        /// <param name="actor">Кто платит ОД.</param>
        /// <returns>Прибавка к цене; у противника всегда 0.</returns>
        private static int PlayerApPenalty(CombatState state, Combatant actor) =>
            ReferenceEquals(actor, state.Player) ? state.PlayerApCostBonus : 0;

        private double RarityAccuracy(ActorTemplate actor) =>
            _rarities.Get(actor.Rarity).AccuracyBonus;

        /// <summary>Создаёт оружие противника из данных; у естественной атаки оружия нет.</summary>
        private ItemInstance? CreateEnemyWeapon(ActorTemplate enemy, int seed)
        {
            var attack = enemy.Combat?.Attack;
            if (attack == null || attack.IsNatural)
            {
                return null;
            }

            var weaponIds = attack.WeaponIds;
            var weaponId = weaponIds.Count == 1
                ? weaponIds[0]
                : weaponIds[new SeededWorldRandom(seed).NextInt(0, weaponIds.Count)];
            var template = _items.Get(weaponId);
            if (template.Weapon == null)
            {
                throw new DataException(enemy.Id, CoreErrors.WeaponWithoutBlock, weaponId);
            }

            return new ItemInstance("enemy_weapon", weaponId, 1, null, null, null, template.Weapon.Magazine);
        }
    }
}
