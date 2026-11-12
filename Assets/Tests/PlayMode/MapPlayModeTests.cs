#nullable enable
using System.Collections;
using AnomalyPath.Core.World;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Карта мира: кнопка, кликабельные вырезки хабов и плашка перехода.</summary>
    public sealed class MapPlayModeTests : PlayModeTestBase
    {
        /// <summary>
        /// Карта открывается кнопкой, вырезка хаба открывает плашку с двумя способами перехода,
        /// но переход в хаб-заглушку не выполняется — после главы его показывает экран-заглушка
        /// (см. <see cref="DevelopmentStubPlayModeTests"/>).
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Map_OpensFromButton_DepotSpotShowsTravelPanel()
        {
            yield return StartGame();

            var screen = FindScreen();
            var session = screen.Session;
            Assert.IsNotNull(session, "сессия не создана");

            screen.ShowMap();
            yield return null;
            Assert.IsNotNull(FindAnywhere("Spot_loc_hub_settlement"), "вырезка деревни не построена");

            session!.State.Player!.CompleteQuest("qst_story_05_angar");
            session.State.World.Map.AddLocation(new LocationInstance(
                "test_hub",
                "loc_hub_depot",
                5,
                "bg_site_house",
                LocationSubtype.Hub,
                3,
                0));
            screen.ShowMap();
            yield return null;

            Click("Spot_loc_hub_depot");
            yield return null;
            Assert.IsNotNull(FindAnywhere("Walk"), "плашка перехода не открыта");
            Assert.IsNotNull(FindAnywhere("Guide"), "в плашке нет перехода с проводником");

            Click("Guide");
            yield return null;

            Assert.AreNotEqual("test_hub", session.State.World.CurrentLocationId, "переход в неготовый хаб выполнен");
        }
    }
}
