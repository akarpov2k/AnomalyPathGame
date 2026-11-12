#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Переходы между хабами по карте мира: цена, шаги, встреча и прибытие.</summary>
    public sealed class HubTravelTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов переходов между хабами.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public HubTravelTests(string dataRoot)
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
                QuoteTests(services, topology, root);
                TravelTests(services, root);
                GeneratorTests(services);
            });
        }

        private void QuoteTests(ServiceContainer services, ITopologyRepository topology, string root)
        {
            Check("хабы: цена пешего перехода — шаг за шагом", () =>
            {
                var session = NewSession(services);
                AddHub(session, "hub_far", "loc_hub_settlement", 4);
                var sessions = NewService(services, topology, root);

                var next = sessions.GetHubTravelQuote(session, "hub_angar", HubTravelMode.OnFoot);
                Equal(1, next.Steps, "шаги до соседа");
                Equal(4.0, next.Cost.Hours, "часы до соседа");
                Equal(0, next.Cost.Stamina, "выносливость отключена");
                True(Math.Abs(next.EncounterChance - 0.2) < 1e-9, "шанс встречи на шаге");

                var far = sessions.GetHubTravelQuote(session, "hub_far", HubTravelMode.OnFoot);
                Equal(3, far.Steps, "шаги до дальнего");
                Equal(12.0, far.Cost.Hours, "часы до дальнего");
                Equal(0, far.Cost.Stamina, "выносливость отключена и для дальнего");
                True(Math.Abs(far.EncounterChance - 0.6) < 1e-9, "шанс встречи растёт с шагами");
            });

            Check("хабы: проводник стоит деньги и не грозит встречей", () =>
            {
                var session = NewSession(services);
                var sessions = NewService(services, topology, root);

                var guide = sessions.GetHubTravelQuote(session, "hub_angar", HubTravelMode.Guide);
                Equal(1, guide.Steps, "шаги");
                Equal(4.0, guide.Cost.Hours, "часы");
                Equal(200, guide.Cost.Money, "деньги");
                Equal(0, guide.Cost.Stamina, "выносливость не тратится");
                Equal(0.0, guide.EncounterChance, "встречи нет");
            });

            Check("хабы: из поля переход недоступен", () =>
            {
                var session = NewSession(services);
                var path = new LocationInstance("path_1", "loc_road_01", 1, "bg", LocationSubtype.Path, 1, 0.1);
                session.State.World.Map.AddLocation(path);
                session.State.World.MoveTo("path_1");
                var sessions = NewService(services, topology, root);

                var quote = sessions.GetHubTravelQuote(session, "hub_angar", HubTravelMode.OnFoot);
                Equal(HubTravelStatus.NotFromHub, quote.Status, "статус");
            });

            Check("хабы: в тот же хаб не перейти", () =>
            {
                var session = NewSession(services);
                var sessions = NewService(services, topology, root);

                var quote = sessions.GetHubTravelQuote(session, session.State.World.CurrentLocationId, HubTravelMode.OnFoot);
                Equal(HubTravelStatus.SameHub, quote.Status, "статус");
            });

            Check("хабы: без денег проводник недоступен", () =>
            {
                var session = NewSession(services, money: 10);
                var sessions = NewService(services, topology, root);

                var quote = sessions.GetHubTravelQuote(session, "hub_angar", HubTravelMode.Guide);
                Equal(HubTravelStatus.NotEnoughMoney, quote.Status, "статус");
            });

            Check("хабы: после сдачи главы хаб-заглушка не открывается", () =>
            {
                var session = NewSession(services);
                AddHub(session, "hub_development", "loc_hub_depot", 2);
                var sessions = NewService(services, topology, root);

                var before = sessions.GetHubTravelQuote(session, "hub_development", HubTravelMode.OnFoot);
                Equal(HubTravelStatus.Success, before.Status, "пока глава не сдана, переход открыт");

                session.State.Player!.CompleteQuest("qst_story_05_angar");
                var start = session.State.World.CurrentLocationId;
                var money = session.State.Player.Money;
                var hours = session.State.World.Clock.Hours;
                var stamina = session.State.World.Stamina;

                var onFoot = sessions.GetHubTravelQuote(session, "hub_development", HubTravelMode.OnFoot);
                Equal(HubTravelStatus.InDevelopment, onFoot.Status, "статус пешего предложения");
                var guide = sessions.GetHubTravelQuote(session, "hub_development", HubTravelMode.Guide);
                Equal(HubTravelStatus.InDevelopment, guide.Status, "статус предложения проводника");

                var result = sessions.TravelToHub(session, "hub_development", HubTravelMode.Guide);
                Equal(HubTravelStatus.InDevelopment, result.Status, "статус перехода");
                Equal(start, session.State.World.CurrentLocationId, "игрок остался на месте");
                Equal(money, session.State.Player.Money, "деньги не списаны");
                Equal(hours, session.State.World.Clock.Hours, "часы не шли");
                Equal(stamina, session.State.World.Stamina, "выносливость не списана");
            });

            Check("хабы: у депо задан квест-заглушка, у деревни — нет", () =>
            {
                var locations = services.Resolve<ILocationRepository>();
                Equal("qst_story_05_angar", locations.Get("loc_hub_depot").StubAfterQuest, "квест депо");
                True(locations.Get("loc_hub_settlement").StubAfterQuest == null, "у деревни квеста-заглушки нет");
            });
        }

        private void TravelTests(ServiceContainer services, string root)
        {
            Check("хабы: проводник доводит до места и списывает деньги и часы", () =>
            {
                var session = NewSession(services);
                var sessions = NewService(services, services.Resolve<ITopologyRepository>(), root);
                var hours = session.State.World.Clock.Hours;
                var money = session.State.Player!.Money;

                var result = sessions.TravelToHub(session, "hub_angar", HubTravelMode.Guide);

                Equal(HubTravelStatus.Success, result.Status, "статус");
                False(session.InCombat, "боя нет");
                Equal("hub_angar", session.State.World.CurrentLocationId, "прибыл");
                Equal("hub_angar", session.State.World.LastHubId, "последний хаб");
                Equal(money - 200, session.State.Player.Money, "деньги");
                True(session.State.World.Clock.Hours > hours, "часы идут");
            });

            Check("хабы: пешком без встречи — идут часы, выносливость не тратится", () =>
            {
                var session = NewSession(services);
                var sessions = NewService(services, WithChance(services, 0), root);
                var stamina = session.State.World.Stamina;

                var result = sessions.TravelToHub(session, "hub_angar", HubTravelMode.OnFoot);

                Equal(HubTravelStatus.Success, result.Status, "статус");
                Equal("hub_angar", session.State.World.CurrentLocationId, "прибыл");
                Equal(stamina, session.State.World.Stamina, "выносливость не тратится");
            });

            Check("хабы: пешком со встречей начинается бой на дороге", () =>
            {
                var session = NewSession(services);
                var sessions = NewService(services, WithChance(services, 1), root);

                var result = sessions.TravelToHub(session, "hub_angar", HubTravelMode.OnFoot);

                Equal(HubTravelStatus.Encounter, result.Status, "статус");
                True(session.InCombat, "бой идёт");
                True(session.Combat!.Context?.OnRoad == true, "бой на дороге");
                Equal("hub_angar", session.PendingHubTravel?.TargetHubId, "цель перехода");
                True(result.EncounterActorId != null, "противник найден");
            });

            Check("хабы: без выносливости пеший переход всё равно возможен", () =>
            {
                var session = NewSession(services);
                var start = session.State.World.CurrentLocationId;
                session.State.World.TrySpendStamina(9);
                var sessions = NewService(services, WithChance(services, 0), root);

                var result = sessions.TravelToHub(session, "hub_angar", HubTravelMode.OnFoot);

                Equal(HubTravelStatus.Success, result.Status, "выносливость не блокирует");
                Equal("hub_angar", session.State.World.CurrentLocationId, "прибыл");
                True(start != session.State.World.CurrentLocationId, "локация сменилась");
            });
        }

        private void GeneratorTests(ServiceContainer services)
        {
            Check("хабы: маршрут строится от текущего хаба", () =>
            {
                var session = NewSession(services);
                var world = session.State.World;
                world.MoveTo("hub_angar");
                var generator = services.Resolve<IWorldGenerator>();

                generator.BeginRoute(world);

                True(world.Map.EdgesFrom("hub_angar").Count > 0, "маршрут начинается в Ангаре");
            });
        }

        private static GameSession NewSession(ServiceContainer services, int money = 1000)
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
                money: money);
            var session = new GameSession(new GameState(WorldState.Start(map), player));
            AddHub(session, "hub_angar", "loc_hub_settlement", 2);
            return session;
        }

        private static void AddHub(GameSession session, string instanceId, string templateId, int tier)
        {
            session.State.World.Map.AddLocation(new LocationInstance(
                instanceId,
                templateId,
                777,
                "bg_hub_angar",
                LocationSubtype.Hub,
                tier,
                0));
        }

        private static ITopologyRepository WithChance(ServiceContainer services, double chance)
        {
            var real = services.Resolve<ITopologyRepository>().Topology;
            return new TopologyRepository(new TopologyTemplate(
                real.Id,
                real.Route,
                real.Costs,
                real.InitialStamina,
                real.DayStartHour,
                real.NightStartHour,
                new HubTravelSettings(
                    new OnFootTravelSettings(4, 2, chance),
                    new GuideTravelSettings(4, 200)),
                real.Rest));
        }

        private static GameSessionService NewService(ServiceContainer services, ITopologyRepository topology, string root)
        {
            var saves = new SaveService(new FileSaveStorage(root), services.Resolve<IItemRepository>());
            return Bootstrap.CreateSession(
                services.Resolve<ITravelService>(),
                services.Resolve<ICombatService>(),
                saves,
                services.Resolve<INewGameService>(),
                services.Resolve<IDeathService>(),
                services.Resolve<ICombatOutcomeService>(),
                progression: services.Resolve<IProgressionService>(),
                stashes: services.Resolve<IStashService>(),
                locationObjects: services.Resolve<ILocationObjectService>(),
                worldGenerator: services.Resolve<IWorldGenerator>(),
                topology: topology,
                ai: services.Resolve<ICombatAi>(),
                spawns: services.Resolve<ISpawnRepository>(),
                actors: services.Resolve<IActorRepository>(),
                rules: services.Resolve<IGameRulesRepository>(),
                locations: services.Resolve<ILocationRepository>());
        }
    }
}
