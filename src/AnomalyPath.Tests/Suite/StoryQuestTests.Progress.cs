#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="StoryQuestTests"/>: взятие, сдача, области прогресса и хуки сессии.</summary>
    public sealed partial class StoryQuestTests
    {
        private void AcceptAndTurnInTests(IStoryQuestService story, IGameSessionService sessions)
        {
            Check("сюжетный квест: выдача, прогресс и сдача", () =>
            {
                var session = NewSession(sessions);
                var player = session.State.Player!;
                True(HasTemplate(story.GetAvailable(session, "npc_giver"), "qst_svc"), "qst_svc доступен");

                var accepted = story.Accept(session, "qst_svc");
                True(accepted.Succeeded, "квест взят");
                Equal(1, story.GetActive(session).Count, "активный один");

                var active = story.GetActive(session)[0];
                False(story.IsReady(session, active), "в начале не готов");
                story.NotifyStashOpened(session, "loc_a");
                story.NotifyStashOpened(session, "loc_b");
                True(story.IsReady(session, active), "две локации — готов");

                var moneyBefore = player.Money;
                var ammoBefore = CountInventory(player, "ammo_545");
                var turnedIn = story.TurnIn(session, "qst_svc");

                True(turnedIn.Succeeded, "сдан");
                Equal(100, player.Money - moneyBefore, "деньги");
                Equal(ammoBefore, CountInventory(player, "ammo_545"), "предметы награды не попали в инвентарь");
                Equal(1, turnedIn.Loot.Count, "предмет награды ждёт экрана наград");
                Equal("ammo_545", turnedIn.Loot[0].ItemId, "вид предмета награды");
                Equal(5, turnedIn.Loot[0].Stack, "штук в награде");
                True(player.HasCompletedQuest("qst_svc"), "в завершённых");
                Equal(
                    QuestResultStatus.AlreadyCompleted,
                    story.TurnIn(session, "qst_svc").Status,
                    "повторная сдача");
            });
        }

        private void LineAndVariantTests(IStoryQuestService story, IGameSessionService sessions)
        {
            Check("сюжетный квест: линия занята и вариант hard", () =>
            {
                var session = NewSession(sessions);
                True(story.Accept(session, "qst_svc").Succeeded, "первый взят");
                Equal(
                    QuestResultStatus.LineBusy,
                    story.Accept(session, "qst_svc2").Status,
                    "линия занята");

                var hardSession = NewSession(sessions);
                True(story.Accept(hardSession, "qst_hard", "hard").Succeeded, "hard взят");
                var active = story.GetActive(hardSession)[0];
                Equal("hard", active.VariantId, "вариант");
                Equal(0, active.ObjectiveAt(0).Current, "прогресс с нуля");

                for (var index = 0; index < 3; index++)
                {
                    story.NotifyStashOpened(hardSession, "loc_" + index);
                }

                False(story.IsReady(hardSession, active), "трёх мало для hard");
                story.NotifyStashOpened(hardSession, "loc_3");
                True(story.IsReady(hardSession, active), "четыре — готов");
            });

            Check("сюжетный квест: неизвестный вариант не берётся", () =>
            {
                var session = NewSession(sessions);
                Equal(
                    QuestResultStatus.VariantNotFound,
                    story.Accept(session, "qst_hard", "impossible").Status,
                    "чужой вариант");
            });
        }

        private void ProgressScopeTests(IStoryQuestService story, IGameSessionService sessions)
        {
            Check("сюжетный квест: рейд сохраняет прогресс при входе в хаб", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_svc");
                story.NotifyStashOpened(session, "loc_a");
                Equal(1, story.GetActive(session)[0].ObjectiveAt(0).Current, "до входа в хаб");

                story.OnEnterHub(session);

                Equal(1, story.GetActive(session)[0].ObjectiveAt(0).Current, "прогресс рейда сохранён");
            });

            Check("сюжетный квест: новый рейд сбрасывает прогресс рейда при выходе из хаба", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_svc");
                story.NotifyStashOpened(session, "loc_a");
                story.OnEnterHub(session);

                story.OnLeaveHub(session);

                Equal(0, story.GetActive(session)[0].ObjectiveAt(0).Current, "новый рейд — с нуля");
            });

            Check("сюжетный квест: жизнь не сбрасывается хабом, но сбрасывается смертью", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_medic");
                story.NotifyStashOpened(session, "loc_a");
                story.OnEnterHub(session);
                Equal(1, story.GetActive(session)[0].ObjectiveAt(0).Current, "хаб не сбрасывает");

                story.OnDeath(session);
                Equal(0, story.GetActive(session)[0].ObjectiveAt(0).Current, "смерть сбрасывает");
            });

            Check("сюжетный квест: смерть сбрасывает и прогресс рейда", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_svc");
                story.NotifyStashOpened(session, "loc_a");

                story.OnDeath(session);

                Equal(0, story.GetActive(session)[0].ObjectiveAt(0).Current, "смерть обнуляет рейд");
            });
        }

        private void ProgressNotificationTests(
            IStoryQuestService story,
            IGameSessionService sessions)
        {
            Check("сюжетный квест: одна цель не зачитывается дважды", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_svc");
                story.NotifyStashOpened(session, "loc_a");
                story.NotifyStashOpened(session, "loc_a");
                Equal(1, story.GetActive(session)[0].ObjectiveAt(0).Current, "дедупликация");
            });

            Check("сюжетный квест: цель reachLocation засчитывает нужный шаблон", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_reach");
                var active = story.GetActive(session)[0];

                story.NotifyLocationReached(session, "loc_x", "loc_route_highway");
                False(story.IsReady(session, active), "чужой шаблон не считается");

                story.NotifyLocationReached(session, "loc_y", "loc_route_forest");
                True(story.IsReady(session, active), "нужный шаблон засчитан");
            });

            Check("сюжетный квест: убийство вне пула не считается", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_kill");
                var active = story.GetActive(session)[0];

                story.NotifyActorKilled(session, "enemy_other");
                Equal(0, active.ObjectiveAt(0).Current, "чужой актёр");

                story.NotifyActorKilled(session, "enemy_bandit");
                Equal(1, active.ObjectiveAt(0).Current, "свой актёр");
            });
        }

        private void SaveRoundTripTests(
            IStoryQuestService story,
            IGameSessionService sessions,
            IItemRepository items)
        {
            Check("сюжетный квест: сейв сохраняет прогресс и рейд", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_medic", "standard");
                story.NotifyStashOpened(session, "loc_a");
                story.NotifyLocationCleared(
                    session,
                    "loc_a",
                    "loc_building_industrial",
                    LocationSubtype.Building);

                var saved = PlayerSaveMapper.ToSaveData(session.State.Player!);
                var json = saved.ToJson();
                var parsed = PlayerSaveData.FromJson(JsonParser.Parse(JsonWriter.Write(json), "test"), "test");
                var restored = PlayerSaveMapper.FromSaveData(parsed, items, "test");

                Equal(1, restored.StoryQuests.Count, "квестов в сейве");
                var quest = restored.StoryQuests[0];
                Equal("qst_medic", quest.QuestId, "id");
                Equal("standard", quest.VariantId, "вариант");
                Equal(QuestProgressScope.Life, quest.Scope, "область");
                Equal(1, quest.ObjectiveAt(0).Current, "прогресс");

                var world = WorldSaveMapper.ToSaveData(session.State);
                var raid = WorldSaveMapper.LoadRaid(world.Raid);
                False(raid.Active, "рейд не начат");
                Equal(1, raid.ClearedLocationIds.Count, "зачищенная локация");
                Equal("loc_a", raid.ClearedLocationIds[0], "id локации");
            });
        }

        private void SessionHookTests(IStoryQuestService story, IGameSessionService sessions)
        {
            Check("сессия: выход из хаба начинает рейд и сбрасывает его прогресс", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_svc");
                story.NotifyStashOpened(session, "loc_a");

                var result = sessions.LeaveHub(session);

                True(result.Succeeded, "вышли из хаба");
                True(session.State.Raid.Active, "рейд начат");
                Equal(0, story.GetActive(session)[0].ObjectiveAt(0).Current, "прогресс сброшен");
            });

            Check("сессия: возврат в хаб завершает рейд", () =>
            {
                var session = NewSession(sessions);
                True(sessions.LeaveHub(session).Succeeded, "вышли из хаба");
                True(session.State.Raid.Active, "рейд идёт");

                True(sessions.ReturnToHub(session).Succeeded, "вернулись в хаб");

                False(session.State.Raid.Active, "рейд завершён");
                Equal(0, session.State.Raid.ClearedLocationIds.Count, "список очищен");
            });

            Check("сессия: смерть сбрасывает прогресс жизни", () =>
            {
                var session = NewSession(sessions);
                story.Accept(session, "qst_medic");
                story.NotifyStashOpened(session, "loc_a");

                sessions.ApplyDeath(session);

                Equal(0, story.GetActive(session)[0].ObjectiveAt(0).Current, "прогресс сброшен");
            });
        }

        private static GameSession NewSession(IGameSessionService sessions) =>
            sessions.StartNew(42, "loadout_rifleman", new NewGameSettings(0, 1));

        private static bool HasTemplate(
            IReadOnlyList<StoryQuestTemplate> templates,
            string questId)
        {
            foreach (var template in templates)
            {
                if (string.Equals(template.Id, questId, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountInventory(PlayerState player, string itemId)        {
            var total = 0;
            foreach (var item in player.Inventory.Items)
            {
                if (string.Equals(item.ItemId, itemId, System.StringComparison.Ordinal))
                {
                    total += item.Stack;
                }
            }

            return total;
        }

        private static string ServiceQuest(
            string id,
            string line,
            string scope,
            string objectives,
            string extra = "",
            string giver = "npc_giver")
        {
            return "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"" + id + "\",\"questKind\":\"story\"," +
                   "\"storyLine\":\"" + line + "\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                   "\"giverActorId\":\"" + giver + "\",\"progressScope\":\"" + scope + "\"," +
                   "\"objectives\":" + objectives + "," +
                   "\"rewards\":{\"money\":100,\"items\":[{\"itemId\":\"ammo_545\",\"count\":5}]}" +
                   (extra.Length > 0 ? "," + extra : string.Empty) +
                   "}";
        }
    }
}
