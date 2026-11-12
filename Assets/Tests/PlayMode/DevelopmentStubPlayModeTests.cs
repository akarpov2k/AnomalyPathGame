#nullable enable
using System.Collections;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Заглушка «Игра в стадии разработки»: после завершения главы 1 (S5 сдан) попытка перехода
    /// в Депо открывает отдельный экран вместо поездки; закрытие крестиком и Esc возвращают
    /// в первый хаб, а игрок остаётся на месте.
    /// </summary>
    public sealed class DevelopmentStubPlayModeTests : PlayModeTestBase
    {
        /// <summary>Переход в Депо не выполняется, а открывает экран-заглушку с текстами каталога.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Stub_DepotTravel_ShowsScreenInsteadOfTravel()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var start = session.State.World.CurrentLocationId;

            FinishChapter(session);
            AddDepotHub(session);
            screen.ShowMap();
            yield return null;

            Click("Spot_loc_hub_depot");
            yield return null;
            Click("Guide");
            yield return null;

            var panel = GameObject.Find("DevelopmentStub");
            Assert.IsNotNull(
                panel,
                "Экран-заглушка не открылся: соберите префаб Tools/AnomalyPath/UI/Собрать префаб «Заглушка»");
            var title = FindDeep(panel!.transform, "StubTitle");
            Assert.IsNotNull(title, "В заглушке нет заголовка");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Development_Stub_Title),
                title!.GetComponent<Text>().text,
                "Заголовок заглушки");
            var message = FindDeep(panel.transform, "StubMessage");
            Assert.IsNotNull(message, "В заглушке нет текста");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Development_Stub_Message),
                message!.GetComponent<Text>().text,
                "Текст заглушки");

            Assert.AreEqual(start, session.State.World.CurrentLocationId, "переход не выполнен");
            Assert.IsNull(session.PendingHubTravel, "переход не начат");
        }

        /// <summary>Кнопка «Понятно» закрывает заглушку и возвращает в первый хаб.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Stub_Close_ReturnsToFirstHub()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;

            FinishChapter(session);
            AddDepotHub(session);
            screen.ShowMap();
            yield return null;
            Click("Spot_loc_hub_depot");
            yield return null;
            Click("Walk");
            yield return null;
            Assert.IsNotNull(GameObject.Find("DevelopmentStub"), "Заглушка не открылась");

            Click("StubClose");
            yield return null;

            Assert.IsFalse(FindAnywhere("DevelopmentStub")!.activeSelf, "Заглушка не закрылась");
            Assert.IsNotNull(GamePanel(), "После закрытия должен открыться игровой экран");
            Assert.AreEqual(
                session.State.World.Map.StartLocationId,
                session.State.World.CurrentLocationId,
                "игрок остаётся в стартовом хабе");
        }

        /// <summary>Esc закрывает заглушку и возвращает в игру.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Stub_Escape_ClosesScreen()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;

            FinishChapter(session);
            AddDepotHub(session);
            screen.ShowMap();
            yield return null;
            Click("Spot_loc_hub_depot");
            yield return null;
            Click("Guide");
            yield return null;
            Assert.IsNotNull(GameObject.Find("DevelopmentStub"), "Заглушка не открылась");

            Assert.IsTrue(screen.HandleEscape(), "Esc должен обработать заглушку");
            yield return null;

            Assert.IsFalse(FindAnywhere("DevelopmentStub")!.activeSelf, "Esc не закрыл заглушку");
            Assert.IsNotNull(GamePanel(), "Esc не вернул в игру");
        }

        /// <summary>
        /// Добавляет на карту хаб из шаблона Депо: после сданного S5 шаблон закрыт заглушкой,
        /// поэтому попытка перехода в него должна открыть экран-заглушку.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        private static void AddDepotHub(GameSession session)
        {
            session.State.World.Map.AddLocation(new LocationInstance(
                "test_hub_development",
                "loc_hub_depot",
                777,
                "bg_site_house",
                LocationSubtype.Hub,
                2,
                0));
        }

        /// <summary>Закрывает главу 1: с этого момента Депо — пустой хаб с заглушкой.</summary>
        /// <param name="session">Игровая сессия.</param>
        private static void FinishChapter(GameSession session) =>
            session.State.Player!.CompleteQuest("qst_story_05_angar");

        private static Transform? FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDeep(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
