#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="HealingTests"/>: медицина в бою и перенос эффектов.</summary>
    public sealed partial class HealingTests
    {
        private void CombatTests(
            ServiceContainer services,
            CombatSettingsTemplate settings,
            IInjuryRepository injuries)
        {
            Check("бой: аптечка лечит за ОД из быстрого слота", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var equipment = PlayerEquipment(services);
                var medkit = new ItemInstance("med", "itm_medkit_normal", 1, charges: 2);
                equipment.SetQuick(0, medkit);

                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("enemy_bandit"),
                    CombatScenario.Ambush);
                True(combat.TryEnemyAttack(state, out var hit), "враг атакует");
                True(hit.Hit, "попадание");
                combat.EndTurn(state);
                Equal(3, state.Player.Ap, "ОД после смены хода");

                var health = state.Player.Health;
                var result = combat.TryUseMedicine(state, medkit);

                True(result.Succeeded, "применение");
                Equal(Math.Min(30, state.Player.MaxHealth - (int)health), result.HealthRestored, "вылечено");
                Equal(3 - 2, state.Player.Ap, "ОД списаны");
                Equal(1, result.ChargesLeft, "заряды");
            });

            Check("голод: медицина дороже на 1 ОД", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var equipment = PlayerEquipment(services);
                var medkit = new ItemInstance("med", "itm_medkit_normal", 1, charges: 2);
                equipment.SetQuick(0, medkit);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("enemy_bandit"),
                    CombatScenario.Ambush,
                    playerApCostBonus: 1);
                combat.TryEnemyAttack(state, out _);
                combat.EndTurn(state);

                var result = combat.TryUseMedicine(state, medkit);

                True(result.Succeeded, "применение");
                Equal(3 - 3, state.Player.Ap, "цена медицины с голодом");
            });

            Check("бой: медицина не из быстрого слота недоступна", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                var medkit = new ItemInstance("med", "itm_medkit_normal", 1, charges: 2);

                False(combat.CanUseMedicine(state, medkit), "не в слоте");
                False(combat.TryUseMedicine(state, medkit).Succeeded, "отказ");
            });

            Check("бой: медицина «только вне боя» в бою не работает", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var equipment = PlayerEquipment(services);
                var splint = new ItemInstance("splint", "itm_splint", 1, charges: 1);
                equipment.SetQuick(0, splint);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));

                False(combat.CanUseMedicine(state, splint), "нельзя");
                False(combat.TryUseMedicine(state, splint).Succeeded, "отказ");
            });

            Check("бой: чужой ход запрещает медицину", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var equipment = PlayerEquipment(services);
                var medkit = new ItemInstance("med", "itm_medkit_normal", 1, charges: 2);
                equipment.SetQuick(0, medkit);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));

                combat.EndTurn(state);
                False(combat.CanUseMedicine(state, medkit), "ход противника");
            });

            Check("бой: медицина снимает травму из статусов", () =>
            {
                var template = InjuryTemplate.FromJson(
                    JsonParser.Parse(
                        "{\"id\":\"injury_test_heal\",\"nameKey\":\"n\",\"icon\":\"icons/injury_test_heal\"," +
                        "\"chance\":0.5," +
                        "\"accuracyBonus\":-0.1,\"duration\":{\"kind\":\"until_healed\",\"amount\":0}}",
                        "injuries"),
                    "injuries");
                var testItem = ItemTemplate.FromJson(
                    JsonParser.Parse(
                        ConsumableJson(
                            "{\"useTime\":1,\"charges\":1,\"usableInCombat\":true," +
                            "\"effects\":[{\"type\":\"heal\",\"amount\":5}]," +
                            "\"healsInjuries\":[\"injury_test_heal\"]}"),
                        "items"),
                    "items");
                var testItems = new ItemRepository(new[] { testItem });
                var combat = NewCombat(services, settings, new StubRandom(0.0), testItems);
                var equipment = PlayerEquipment(services);
                var kit = new ItemInstance("kit", testItem.Id, 1, charges: 1);
                equipment.SetQuick(0, kit);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("mutant_bear"));
                state.Player.ApplyStatus(new CombatStatus(template, 0));

                var result = combat.TryUseMedicine(state, kit);

                True(result.Succeeded, "применение");
                Equal(1, result.InjuriesHealed.Count, "снята травма");
                Equal(0, state.Player.Statuses.Count, "статусов нет");
                True(result.ItemConsumed, "израсходован");
                True(equipment.GetQuick(0) == null, "слот пуст");
            });

            Check("бой: стимулятор восстанавливает ОД", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var equipment = PlayerEquipment(services);
                var stim = new ItemInstance("stim", "itm_stim_adrenaline", 1, charges: 1);
                equipment.SetQuick(0, stim);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));
                var ak = new ItemInstance("stim_ak", "wpn_ak74", 1, 100, 100, null, 30);
                True(combat.TryUseSkill(state, ak, "skill_burst", out _), "очередь");

                var result = combat.TryUseMedicine(state, stim);

                True(result.Succeeded, "применение");
                Equal(3, result.ApRestored, "ОД");
                Equal(3 - 2 - 1 + 3, state.Player.Ap, "ОД после");
                Equal(3, state.Player.MaxAp, "максимум");
            });

            Check("бой: стимулятор защиты даёт модификатор до конца боя", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var equipment = PlayerEquipment(services);
                var stim = new ItemInstance("guard", "itm_stim_guard", 1, charges: 1);
                equipment.SetQuick(0, stim);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("enemy_bandit"));

                var result = combat.TryUseMedicine(state, stim);

                True(result.Succeeded, "применение");
                Equal(1, state.Player.Statuses.Count, "статус");
                False(state.Player.Statuses[0].IsInjury, "не травма");
                Equal(3, state.Player.Modifiers.ProtectionOf("dmg_bullet"), "защита");
                Equal(0, state.Player.Modifiers.ProtectionOf("dmg_fire"), "другой тип не тронут");
            });

            Check("бой: нехватка ОД не даёт применить медицину", () =>
            {
                var slow = ItemTemplate.FromJson(
                    JsonParser.Parse(
                        ConsumableJson("{\"useTime\":99,\"charges\":1,\"usableInCombat\":true," +
                                       "\"effects\":[{\"type\":\"heal\",\"amount\":5}]}"),
                        "items"),
                    "items");
                var testItems = new ItemRepository(new[] { slow });
                var combat = NewCombat(services, settings, new StubRandom(0.0), testItems);
                var equipment = PlayerEquipment(services);
                var item = new ItemInstance("slow", slow.Id, 1, charges: 1);
                equipment.SetQuick(0, item);
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    equipment,
                    services.Resolve<IActorRepository>().Get("mutant_bear"));

                False(combat.CanUseMedicine(state, item), "цена больше бюджета");
                False(combat.TryUseMedicine(state, item).Succeeded, "отказ");
                Equal(3, state.Player.Ap, "ОД целы");
            });
        }

        private void SessionMedicineTests(ServiceContainer services)
        {
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

                Check("сессия: медицина из быстрого слота применяется в свой ход", () =>
                {
                    var player = NewPlayer(100);
                    var stim = new ItemInstance("med", "itm_stim_adrenaline", 1, charges: 1);
                    player.Equipment.SetQuick(0, stim);
                    player.Equipment.Set(
                        EquipmentSlot.Main,
                        new ItemInstance("med_ak", "wpn_ak74", 1, magazine: 30));
                    var session = new GameSession(
                        new GameState(
                            WorldState.Start(services.Resolve<IWorldGenerator>().Generate(42)),
                            player,
                            null));
                    True(
                        sessions.StartCombat(
                            session,
                            actors.Get("actor_player"),
                            actors.Get("enemy_bandit")),
                        "бой начат");
                    var state = session.Combat!;
                    True(sessions.TryUseCombatSkill(session, "skill_burst", out _), "очередь потратила ОД");
                    var apBefore = state.Player.Ap;

                    True(sessions.CanUseMedicine(session, stim), "медицина доступна");
                    True(sessions.TryUseMedicine(session, stim, out var result), "применение");
                    True(result.Succeeded, "итог применения");
                    True(result.ApRestored > 0, "ОД восстановлены");
                    Equal(apBefore - 1 + result.ApRestored, state.Player.Ap, "ОД после медицины");
                    Equal(0, result.ChargesLeft, "зарядов не осталось");
                });

                Check("сессия: травмы игрока попадают в бой", () =>
                {
                    var player = NewPlayer(100);
                    player.SetInjuries(new[]
                    {
                        new InjuryState("injury_head_concussion", InjuryDurationKind.Hours, 2)
                    });
                    var session = new GameSession(
                        new GameState(
                            WorldState.Start(services.Resolve<IWorldGenerator>().Generate(42)),
                            player,
                            null));
                    True(
                        sessions.StartCombat(
                            session,
                            actors.Get("actor_player"),
                            actors.Get("enemy_bandit")),
                        "бой начат");
                    Equal(1, session.Combat!.Player.Statuses.Count, "статусов в бою");
                    True(session.Combat.Player.Statuses[0].IsInjury, "статус — травма");
                });

                Check("сессия: без боя медицина через бой недоступна", () =>
                {
                    var session = new GameSession(
                        new GameState(
                            WorldState.Start(services.Resolve<IWorldGenerator>().Generate(42)),
                            NewPlayer(100),
                            null));
                    var medkit = new ItemInstance("med", "itm_medkit_normal", 1, charges: 1);

                    False(sessions.CanUseMedicine(session, medkit), "доступность");
                    False(sessions.TryUseMedicine(session, medkit, out _), "применение");
                });

                Check("сессия: медицина требует сессию и предмет", () =>
                {
                    Throws<ArgumentNullException>(
                        () => sessions.CanUseMedicine(null!, Instance("itm_medkit_normal")),
                        "доступность без сессии");
                    Throws<ArgumentNullException>(
                        () => sessions.TryUseMedicine(null!, Instance("itm_medkit_normal"), out _),
                        "применение без сессии");
                });
            });
        }

        private void EffectPersistenceTests(ServiceContainer services)
        {
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

                Check("сессия: эффект медицины не попадает в травмы игрока", () =>
                {
                    var player = NewPlayer(100);
                    var session = new GameSession(
                        new GameState(
                            WorldState.Start(services.Resolve<IWorldGenerator>().Generate(42)),
                            player,
                            null));
                    True(
                        sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")),
                        "бой начат");
                    session.Combat!.Player.ApplyStatus(new CombatStatus(
                        "itm_stim_guard",
                        new CombatModifiers(0.05, 0)));
                    session.Combat.Player.ApplyStatus(new CombatStatus(
                        services.Resolve<IInjuryRepository>().Get("injury_broken_leg"),
                        2));

                    sessions.EndCombat(session);

                    Equal(1, player.Injuries.Count, "только травма");
                    Equal("injury_broken_leg", player.Injuries[0].Id, "id травмы");
                });
            });
        }

    }
}
