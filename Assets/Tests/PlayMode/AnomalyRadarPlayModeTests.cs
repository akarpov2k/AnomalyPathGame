#nullable enable
using System.Collections;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Minigames;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Аномалия на игровом экране: клик по объекту сразу открывает префабный радар,
    /// скан клетки бесплатен, обыск тратит попытку и открывает окно лута, «Уйти» возвращает на поле.
    /// </summary>
    public sealed class AnomalyRadarPlayModeTests : PlayModeTestBase
    {
        private const string SpawnId = "spawn_test_anomaly";
        private const string TitleKey = "spawn.spawn_anomaly_chem.title";
        private const string AnomalyId = "anomaly_chem";

        /// <summary>Сигнал 4 отличается от зелёных 1–3 тёплым золотым цветом.</summary>
        [Test]
        public void MinigamePalette_Signal4_StandsOut()
        {
            Assert.Greater(MinigamePalette.Signal4.r, MinigamePalette.Signal4.g, "4 — тёплый цвет");
            Assert.Greater(MinigamePalette.Signal4.r - MinigamePalette.Signal3.r, 0.3f, "4 заметно отличается от 3");
        }

        /// <summary>Действие аномалии сразу открывает радар, отдельного кодового окна нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Anomaly_Action_OpensRadar()
        {
            yield return StartGame();
            yield return CloseRadarIfOpen();
            var screen = FindScreen();
            screen!.Sessions.LeaveHub(screen.Session!);
            var anomaly = AddAnomaly(screen);

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);
            yield return null;

            var radar = GameObject.Find("RadarMinigame");
            Assert.IsNotNull(radar, "Радар не открылся");
            Assert.IsTrue(radar!.activeSelf, "Радар скрыт");
            Assert.IsNull(GameObject.Find("Anomaly"), "Отдельного кодового окна аномалии больше нет");
            Assert.IsNotNull(anomaly, "Аномалия не добавлена на локацию");
            Click("RadarLeave");
            yield return null;
        }

        /// <summary>«Уйти» в радаре возвращает на игровое поле.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Anomaly_Radar_LeaveReturnsToGame()
        {
            yield return StartGame();
            yield return CloseRadarIfOpen();
            var screen = FindScreen();
            screen!.ShowGame(false);
            yield return null;
            screen.Sessions.LeaveHub(screen.Session!);
            AddAnomaly(screen);

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);
            yield return null;
            Click("RadarLeave");
            yield return null;

            var radar = FindAnywhere("RadarMinigame");
            Assert.IsNotNull(radar, "Панель радара пропала");
            Assert.IsFalse(radar!.activeSelf, "Радар не закрылся");
            Assert.IsNotNull(GameObject.Find("InGame"), "Игровое поле не вернулось");
        }

        /// <summary>Скан клетки бесплатен и показывает силу сигнала.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Anomaly_Radar_ScanCellIsFree()
        {
            yield return StartGame();
            yield return CloseRadarIfOpen();
            var screen = FindScreen();
            screen!.Sessions.LeaveHub(screen.Session!);
            var anomaly = AddAnomaly(screen);

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);
            yield return null;

            Click("Cell0");
            Assert.IsTrue(
                GameObject.Find("RadarScan")!.GetComponent<Button>().interactable,
                "После выбора клетки кнопка «Сканировать» должна быть доступна");
            Click("RadarScan");
            yield return null;

            Assert.AreEqual(1, anomaly.ScannedCells.Count, "Скан не отмечен");
            Assert.AreEqual(0, anomaly.AttemptsUsed, "Скан не должен тратить попытку");
            var cell = GameObject.Find("Cell0");
            Assert.IsNotNull(cell, "Клетка поля не найдена");
            Assert.AreNotEqual("?", cell!.GetComponentInChildren<Text>(true).text, "Сигнал не показан");
        }

        /// <summary>Обыск клетки с артефактом тратит попытку и открывает окно лута.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Anomaly_Radar_SearchCellFindsArtifactAndOpensLoot()
        {
            yield return StartGame();
            yield return CloseRadarIfOpen();
            var screen = FindScreen();
            screen!.Sessions.LeaveHub(screen.Session!);
            var anomaly = AddAnomaly(screen);
            anomaly.PlaceRadar(new[] { 0 }, new[] { 0 });

            screen.ShowGame(false);
            yield return null;
            ClickObjectAction(SpawnId);
            yield return null;

            Click("Cell0");
            Click("RadarSearch");
            yield return null;

            Assert.AreEqual(1, anomaly.AttemptsUsed, "Обыск не потратил попытку");
            Assert.AreEqual(1, anomaly.Loot.Count, "Артефакт не попал в аномалию");
            var rewards = FindAnywhere("Rewards");
            Assert.IsNotNull(rewards, "Экран добычи не создан");
            Assert.IsTrue(rewards!.activeSelf, "Экран добычи не открылся после находки");
            Assert.IsNull(GameObject.Find("LootDialog"), "Диалога лута быть не должно");

            Assert.IsTrue(FindScreen().HandleEscape(), "Esc должен обработать экран добычи");
            yield return null;
            Click("Закрыть");
            yield return null;
            Assert.IsFalse(rewards.activeSelf, "Экран добычи не закрылся");
            Assert.IsTrue(GameObject.Find("RadarMinigame")!.activeSelf, "Радар должен остаться открытым");
        }

        /// <summary>Закрывает радар, оставшийся от предыдущего теста.</summary>
        /// <returns>Корутина закрытия.</returns>
        private static IEnumerator CloseRadarIfOpen()
        {
            var radar = FindAnywhere("RadarMinigame");
            if (radar != null && radar.activeSelf)
            {
                Click("RadarLeave");
                yield return null;
            }
        }

        /// <summary>Добавляет в текущую локацию аномалию с отдельным id спавна для теста.</summary>
        private static AnomalyLocationObject AddAnomaly(GameScreen screen)
        {
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_radar_" + System.Guid.NewGuid().ToString("N"),
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            var anomaly = new AnomalyLocationObject(SpawnId, TitleKey, AnomalyId);
            location.AddObject(anomaly);
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            return anomaly;
        }

        private static GameObject? FindObjectAction()
        {
            var root = GameObject.Find("InGame");
            if (root == null)
            {
                return null;
            }

            var actionName = "Object_" + SpawnId;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (string.Equals(button.name, actionName, System.StringComparison.Ordinal))
                {
                    return button.gameObject;
                }
            }

            return null;
        }

        private static void ClickObjectAction(string spawnId)
        {
            var action = FindObjectAction();
            Assert.IsNotNull(action, $"Действие объекта {spawnId} не найдено");
            var button = action!.GetComponent<Button>();
            Assert.IsTrue(button.interactable, "Действие аномалии неактивно");
            button.onClick.Invoke();
        }
    }
}
