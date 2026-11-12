#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Игровая сессия: переходы, бой, атака, ходы и ограничения.</summary>
    public sealed partial class SessionTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов сессии.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public SessionTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var travel = services.Resolve<ITravelService>();
            var combat = services.Resolve<ICombatService>();
            var newGame = services.Resolve<INewGameService>();
            var actors = services.Resolve<IActorRepository>();
            var items = services.Resolve<IItemRepository>();

            CompositionTests();

            WithFixture(new Dictionary<string, string>(), root =>
            {
                var saves = new SaveService(new FileSaveStorage(root), items);
                var sessions = Bootstrap.CreateSession(
                    travel,
                    combat,
                    saves,
                    newGame,
                    services.Resolve<IDeathService>(),
                    services.Resolve<ICombatOutcomeService>(),
                    progression: services.Resolve<IProgressionService>(),
                    stashes: services.Resolve<IStashService>(),
                    locationObjects: services.Resolve<ILocationObjectService>(),
                    worldGenerator: services.Resolve<IWorldGenerator>(),
                    topology: services.Resolve<ITopologyRepository>(),
                    ai: services.Resolve<ICombatAi>(),
                    rules: services.Resolve<IGameRulesRepository>(),
                    needs: services.Resolve<INeedsService>());
                TravelTests(services, sessions);
                HubReturnTests(services, sessions);
                WalkSessionTests(services, sessions);
                LeaveHubTests(services, sessions);
                StashSessionTests(services, sessions);
                CombatTests(services, sessions, actors, items);
                DeathSessionTests(services, sessions, items);
                SaveLoadTests(services, sessions);
            });
        }

        private void CompositionTests()
        {
            Check("сессия: сборка фасада требует обязательные зависимости", () =>
            {
                Throws<ArgumentNullException>(
                    () => Bootstrap.CreateSession(null!, null!, null!, null!, null!, null!),
                    "переход");
            });
        }

        private void StashSessionTests(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сессия: тайник в хабе не проверяется", () =>
            {
                var session = NewSession(services, withPlayer: true);
                False(sessions.RollLocationStash(session), "хаб");
                False(session.State.World.CurrentLocation.StashPresent, "тайника нет");
            });

            Check("сессия: вскрытие тайника делегируется сервису", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var location = session.State.World.CurrentLocation;
                location.MarkStashRolled();
                location.MarkStashPresent();
                location.MarkStashLockSolved();

                var result = sessions.OpenStash(session);

                Equal(StashOpenStatus.Opened, result.Status, "статус");
                True(location.StashOpened, "тайник вскрыт");
            });
        }

        private void TravelTests(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сессия: видимые рёбра и переход", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var edges = sessions.AvailableEdges(session);
                True(edges.Count > 0, "рёбра есть");

                var edge = edges[0];
                var result = sessions.Travel(session, edge.EdgeId);

                True(result.Succeeded, "переход выполнен");
                Equal(edge.ToId, session.State.World.CurrentLocationId, "текущая локация");
            });

            Check("сессия: при переходе выбирается объект локации", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var edge = sessions.AvailableEdges(session)[0];

                True(sessions.Travel(session, edge.EdgeId).Succeeded, "переход");

                var location = session.State.World.CurrentLocation;
                True(location.Spawned, "спавн объектов выполнен при переходе");
                True(location.Objects.Count >= 0, "список объектов доступен");
            });

            Check("сессия: в бою рёбер нет", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var edge = sessions.AvailableEdges(session)[0];
                var enemy = services.Resolve<IActorRepository>().Get("enemy_bandit");
                var player = services.Resolve<IActorRepository>().Get("actor_player");

                True(sessions.StartCombat(session, player, enemy), "бой начат");
                Equal(0, sessions.AvailableEdges(session).Count, "рёбра скрыты");
                Equal(TravelStatus.InCombat, sessions.Travel(session, edge.EdgeId).Status, "переход запрещён");
            });
        }


        private void DeathSessionTests(
            ServiceContainer services,
            IGameSessionService sessions,
            IItemRepository items)
        {
            Check("сессия: смерть забирает инвентарь и незастрахованные вещи", () =>
            {
                var session = NewSession(services, withPlayer: true, new NewGameSettings(0, 1));
                var player = session.State.Player!;
                var template = items.Get("wpn_pm");
                player.Inventory.Place(
                    new ItemInstance("inst_inventory", "wpn_pm", 1),
                    template,
                    0,
                    0,
                    false);
                player.Equipment.Set(EquipmentSlot.Main, new ItemInstance("inst_worn", "wpn_pm", 1));

                var result = sessions.ApplyDeath(session);

                Equal(0, player.Inventory.Items.Count, "инвентарь пуст");
                True(player.Equipment.Get(EquipmentSlot.Main) == null, "оружие потеряно");
                Equal(2, result.Lost.Count, "потерь");
            });
        }

        private void SaveLoadTests(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сессия: новая игра по набору", () =>
            {
                var settings = new NewGameSettings(0.25, 1.75);
                var session = sessions.StartNew(7, "loadout_scout", settings);

                Equal(7, session.State.World.Map.Seed, "seed");
                True(session.State.Player != null, "игрок");
                True(session.State.Settings != null, "настройки");
                True(sessions.AvailableEdges(session).Count > 0, "рёбра");
            });

            Check("сессия: настройки сложности сохраняются", () =>
            {
                var session = NewSession(
                    services,
                    withPlayer: true,
                    new NewGameSettings(
                        0.25,
                        1.75,
                        permadeath: true,
                        xpLossChance: 0.7,
                        nightAccuracyPenalty: 0.35));
                sessions.Save(session, "slot_settings");

                var loaded = sessions.Load("slot_settings");
                True(loaded.State.Settings != null, "настройки загружены");
                Equal(0.25, loaded.State.Settings!.InsuranceChance, "insuranceChance");
                Equal(1.75, loaded.State.Settings.TradeDifficulty, "tradeDifficulty");
                True(loaded.State.Settings.Permadeath, "permadeath");
                Equal(0.7, loaded.State.Settings.XpLossChance, "xpLossChance");
                Equal(0.35, loaded.State.Settings.NightAccuracyPenalty, "nightAccuracyPenalty");
            });

            Check("сессия: загрузка подтягивает уровень по накопленному опыту", () =>
            {
                var session = NewSession(services, withPlayer: true);
                session.State.Player!.AddXp(744);
                sessions.Save(session, "slot_level");

                var loaded = sessions.Load("slot_level");

                Equal(6, loaded.State.Player!.Level, "уровень");
                Equal(2, loaded.State.Player.TalentPoints, "очки талантов за уровни 5 и 6");
                Equal(4, loaded.State.Player.LevelHealthBonus, "прибавка здоровья");
            });

            Check("сессия: бонус здоровья за уровни учитывается в бою", () =>
            {
                var session = NewSession(services, withPlayer: true);
                services.Resolve<IProgressionService>().AddXp(session.State.Player!, 537);
                var actors = services.Resolve<IActorRepository>();

                True(
                    sessions.StartCombat(
                        session,
                        actors.Get("actor_player"),
                        actors.Get("enemy_bandit")),
                    "бой начат");

                Equal(102, session.Combat!.Player.MaxHealth, "100 базовых + 2 за пятый уровень");
            });

            Check("сессия: сохранение и загрузка состояния", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var edge = sessions.AvailableEdges(session)[0];
                True(sessions.Travel(session, edge.EdgeId).Succeeded, "переход");

                sessions.Save(
                    session,
                    "slot_1",
                    42,
                    new DateTimeOffset(2026, 10, 22, 12, 0, 0, TimeSpan.Zero));

                var loaded = sessions.Load("slot_1");
                Equal(session.State.World.CurrentLocationId, loaded.State.World.CurrentLocationId, "локация");
                Equal(session.State.World.Clock.Day, loaded.State.World.Clock.Day, "день");
                Equal(session.State.World.Clock.Hours, loaded.State.World.Clock.Hours, "часы");
                True(loaded.State.Player != null, "игрок");
            });

            Check("сессия: в бою сохранение запрещено, бой в сейв не попадает", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var actors = services.Resolve<IActorRepository>();
                True(
                    sessions.StartCombat(
                        session,
                        actors.Get("actor_player"),
                        actors.Get("enemy_bandit")),
                    "бой начат");

                var error = Throws<InvalidOperationException>(
                    () => sessions.Save(session, "slot_2"),
                    "сохранение в бою");
                Contains(error.Message, CoreText.Get(CoreErrors.SaveInCombat), "текст ошибки");

                sessions.EndCombat(session);
                sessions.Save(session, "slot_2");

                var loaded = sessions.Load("slot_2");
                False(loaded.InCombat, "боя нет");
                True(loaded.State.Player != null, "игрок загружен");
            });

            Check("сессия: отсутствующий слот — ошибка", () =>
            {
                Throws<DataException>(() => sessions.Load("missing_slot"), "слот");
            });

            Check("сессия: сохранение без сессии — ошибка", () =>
            {
                Throws<ArgumentNullException>(() => sessions.Save(null!, "slot_1"), "сессия");
            });

            Check("сессия: сейв без игрока загружается", () =>
            {
                var session = NewSession(services, withPlayer: false);
                sessions.Save(session, "slot_3");

                var loaded = sessions.Load("slot_3");
                True(loaded.State.Player == null, "игрока нет");
            });
        }

        private static GameSession NewSession(
            ServiceContainer services,
            bool withPlayer,
            NewGameSettings? settings = null)
        {
            var generator = services.Resolve<IWorldGenerator>();
            var world = WorldState.Start(generator.Generate(42));
            PlayerState? player = null;
            if (withPlayer)
            {
                var grid = new GridContainer("player", 8, 4);
                var equipment = new Equipment("player", 4);
                var needs = new NeedsState(100, 100, 100);
                player = new PlayerState(
                    "player",
                    1,
                    0,
                    100,
                    8,
                    grid,
                    equipment,
                    new string[0],
                    needs);
            }

            return new GameSession(new GameState(world, player, settings));
        }
    }
}
