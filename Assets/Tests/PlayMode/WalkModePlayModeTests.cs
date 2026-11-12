#nullable enable
using System.Collections;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Screens.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Поход на настоящем экране: бесконечная дорога без выбора переходов, ответвления
    /// «вверх» с возвратом на новую дорогу и кнопка «В хаб».
    /// </summary>
    public sealed class WalkModePlayModeTests : PlayModeTestBase
    {
        /// <summary>Режим похода включается на пути и двигает полоску прогресса.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_OnPath_BuildsLaneAndMovesProgress()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var root = FindAnywhere("WalkLane");
            Assert.IsNotNull(root, "Режим похода не построен");
            Assert.IsTrue(root!.activeSelf, "Полоса похода должна быть видна");
            var lane = root.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя WalkLane");
            if (BackdropPartsActive(lane!))
            {
                Assert.IsFalse(lane.Road!.gameObject.activeInHierarchy,
                    "земля нарисована в кадре, отдельная полоса дороги скрыта");
            }
            else
            {
                Assert.IsNotNull(lane.Road, "Нет дороги внизу");
                Assert.IsNotNull(lane.Road!.sprite, "Дорога без картинки");
                Assert.IsTrue(lane.Road.gameObject.activeInHierarchy, "Дорога не показана");
            }

            var forward = FindAnywhere("WalkForward");
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            Assert.IsNotNull(FindAnywhere("WalkProgress"), "Нет полоски пути");
            var label = FindAnywhere("WalkProgressLabel")?.GetComponent<Text>();
            Assert.IsNotNull(label, "Нет подписи полоски пути");
            var before = label!.text;

            var button = forward!.GetComponent<Button>();
            Assert.IsNotNull(button, "У «Идти» нет кнопки");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(0.6f);
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;

            Assert.AreNotEqual(before, label.text, "Полоска пути не сдвинулась");
            Capture(play, "walk-mode-real");

            play.SetWalkMode(false);
            yield return null;
            var off = FindAnywhere("WalkLane");
            Assert.IsTrue(off == null || !off.activeSelf, "Режим похода не выключился");
        }

        /// <summary>Кнопка «В хаб» в режиме похода возвращает на хаб.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_ReturnButton_GoesToHub()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var back = FindAnywhere("WalkReturn")?.GetComponent<Button>();
            Assert.IsNotNull(back, "Нет кнопки «В хаб»");
            back!.onClick.Invoke();
            yield return null;

            var screen = FindScreen();
            Assert.IsNotNull(screen.Session, "Нет сессии");
            Assert.AreEqual("Hub", screen.Session!.State.World.CurrentLocation.Subtype.ToString(), "должен быть хаб");
            Assert.IsFalse(
                FindAnywhere("WalkLane")?.activeInHierarchy ?? false,
                "режим похода не должен оставаться на хабе");
        }

        /// <summary>У ответвления показывается картинка цели, а не вставленная тропа.</summary>
        /// <remarks>
        /// Переход поднят над горизонтальной дорогой, а по X стоит там, где в этом сегменте
        /// нарисована вертикальная тропа (<c>walkPaths</c>).
        /// </remarks>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Branch_ShowsTargetArtWithoutPath()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var start = new LocationInstance(
                "test_art_start",
                "loc_route_forest",
                1,
                "bg_route_forest",
                LocationSubtype.Path,
                1,
                0.1);
            start.MarkSpawned();
            start.MarkStashRolled();
            var target = new LocationInstance(
                "test_art_target",
                "loc_site_house",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            target.MarkSpawned();
            target.MarkStashRolled();
            world.Map.AddLocation(start);
            world.Map.AddLocation(target);
            world.Map.AddEdge(new WorldEdge(
                "test_art_target.out",
                start.InstanceId,
                target.InstanceId,
                "path",
                1,
                new TravelCost(0, 0.5),
                false,
                false));
            world.MoveTo(start.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var branch = FindAnywhere("WalkBranch0");
            Assert.IsNotNull(branch, "Ответвление не построено");
            Assert.IsTrue(branch!.activeInHierarchy, "Ответвление скрыто");
            var transition = branch.transform.Find("TransitionObject");
            Assert.IsNotNull(transition, "У ответвления нет картинки перехода");
            var image = transition!.GetComponent<Image>();
            Assert.IsNotNull(image, "Картинка перехода без Image");
            Assert.IsTrue(transition.gameObject.activeInHierarchy, "Картинка перехода скрыта");
            Assert.IsNotNull(image!.sprite, "У картинки перехода нет спрайта");
            var branchRect = (RectTransform)branch.transform;
            var worldRect = branchRect.parent.GetComponent<RectTransform>();
            var laneView = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(laneView, "Нет слоя похода");
            var player = ((RectTransform)laneView!.transform).Find("Player")?.GetComponent<RectTransform>();
            Assert.IsNotNull(player, "Нет фигуры игрока");
            Assert.Greater(
                branchRect.anchoredPosition.y,
                player!.anchoredPosition.y + 100f,
                "переход поднят над горизонтальной дорогой");
            Assert.AreEqual(0f, image.rectTransform.anchoredPosition.y, 0.01f,
                "низ картинки перехода совпадает с точкой на вертикальной тропе");
            var partWidth = worldRect.rect.width / WalkMetrics.BackdropPartCount;
            var fraction = (branchRect.anchoredPosition.x / partWidth) % 1f;
            Assert.GreaterOrEqual(fraction, 0f, "переход внутри своего сегмента");
            Assert.LessOrEqual(fraction, 1f, "переход внутри своего сегмента");
            var path = branch.transform.Find("Path");
            Assert.IsTrue(path == null || !path.gameObject.activeInHierarchy, "вставленная тропа не должна показываться");
        }

        /// <summary>Полоса неба начинается ровно на верхней кромке сегментов, без щели.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_BackdropParts_MeetSkyBandWithoutGap()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            Assert.IsTrue(BackdropPartsActive(lane!), "сегменты фона не показаны");
            var laneHeight = ((RectTransform)lane!.transform).rect.height;
            var part = lane.BackdropParts[0].rectTransform;
            Assert.AreEqual(
                laneHeight * (1f - WalkMetrics.SkyBandHeight),
                part.rect.height,
                0.5f,
                "сегмент тянется вместе с экраном под полосой неба");
            var band = lane.SkyView!.transform.Find("SkyBand") as RectTransform;
            Assert.IsNotNull(band, "Нет полосы неба");
            Assert.AreEqual(
                laneHeight * (1f - WalkMetrics.SkyBandBottom),
                band!.rect.height,
                0.5f,
                "полоса неба заходит на кадр и растворяется в нём");
            Assert.Less(
                WalkMetrics.SkyBandBottom,
                1f - WalkMetrics.SkyBandHeight,
                "нижняя кромка неба перекрывает верх кадра, а не стыкуется с ним");
        }

        /// <summary>В помещении нет полосы неба, а под персонажем едет полоса пола.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Interior_BackdropFillsScreenWithoutSky()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_interior",
                "loc_site_house_interior",
                1,
                "bg_interior_room",
                LocationSubtype.Building,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            var laneHeight = ((RectTransform)lane!.transform).rect.height;
            Assert.IsTrue(BackdropPartsActive(lane), "сегменты помещения не показаны");
            var part = lane.BackdropParts[0].rectTransform;
            Assert.AreEqual(
                1f,
                part.anchorMax.y,
                0.001f,
                "кадр помещения доходит до верхней кромки, без пустой полосы неба");
            Assert.AreEqual(0f, part.offsetMax.y, 0.01f, "верх кадра без отступа под полосу неба");
            Assert.AreEqual(
                WalkMetrics.FloorStripHeight,
                part.anchorMin.y * laneHeight,
                0.5f,
                "кадр помещения начинается на кромке полосы пола");
            Assert.AreEqual(0f, part.offsetMin.y, 0.01f, "низ кадра без отступа");
            Assert.IsNotNull(lane.Road, "Нет полосы пола");
            Assert.IsTrue(lane.Road!.gameObject.activeInHierarchy, "полоса пола в помещении скрыта");
            Assert.IsNotNull(lane.Road.sprite, "у полосы пола нет текстуры");
            Assert.IsFalse(
                lane.SkyView != null && lane.SkyView.SkyDay != null &&
                lane.SkyView.SkyDay!.gameObject.activeInHierarchy,
                "полоса неба в помещении скрыта");
            Capture(play, "walk-interior-full");
        }

        /// <summary>Широкая машина стоит низом на дороге, а не висит над ней.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_WreckedCar_StandsOnRoad()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_car",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.3);
            location.MarkSpawned();
            location.MarkStashRolled();
            var car = new LootLocationObject(
                "spawn_car_wreck",
                "spawn.spawn_car_wreck.title",
                "loot_human");
            car.SetLoot(new List<ItemInstance>
            {
                new ItemInstance("test_car_item", "ammo_545", 1)
            });
            location.AddObject(car);
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(2.6f);
            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;

            WalkFigureView? wreck = null;
            foreach (var figure in Object.FindObjectsByType<WalkFigureView>(FindObjectsInactive.Include))
            {
                if (!figure.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var sprite = figure.transform.Find("Icon")?.GetComponent<Image>()?.sprite;
                if (sprite != null && sprite.name.Contains("car_wreck"))
                {
                    wreck = figure;
                    break;
                }
            }

            Assert.IsNotNull(wreck, "разбитая машина не показана");
            AssertFigureStandsOnRoad(wreck!);
            Capture(play, "walk-car-stand");
        }

        /// <summary>Объекты раскладываются по всей дороге, а не стоят в одном месте.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Objects_SpreadAcrossLane()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_spread",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            for (var index = 0; index < 3; index++)
            {
                var loot = new LootLocationObject(
                    "spawn_road_crate",
                    "spawn.spawn_road_crate.title",
                    "loot_human");
                loot.SetLoot(new List<ItemInstance>
                {
                    new ItemInstance("test_spread_item_" + index, "ammo_545", 1)
                });
                location.AddObject(loot);
            }

            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var values = new List<float>();
            foreach (var figure in Object.FindObjectsByType<WalkFigureView>(FindObjectsInactive.Include))
            {
                if (figure.gameObject.activeInHierarchy)
                {
                    values.Add(figure.Rect.anchoredPosition.x);
                    AssertFigureStandsOnRoad(figure);
                }
            }

            Assert.AreEqual(3, values.Count, "показаны не все объекты");
            var min = Mathf.Min(values[0], Mathf.Min(values[1], values[2]));
            var max = Mathf.Max(values[0], Mathf.Max(values[1], values[2]));
            Assert.Greater(max - min, 3500f, "объекты стоят в одном месте");
            Capture(play, "walk-objects-spread");
        }

        /// <summary>Забранный объект не сдвигает остальные: у каждого своё место на дороге.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_LootedObject_KeepsOtherPositions()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_positions",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            for (var index = 0; index < 3; index++)
            {
                var loot = new LootLocationObject(
                    "spawn_road_crate",
                    "spawn.spawn_road_crate.title",
                    "loot_human");
                loot.SetLoot(new List<ItemInstance>
                {
                    new ItemInstance("test_position_item_" + index, "ammo_545", 1)
                });
                location.AddObject(loot);
            }

            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var before = ActiveFigurePositions();
            Assert.AreEqual(3, before.Count, "показаны не все объекты");

            location.Objects[1].MarkLooted();
            play.SetWalkMode(true);
            yield return null;

            var after = ActiveFigurePositions();
            Assert.AreEqual(2, after.Count, "забранный объект остался на дороге");
            Assert.AreEqual(before[0], after[0], 0.5f, "левый объект не сдвинулся");
            Assert.AreEqual(before[2], after[1], 0.5f, "правый объект не сдвинулся");
            Capture(play, "walk-looted-positions");
        }

        /// <summary>Конец отрезка дороги сам загружает следующую дорогу, без выбора перехода.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_RoadEnd_ContinuesWithoutChoice()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var before = world.CurrentLocation.InstanceId;
            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(24f);
            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSeconds(0.8f);

            Assert.AreNotEqual(before, world.CurrentLocation.InstanceId, "дорога не продолжилась");
            Assert.AreEqual("Path", world.CurrentLocation.Subtype.ToString(), "новая локация должна быть дорогой");
            Assert.IsTrue(FindAnywhere("WalkLane")?.activeInHierarchy ?? false, "поход не остался на экране");
            Assert.AreEqual("WalkLane", FindAnywhere("WalkLane")!.name, "слой похода должен быть активен");
        }

        /// <summary>Ветка «вверх» входит в зону, а конец зоны возвращает на новую дорогу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_UpBranch_EntersAreaAndReturnsToRoad()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var current = world.CurrentLocation;
            var startLocation = new LocationInstance(
                "test_walk_start",
                "loc_route_forest",
                1,
                "bg_route_forest",
                LocationSubtype.Path,
                1,
                0.1);
            var branchLocation = new LocationInstance(
                "test_walk_branch",
                "loc_site_house",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            var roadLocation = new LocationInstance(
                "test_walk_road",
                current.TemplateId,
                1,
                "bg_route_forest",
                LocationSubtype.Path,
                1,
                0.1);
            world.Map.AddLocation(startLocation);
            world.Map.AddLocation(branchLocation);
            world.Map.AddLocation(roadLocation);
            world.Map.AddEdge(new WorldEdge(
                "test_walk_branch.out",
                startLocation.InstanceId,
                branchLocation.InstanceId,
                "path",
                1,
                new TravelCost(0, 0.5),
                false,
                false));
            world.Map.AddEdge(new WorldEdge(
                "test_walk_branch_road.out",
                branchLocation.InstanceId,
                roadLocation.InstanceId,
                "road",
                1,
                new TravelCost(0, 0.5),
                false,
                false));
            world.MoveTo(startLocation.InstanceId);
            play.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            var upObject = FindAnywhere("WalkUp");
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (upObject != null && upObject.activeInHierarchy)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;

            var up = FindAnywhere("WalkUp")?.GetComponent<Button>();
            Assert.IsNotNull(up, "Нет кнопки «Вверх»");
            Assert.IsTrue(up!.gameObject.activeInHierarchy, "У ветки не появилась кнопка «Вверх»");
            Capture(play, "walk-branch-prompt");
            up.onClick.Invoke();
            yield return new WaitForSeconds(0.8f);

            Assert.AreNotEqual("Path", world.CurrentLocation.Subtype.ToString(), "не вошли в зону ветки");
            Capture(play, "walk-branch-real");

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (world.CurrentLocation.Subtype == LocationSubtype.Path)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual("Path", world.CurrentLocation.Subtype.ToString(), "зона не вернула на дорогу");
        }

        /// <summary>Промышленный фон с нарисованной тропой ставит ветку в её конец.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_IndustrialBackdrop_ShowsBakedBranch()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var startLocation = new LocationInstance(
                "test_walk_highway",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            var branchLocation = new LocationInstance(
                "test_walk_industrial",
                "loc_building_industrial",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            world.Map.AddLocation(startLocation);
            world.Map.AddLocation(branchLocation);
            world.Map.AddEdge(new WorldEdge(
                "test_walk_industrial.out",
                startLocation.InstanceId,
                branchLocation.InstanceId,
                "path",
                1,
                new TravelCost(0, 0.5),
                false,
                false));
            world.MoveTo(startLocation.InstanceId);
            play.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (FindAnywhere("WalkUp")?.activeInHierarchy ?? false)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            var up = FindAnywhere("WalkUp")?.GetComponent<Button>();
            Assert.IsNotNull(up, "Нет кнопки «Вверх»");
            Assert.IsTrue(up!.gameObject.activeInHierarchy, "У промышленной ветки не появилась кнопка");
            Capture(play, "walk-branch-industrial");
        }

        /// <summary>Внутренность панельного дома использует сгенерированный фон.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_InteriorBackdrop_ShowsGeneratedRoom()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var interior = new LocationInstance(
                "test_walk_interior",
                "loc_building_block_interior",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            world.Map.AddLocation(interior);
            world.MoveTo(interior.InstanceId);
            play.SetWalkMode(true);
            yield return null;

            Assert.IsTrue(FindAnywhere("WalkLane")?.activeInHierarchy ?? false, "поход не построен");
            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            if (BackdropPartsActive(lane!))
            {
                foreach (var part in lane.BackdropParts)
                {
                    StringAssert.Contains("interior", part.sprite!.name, "внутри части фона — помещение");
                }
            }
            else
            {
                StringAssert.Contains("interior", lane.Road!.sprite!.name, "внутри должен быть пол помещения");
            }
            Assert.IsNotNull(lane.SkyView, "нет слоя неба");
            Assert.IsFalse(lane.SkyView!.Sun!.gameObject.activeInHierarchy, "внутри помещения солнца нет");
            Assert.IsFalse(lane.SkyView.Moon!.gameObject.activeInHierarchy, "внутри помещения луны нет");
            Assert.IsFalse(lane.SkyView.SkyDay!.gameObject.activeInHierarchy, "внутри помещения нет полосы неба");
            Capture(play, "walk-interior-real");
        }

        /// <summary>Внутренность заканчивается и возвращает на новую дорогу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_InteriorEnd_ContinuesToRoad()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var interior = new LocationInstance(
                "test_walk_interior_end",
                "loc_building_block_interior",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            var next = new LocationInstance(
                "test_walk_interior_next",
                "loc_route_forest",
                1,
                "bg_route_forest",
                LocationSubtype.Path,
                1,
                0.2);
            world.Map.AddLocation(interior);
            world.Map.AddLocation(next);
            world.Map.AddEdge(new WorldEdge(
                "test_walk_interior_end.out",
                interior.InstanceId,
                next.InstanceId,
                "corridor",
                1,
                new TravelCost(0, 0.1),
                false,
                false));
            world.MoveTo(interior.InstanceId);
            screen.ShowGame(false);
            yield return null;
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 110; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (!string.Equals(world.CurrentLocationId, interior.InstanceId, System.StringComparison.Ordinal))
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return new WaitForSeconds(1.5f);
            Assert.AreNotEqual(interior.InstanceId, world.CurrentLocationId, "внутренность не закончилась");
            Assert.AreEqual("loc_route_forest", world.CurrentLocation.TemplateId, "после внутренности не дорога");
            Capture(play, "walk-interior-end");
        }

        /// <summary>Осмотр лута открывает награду и не сдвигает персонажа по дороге.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_LootObject_KeepsPositionAfterReward()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_walk_loot",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            var loot = new LootLocationObject(
                "spawn_road_crate",
                "spawn.spawn_road_crate.title",
                "loot_human");
            loot.SetLoot(new List<ItemInstance>
            {
                new ItemInstance("test_walk_loot_item", "ammo_545", 1)
            });
            location.AddObject(loot);
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            var interact = FindAnywhere("WalkInteract");
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 32; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (interact != null && interact.activeInHierarchy)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.IsTrue(interact != null && interact.activeInHierarchy, "у объекта не появилось действие");
            var progress = FindAnywhere("WalkProgressLabel")?.GetComponent<Text>();
            Assert.IsNotNull(progress, "Нет подписи прогресса");
            var before = progress!.text;

            interact!.GetComponent<Button>()!.onClick.Invoke();
            yield return null;
            var rewards = GameObject.Find("Rewards");
            Assert.IsNotNull(rewards, "Экран награды не открылся");
            Assert.IsTrue(rewards!.activeSelf, "Экран награды скрыт");
            Click("RewardClose");
            yield return null;
            Assert.AreEqual(before, progress.text, "персонаж сдвинулся после обыска");
            Assert.AreEqual(location.InstanceId, world.CurrentLocationId, "локация сменилась после обыска");
        }

        /// <summary>Аномалия из walk-режима открывает радар и возвращает на ту же дорогу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Anomaly_OpensRadarAndKeepsPosition()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_walk_anomaly",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            location.AddObject(new AnomalyLocationObject(
                "spawn_anomaly_chem",
                "spawn.spawn_anomaly_chem.title",
                "anomaly_chem"));
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            var interact = FindAnywhere("WalkInteract");
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 32; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (interact != null && interact.activeInHierarchy)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.IsTrue(interact != null && interact.activeInHierarchy, "у аномалии не появилось действие");
            var progress = FindAnywhere("WalkProgressLabel")?.GetComponent<Text>();
            Assert.IsNotNull(progress, "Нет подписи прогресса");
            var before = progress!.text;

            interact!.GetComponent<Button>()!.onClick.Invoke();
            yield return null;
            var radar = FindAnywhere("RadarMinigame");
            Assert.IsNotNull(radar, "Радар не открылся");
            Assert.IsTrue(radar!.activeInHierarchy, "Радар скрыт");
            Click("RadarLeave");
            yield return null;
            Assert.AreEqual(before, progress.text, "персонаж сдвинулся после аномалии");
            Assert.AreEqual(location.InstanceId, world.CurrentLocationId, "локация сменилась после аномалии");
        }

        /// <summary>Противник из walk-режима открывает бой.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Enemy_OpensCombat()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_walk_enemy",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            location.AddObject(new EnemyLocationObject(
                "spawn_enemy_bandit",
                "spawn.spawn_enemy_bandit.title",
                "enemy_bandit"));
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            var interact = FindAnywhere("WalkInteract");
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 32; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (interact != null && interact.activeInHierarchy)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.IsTrue(interact != null && interact.activeInHierarchy, "у противника не появилось действие");
            interact!.GetComponent<Button>()!.onClick.Invoke();
            yield return null;
            Assert.IsTrue(screen.Session!.InCombat, "бой не начался");
            Assert.IsNotNull(GameObject.Find("Combat"), "боевой экран не открылся");
        }

        /// <summary>NPC из walk-режима открывает диалог.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Npc_OpensDialog()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_walk_npc",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            location.AddObject(new NpcLocationObject(
                "spawn_atp_shustry",
                "spawn.atp.shustry.title",
                "npc_atp_shustry"));
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            var interact = FindAnywhere("WalkInteract");
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 32; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (interact != null && interact.activeInHierarchy)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.IsTrue(interact != null && interact.activeInHierarchy, "у NPC не появилось действие");
            interact!.GetComponent<Button>()!.onClick.Invoke();
            yield return null;
            Assert.IsNotNull(GameObject.Find("NpcDialog"), "Диалог NPC не открылся");
        }

        /// <summary>Тайник из walk-режима открывает взлом.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Stash_OpensLock()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var location = new LocationInstance(
                "test_walk_stash",
                "loc_route_highway",
                1,
                "bg_route_highway",
                LocationSubtype.Path,
                1,
                0.1);
            location.MarkSpawned();
            location.MarkStashRolled();
            location.MarkStashPresent();
            world.Map.AddLocation(location);
            world.MoveTo(location.InstanceId);
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            var interact = FindAnywhere("WalkInteract");
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 32; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (interact != null && interact.activeInHierarchy)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.IsTrue(interact != null && interact.activeInHierarchy, "у тайника не появилось действие");
            interact!.GetComponent<Button>()!.onClick.Invoke();
            yield return null;
            Assert.IsNotNull(FindAnywhere("LockMinigame"), "Взлом тайника не открылся");
        }

        /// <summary>Кнопка бега двигает по дороге примерно в два раза быстрее.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_RunButton_MovesFaster()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            var run = FindAnywhere("WalkRun")?.GetComponent<Button>();
            var progress = FindAnywhere("WalkProgressLabel")?.GetComponent<Text>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            Assert.IsNotNull(run, "Нет кнопки «Бег»");
            Assert.IsNotNull(progress, "Нет подписи пути");
            var pointer = new PointerEventData(EventSystem.current);

            var before = progress!.text;
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(1f);
            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            var normal = progress.text;
            Assert.AreNotEqual(before, normal, "обычная ходьба не двигает путь");

            ExecuteEvents.Execute(run!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            yield return new WaitForSeconds(1f);
            ExecuteEvents.Execute(run.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            Assert.AreNotEqual(normal, progress.text, "бег не двигает путь");
        }

        /// <summary>Каждая не-хабовая локация строит дорогу, фон и свою дорогу-полосу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_AllLocations_BuildLane()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;
            var play = Object.FindAnyObjectByType<PlayScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(play, "Нет игрового экрана");
            var world = screen.Session!.State.World;
            var failures = new List<string>();
            var checkedCount = 0;

            foreach (var template in screen.Locations.All)
            {
                if (template.Subtype == LocationSubtype.Hub)
                {
                    continue;
                }

                var location = new LocationInstance(
                    "matrix_" + template.Id,
                    template.Id,
                    1,
                    "bg_route_highway",
                    template.Subtype,
                    1,
                    0.1);
                location.MarkSpawned();
                location.MarkStashRolled();
                world.Map.AddLocation(location);
                world.MoveTo(location.InstanceId);
                play.SetWalkMode(true);
                yield return null;

                var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
                if (lane == null || !lane.gameObject.activeInHierarchy)
                {
                    failures.Add(template.Id + ": дорога не появилась");
                    continue;
                }

                if (template.WalkParts.Count == WalkMetrics.BackdropPartCount)
                {
                    var road = lane.Road;
                    if (!template.Interior && road != null && road.gameObject.activeInHierarchy)
                    {
                        failures.Add(template.Id + ": отдельная полоса дороги не скрыта");
                    }

                    if (template.Interior &&
                        (road == null || road.sprite == null || !road.gameObject.activeInHierarchy))
                    {
                        failures.Add(template.Id + ": в помещении нет полосы пола");
                    }

                    foreach (var partId in template.WalkParts)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(partId);
                        var found = false;
                        foreach (var part in lane.BackdropParts)
                        {
                            found |= part != null && part.sprite != null &&
                                     part.sprite.name.Contains(fileName);
                        }

                        if (!found)
                        {
                            failures.Add(template.Id + ": нет части фона " + partId);
                        }
                    }
                }
                else
                {
                    if (lane.Road == null || lane.Road.sprite == null)
                    {
                        failures.Add(template.Id + ": нет спрайта дороги");
                    }

                    if (!string.IsNullOrWhiteSpace(template.WalkBackdrop) &&
                        (lane.Backdrop == null || lane.Backdrop.sprite == null))
                    {
                        failures.Add(template.Id + ": нет фона похода");
                    }
                    else if (!string.IsNullOrWhiteSpace(template.WalkBackdrop) &&
                             !lane.Backdrop!.sprite!.name.Contains(
                                 Path.GetFileNameWithoutExtension(template.WalkBackdrop)))
                    {
                        failures.Add(template.Id + ": фон не совпадает с walkBackdrop");
                    }
                }

                checkedCount++;
                Capture(play, "walk-p1-" + template.Id);
            }

            Assert.AreEqual(
                screen.Locations.All.Count - 2,
                checkedCount,
                "проверены все не-хабовые локации: " + string.Join("; ", failures));
            Assert.IsEmpty(failures, string.Join("; ", failures));
        }

        /// <summary>Из разрушенного панельного дома есть переход внутрь.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_BuildingBlock_EntersInterior()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var startLocation = new LocationInstance(
                "test_walk_block",
                "loc_building_block",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            var interior = new LocationInstance(
                "test_walk_block_inside",
                "loc_building_block_interior",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            world.Map.AddLocation(startLocation);
            world.Map.AddLocation(interior);
            world.Map.AddEdge(new WorldEdge(
                "test_walk_block_inside.out",
                startLocation.InstanceId,
                interior.InstanceId,
                "corridor",
                1,
                new TravelCost(0, 0.1),
                false,
                false));
            world.MoveTo(startLocation.InstanceId);
            play.SetWalkMode(true);
            yield return null;

            var forward = FindAnywhere("WalkForward")?.GetComponent<Button>();
            Assert.IsNotNull(forward, "Нет кнопки «Идти»");
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(forward!.gameObject, pointer, ExecuteEvents.pointerDownHandler);
            for (var i = 0; i < 60; i++)
            {
                yield return new WaitForSeconds(0.25f);
                if (FindAnywhere("WalkUp")?.activeInHierarchy ?? false)
                {
                    break;
                }
            }

            ExecuteEvents.Execute(forward.gameObject, pointer, ExecuteEvents.pointerUpHandler);
            yield return null;
            var up = FindAnywhere("WalkUp")?.GetComponent<Button>();
            Assert.IsNotNull(up, "Нет кнопки «Вверх»");
            Assert.IsTrue(up!.gameObject.activeInHierarchy, "У двери нет перехода внутрь");
            up.onClick.Invoke();
            yield return new WaitForSeconds(0.8f);

            Assert.AreEqual("loc_building_block_interior", world.CurrentLocation.TemplateId, "не вошли внутрь");
            Capture(play, "walk-building-interior");
        }

        /// <summary>Заброшенный дом получает лесной фон и грунтовую дорогу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_HouseBackdrop_UsesForestAndDirtRoad()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var house = new LocationInstance(
                "test_walk_house",
                "loc_site_house",
                1,
                "bg_site_house",
                LocationSubtype.Building,
                1,
                0.2);
            world.Map.AddLocation(house);
            world.MoveTo(house.InstanceId);
            play.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            if (BackdropPartsActive(lane!))
            {
                var housePart = false;
                foreach (var part in lane.BackdropParts)
                {
                    housePart |= part.sprite!.name.Contains("house");
                }

                Assert.IsTrue(housePart, "под домом должна быть часть фона с домом");
            }
            else
            {
                Assert.IsNotNull(lane.Road?.sprite, "Нет дороги");
                StringAssert.Contains("dirt", lane.Road!.sprite!.name, "под домом должна быть грунтовка");
            }
            Capture(play, "walk-house-real");
        }

        /// <summary>У фигуры игрока есть кадры ходьбы, бега и покоя из префаба.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Player_HasWalkRunAndIdleFrames()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            var player = lane!.PlayerView;
            Assert.IsNotNull(player, "У игрока нет покадровой анимации");
            Assert.AreEqual(8, player!.WalkFrames.Length, "кадров ходьбы");
            Assert.AreEqual(8, player.RunFrames.Length, "кадров бега");
            Assert.IsNotNull(player.IdleFrame, "нет кадра покоя");
            Assert.AreSame(player.IdleFrame, player.Current, "в покое должен стоять кадр покоя");

            player.SetState(true, false, false);
            player.Advance(0.2f);
            CollectionAssert.Contains(player.WalkFrames, player.Current, "на ходу не кадр ходьбы");
            Capture(play, "walk-player-walk");

            player.SetState(true, true, false);
            player.Advance(0.2f);
            CollectionAssert.Contains(player.RunFrames, player.Current, "в беге не кадр бега");
            Capture(play, "walk-player-run");

            player.Reset();
            Assert.AreSame(player.IdleFrame, player.Current, "сброс не вернул кадр покоя");
        }

        /// <summary>Анимация меняет кадры, ставит бег, разворачивает назад и гаснет в покое.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Player_SwitchesFramesByState()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            var player = lane!.PlayerView;
            Assert.IsNotNull(player, "У игрока нет покадровой анимации");

            player!.SetState(true, false, false);
            var first = player.Current;
            Assert.IsTrue(player.Moving, "игрок не пошёл");
            Assert.AreEqual(0, player.FrameIndex, "первый кадр ходьбы");

            player.Advance(0.1f);
            Assert.AreNotSame(first, player.Current, "кадр ходьбы не сменился");

            player.SetState(true, true, false);
            Assert.IsTrue(player.Running, "игрок не побежал");
            player.Advance(0.05f);
            CollectionAssert.Contains(player.RunFrames, player.Current, "на бегу не кадр бега");

            player.SetState(true, false, true);
            Assert.IsTrue(player.Backward, "шаг назад не включился");
            Assert.Less(player.transform.localScale.x, 0f, "назад игрок должен смотреть влево");
            player.Advance(0.1f);
            CollectionAssert.Contains(player.WalkFrames, player.Current, "назад не кадр ходьбы");

            player.SetState(false, false, false);
            Assert.IsFalse(player.Moving, "игрок не остановился");
            Assert.AreSame(player.IdleFrame, player.Current, "после остановки должен быть покой");
            Assert.Greater(player.transform.localScale.x, 0f, "после остановки игрок смотрит вправо");
            yield return null;
        }

        /// <summary>Днём над дорогой светит солнце из спрайта, луны нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Sky_ShowsSunWithArt()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            var sky = lane!.SkyView;
            Assert.IsNotNull(sky, "Нет слоя неба");
            Assert.IsNotNull(sky!.Sun, "Нет солнца");
            Assert.IsNotNull(sky.Moon, "Нет луны");
            Assert.IsNotNull(sky.Sun!.sprite, "У солнца нет картинки");
            Assert.IsNotNull(sky.Moon!.sprite, "У луны нет картинки");
            StringAssert.Contains("sky_sun", sky.Sun.sprite!.name);
            StringAssert.Contains("sky_moon", sky.Moon.sprite!.name);
            Assert.IsNotNull(sky.SkyDay, "Нет дневной полосы неба");
            Assert.IsNotNull(sky.SkyNight, "Нет ночной полосы неба");
            Assert.IsTrue(sky.SkyDay!.gameObject.activeInHierarchy, "днём видна полоса неба");
            Assert.IsFalse(sky.SkyNight!.gameObject.activeInHierarchy, "днём ночных облаков нет");
            Assert.IsFalse(sky.State.Night, "старт должен быть днём");
            Assert.IsTrue(sky.Sun.gameObject.activeInHierarchy, "днём светит солнце");
            Assert.IsFalse(sky.Moon!.gameObject.activeInHierarchy, "днём луны нет");
            var expectedY = (1f - WalkMetrics.SkyBandHeight) +
                            (WalkMetrics.SkyBandHeight *
                             Mathf.Lerp(WalkMetrics.SkySunLow, WalkMetrics.SkySunHigh, sky.State.Elevation));
            Assert.AreEqual(expectedY, sky.Sun.rectTransform.anchorMin.y, 0.001f, "солнце внутри полосы неба");
            Assert.Greater(sky.Sun.rectTransform.anchorMin.y, 1f - WalkMetrics.SkyBandHeight, "солнце не ниже полосы");
            var backdrop = FindAnywhere("Backdrop")?.GetComponent<RectTransform>();
            Assert.IsNotNull(backdrop, "Нет фона локации");
            Assert.AreEqual(
                1f - WalkMetrics.SkyBandHeight,
                backdrop!.anchorMax.y,
                0.001f,
                "кадр сжат по вертикали и начинается под полосой неба");
            Capture(play, "walk-sky-day");
        }

        /// <summary>Светило ходит по дуге, ночью светит луна, внутри помещения светил нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Sky_LuminaryMovesAndHidesIndoors()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            play!.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            var sky = lane!.SkyView;
            Assert.IsNotNull(sky, "Нет слоя неба");

            var light = new SkyColor(1f, 0.9f, 0.8f);
            var tint = new SkyColor(0.9f, 0.5f, 0.2f);
            sky!.Show(new SkyState(false, 1f, 1f, light, tint, 0.3f, 0.8f), false);
            Assert.AreEqual(0.9f, sky.Sun!.rectTransform.anchorMin.x, 0.01f, "закат справа");
            Assert.AreEqual(
                1f - WalkMetrics.SkyBandHeight + (WalkMetrics.SkyBandHeight * WalkMetrics.SkySunHigh),
                sky.Sun.rectTransform.anchorMin.y,
                0.01f,
                "зенит у верхней границы полосы");

            sky.Show(new SkyState(false, 0f, 0f, light, tint, 0.3f, 0.8f), false);
            Assert.AreEqual(0.1f, sky.Sun.rectTransform.anchorMin.x, 0.01f, "восход слева");
            Assert.AreEqual(
                1f - WalkMetrics.SkyBandHeight + (WalkMetrics.SkyBandHeight * WalkMetrics.SkySunLow),
                sky.Sun.rectTransform.anchorMin.y,
                0.01f,
                "восход у нижней границы полосы");

            var night = new SkyState(true, 0.5f, 0.25f, light, new SkyColor(0.05f, 0.09f, 0.2f), 0.55f, 0.9f);
            sky.Show(night, false);
            Assert.IsTrue(sky.Moon!.gameObject.activeSelf, "ночью светит луна");
            Assert.IsFalse(sky.Sun.gameObject.activeSelf, "ночью солнца нет");
            Assert.AreEqual(0.55f, sky.Tint!.color.a, 0.01f, "ночной оттенок");

            sky.Show(night, true);
            Assert.IsFalse(sky.Sun.gameObject.activeSelf, "внутри солнца нет");
            Assert.IsFalse(sky.Moon.gameObject.activeSelf, "внутри луны нет");
            Assert.AreEqual(0.55f * WalkMetrics.IndoorTintFactor, sky.Tint.color.a, 0.01f, "внутри оттенок слабее");
            yield return null;
        }

        /// <summary>Вечером и ночью небо переключается на луну по часам мира.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator WalkMode_Sky_NightShowsMoon()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var play = Object.FindAnyObjectByType<PlayScreen>();
            Assert.IsNotNull(play, "Нет игрового экрана");
            var clock = FindScreen().Session!.State.World.Clock;
            clock.Advance(9.5);
            play!.SetWalkMode(false);
            yield return null;
            play.SetWalkMode(true);
            yield return null;

            var lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода");
            var sky = lane!.SkyView;
            Assert.IsNotNull(sky, "Нет слоя неба");
            Assert.IsTrue(sky!.Sun!.gameObject.activeInHierarchy, "на закате светит солнце");
            Assert.Less(sky.Tint!.color.a, 0.15f, "к закату сцена почти не красится — красное только небо");
            Capture(play, "walk-sky-dusk");

            clock.Advance(2.5);
            play.SetWalkMode(false);
            yield return null;
            play.SetWalkMode(true);
            yield return null;

            lane = FindAnywhere("WalkLane")?.GetComponent<WalkLaneView>();
            Assert.IsNotNull(lane, "Нет слоя похода после заката");
            sky = lane!.SkyView;
            Assert.IsNotNull(sky, "Нет слоя неба после заката");
            Assert.IsTrue(sky!.State.Night, "после 22:00 должна быть ночь");
            Assert.IsTrue(sky.Moon!.gameObject.activeInHierarchy, "ночью светит луна");
            Assert.IsFalse(sky.Sun!.gameObject.activeInHierarchy, "ночью солнца нет");
            Assert.IsTrue(sky.SkyNight!.gameObject.activeInHierarchy, "ночью видны ночные облака");
            Assert.Greater(sky.NightSky, 0.5f, "ночная полоса проявилась");
            Assert.Greater(sky.Tint!.color.a, 0.15f, "ночной оттенок мягкий, но заметный");
            Capture(play, "walk-sky-night");
        }

        /// <summary>Показаны ли три переставляемые части фона вместо одиночного кадра.</summary>
        /// <param name="lane">Слой похода.</param>
        /// <returns><c>true</c> — все три части активны и с картинками.</returns>
        private static bool BackdropPartsActive(WalkLaneView lane)
        {
            if (lane.BackdropParts.Count != WalkMetrics.BackdropPartCount)
            {
                return false;
            }

            foreach (var part in lane.BackdropParts)
            {
                if (part == null || !part.gameObject.activeInHierarchy || part.sprite == null)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Позиции X всех активных фигур на дороге, по возрастанию.</summary>
        /// <returns>Список X в единицах канваса.</returns>
        private static List<float> ActiveFigurePositions()
        {
            var values = new List<float>();
            foreach (var figure in Object.FindObjectsByType<WalkFigureView>(FindObjectsInactive.Include))
            {
                if (figure.gameObject.activeInHierarchy)
                {
                    values.Add(figure.Rect.anchoredPosition.x);
                }
            }

            values.Sort();
            return values;
        }

        /// <summary>Проверяет, что картинка объекта стоит низом на линии земли и не растянута.</summary>
        /// <param name="figure">Фигура на дороге.</param>
        private static void AssertFigureStandsOnRoad(WalkFigureView figure)
        {
            var iconTransform = figure.transform.Find("Icon");
            Assert.IsNotNull(iconTransform, "У фигуры нет картинки");
            var icon = (RectTransform)iconTransform;
            Assert.AreEqual(0f, icon.anchoredPosition.y, 0.01f, "низ картинки объекта стоит на линии земли");
            var image = iconTransform.GetComponent<Image>();
            Assert.IsNotNull(image, "Картинка фигуры без Image");
            if (image!.sprite != null)
            {
                Assert.AreEqual(
                    image.sprite.rect.width / image.sprite.rect.height,
                    icon.rect.width / icon.rect.height,
                    0.02f,
                    "пропорции картинки сохранены");
            }

            var plate = figure.transform.Find("LabelPlate") as RectTransform;
            Assert.IsNotNull(plate, "У фигуры нет плашки подписи");
            Assert.AreEqual(
                -WalkMetrics.FigureLabelHeight,
                plate!.anchoredPosition.y,
                0.01f,
                "плашка подписи лежит под линией земли");
        }

        private static void Capture(PlayScreen play, string name)
        {
            Directory.CreateDirectory("Logs/Screenshots");
            var camera = Camera.main;
            var canvas = play.GetComponentInParent<Canvas>();
            if (camera == null || canvas == null)
            {
                return;
            }

            var mode = canvas.renderMode;
            var target = camera.targetTexture;
            var mask = camera.cullingMask;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 5f;
            camera.cullingMask = ~0;

            var texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            texture.Create();
            camera.targetTexture = texture;
            camera.Render();
            RenderTexture.active = texture;
            var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine("Logs/Screenshots", name + ".png"), image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = target;
            camera.cullingMask = mask;
            canvas.renderMode = mode;
            UnityEngine.Object.DestroyImmediate(image);
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}

