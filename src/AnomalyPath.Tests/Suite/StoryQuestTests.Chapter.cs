#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="StoryQuestTests"/>: глава 1 — S1–S5, Депо и события мира.</summary>
    public sealed partial class StoryQuestTests
    {
        private void S1TurnInTest(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сюжетный квест: S1 проходится за рейд и сдаётся в хабе", () =>
            {
                var story = services.Resolve<IStoryQuestService>();
                var dialogs = services.Resolve<IDialogService>();
                var session = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                var player = session.State.Player!;

                var root = dialogs.Start(session, "dlg_trd_hub_smuggler", "trd_hub_smuggler");
                True(root != null, "диалог Контрабандиста открылся");
                True(
                    dialogs.Choose(session, "dlg_trd_hub_smuggler", root!.NodeId, "s1_std").Succeeded,
                    "S1 взят в диалоге");
                True(story.TryGetActive(session, "qst_story_01_cordon", out var active), "S1 активен");

                True(sessions.LeaveHub(session).Succeeded, "вышли из хаба");

                // Зачистки рейда приходят теми же событиями, что и в игре; маршрут может
                // увести в тупиковую ветку, поэтому сама геометрия здесь не проверяется.
                for (var index = 0; index < 4; index++)
                {
                    story.NotifyLocationCleared(
                        session,
                        "loc_raid_" + index,
                        "loc_building_industrial",
                        LocationSubtype.Building);
                }

                Equal(4, active.ObjectiveAt(0).Current, "четыре зачищенные локации рейда");
                True(story.IsReady(session, active), "S1 готов ещё до хаба");

                session.State.World.RestoreStamina(100, 0);
                True(sessions.ReturnToHub(session).Succeeded, "вернулись в хаб");
                Equal(4, active.ObjectiveAt(0).Current, "счёт рейда пережил вход в хаб");
                True(story.IsReady(session, active), "S1 готов в хабе");

                var ready = dialogs.Start(session, "dlg_trd_hub_smuggler", "trd_hub_smuggler");
                True(ready != null, "диалог сдачи открылся");
                True(
                    dialogs.Choose(session, "dlg_trd_hub_smuggler", ready!.NodeId, "s1_ready").Succeeded,
                    "фраза о сдаче доступна");
                True(
                    dialogs.Choose(session, "dlg_trd_hub_smuggler", "s1_ready", "std").Succeeded,
                    "награда предложена");
                var turnIn = dialogs.Choose(session, "dlg_trd_hub_smuggler", "s1_reward_std", "thanks");
                True(turnIn.Succeeded, "S1 сдан");
                True(player.HasCompletedQuest("qst_story_01_cordon"), "S1 в завершённых");
                True(turnIn.Loot.Count > 0, "награда ждёт окна наград");
            });
        }

        private void ChainInRouteTest(ServiceContainer services, IGameSessionService sessions)
        {
            Check("цепочка АТП: включается в маршрут только при активном S4", () =>
            {
                var story = services.Resolve<IStoryQuestService>();
                var entryId = QuestChainInstanceIds.For("chain_atp", 0);

                var plain = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                True(sessions.LeaveHub(plain).Succeeded, "вышли без квеста");
                False(plain.State.World.Map.ContainsLocation(entryId), "без S4 цепочки в маршруте нет");

                var session = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                session.State.Player!.CompleteQuest("qst_story_03_dogs");
                True(story.Accept(session, "qst_story_04_atp").Succeeded, "S4 взят");
                True(sessions.LeaveHub(session).Succeeded, "вышли с активным S4");

                var map = session.State.World.Map;
                True(map.ContainsLocation(entryId), "заставка АТП в маршруте");
                True(ChainEntryFromRoute(session) != null, "вход в цепочку с локации маршрута");

                var last = map.GetLocation(QuestChainInstanceIds.For("chain_atp", 4));
                True(
                    HasEdge(map, last.InstanceId, map.StartLocationId),
                    "из диспетчерской есть переход в хаб");
                True(HasExitToRoad(map, last.InstanceId), "из диспетчерской есть выход на новую дорогу");
            });
        }

        private void S4TurnInTest(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сюжетный квест: цепочка АТП проходится из маршрута и S4 сдаётся", () =>
            {
                var story = services.Resolve<IStoryQuestService>();
                var session = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                var player = session.State.Player!;
                player.CompleteQuest("qst_story_03_dogs");
                True(story.Accept(session, "qst_story_04_atp").Succeeded, "S4 взят");
                True(sessions.LeaveHub(session).Succeeded, "вышли из хаба");

                var map = session.State.World.Map;
                var entryEdge = ChainEntryFromRoute(session);
                True(entryEdge != null, "переход в цепочку найден");
                session.State.World.RestoreStamina(100, 0);
                True(sessions.Travel(session, entryEdge!.EdgeId).Succeeded, "вошли на КПП АТП");
                Equal(
                    QuestChainInstanceIds.For("chain_atp", 0),
                    session.State.World.CurrentLocationId,
                    "КПП АТП");

                for (var index = 0; index < 5; index++)
                {
                    DefeatEnemies(session.State.World.CurrentLocation);
                    var current = QuestChainInstanceIds.For("chain_atp", index);
                    var next = index < 4
                        ? QuestChainInstanceIds.For("chain_atp", index + 1)
                        : map.StartLocationId;
                    var edge = FindEdge(map, current, next);
                    True(edge != null, "переход с шага " + index);
                    session.State.World.RestoreStamina(100, 0);
                    True(sessions.Travel(session, edge!.EdgeId).Succeeded, "шаг " + index + " пройден");
                }

                True(
                    story.TryGetActive(session, "qst_story_04_atp", out var active),
                    "S4 активен");
                True(story.IsReady(session, active), "цепочка зачищена");
                var money = player.Money;
                True(story.TurnIn(session, "qst_story_04_atp").Succeeded, "S4 сдан");
                Equal(2500, player.Money - money, "награда S4");
                True(player.HasUnlock("unlock_smuggler_premium"), "открытие выдано");
                False(map.ContainsLocation(QuestChainInstanceIds.For("chain_atp", 0)), "цепочка снята с карты");
            });
        }

        private void S5DialogueTurnInTest(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сюжетный квест: S5 сдаётся в диалоге с Гвоздём в Депо", () =>
            {
                var story = services.Resolve<IStoryQuestService>();
                var dialogs = services.Resolve<IDialogService>();
                var session = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                var player = session.State.Player!;
                player.CompleteQuest("qst_story_04_atp");
                True(story.Accept(session, "qst_story_05_angar").Succeeded, "S5 взят");

                var depotId = StoryWorldService.LocationInstanceId("loc_hub_depot");
                var travel = sessions.TravelToHub(session, depotId, HubTravelMode.Guide);
                True(travel.Succeeded, "доехали до Депо");
                Equal(LocationSubtype.Hub, session.State.World.CurrentLocation.Subtype, "Депо — хаб");

                var view = dialogs.Start(session, "dlg_angar_gvozd", "npc_angar_gvozd");
                True(view != null, "диалог Гвоздя открылся");
                True(story.TryGetActive(session, "qst_story_05_angar", out var active), "S5 активен");
                True(story.IsReady(session, active), "S5 готов после разговора");

                True(
                    dialogs.Choose(session, "dlg_angar_gvozd", view!.NodeId, "ready").Succeeded,
                    "фраза сдачи доступна");
                var turnIn = dialogs.Choose(session, "dlg_angar_gvozd", "s5_ready", "thanks");
                True(turnIn.Succeeded, "S5 сдан");
                True(player.HasCompletedQuest("qst_story_05_angar"), "глава закрыта");
                True(player.HasUnlock("unlock_angar"), "Ангар открыт");
            });
        }

        /// <summary>
        /// Депо появляется на карте только вместе с S5: до сдачи S4 и взятия S5 перехода нет,
        /// а без сданного S4 квест S5 вообще не выдаётся.
        /// </summary>
        private void DepotLockTests(ServiceContainer services, IGameSessionService sessions)
        {
            Check("Депо: переход закрыт, пока S4 не сдан и S5 не взят", () =>
            {
                var story = services.Resolve<IStoryQuestService>();
                var depotId = StoryWorldService.LocationInstanceId("loc_hub_depot");

                var fresh = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                False(fresh.State.World.Map.ContainsLocation(depotId), "в новой игре Депо на карте нет");
                Equal(
                    HubTravelStatus.TargetNotFound,
                    sessions.GetHubTravelQuote(fresh, depotId, HubTravelMode.Guide).Status,
                    "переход в Депо не предлагается");
                Equal(
                    HubTravelStatus.TargetNotFound,
                    sessions.TravelToHub(fresh, depotId, HubTravelMode.Guide).Status,
                    "поездка в Депо отклонена");

                var s3 = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                s3.State.Player!.CompleteQuest("qst_story_03_dogs");
                Equal(
                    QuestResultStatus.RequirementsNotMet,
                    story.Accept(s3, "qst_story_05_angar").Status,
                    "без S4 квест S5 не выдаётся");

                var withS4 = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                withS4.State.Player!.CompleteQuest("qst_story_03_dogs");
                True(story.Accept(withS4, "qst_story_04_atp").Succeeded, "S4 взят");
                False(withS4.State.World.Map.ContainsLocation(depotId), "пока S4 активен, Депо нет");
                Equal(
                    HubTravelStatus.TargetNotFound,
                    sessions.GetHubTravelQuote(withS4, depotId, HubTravelMode.Guide).Status,
                    "пока S4 активен, перехода нет");

                var s4Done = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                s4Done.State.Player!.CompleteQuest("qst_story_03_dogs");
                s4Done.State.Player.CompleteQuest("qst_story_04_atp");
                False(s4Done.State.World.Map.ContainsLocation(depotId), "после S4, но без S5, Депо нет");
                Equal(
                    HubTravelStatus.TargetNotFound,
                    sessions.GetHubTravelQuote(s4Done, depotId, HubTravelMode.Guide).Status,
                    "после S4, но без S5, перехода нет");
                Equal(
                    HubTravelStatus.TargetNotFound,
                    sessions.TravelToHub(s4Done, depotId, HubTravelMode.Guide).Status,
                    "после S4, но без S5, поездка отклонена");

                True(story.Accept(s4Done, "qst_story_05_angar").Succeeded, "S5 взят");
                True(s4Done.State.World.Map.ContainsLocation(depotId), "вместе с S5 Депо появилось");
                True(
                    sessions.GetHubTravelQuote(s4Done, depotId, HubTravelMode.Guide).Available,
                    "переход в Депо открылся");
                True(
                    sessions.TravelToHub(s4Done, depotId, HubTravelMode.Guide).Succeeded,
                    "поездка в Депо прошла");
                Equal(depotId, s4Done.State.World.CurrentLocationId, "игрок в Депо");
            });
        }

        private void EventQuestTurnInTests(ServiceContainer services, IGameSessionService sessions)
        {
            Check("сюжетный квест: S2 сдаётся после убийств, тайника и осмотра", () =>
            {
                var story = services.Resolve<IStoryQuestService>();
                var session = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                session.State.Player!.CompleteQuest("qst_story_01_cordon");
                True(story.Accept(session, "qst_story_02_job").Succeeded, "S2 взят");

                story.NotifyActorKilled(session, "enemy_bandit");
                story.NotifyActorKilled(session, "enemy_bandit");
                story.NotifyStashOpened(session, "loc_stash");
                for (var index = 0; index < 5; index++)
                {
                    story.NotifyObjectInspected(session, "loc_" + index, LocationObjectKind.Loot, "spawn_" + index);
                }

                True(
                    story.TryGetActive(session, "qst_story_02_job", out var active) &&
                    story.IsReady(session, active),
                    "S2 готов");
                True(story.TurnIn(session, "qst_story_02_job").Succeeded, "S2 сдан");
            });

            Check("сюжетный квест: S3 и M1 сдаются после убийств мутантов", () =>
            {
                var story = services.Resolve<IStoryQuestService>();

                var dogs = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                dogs.State.Player!.CompleteQuest("qst_story_02_job");
                True(story.Accept(dogs, "qst_story_03_dogs").Succeeded, "S3 взят");
                for (var index = 0; index < 10; index++)
                {
                    story.NotifyActorKilled(dogs, "mutant_wolf");
                }

                True(
                    story.TryGetActive(dogs, "qst_story_03_dogs", out var st3) && story.IsReady(dogs, st3),
                    "S3 готов");
                True(story.TurnIn(dogs, "qst_story_03_dogs").Succeeded, "S3 сдан");

                var medic = sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));
                medic.State.Player!.CompleteQuest("qst_story_03_dogs");
                True(story.Accept(medic, "qst_side_medic_mutants").Succeeded, "M1 взят");
                for (var index = 0; index < 20; index++)
                {
                    story.NotifyActorKilled(medic, "mutant_wolf");
                }

                True(
                    story.TryGetActive(medic, "qst_side_medic_mutants", out var m1) && story.IsReady(medic, m1),
                    "M1 готов");
                True(story.TurnIn(medic, "qst_side_medic_mutants").Succeeded, "M1 сдан");
            });
        }

        /// <summary>Есть ли из локации прямой выход на дорогу (для возврата из зоны).</summary>
        /// <param name="map">Карта мира.</param>
        /// <param name="fromId">Id локации.</param>
        /// <returns><c>true</c>, если есть прямое ребро на путь.</returns>
        private static bool HasExitToRoad(WorldMap map, string fromId)
        {
            foreach (var edge in map.EdgesFrom(fromId))
            {
                if (!edge.EdgeId.EndsWith(".out", System.StringComparison.Ordinal) ||
                    !map.TryGetLocation(edge.ToId, out var target) || target == null)
                {
                    continue;
                }

                if (target.Subtype == LocationSubtype.Path)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Ребро маршрута, ведущее в первый шаг активной цепочки.</summary>
    }
}
