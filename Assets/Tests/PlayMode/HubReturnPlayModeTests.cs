#nullable enable
using System.Collections;
using AnomalyPath.Core.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Возврат в хаб с игрового экрана: действие на пути и открытие большого хаба.</summary>
    public sealed class HubReturnPlayModeTests : PlayModeTestBase
    {
        /// <summary>С пути действие «В хаб» есть, возврат открывает большой хаб, а не комнату торговца.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator HubReturn_FromPath_ShowsActionAndReturnsToHub()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;

            Click("LeaveHub");
            yield return null;

            Assert.AreEqual(LocationSubtype.Path, world.CurrentLocation.Subtype, "стоим на пути");
            Assert.AreEqual(1, world.CurrentLocation.RouteStep, "первый шаг");
            var action = GameObject.Find("ReturnToHub");
            Assert.IsNotNull(action, "На пути нет действия «В хаб»");

            Click("ReturnToHub");
            yield return null;

            Assert.AreEqual(LocationSubtype.Hub, world.CurrentLocation.Subtype, "вернулись в хаб");
            Assert.IsNotNull(GameObject.Find("BigHub"), "После возврата не открылся большой хаб");
            Assert.IsNull(GameObject.Find("Hub"), "В большом хабе не должно быть комнаты торговца");
            Assert.IsNull(GameObject.Find("ReturnToHub"), "В хабе действия «В хаб» нет");
        }

        /// <summary>Из здания в хаб теперь можно вернуться: действие есть и ведёт в хаб.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator HubReturn_FromBuilding_ReturnsToHub()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen!.Session!.State.World;
            var building = FindSubtype(world.Map, LocationSubtype.Building) ?? AddBuilding(world);
            world.MoveTo(building.InstanceId);
            screen.ShowGame(false);
            yield return null;

            Assert.IsNotNull(GameObject.Find("ReturnToHub"), "Из здания в хаб есть действие");
            Click("ReturnToHub");
            yield return null;

            Assert.AreEqual(LocationSubtype.Hub, world.CurrentLocation.Subtype, "вернулись в хаб");
        }

        private static LocationInstance AddBuilding(WorldState world)
        {
            var location = new LocationInstance(
                "locinst_test_building",
                "loc_site_house",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            world.Map.AddLocation(location);
            return location;
        }

        private static LocationInstance? FindSubtype(WorldMap map, LocationSubtype subtype)
        {
            foreach (var location in map.Locations)
            {
                if (location.Subtype == subtype)
                {
                    return location;
                }
            }

            return null;
        }
    }
}
