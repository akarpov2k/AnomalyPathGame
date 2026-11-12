#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="StoryQuestTests"/>: сервис, сейв и события сессии.</summary>
    public sealed partial class StoryQuestTests
    {
        private void ServiceTests(ServiceContainer services)
        {
            ChainTests(services);
            HangarTests(services);
            ChapterTurnInTests(services);
            var items = services.Resolve<IItemRepository>();
            var inventory = services.Resolve<IInventoryService>();
            var travel = services.Resolve<ITravelService>();
            var combat = services.Resolve<ICombatService>();
            var newGame = services.Resolve<INewGameService>();
            var death = services.Resolve<IDeathService>();
            var outcomes = services.Resolve<ICombatOutcomeService>();

            WithFixture(
                new Dictionary<string, string>
                {
                    ["catalog.json"] = Catalog(
                        "quests/qst_svc.json",
                        "quests/qst_svc2.json",
                        "quests/qst_medic.json",
                        "quests/qst_hard.json",
                        "quests/qst_reach.json",
                        "quests/qst_kill.json"),
                    ["quests/qst_svc.json"] = ServiceQuest(
                        "qst_svc",
                        "main",
                        "raid",
                        "[{\"type\":\"openStashes\",\"count\":2}]"),
                    ["quests/qst_svc2.json"] = ServiceQuest(
                        "qst_svc2",
                        "main",
                        "life",
                        "[{\"type\":\"openStashes\",\"count\":1}]"),
                    ["quests/qst_medic.json"] = ServiceQuest(
                        "qst_medic",
                        "medic",
                        "life",
                        "[{\"type\":\"openStashes\",\"count\":1}]"),
                    ["quests/qst_hard.json"] = ServiceQuest(
                        "qst_hard",
                        "hardline",
                        "cumulative",
                        "[{\"type\":\"openStashes\",\"count\":2}]",
                        "\"variants\":[{\"id\":\"hard\",\"objectives\":" +
                        "[{\"type\":\"openStashes\",\"count\":4}],\"rewards\":{\"money\":70}}]"),
                    ["quests/qst_reach.json"] = ServiceQuest(
                        "qst_reach",
                        "reachline",
                        "cumulative",
                        "[{\"type\":\"reachLocation\",\"locationId\":\"loc_route_forest\"}]"),
                    ["quests/qst_kill.json"] = ServiceQuest(
                        "qst_kill",
                        "killline",
                        "cumulative",
                        "[{\"type\":\"killActors\",\"count\":2," +
                        "\"actorPool\":[\"enemy_bandit\"]}]")
                },
                root =>
                {
                    var data = new DataLoader(new FileSystemDataSource(root)).Load();
                    var repository = StoryQuestRepository.FromLoadedData(data);
                    var story = new StoryQuestService(repository, items, inventory);
                    var saves = new SaveService(new FileSaveStorage(root), items);
                    var sessions = Bootstrap.CreateSession(
                        travel,
                        combat,
                        saves,
                        newGame,
                        death,
                        outcomes,
                        worldGenerator: services.Resolve<IWorldGenerator>(),
                        storyQuests: story);

                    AcceptAndTurnInTests(story, sessions);
                    LineAndVariantTests(story, sessions);
                    ProgressScopeTests(story, sessions);
                    ProgressNotificationTests(story, sessions);
                    SaveRoundTripTests(story, sessions, items);
                    SessionHookTests(story, sessions);
                });
        }

        private void ChainTests(ServiceContainer services)
        {
            Check("цепочка АТП: S4 создаёт пять локаций с противниками", () =>
            {
                var newGame = services.Resolve<INewGameService>();
                var story = services.Resolve<IStoryQuestService>();
                var world = services.Resolve<IStoryWorldService>();
                var chainRepository = services.Resolve<IQuestChainRepository>();
                var session = new GameSession(newGame.Create(
                    42,
                    "loadout_rifleman",
                    new NewGameSettings(0, 1)));
                var player = session.State.Player!;
                player.CompleteQuest("qst_story_03_dogs");

                True(story.Accept(session, "qst_story_04_atp").Succeeded, "S4 взят");
                True(world.HasChain(session, "chain_atp"), "цепочка на карте");

                var chain = chainRepository.Get("chain_atp");
                Equal(5, chain.Locations.Count, "шагов в цепочке");
                for (var index = 0; index < chain.Locations.Count; index++)
                {
                    var instanceId = StoryWorldService.InstanceId("chain_atp", index);
                    True(session.State.World.Map.TryGetLocation(instanceId, out var location), "локация есть");
                    True(location!.Objects.Count > 0, "объект на локации");
                }

                var dispatch = session.State.World.Map.GetLocation(
                    StoryWorldService.InstanceId("chain_atp", 4));
                True(
                    dispatch.Objects.Count == 2,
                    "в диспетчерской противник и NPC");

                True(world.EnsureChain(session, "chain_atp"), "повторный вызов безопасен");
                Equal(
                    5,
                    CountChainLocations(session),
                    "локаций не стало больше");
            });

            Check("цепочка АТП: зачистка выдаёт флешку и S4 сдаётся", () =>
            {
                var newGame = services.Resolve<INewGameService>();
                var story = services.Resolve<IStoryQuestService>();
                var session = new GameSession(newGame.Create(
                    42,
                    "loadout_rifleman",
                    new NewGameSettings(0, 1)));
                var player = session.State.Player!;
                player.CompleteQuest("qst_story_03_dogs");
                story.Accept(session, "qst_story_04_atp");

                var templates = new[]
                {
                    "loc_atp_gate", "loc_atp_yard", "loc_atp_garage",
                    "loc_atp_barracks", "loc_atp_dispatch"
                };
                for (var index = 0; index < templates.Length; index++)
                {
                    story.NotifyLocationCleared(
                        session,
                        StoryWorldService.InstanceId("chain_atp", index),
                        templates[index],
                        LocationSubtype.Building);
                }

                True(story.TryGetActive(session, "qst_story_04_atp", out var active), "S4 активен");
                True(story.IsReady(session, active), "цепочка засчитана");
                Equal(1, CountInventory(player, "itm_atp_flash"), "флешка выдана");

                var money = player.Money;
                var result = story.TurnIn(session, "qst_story_04_atp");

                True(result.Succeeded, "S4 сдан");
                Equal(2500, player.Money - money, "награда");
                Equal(0, CountInventory(player, "itm_atp_flash"), "флешка изъята");
                True(player.HasUnlock("unlock_smuggler_premium"), "открытие выдано");
            });
        }

        private void HangarTests(ServiceContainer services)
        {
            Check("Ангар: S5 создаёт локацию, ведёт к Гвоздю и закрывает главу", () =>
            {
                var newGame = services.Resolve<INewGameService>();
                var story = services.Resolve<IStoryQuestService>();
                var world = services.Resolve<IStoryWorldService>();
                var session = new GameSession(newGame.Create(
                    42,
                    "loadout_rifleman",
                    new NewGameSettings(0, 1)));
                var player = session.State.Player!;

                Equal(
                    QuestResultStatus.RequirementsNotMet,
                    story.Accept(session, "qst_story_05_angar").Status,
                    "без S4 не выдаётся");

                player.CompleteQuest("qst_story_04_atp");
                True(story.Accept(session, "qst_story_05_angar").Succeeded, "S5 взят");

                var instanceId = StoryWorldService.LocationInstanceId("loc_hub_depot");
                True(
                    session.State.World.Map.TryGetLocation(instanceId, out var depot),
                    "Депо создано");
                True(depot!.Objects.Count == 1, "Гвоздь на локации");

                story.NotifyLocationReached(session, instanceId, "loc_hub_depot");
                story.NotifyTalkedToActor(session, "npc_angar_gvozd");
                True(
                    story.TryGetActive(session, "qst_story_05_angar", out var active),
                    "S5 активен");
                True(story.IsReady(session, active), "S5 готов");

                var money = player.Money;
                True(story.TurnIn(session, "qst_story_05_angar").Succeeded, "S5 сдан");
                Equal(3000, player.Money - money, "награда");
                True(player.HasUnlock("unlock_angar"), "Ангар открыт");
            });
        }

        /// <summary>
        /// Сквозная проверка сдачи всех сюжетных квестов главы 1: S1 — рейд по настоящему
        /// маршруту и возврат в хаб, S4 — вход в цепочку АТП из маршрута, S5 — диалог в Депо,
        /// S2/S3/M1 — цели по событиям мира.
        /// </summary>
        private void ChapterTurnInTests(ServiceContainer services)
        {
            var sessions = CreateSessions(services);
            S1TurnInTest(services, sessions);
            ChainInRouteTest(services, sessions);
            S4TurnInTest(services, sessions);
            S5DialogueTurnInTest(services, sessions);
            DepotLockTests(services, sessions);
            EventQuestTurnInTests(services, sessions);
        }

        /// <summary>
        /// Служба сессии как в игре: тесты главы идут через неё, а не через голую новую игру.
        /// Файл сохранения тут не пишется, каталог нужен только конструктору.
        /// </summary>
        private static IGameSessionService CreateSessions(ServiceContainer services)
        {
            var root = Path.Combine(
                Path.GetTempPath(),
                "anomalypath-story",
                Guid.NewGuid().ToString("N"));
            var saves = new SaveService(new FileSaveStorage(root), services.Resolve<IItemRepository>());
            return Bootstrap.CreateSession(
                services.Resolve<ITravelService>(),
                services.Resolve<ICombatService>(),
                saves,
                services.Resolve<INewGameService>(),
                services.Resolve<IDeathService>(),
                services.Resolve<ICombatOutcomeService>(),
                services.Resolve<IQuestService>(),
                services.Resolve<ITalentService>(),
                services.Resolve<IProgressionService>(),
                services.Resolve<IStashService>(),
                services.Resolve<ILocationObjectService>(),
                services.Resolve<IWorldGenerator>(),
                services.Resolve<ITopologyRepository>(),
                storyQuests: services.Resolve<IStoryQuestService>(),
                locations: services.Resolve<ILocationRepository>());
        }

        private static WorldEdge? ChainEntryFromRoute(GameSession session)
        {
            var map = session.State.World.Map;
            var entryId = QuestChainInstanceIds.For("chain_atp", 0);
            var current = session.State.World.CurrentLocationId;
            if (current == null)
            {
                return null;
            }

            return FindEdge(map, current, entryId);
        }

        private static WorldEdge? FindEdge(WorldMap map, string fromId, string toId)
        {
            if (!map.TryGetLocation(fromId, out _))
            {
                return null;
            }

            foreach (var edge in map.EdgesFrom(fromId))
            {
                if (string.Equals(edge.ToId, toId, System.StringComparison.Ordinal))
                {
                    return edge;
                }
            }

            return null;
        }

        private static bool HasEdge(WorldMap map, string fromId, string toId) =>
            FindEdge(map, fromId, toId) != null;

        private static void DefeatEnemies(LocationInstance location)
        {
            foreach (var item in location.Objects)
            {
                if (item is EnemyLocationObject enemy)
                {
                    location.MarkEnemyDefeated(enemy.ActorId);
                }
            }
        }

        private static int CountChainLocations(GameSession session)
        {
            var count = 0;
            foreach (var location in session.State.World.Map.Locations)
            {
                if (location.InstanceId.StartsWith("chain_atp_", System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }

    }
}
