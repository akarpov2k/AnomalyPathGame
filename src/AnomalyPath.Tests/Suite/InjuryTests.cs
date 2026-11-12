#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Травмы: данные, ссылки из скиллов, бросок при попадании, длительность часами и до лечения,
    /// перенос травм игрока между боями и сейв.
    /// </summary>
    public sealed class InjuryTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов травм.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public InjuryTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var injuries = services.Resolve<IInjuryRepository>();
            var skills = services.Resolve<ISkillRepository>();
            var settings = CombatSettings();

            Check("травмы: данные загружены", () =>
            {
                Equal(3, injuries.All.Count, "травм");
                True(injuries.Contains("injury_broken_leg"), "нога");
                True(injuries.Contains("injury_broken_arm"), "рука");
                True(injuries.Contains("injury_head_concussion"), "голова");

                var leg = injuries.Get("injury_broken_leg");
                Equal(1, leg.Modifiers.ActionApCostBonus, "цена действия");
                Equal(0, leg.Modifiers.AccuracyBonus, "точность не меняет");
                Equal(InjuryDurationKind.UntilHealed, leg.DurationKind, "вид длительности");
                Equal(0, leg.DurationAmount, "остатка нет");
                True(!leg.IsTimed, "до лечения");

                var head = injuries.Get("injury_head_concussion");
                Equal(-0.2, head.Modifiers.AccuracyBonus, "поправка к базе");
                Equal(InjuryDurationKind.Hours, head.DurationKind, "часы");
                Equal(4, head.DurationAmount, "часов");

                var arm = injuries.Get("injury_broken_arm");
                Equal(1, arm.Modifiers.ActionApCostBonus, "цена действия руки");
                True(!arm.IsTimed, "до лечения");
            });

            Check("травмы: скилл ссылается на них, а скилл без травм пуст", () =>
            {
                var maul = skills.Get("skill_maul");
                Equal(3, maul.Injuries.Count, "травм у удара лапой");
                Equal("injury_broken_arm", maul.Injuries[0], "первая травма");
                Equal(0, skills.Get("skill_shot").Injuries.Count, "у выстрела травм нет");
            });

            Check("попадание: травма выбирается случайно и накладывается", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = MeleeBear(services, combat);
                True(combat.TryUseSkill(state, null, "skill_maul", out var result), "удар лапой");

                Equal(1, result.Injuries.Count, "травм за попадание");
                Equal("injury_broken_arm", result.Injuries[0], "выбрана первая травма");
                Equal(1, state.Player.Statuses.Count, "статусов у игрока");
                Equal("injury_broken_arm", state.Player.Statuses[0].Id, "id травмы");
                Equal(1, state.Player.Modifiers.ActionApCostBonus, "цена действий выросла");
            });

            Check("попадание: шанс травмы не сработал", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.99));
                var state = MeleeBear(services, combat);
                True(combat.TryUseSkill(state, null, "skill_maul", out var result), "удар прошёл");

                Equal(0, result.Injuries.Count, "травм нет");
                Equal(0, state.Player.Statuses.Count, "статусов нет");
            });

            Check("попадание: скилл без травм не накладывает статус", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                True(combat.TryUseSkill(state, Weapon("wpn_ak74"), "skill_shot", out var result), "выстрел");

                Equal(0, result.Injuries.Count, "травм нет");
                Equal(0, state.Enemy.Statuses.Count, "статусов у врага нет");
            });

            Check("повтор: травма продлевает длительность, а не складывает модификаторы", () =>
            {
                var template = injuries.Get("injury_head_concussion");
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));

                state.Player.ApplyStatus(new CombatStatus(template, 2));
                state.Player.ApplyStatus(new CombatStatus(template, 2));

                Equal(1, state.Player.Statuses.Count, "один статус");
                Equal(4, state.Player.Statuses[0].Remaining, "длительность сложилась");
                Equal(-0.2, state.Player.Modifiers.AccuracyBonus, "модификатор не удвоился");
            });

            Check("нога: цена скилла растёт на единицу", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                state.Player.ApplyStatus(new CombatStatus(injuries.Get("injury_broken_leg"), 0));

                var action = FindAction(combat.GetActions(state, Weapon("wpn_ak74")), "skill_shot");
                Equal(2, action.ApCost, "выстрел стоит 2 ОД");
            });

            Check("рука: цена атаки растёт на единицу", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                state.Player.ApplyStatus(new CombatStatus(injuries.Get("injury_broken_arm"), 0));

                var action = FindAction(combat.GetActions(state, Weapon("wpn_ak74")), "skill_shot");
                Equal(2, action.ApCost, "выстрел стоит 2 ОД");
            });

            Check("голова: точность падает на 0.2 базового шанса", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var clean = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                var injured = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                var weapon = Weapon("wpn_ak74");
                var before = combat.HitChance(clean, weapon);

                injured.Player.ApplyStatus(new CombatStatus(injuries.Get("injury_head_concussion"), 4));
                var after = combat.HitChance(injured, weapon);

                True(before > after, "точность снизилась");
                True(Math.Abs(after - before * 0.75) < 1e-6, "база 0.8 стала 0.6: шанс умножился на 0.75");
            });

            Check("травмы: часы тикают только часовые травмы", () =>
            {
                var player = NewPlayer();
                player.ApplyInjury(new InjuryState("injury_head_concussion", InjuryDurationKind.Hours, 4));
                player.ApplyInjury(new InjuryState("injury_broken_leg", InjuryDurationKind.UntilHealed, 0));
                player.ApplyInjury(new InjuryState("injury_broken_arm", InjuryDurationKind.UntilHealed, 0));

                player.TickInjuries(InjuryDurationKind.Hours, 2);
                Equal(2, player.Injuries[0].Remaining, "остаток часов");
                Equal(3, player.Injuries.Count, "все травмы на месте");

                player.TickInjuries(InjuryDurationKind.Hours, 2);
                Equal(2, player.Injuries.Count, "часовой ушиб снят");
                Equal("injury_broken_leg", player.Injuries[0].Id, "первой снята часовая травма");

                player.TickInjuries(InjuryDurationKind.Hours, 100);
                Equal(2, player.Injuries.Count, "до лечения часами не тикает");
            });

            Check("травмы: лечение снимает травму по id", () =>
            {
                var player = NewPlayer();
                player.ApplyInjury(new InjuryState("injury_broken_arm", InjuryDurationKind.UntilHealed, 0));

                True(player.RemoveInjury("injury_broken_arm"), "снята");
                False(player.RemoveInjury("injury_broken_arm"), "повторно не снимается");
                Equal(0, player.Injuries.Count, "травм нет");
            });

            Check("травмы игрока попадают в бой с модификаторами", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"),
                    playerInjuries: new[] { new InjuryState("injury_broken_leg", InjuryDurationKind.UntilHealed, 0) });

                Equal(1, state.Player.Statuses.Count, "статусов");
                Equal("injury_broken_leg", state.Player.Statuses[0].Id, "id");
                Equal(InjuryDurationKind.UntilHealed, state.Player.Statuses[0].Kind, "вид длительности");
                Equal(1, state.Player.Modifiers.ActionApCostBonus, "модификатор из данных");
                var action = FindAction(combat.GetActions(state, Weapon("wpn_ak74")), "skill_shot");
                Equal(2, action.ApCost, "выстрел стал дороже на ОД");
            });

            WithFixture(new Dictionary<string, string>(), root =>
            {
                var sessions = Bootstrap.CreateSession(
                    services.Resolve<ITravelService>(),
                    services.Resolve<ICombatService>(),
                    new SaveService(new FileSaveStorage(root), services.Resolve<IItemRepository>()),
                    services.Resolve<INewGameService>(),
                    services.Resolve<IDeathService>(),
                    services.Resolve<ICombatOutcomeService>());
                var actors = services.Resolve<IActorRepository>();

                Check("сессия: бой возвращает травмы игрока и не сохраняет травму врага", () =>
                {
                    var session = NewSession(services);
                    session.State.Player!.ApplyInjury(
                        new InjuryState("injury_head_concussion", InjuryDurationKind.Hours, 2));

                    True(
                        sessions.StartCombat(
                            session,
                            actors.Get("actor_player"),
                            actors.Get("enemy_bandit")),
                        "бой начат");
                    Equal(1, session.Combat!.Player.Statuses.Count, "травма перенесена в бой");
                    session.Combat.Enemy.ApplyStatus(
                        new CombatStatus(injuries.Get("injury_broken_arm"), 0));

                    sessions.EndCombat(session);

                    Equal(1, session.State.Player.Injuries.Count, "травма игрока вернулась");
                    Equal("injury_head_concussion", session.State.Player.Injuries[0].Id, "id травмы");
                    Equal(
                        2,
                        session.State.Player.Injuries[0].Remaining,
                        "остаток пережит без изменений");
                });
            });

            Check("сейв: травма переживает запись и чтение", () =>
            {
                var json = new InjurySaveData("injury_head_concussion", InjuryDurationKind.Hours, 2).ToJson();
                var restored = InjurySaveData.FromJson(json, "save");

                Equal("injury_head_concussion", restored.Id, "id");
                Equal(InjuryDurationKind.Hours, restored.Kind, "вид");
                Equal(2, restored.Remaining, "остаток");
            });

            Check("сейв: неизвестный вид длительности отклоняется", () =>
            {
                var json = JsonParser.Parse(
                    "{\"id\":\"injury_broken_leg\",\"kind\":\"weeks\",\"remaining\":1}",
                    "save");
                Throws<DataException>(() => InjurySaveData.FromJson(json, "save"), "вид");
            });

            Check("разбор: правила травмы", () =>
            {
                const string valid = "{\"id\":\"injury_x\",\"nameKey\":\"n\",\"icon\":\"icons/injury_x\"," +
                                     "\"chance\":0.2,\"actionApCostBonus\":1," +
                                     "\"accuracyBonus\":-0.2,\"duration\":{\"kind\":\"hours\",\"amount\":2}}";
                var injury = InjuryTemplate.FromJson(JsonParser.Parse(valid, "injuries"), "injuries");
                Equal(0.2, injury.Chance, "шанс");
                Equal(InjuryDurationKind.Hours, injury.DurationKind, "вид");
                Equal("icons/injury_x", injury.Icon, "иконка");
                Equal(1, injury.Modifiers.ActionApCostBonus, "цена действия");
                Equal(-0.2, injury.Modifiers.AccuracyBonus, "точность");

                Throws<DataException>(
                    () => InjuryTemplate.FromJson(
                        JsonParser.Parse(
                            valid.Replace("\"icon\":\"icons/injury_x\",", string.Empty),
                            "injuries"),
                        "injuries"),
                    "нет иконки");
                Throws<DataException>(
                    () => InjuryTemplate.FromJson(
                        JsonParser.Parse(
                            valid.Replace("\"chance\":0.2", "\"chance\":0"),
                            "injuries"),
                        "injuries"),
                    "шанс 0");
                Throws<DataException>(
                    () => InjuryTemplate.FromJson(
                        JsonParser.Parse(
                            valid.Replace("\"kind\":\"hours\"", "\"kind\":\"days\""),
                            "injuries"),
                        "injuries"),
                    "неизвестный вид");
                Throws<DataException>(
                    () => InjuryTemplate.FromJson(
                        JsonParser.Parse(valid.Replace("\"amount\":2", "\"amount\":0"), "injuries"),
                        "injuries"),
                    "часы без длительности");
                Throws<DataException>(
                    () => InjuryTemplate.FromJson(
                        JsonParser.Parse(
                            "{\"id\":\"injury_x\",\"nameKey\":\"n\",\"icon\":\"icons/injury_x\",\"chance\":0.2}",
                            "injuries"),
                        "injuries"),
                    "нет duration");
                Throws<DataException>(
                    () => InjuryTemplate.FromJson(
                        JsonParser.Parse(
                            "{\"id\":\"injury_x\",\"nameKey\":\"n\",\"icon\":\"icons/injury_x\",\"chance\":0.2," +
                            "\"accuracyBonus\":-0.2,\"duration\":{\"kind\":\"until_healed\",\"amount\":3}}",
                            "injuries"),
                        "injuries"),
                    "до лечения с остатком");
            });

            Check("разбор: скилл не дублирует травмы", () =>
            {
                const string skill = "{\"id\":\"skill_x\",\"nameKey\":\"n\",\"descriptionKey\":\"d\"," +
                                     "\"type\":\"attack\",\"apCost\":1,\"accuracy\":1.0," +
                                     "\"injuries\":[\"injury_broken_leg\",\"injury_broken_leg\"]," +
                                     "\"effects\":[{\"type\":\"damage\",\"multiplier\":1.0,\"hits\":1}]}";
                Throws<DataException>(
                    () => SkillTemplate.FromJson(JsonParser.Parse(skill, "skills"), "skills"),
                    "дубль травмы");
            });
        }

        private CombatSettingsTemplate CombatSettings() =>
            CombatSettingsTemplate.FromJson(
                JsonParser.Parse(File.ReadAllText(Path.Combine(_dataRoot, "combat.json")), "combat"),
                "combat");

        private static PlayerState NewPlayer() =>
            new PlayerState(
                "player",
                1,
                0,
                100,
                8,
                new GridContainer("player", 8, 4),
                new Equipment("player", 4),
                Array.Empty<string>(),
                new NeedsState(100, 100, 100));

        private static GameSession NewSession(ServiceContainer services) =>
            new GameSession(new GameState(
                WorldState.Start(services.Resolve<IWorldGenerator>().Generate(42)),
                NewPlayer(),
                null));

        private static CombatService NewCombat(
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

        private static Equipment PlayerEquipment(ServiceContainer services) =>
            new Equipment("player", services.Resolve<IEquipmentSettings>().QuickSlots);

        private static CombatState MeleeBear(ServiceContainer services, ICombatService combat) =>
            combat.Start(
                services.Resolve<IActorRepository>().Get("actor_player"),
                PlayerEquipment(services),
                services.Resolve<IActorRepository>().Get("mutant_bear"),
                CombatScenario.Ambush);

        private static CombatAction FindAction(IReadOnlyList<CombatAction> actions, string skillId)
        {
            foreach (var action in actions)
            {
                if (string.Equals(action.SkillId, skillId, StringComparison.Ordinal))
                {
                    return action;
                }
            }

            throw new InvalidOperationException("Действие не найдено: " + skillId);
        }

        private static ItemInstance Weapon(string id) =>
            new ItemInstance("w", id, 1, 100, 100, null, 30);

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
