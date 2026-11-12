#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Templates;
using Equipment = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Медицина: данные расходников, применение вне боя, применение в бою за ОД,
    /// снятие травм, аптечка противника и сейв зарядов.
    /// </summary>
    public sealed partial class HealingTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов лечения.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public HealingTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var items = services.Resolve<IItemRepository>();
            var medicines = services.Resolve<IConsumableService>();
            var injuries = services.Resolve<IInjuryRepository>();
            var settings = CombatSettings();

            DataTests(items, services, settings);
            OutOfCombatTests(medicines, items, services);
            CombatTests(services, settings, injuries);
            SessionMedicineTests(services);
            EffectPersistenceTests(services);
            EnemyMedkitTests(services, settings);
            SaveTests(items, services);
            ParserTests();
            ValidationTests();
        }

        private void DataTests(IItemRepository items, ServiceContainer services, CombatSettingsTemplate settings)
        {
            Check("медицина: одиннадцать расходников загружены", () =>
            {
                var count = 0;
                foreach (var item in items.All)
                {
                    if (item.Category == ItemCategory.Consumable)
                    {
                        count++;
                    }
                }

                Equal(11, count, "расходников");
            });

            Check("медицина: аптечка — боевая, лечит здоровье", () =>
            {
                var medkit = items.Get("itm_medkit_normal");
                var block = medkit.Consumable!;
                Equal(2, block.UseTime, "цена в ОД");
                Equal(2, block.Charges, "применений");
                True(block.UsableInCombat, "можно в бою");
                Equal(30, block.TotalHeal(), "лечение");
                Equal(0, block.HealsInjuries.Count, "травм не лечит");
                True(Contains(medkit.EquipSlots, "quick"), "быстрый слот");
            });

            Check("медицина: шина лечит руки и ноги, но не голову", () =>
            {
                var splint = items.Get("itm_splint").Consumable!;
                False(splint.UsableInCombat, "только вне боя");
                Equal(2, splint.HealsInjuries.Count, "травм");
                True(Contains(splint.HealsInjuries, "injury_broken_arm"), "рука");
                True(Contains(splint.HealsInjuries, "injury_broken_leg"), "нога");
                False(Contains(splint.HealsInjuries, "injury_head_concussion"), "голова");
                Equal(0, items.Get("itm_splint").EquipSlots.Count, "в быстрый слот не кладётся");
            });

            Check("медицина: сурв лечит все три травмы", () =>
            {
                var surv = items.Get("itm_surv").Consumable!;
                Equal(3, surv.HealsInjuries.Count, "травм");
                True(Contains(surv.HealsInjuries, "injury_head_concussion"), "голова");
                Equal(3, surv.Charges, "применений");
            });

            Check("медицина: большая аптечка лечит здоровье и все травмы", () =>
            {
                var large = items.Get("itm_medkit_large").Consumable!;
                Equal(100, large.TotalHeal(), "лечение");
                Equal(3, large.HealsInjuries.Count, "травм");
                False(large.UsableInCombat, "только вне боя");
            });

            Check("медицина: стимуляторы дают ОД и защиту", () =>
            {
                var adrenaline = items.Get("itm_stim_adrenaline").Consumable!;
                Equal(3, adrenaline.TotalApRestore(), "ОД");
                True(adrenaline.UsableInCombat, "боевой");

                var guard = items.Get("itm_stim_guard").Consumable!;
                var effect = guard.Effects[0];
                Equal(ConsumableEffectType.Modifier, effect.Type, "тип");
                Equal(ConsumableStat.Protection, effect.Stat ?? ConsumableStat.Accuracy, "характеристика");
                Equal(3.0, effect.Value, "величина");
                Equal("dmg_bullet", effect.DamageTypeId, "тип урона");
            });

            Check("медицина: у наёмника есть аптечка, у бандита нет", () =>
            {
                var merc = services.Resolve<IActorRepository>().Get("enemy_merc");
                Equal("itm_medkit_army", merc.Combat!.MedkitItemId, "аптечка наёмника");
                Equal(2, merc.Combat.MedkitCharges, "применений");

                var bandit = services.Resolve<IActorRepository>().Get("enemy_bandit");
                True(bandit.Combat!.MedkitItemId == null, "у бандита аптечки нет");
                Equal(2, bandit.Combat.Attack!.WeaponIds.Count, "у бандита два варианта оружия");

                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    merc);
                Equal(2, state.EnemyMedkitCharges, "заряды в бою");
            });
        }


        private static CombatService NewCombat(
            ServiceContainer services,
            CombatSettingsTemplate settings,
            ICombatRandom random,
            IItemRepository? items = null) =>
            new CombatService(
                items ?? services.Resolve<IItemRepository>(),
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

        private static Equipment PlayerEquipment(ServiceContainer services) =>
            new Equipment("player", services.Resolve<IEquipmentSettings>().QuickSlots);

        private static PlayerState NewPlayer(int health) =>
            new PlayerState(
                "player",
                1,
                0,
                health,
                8,
                new GridContainer("player", 8, 6),
                new Equipment("player", 4),
                Array.Empty<string>(),
                new NeedsState(100, 100, 100));

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            foreach (var item in values)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static ItemTemplate Parse(string json) => ItemTemplate.FromJson(JsonParser.Parse(json, "items"), "items");

        private static string ConsumableJson(string consumable, string extra = "") =>
            "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"itm_test\"," +
            "\"nameKey\":\"n\",\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1," +
            "\"tier\":1,\"icon\":\"i\"" + extra + ",\"consumable\":" + consumable + "}";

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
