#nullable enable
using System.Collections;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Диалог NPC вне комнаты торговца: общее окно с меню «Поговорить», ответ и фразы
    /// из данных, завершающая фраза уводит на следующую локацию, Esc возвращает на текущую.
    /// </summary>
    public sealed class NpcDialogPlayModeTests : PlayModeTestBase
    {
        private const string NpcSpawnId = "spawn_atp_shustry";
        private const string NpcActorId = "npc_atp_shustry";

        /// <summary>Меню диалога NPC показывает одну полосу «Поговорить».</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NpcDialog_Menu_ShowsOnlyTalk()
        {
            yield return StartGame();
            var screen = FindScreen();
            GoToLocation(screen);
            OpenShustryDialog();

            Assert.IsNotNull(GameObject.Find("NpcDialog"), "Окно диалога NPC не открылось");
            Assert.IsNotNull(GameObject.Find("DialogMenu"), "Меню диалога не показано");
            Assert.IsNull(GameObject.Find("DialogTalkWindow"), "Окно разговора не должно быть открыто");
            Assert.AreEqual("Поговорить", Label("DialogTalk"), "Единственная полоса меню");
            Assert.IsNull(GameObject.Find("DialogTrade"), "У NPC нет торговли");
            Assert.IsNull(GameObject.Find("DialogFarewell"), "У NPC нет прощания");
        }

        /// <summary>«Поговорить» показывает ответ Шустрого и его фразы из данных.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NpcDialog_Talk_ShowsShustryReply()
        {
            yield return StartGame();
            var screen = FindScreen();
            GoToLocation(screen);
            OpenShustryDialog();

            Click("DialogTalk");
            yield return null;

            Assert.IsNotNull(GameObject.Find("DialogTalkWindow"), "Окно разговора не открылось");
            Assert.IsNull(GameObject.Find("DialogMenu"), "Меню должно скрыться");
            Assert.AreEqual(
                screen.Texts.Get("actor.npc_atp_shustry.name"),
                Label("DialogSpeaker"),
                "Имя собеседника");
            Assert.AreEqual(
                screen.Texts.Get("dialog.dlg_npc_atp_shustry.root"),
                Label("DialogReply"),
                "Ответ берётся из диалога");
            Assert.IsNotNull(GameObject.Find("DialogPhrase_bye"), "Нет завершающей фразы");
        }

        /// <summary>Завершающая фраза уводит на следующую локацию маршрута.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NpcDialog_FinalPhrase_TravelsToNextLocation()
        {
            yield return StartGame();
            var screen = FindScreen();
            GoToLocation(screen);
            var session = screen.Session!;
            var world = session.State.World;
            var edges = screen.Sessions.AvailableEdges(session);
            Assert.AreEqual(1, edges.Count, "У тестовой локации должен быть один переход");
            var next = edges[0].ToId;

            OpenShustryDialog();
            Click("DialogTalk");
            yield return null;
            Click("DialogPhrase_bye");
            yield return null;

            Assert.IsNull(GameObject.Find("NpcDialog"), "Диалог должен закрыться");
            Assert.AreEqual(
                next,
                world.CurrentLocationId,
                "Окончание диалога должно увести на следующую локацию");
        }

        /// <summary>Esc возвращает на текущую локацию, где стоит NPC, и ничего не переключает.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NpcDialog_Escape_ReturnsToCurrentLocation()
        {
            yield return StartGame();
            var screen = FindScreen();
            GoToLocation(screen);
            var current = screen.Session!.State.World.CurrentLocationId;
            OpenShustryDialog();

            Click("DialogTalk");
            yield return null;
            Assert.IsTrue(screen.HandleEscape(), "Esc должен закрыть диалог");
            yield return null;

            Assert.IsNull(GameObject.Find("NpcDialog"), "Диалог должен закрыться");
            Assert.AreEqual(current, screen.Session!.State.World.CurrentLocationId, "Локация не меняется");
            Assert.IsNotNull(GameObject.Find("InGame"), "Нужно вернуться на экран текущей локации");
        }

        private static void GoToLocation(GameScreen screen)
        {
            var session = screen.Session!;
            Assert.IsTrue(screen.Sessions.LeaveHub(session).Succeeded, "Не удалось выйти из хаба");

            var world = session.State.World;
            var current = new LocationInstance(
                "npc_dialog_test",
                "loc_route_highway",
                1,
                "bg_road_dawn",
                LocationSubtype.Path,
                1,
                0.2,
                1);
            current.MarkSpawned();
            current.MarkStashRolled();
            current.AddObject(new NpcLocationObject(NpcSpawnId, "spawn.atp.shustry.title", NpcActorId));

            var next = new LocationInstance(
                "npc_dialog_next",
                "loc_route_highway",
                2,
                "bg_road_dawn",
                LocationSubtype.Path,
                1,
                0.2,
                2);
            next.MarkSpawned();
            next.MarkStashRolled();

            world.Map.AddLocation(current);
            world.Map.AddLocation(next);
            world.Map.AddEdge(new WorldEdge(
                "npc_dialog_edge",
                current.InstanceId,
                next.InstanceId,
                "road",
                1,
                TravelCost.Zero,
                oneWay: false,
                hidden: false));
            world.MoveTo(current.InstanceId);
            screen.ShowGame(false);
        }

        private static void OpenShustryDialog()
        {
            Click("Object_" + NpcSpawnId);
            Assert.IsNotNull(GameObject.Find("NpcDialog"), "Клик по NPC не открыл диалог");
        }

        private static string Label(string name)
        {
            var target = GameObject.Find(name);
            Assert.IsNotNull(target, $"Нет объекта «{name}»");
            var label = target!.GetComponentInChildren<Text>();
            Assert.IsNotNull(label, $"У «{name}» нет подписи");
            return label!.text;
        }
    }
}
