#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Потребности: расход еды и воды, штрафы голода и урон жажды.</summary>
    public sealed class NeedsTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов потребностей.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public NeedsTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var needs = services.Resolve<INeedsService>();
            var topology = services.Resolve<ITopologyRepository>();

            WithFixture(new Dictionary<string, string>(), root =>
            {
                ConsumptionTests(services, needs);
                MaxTests();
                PenaltyTests(services, needs);
                TravelTests(services, topology, root);
                CombatTests(services, root);
                SaveTests();
            });
        }

        private void MaxTests()
        {
            Check("потребности: максимумы еды и воды независимы", () =>
            {
                var state = new NeedsState(50, 50, 100, 120, 0, 0);
                Equal(100, state.FoodMax, "максимум еды");
                Equal(120, state.WaterMax, "максимум воды");

                state.SetHunger(130);
                Equal(100, state.Hunger, "сытость по пределу еды");
                state.SetThirst(130);
                Equal(120, state.Thirst, "питьё по пределу воды");

                state.SetMax(150, 140);
                Equal(150, state.FoodMax, "еда выросла");
                Equal(140, state.WaterMax, "вода выросла");
                state.SetHunger(160);
                Equal(150, state.Hunger, "новый предел еды");

                state.SetMax(100, 100);
                Equal(100, state.Hunger, "сытость подрезана");
                Equal(100, state.Thirst, "питьё подрезано");
            });
        }

        private void ConsumptionTests(ServiceContainer services, INeedsService needs)
        {
            Check("потребности: за час уходит 2 еды и 3 воды", () =>
            {
                var session = NewSession(services);

                var result = needs.Consume(session.State, 1);

                Equal(2, result.FoodSpent, "еда за час");
                Equal(3, result.WaterSpent, "вода за час");
                Equal(98, result.Hunger, "сытость");
                Equal(97, result.Thirst, "питьё");
            });

            Check("потребности: неполный час копится в остаток", () =>
            {
                var session = NewSession(services);

                var first = needs.Consume(session.State, 0.5);
                Equal(1, first.FoodSpent, "еда за полчаса");
                Equal(1, first.WaterSpent, "вода за полчаса");
                True(Math.Abs(first.WaterCarry - 0.5) < 1e-9, "остаток воды");
                True(Math.Abs(first.FoodCarry) < 1e-9, "остаток еды");

                var second = needs.Consume(session.State, 0.5);
                Equal(1, second.FoodSpent, "еда за вторые полчаса");
                Equal(2, second.WaterSpent, "вода за вторые полчаса с остатком");
                True(Math.Abs(second.WaterCarry) < 1e-9, "остаток воды закрыт");
                Equal(98, second.Hunger, "еда за час суммарно");
                Equal(97, second.Thirst, "вода за час суммарно");
            });

            Check("потребности: нулевые часы ничего не тратят", () =>
            {
                var session = NewSession(services);

                var result = needs.Consume(session.State, 0);

                Equal(0, result.FoodSpent, "еда");
                Equal(0, result.WaterSpent, "вода");
                Equal(100, result.Hunger, "сытость");
            });
        }

        private void PenaltyTests(ServiceContainer services, INeedsService needs)
        {
            Check("голод: цена перехода растёт на 20% с округлением вверх", () =>
            {
                var session = NewSession(services);
                session.State.Player!.Needs.SetHunger(0);

                Equal(3, needs.TravelStaminaCost(session.State, 2), "цена 2");
                Equal(4, needs.TravelStaminaCost(session.State, 3), "цена 3");
                Equal(0, needs.TravelStaminaCost(session.State, 0), "нулевая цена");
            });

            Check("голод: каждый ход в бою дороже на 1 ОД", () =>
            {
                var session = NewSession(services);
                session.State.Player!.Needs.SetHunger(0);

                Equal(1, needs.ApCostBonus(session.State), "прибавка к ОД");
            });

            Check("жажда: урон за переход — 3 HP без защиты", () =>
            {
                var session = NewSession(services);
                session.State.Player!.SetHealth(20);
                session.State.Player.Needs.SetThirst(0);

                var damage = needs.ApplyThirstDamage(session.State);

                Equal(3, damage.Damage, "прошло урона");
                Equal(17, damage.HealthAfter, "здоровье");
            });

            Check("жажда: смерть наступает по общим правилам", () =>
            {
                var session = NewSession(services);
                session.State.Player!.SetHealth(2);
                session.State.Player.Needs.SetThirst(0);

                var damage = needs.ApplyThirstDamage(session.State);

                True(damage.Died, "умер");
                True(damage.RespawnHubId != null, "вернулся в хаб");
            });

            Check("сытый и напившийся: штрафов нет", () =>
            {
                var session = NewSession(services);

                Equal(2, needs.TravelStaminaCost(session.State, 2), "цена без надбавки");
                Equal(0, needs.ApCostBonus(session.State), "без прибавки ОД");
                Equal(0, needs.TurnDamage(session.State), "без урона жажды");
                False(needs.ApplyThirstDamage(session.State).Died, "смерти нет");
            });
        }

        private void TravelTests(
            ServiceContainer services,
            ITopologyRepository topology,
            string root)
        {
            Check("потребности: переход тратит еду и воду и бьёт жаждой", () =>
            {
                var sessions = NewService(services, root, services.Resolve<INeedsService>());
                var session = NewSession(services);
                AddPath(session);
                session.State.Player!.SetHealth(20);
                session.State.Player.Needs.SetThirst(0);

                var result = sessions.Travel(session, "edge_needs");

                Equal(TravelStatus.Success, result.Status, "статус");
                Equal(98, session.State.Player.Needs.Hunger, "еда за час");
                Equal(0, session.State.Player.Needs.Thirst, "вода осталась на нуле");
                True(result.Damage != null, "урон жажды в результате");
                Equal(17, session.State.Player.Health, "здоровье после жажды");
            });

            Check("потребности: отдых тратит еду и воду", () =>
            {
                var sessions = NewService(services, root, services.Resolve<INeedsService>());
                var session = NewSession(services);
                session.State.World.TrySpendStamina(4);

                sessions.Rest(session, 2);

                Equal(96, session.State.Player!.Needs.Hunger, "еда за два часа");
                Equal(94, session.State.Player.Needs.Thirst, "вода за два часа");
            });

            Check("голод: возврат в хаб не тратит выносливость", () =>
            {
                var sessions = NewService(services, root, services.Resolve<INeedsService>());
                var session = NewSession(services);
                AddPath(session);
                session.State.World.MoveTo("path_needs");
                session.State.Player!.Needs.SetHunger(0);
                var stamina = session.State.World.Stamina;

                sessions.ReturnToHub(session);

                Equal(stamina, session.State.World.Stamina, "выносливость не тратится");
            });

            Check("голод: пеший переход между хабами не тратит выносливость", () =>
            {
                var sessions = NewService(services, root, services.Resolve<INeedsService>());
                var session = NewSession(services);
                AddFarHub(session);
                session.State.Player!.Needs.SetHunger(0);

                var quote = sessions.GetHubTravelQuote(session, "hub_far", HubTravelMode.OnFoot);

                Equal(0, quote.Cost.Stamina, "выносливость отключена");
            });
        }

        private void CombatTests(ServiceContainer services, string root)
        {
            Check("голод и жажда: штрафы переходят в бой", () =>
            {
                var sessions = NewService(services, root, services.Resolve<INeedsService>());
                var session = NewSession(services);
                session.State.Player!.Needs.SetHunger(0);
                session.State.Player.Needs.SetThirst(0);
                session.State.Player.SetHealth(50);
                StartCombat(services, sessions, session);

                Equal(1, session.Combat!.PlayerApCostBonus, "прибавка к ОД");
                Equal(3, session.Combat.PlayerTurnDamage, "урон жажды за ход");
                Equal(97, session.Combat.Player.Health, "первый ход уже с уроном");
            });

            Check("жажда: каждый новый ход игрока снимает здоровье", () =>
            {
                var sessions = NewService(services, root, services.Resolve<INeedsService>());
                var combatService = services.Resolve<ICombatService>();
                var session = NewSession(services);
                session.State.Player!.Needs.SetHunger(0);
                session.State.Player.Needs.SetThirst(0);
                session.State.Player.SetHealth(50);
                StartCombat(services, sessions, session);
                var combat = session.Combat!;

                combatService.EndTurn(combat);
                Equal(97, combat.Player.Health, "ход противника без урона");
                combatService.EndTurn(combat);
                Equal(94, combat.Player.Health, "новый ход игрока с уроном");
            });
        }

        private void SaveTests()
        {
            Check("потребности: дробный остаток переживает сохранение", () =>
            {
                var data = new NeedsSaveData(50, 40, 100, 100, 0.25, 0.75);
                var json = JsonParser.Parse(JsonWriter.Write(data.ToJson()), "needs");

                var parsed = NeedsSaveData.FromJson(json, "needs");

                Equal(50, parsed.Hunger, "сытость");
                True(Math.Abs(parsed.FoodCarry - 0.25) < 1e-9, "остаток еды");
                True(Math.Abs(parsed.WaterCarry - 0.75) < 1e-9, "остаток воды");
            });

            Check("потребности: остаток доходит до сейва игрока", () =>
            {
                var services = BuildServices();
                var session = NewSession(services);
                services.Resolve<INeedsService>().Consume(session.State, 0.5);
                var player = session.State.Player!;

                var data = PlayerSaveMapper.ToSaveData(player);
                var restored = PlayerSaveMapper.FromSaveData(
                    data,
                    services.Resolve<IItemRepository>(),
                    "player");

                True(Math.Abs(restored.Needs.FoodCarry - player.Needs.FoodCarry) < 1e-9, "остаток еды");
                True(Math.Abs(restored.Needs.WaterCarry - player.Needs.WaterCarry) < 1e-9, "остаток воды");
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

        private static void AddFarHub(GameSession session)
        {
            session.State.World.Map.AddLocation(new LocationInstance(
                "hub_far",
                "loc_hub_depot",
                7,
                "bg_hub_angar",
                LocationSubtype.Hub,
                2,
                0));
        }

        private static void AddPath(GameSession session)
        {
            var world = session.State.World;
            var map = world.Map;
            map.AddLocation(new LocationInstance(
                "path_needs",
                "loc_route_highway",
                5,
                "bg",
                LocationSubtype.Path,
                0,
                0.1,
                routeStep: 1));
            map.AddEdge(new WorldEdge(
                "edge_needs",
                world.CurrentLocationId,
                "path_needs",
                "road",
                1,
                new TravelCost(0, 1),
                oneWay: false,
                hidden: false));
        }

        private static GameSessionService NewService(
            ServiceContainer services,
            string root,
            INeedsService needs)
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
                topology: services.Resolve<ITopologyRepository>(),
                ai: services.Resolve<ICombatAi>(),
                spawns: services.Resolve<ISpawnRepository>(),
                actors: services.Resolve<IActorRepository>(),
                rules: services.Resolve<IGameRulesRepository>(),
                needs: needs);
        }
    }
}
