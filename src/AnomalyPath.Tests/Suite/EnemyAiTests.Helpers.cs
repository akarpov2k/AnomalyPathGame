#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Services;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="EnemyAiTests"/>: помощники — бой, актёры, снаряжение и бросок.</summary>
    public sealed partial class EnemyAiTests
    {
        private static bool HasMedkit(IReadOnlyList<EnemyTurnStep> plan)
        {
            for (var i = 0; i < plan.Count; i++)
            {
                if (plan[i].Kind == EnemyTurnActionKind.Medkit)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Готовит бой, где игрок первым ходит и ранит противника выстрелами из АК-74,
        /// затем передаёт ход противнику: состояние остаётся с ходом ИИ.
        /// </summary>
        private static CombatState WeakenedEnemy(
            ServiceContainer services,
            CombatSettingsTemplate settings,
            ActorTemplate enemy,
            int shots,
            int playerHealth,
            out ICombatService combat,
            out CombatAi ai)
        {
            combat = NewCombat(services, settings, new StubRandom(0));
            ai = new CombatAi(combat, services.Resolve<IItemRepository>(), settings);
            var state = combat.Start(Player(playerHealth), EmptyEquipment(services), enemy);
            var rifle = new ItemInstance("ai_rifle", "wpn_ak74", 1, 100, 100, null, 30);
            for (var i = 0; i < shots; i++)
            {
                if (combat.TryAttack(state, rifle, out _))
                {
                    continue;
                }

                // ОД кончились: ход уходит противнику и возвращается без его действий.
                combat.EndTurn(state);
                combat.EndTurn(state);
                combat.TryAttack(state, rifle, out _);
            }

            combat.EndTurn(state);
            return state;
        }

        private static CombatSettingsTemplate Settings(double medkitHpThreshold = 0.3) => new CombatSettingsTemplate(
            "formula_damage_base",
            "formula_combat_xp",
            0,
            null,
            ApRestoreKind.Full,
            0,
            1,
            1,
            5,
            medkitHpThreshold);

        private static ICombatService NewCombat(
            ServiceContainer services,
            CombatSettingsTemplate settings,
            ICombatRandom random) =>
            new CombatService(
                services.Resolve<IItemRepository>(),
                services.Resolve<IDamageTypeRepository>(),
                services.Resolve<IEquipmentService>(),
                services.Resolve<IArmorWearService>(),
                services.Resolve<IFormulaRepository>(),
                settings,
                BalanceTemplate.Empty,
                random,
                services.Resolve<IInventoryService>(),
                services.Resolve<ISkillRepository>(),
                services.Resolve<IRarityRepository>(),
                services.Resolve<IInjuryRepository>());

        private static Equipment EmptyEquipment(ServiceContainer services) =>
            new Equipment("player", services.Resolve<IEquipmentSettings>().QuickSlots);

        private static ActorTemplate Player(int health) =>
            ActorTemplate.FromJson(
                JsonParser.Parse(
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"ai_player\",\"type\":\"npc\",\"nameKey\":\"n\"," +
                    "\"combat\":{\"tier\":1,\"level\":1,\"health\":" + health + ",\"ap\":8,\"accuracy\":0.8}}",
                    "test"),
                "test");

        private static ActorTemplate Enemy(
            string id,
            string weaponId,
            int ap,
            double accuracy,
            string? medkitItemId = null,
            int medkitCharges = 0)
        {
            var medkit = medkitItemId == null
                ? string.Empty
                : ",\"medkitItemId\":\"" + medkitItemId + "\",\"medkitCharges\":" + medkitCharges;
            return ActorTemplate.FromJson(
                JsonParser.Parse(
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"" + id + "\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                    "\"combat\":{\"tier\":1,\"level\":1,\"health\":100,\"ap\":" + ap + ",\"xp\":1," +
                    "\"accuracy\":" + accuracy.ToString("0.0", CultureInfo.InvariantCulture) + "," +
                    "\"attack\":{\"weaponId\":\"" + weaponId + "\"}" + medkit + "}}",
                    "test"),
                "test");
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
