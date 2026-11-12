#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Services;


namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="CombatTests"/>: помощники — бой, оружие, магазин и настройки.</summary>
    public sealed partial class CombatTests
    {
        private static CombatService Custom(ServiceContainer services, ICombatRandom random)
        {
            return new CombatService(
                services.Resolve<IItemRepository>(),
                services.Resolve<IDamageTypeRepository>(),
                services.Resolve<IEquipmentService>(),
                services.Resolve<IArmorWearService>(),
                services.Resolve<IFormulaRepository>(),
                Settings(),
                BalanceTemplate.Empty,
                random,
                services.Resolve<IInventoryService>(),
                services.Resolve<ISkillRepository>(),
                services.Resolve<IRarityRepository>(),
                services.Resolve<IInjuryRepository>());
        }

        private static CombatSettingsTemplate Settings(
            ApRestoreKind restore = ApRestoreKind.Full,
            int perTurn = 0) => new CombatSettingsTemplate(
            "formula_damage_base",
            "formula_combat_xp",
            0,
            null,
            restore,
            perTurn,
            1);

        private static ItemInstance Weapon(string id) =>
            new ItemInstance("w", id, 1, 100, 100, null, MagazineSize(id));

        private static int MagazineSize(string id) => id switch
        {
            "wpn_pm" => 8,
            "wpn_sv98" => 10,
            _ => 30,
        };

        private static CombatState Start(
            ICombatService combat,
            IActorRepository actors,
            IEquipmentService equipment,
            IEquipmentSettings settings,
            int playerApCostBonus = 0)
        {
            var state = new Equipment("player", settings.QuickSlots);
            return combat.Start(
                actors.Get("actor_player"),
                state,
                actors.Get("enemy_bandit"),
                playerApCostBonus: playerApCostBonus);
        }

        private sealed class StubRandom : ICombatRandom
        {
            private readonly double _value;

            public StubRandom(double value)
            {
                _value = value;
            }

            public double NextDouble() => _value;
        }
    }
}
