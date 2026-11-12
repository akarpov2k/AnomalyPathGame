#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Отдых: скорость восстановления, предел часов, движение времени и нападение вне хаба.</summary>
    public sealed class RestTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов отдыха.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public RestTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var topology = services.Resolve<ITopologyRepository>();

            WithFixture(new Dictionary<string, string>(), root =>
            {
                SettingsTests(topology);
                QuoteTests(services, topology, root);
                HubRestTests(services, topology, root);
                RaidRestTests(services, topology, root);
                EncounterTests(services, root);
            });
        }

        private void SettingsTests(ITopologyRepository topology)
        {
            var rest = topology.Topology.Rest;
            Check("отдых: ставка восстановления и риск приходят из данных", () =>
            {
                Equal(2, rest.PerHour, "выносливость за час");
                True(Math.Abs(rest.RaidAttackChancePerHour - 0.1) < 1e-9, "риск за час");
            });

            Check("отдых: за час восстанавливается фиксированная ставка", () =>
            {
                Equal(2, rest.StaminaPerHour(10, inHub: true), "хаб при максимуме 10");
                Equal(2, rest.StaminaPerHour(10, inHub: false), "вне хаба при максимуме 10");
                Equal(2, rest.StaminaPerHour(100, inHub: true), "хаб при максимуме 100");
                Equal(2, rest.StaminaPerHour(100, inHub: false), "вне хаба при максимуме 100");
            });

            Check("отдых: предел часов доходит до полного восстановления", () =>
            {
                Equal(2, rest.MaxHours(4, 10, inHub: true), "четыре нехватки в хабе");
                Equal(2, rest.MaxHours(4, 10, inHub: false), "четыре нехватки вне хаба");
                Equal(1, rest.MaxHours(1, 10, inHub: true), "одна нехватка");
                Equal(0, rest.MaxHours(0, 10, inHub: true), "восстанавливать нечего");
            });

            Check("отдых: талант ускоряет восстановление выносливости", () =>
            {
                Equal(5, rest.StaminaPerHour(10, inHub: true, recoveryBonus: 3), "ставка с бонусом");
                Equal(1, rest.MaxHours(4, 10, inHub: true, recoveryBonus: 3), "часов с бонусом");
            });

            Check("отдых: шанс нападения растёт на 10% за час и упирается в 100%", () =>
            {
                True(Math.Abs(rest.AttackChance(4) - 0.4) < 1e-9, "четыре часа");
                True(Math.Abs(rest.AttackChance(10) - 1.0) < 1e-9, "десять часов");
                True(Math.Abs(rest.AttackChance(20) - 1.0) < 1e-9, "больше десяти часов");
            });
        }

        private void QuoteTests(ServiceContainer services, ITopologyRepository topology, string root)
        {
            Check("отдых: в хабе полная выносливость — отдых не нужен", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);

                var quote = sessions.GetRestQuote(session);

                Equal(RestStatus.FullStamina, quote.Status, "статус");
                True(quote.LocationIsHub, "отдых в хабе");
                Equal(0, quote.MaxHours, "часов отдыха");
                Equal(0, quote.Missing, "нехватка");
                True(Math.Abs(quote.AttackChancePerHour) < 1e-9, "в хабе риска нет");
            });

            Check("отдых: в хабе час даёт 2 выносливости при максимуме 10", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);

                var quote = sessions.GetRestQuote(session);

                Equal(RestStatus.Success, quote.Status, "статус");
                Equal(6, quote.Stamina, "текущая выносливость");
                Equal(10, quote.StaminaMax, "максимум");
                Equal(4, quote.Missing, "нехватка");
                Equal(2, quote.StaminaPerHour, "за час в хабе");
                Equal(2, quote.MaxHours, "полное восстановление за 2 часа");
                True(Math.Abs(quote.AttackChance(2)) < 1e-9, "в хабе не нападают");
            });

            Check("отдых: таланты повышают максимум и часовую ставку", () =>
            {
                var sessions = NewService(
                    services,
                    root,
                    topology,
                    talents: services.Resolve<ITalentService>());
                var session = NewTalentSession(services);
                session.State.World.TrySpendStamina(3);
                var baseMax = session.State.World.StaminaMax;

                var quote = sessions.GetRestQuote(session);

                Equal(baseMax + 25, quote.StaminaMax, "максимум с талантом выносливости");
                Equal(5, quote.StaminaPerHour, "ставка с талантом восстановления");
                Equal(6, quote.MaxHours, "часов до полного восстановления");
            });

            Check("отдых: вне хаба час даёт 2 выносливости и 10% риска", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);
                MoveToPath(session);

                var quote = sessions.GetRestQuote(session);

                Equal(RestStatus.Success, quote.Status, "статус");
                False(quote.LocationIsHub, "отдых вне хаба");
                Equal(2, quote.StaminaPerHour, "за час вне хаба");
                Equal(2, quote.MaxHours, "полное восстановление за 2 часа");
                True(Math.Abs(quote.AttackChancePerHour - 0.1) < 1e-9, "риск за час");
                True(Math.Abs(quote.AttackChance(4) - 0.4) < 1e-9, "риск за выбранные часы");
            });

            Check("отдых: в бою отдыхать нельзя", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);
                StartCombat(services, sessions, session);

                var quote = sessions.GetRestQuote(session);

                Equal(RestStatus.InCombat, quote.Status, "статус");
                False(quote.Available, "отдых недоступен");
            });

            Check("отдых: без топологии отдых недоступен", () =>
            {
                var sessions = NewService(services, root, topology: null);
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);

                var quote = sessions.GetRestQuote(session);

                Equal(RestStatus.Unavailable, quote.Status, "статус");
                Equal(0, quote.MaxHours, "часов отдыха");
            });
        }

        private void HubRestTests(ServiceContainer services, ITopologyRepository topology, string root)
        {
            Check("отдых: в хабе два часа восстанавливают четыре выносливости", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                var world = session.State.World;
                world.TrySpendStamina(4);
                var hours = world.Clock.AbsoluteHours;

                var result = sessions.Rest(session, 2);

                Equal(RestStatus.Success, result.Status, "статус");
                Equal(2, result.Hours, "часов");
                Equal(4, result.StaminaRestored, "восстановлено");
                Equal(10, world.Stamina, "выносливость");
                True(Math.Abs(world.Clock.AbsoluteHours - (hours + 2)) < 1e-9, "часы мира идут");
                False(session.InCombat, "нападения в хабе нет");
            });

            Check("отдых: больше предела часов — отказ без движения времени", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                var world = session.State.World;
                world.TrySpendStamina(4);
                var hours = world.Clock.AbsoluteHours;

                var result = sessions.Rest(session, 3);

                Equal(RestStatus.InvalidHours, result.Status, "статус");
                Equal(6, world.Stamina, "выносливость не изменилась");
                True(Math.Abs(world.Clock.AbsoluteHours - hours) < 1e-9, "время не двигалось");
            });

            Check("отдых: ноль часов ничего не меняет", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                var world = session.State.World;
                world.TrySpendStamina(4);
                var hours = world.Clock.AbsoluteHours;

                var result = sessions.Rest(session, 0);

                Equal(RestStatus.Success, result.Status, "статус");
                Equal(0, result.StaminaRestored, "восстановлено");
                Equal(6, world.Stamina, "выносливость");
                True(Math.Abs(world.Clock.AbsoluteHours - hours) < 1e-9, "время не двигалось");
            });

            Check("отдых: часовые травмы тикают за каждый час", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                var world = session.State.World;
                world.TrySpendStamina(4);
                session.State.Player!.SetInjuries(new[]
                {
                    new InjuryState("inj_test", InjuryDurationKind.Hours, 3)
                });

                sessions.Rest(session, 2);

                Equal(1, session.State.Player.Injuries[0].Remaining, "остаток травмы");
            });
        }

        private void RaidRestTests(ServiceContainer services, ITopologyRepository topology, string root)
        {
            Check("отдых: вне хаба два часа восстанавливают четыре выносливости", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                var world = session.State.World;
                world.TrySpendStamina(4);
                MoveToPath(session);
                var hours = world.Clock.AbsoluteHours;

                var result = sessions.Rest(session, 2);

                Equal(RestStatus.Success, result.Status, "статус без нападения");
                Equal(4, result.StaminaRestored, "восстановлено");
                Equal(10, world.Stamina, "выносливость");
                True(Math.Abs(world.Clock.AbsoluteHours - (hours + 2)) < 1e-9, "часы мира идут");
            });

            Check("отдых: вне хаба больше предела часов — отказ", () =>
            {
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);
                MoveToPath(session);

                var result = sessions.Rest(session, 5);

                Equal(RestStatus.InvalidHours, result.Status, "статус");
            });
        }

        private void EncounterTests(ServiceContainer services, string root)
        {
            Check("отдых: нападение вне хаба начинается после полного отдыха", () =>
            {
                var topology = WithRestChance(services, 1);
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                var world = session.State.World;
                world.TrySpendStamina(4);
                MoveToPath(session);

                var result = sessions.Rest(session, 1);

                Equal(RestStatus.Encounter, result.Status, "статус");
                True(session.InCombat, "бой идёт");
                True(session.Combat!.Context?.OnRoad == true, "бой вне объектов локации");
                True(result.EncounterActorId != null, "противник найден");
                Equal(8, world.Stamina, "выносливость восстановлена до боя");
            });

            Check("отдых: в хабе нападения нет даже при полном риске", () =>
            {
                var topology = WithRestChance(services, 1);
                var sessions = NewService(services, root, topology);
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);

                var result = sessions.Rest(session, 2);

                Equal(RestStatus.Success, result.Status, "статус");
                False(session.InCombat, "боя нет");
            });

            Check("отдых: бросок нападения детерминирован", () =>
            {
                var topology = WithRestChance(services, 1);
                var sessions = NewService(services, root, topology);
                var first = NewSession(services);
                first.State.World.TrySpendStamina(4);
                MoveToPath(first);
                var second = NewSession(services);
                second.State.World.TrySpendStamina(4);
                MoveToPath(second);

                var firstResult = sessions.Rest(first, 1);
                var secondResult = sessions.Rest(second, 1);

                Equal(firstResult.EncounterActorId, secondResult.EncounterActorId, "один и тот же противник");
            });

            Check("отдых: без противников в пуле боя нет", () =>
            {
                var topology = WithRestChance(services, 1);
                var sessions = NewService(services, root, topology, spawns: EmptySpawns(services));
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);
                MoveToPath(session);

                var result = sessions.Rest(session, 1);

                Equal(RestStatus.Success, result.Status, "статус");
                False(session.InCombat, "боя нет");
            });
        }

        private static void StartCombat(ServiceContainer services, GameSessionService sessions, GameSession session)
        {
            var rules = services.Resolve<IGameRulesRepository>().Require();
            var actors = services.Resolve<IActorRepository>();
            var playerActor = actors.Get(rules.PlayerActorId);
            var enemy = FindEnemy(actors);
            sessions.StartCombat(session, playerActor, enemy);
        }

        private static ActorTemplate FindEnemy(IActorRepository actors)
        {
            foreach (var actor in actors.All)
            {
                if (actor.Combat != null)
                {
                    return actor;
                }
            }

            throw new InvalidOperationException("В данных нет противника для боя");
        }

        private static GameSession NewSession(ServiceContainer services)
        {
            var map = services.Resolve<IWorldGenerator>().Generate(42);
            var player = new PlayerState(
                "player",
                1,
                0,
                100,
                5,
                new GridContainer("inventory", 8, 4),
                new Equipment("equipment", 4),
                Array.Empty<string>(),
                new NeedsState(100, 100, 100),
                money: 1000);
            return new GameSession(new GameState(WorldState.Start(map), player));
        }

        private static GameSession NewTalentSession(ServiceContainer services)
        {
            var map = services.Resolve<IWorldGenerator>().Generate(42);
            var ranks = new Dictionary<string, int>
            {
                ["tal_endurance"] = 5,
                ["tal_second_wind"] = 3
            };
            var player = new PlayerState(
                "player",
                1,
                0,
                100,
                5,
                new GridContainer("inventory", 8, 4),
                new Equipment("equipment", 4),
                Array.Empty<string>(),
                new NeedsState(100, 100, 100),
                money: 1000,
                talentRanks: ranks);
            return new GameSession(new GameState(WorldState.Start(map), player));
        }

        private static void MoveToPath(GameSession session)
        {
            var map = session.State.World.Map;
            map.AddLocation(new LocationInstance(
                "path_rest",
                "loc_road_01",
                5,
                "bg",
                LocationSubtype.Path,
                0,
                0.1,
                routeStep: 1));
            session.State.World.MoveTo("path_rest");
        }

        private static ITopologyRepository WithRestChance(ServiceContainer services, double chance)
        {
            var real = services.Resolve<ITopologyRepository>().Topology;
            return new TopologyRepository(new TopologyTemplate(
                real.Id,
                real.Route,
                real.Costs,
                real.InitialStamina,
                real.DayStartHour,
                real.NightStartHour,
                real.Travel,
                new RestSettings(real.Rest.PerHour, chance)));
        }

        private static ISpawnRepository EmptySpawns(ServiceContainer services)
        {
            var real = services.Resolve<ISpawnRepository>();
            var entries = new List<SpawnEntry>();
            foreach (var entry in real.All)
            {
                if (entry.ObjectKind != LocationObjectKind.Enemy)
                {
                    entries.Add(entry);
                }
            }

            return new SpawnRepository(entries);
        }

        private static GameSessionService NewService(
            ServiceContainer services,
            string root,
            ITopologyRepository? topology = null,
            ISpawnRepository? spawns = null,
            ITalentService? talents = null)
        {
            var saves = new SaveService(new FileSaveStorage(root), services.Resolve<IItemRepository>());
            return Bootstrap.CreateSession(
                services.Resolve<ITravelService>(),
                services.Resolve<ICombatService>(),
                saves,
                services.Resolve<INewGameService>(),
                services.Resolve<IDeathService>(),
                services.Resolve<ICombatOutcomeService>(),
                talents: talents,
                progression: services.Resolve<IProgressionService>(),
                stashes: services.Resolve<IStashService>(),
                locationObjects: services.Resolve<ILocationObjectService>(),
                worldGenerator: services.Resolve<IWorldGenerator>(),
                topology: topology,
                ai: services.Resolve<ICombatAi>(),
                spawns: spawns ?? services.Resolve<ISpawnRepository>(),
                actors: services.Resolve<IActorRepository>(),
                rules: services.Resolve<IGameRulesRepository>());
        }
    }
}
