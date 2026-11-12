#nullable enable
using System.Collections;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Сюжет главы 1 в интерфейсе: цепочка АТП появляется среди переходов маршрута,
    /// а Гвоздь в Депо открывает диалог и принимает S5.
    /// </summary>
    public sealed class StoryChapterPlayModeTests : PlayModeTestBase
    {
        /// <summary>При активном S4 цепочка АТП стоит в переходах маршрута.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Story_S4_ChainOfferedOnRoute()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            session.State.Player!.CompleteQuest("qst_story_03_dogs");
            Assert.IsTrue(
                screen.StoryQuests.Accept(session, "qst_story_04_atp").Succeeded,
                "S4 не взят");

            session.State.World.RestoreStamina(100, 0);
            Click("LeaveHub");
            yield return null;
            yield return null;

            var entry = QuestChainInstanceIds.For("chain_atp", 0);
            var offered = false;
            var edges = screen.Sessions.AvailableEdges(session);
            foreach (var edge in edges)
            {
                if (string.Equals(edge.ToId, entry, System.StringComparison.Ordinal))
                {
                    offered = true;
                }
            }

            Assert.IsTrue(offered, "в переходах маршрута нет входа в цепочку АТП");

            var shown = 0;
            foreach (var slot in new[] { "Center", "Right", "Left" })
            {
                if (FindTransition(slot) != null)
                {
                    shown++;
                }
            }

            Assert.AreEqual(
                Mathf.Min(3, edges.Count),
                shown,
                "показаны не все переходы маршрута");
        }

        /// <summary>
        /// До сдачи S4 и взятия S5 Депо нет на карте, и перехода к нему нет; вместе с S5
        /// вырезка появляется и предлагает оба способа перехода.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Story_Depot_NotOnMapUntilQuestFive()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;

            screen.ShowMap();
            yield return null;
            Assert.IsNotNull(FindAnywhere("Spot_loc_hub_settlement"), "нет вырезки Поселения");
            Assert.IsNull(FindAnywhere("Spot_loc_hub_depot"), "Депо на карте в начале игры");

            screen.ShowGame(false);
            yield return null;
            session.State.Player!.CompleteQuest("qst_story_03_dogs");
            session.State.Player.CompleteQuest("qst_story_04_atp");
            screen.ShowMap();
            yield return null;
            Assert.IsNull(
                FindAnywhere("Spot_loc_hub_depot"),
                "Депо на карте, хотя S5 ещё не взят");

            screen.ShowGame(false);
            yield return null;
            Assert.IsTrue(
                screen.StoryQuests.Accept(session, "qst_story_05_angar").Succeeded,
                "S5 не взят");
            screen.ShowMap();
            yield return null;
            Assert.IsNotNull(
                FindAnywhere("Spot_loc_hub_depot"),
                "после S5 Депо не появилось на карте");

            Click("Spot_loc_hub_depot");
            yield return null;
            Assert.IsNotNull(FindAnywhere("Walk"), "пеший переход в Депо не предлагается");
            Assert.IsNotNull(FindAnywhere("Guide"), "переход с проводником в Депо не предлагается");
        }

        /// <summary>В Депо Гвоздь показан на хабе, открывает диалог и принимает S5.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Story_S5_GvozdInDepot_OpensDialogAndTurnsIn()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            session.State.Player!.CompleteQuest("qst_story_04_atp");
            Assert.IsTrue(
                screen.StoryQuests.Accept(session, "qst_story_05_angar").Succeeded,
                "S5 не взят");

            var travel = screen.Sessions.TravelToHub(
                session,
                "story_loc_hub_depot",
                HubTravelMode.Guide);
            Assert.IsTrue(travel.Succeeded, "поездка в Депо не удалась: " + travel.Status);
            screen.ShowGame(false);
            yield return null;

            var npc = GameObject.Find("Object_spawn_angar_gvozd");
            Assert.IsNotNull(npc, "на большом хабе не показан Гвоздь");
            var button = npc!.GetComponent<Button>();
            Assert.IsNotNull(button, "у фигуры Гвоздя нет кнопки");
            button!.onClick.Invoke();
            yield return null;

            Assert.IsNotNull(GameObject.Find("NpcDialog"), "диалог Гвоздя не открылся");
            Click("DialogTalk");
            yield return null;
            Click("DialogPhrase_ready");
            yield return null;
            Click("DialogPhrase_thanks");
            yield return null;

            Assert.IsTrue(
                session.State.Player.HasCompletedQuest("qst_story_05_angar"),
                "S5 не сдан в диалоге");
            Assert.IsTrue(session.State.Player.HasUnlock("unlock_angar"), "Ангар не открыт");
        }
    }
}
