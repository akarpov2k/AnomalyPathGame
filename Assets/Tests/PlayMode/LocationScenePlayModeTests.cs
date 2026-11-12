#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Сцена локации: небо по фазе суток, дорога и объекты-переходы по сторонам дороги.
    /// Проверяются раскладка мест, подсветка при наведении и отсутствие кнопок переходов.
    /// </summary>
    public sealed class LocationScenePlayModeTests : PlayModeTestBase
    {
        /// <summary>Один переход без ветвей стоит по центру дороги.</summary>
        [Test]
        public void Slots_SingleObject_StaysInCenter()
        {
            var layout = PlaySceneSlots.Layout(1, -1);

            CollectionAssert.AreEqual(new[] { PlaySceneSlot.Center }, layout);
        }

        /// <summary>
        /// Одиночный переход стоит низом на точке схода дороги: по центру по горизонтали и
        /// выше неё картинка не висит.
        /// </summary>
        [Test]
        public void Slots_CenterRect_StandsOnRoadHorizon()
        {
            var rect = PlaySceneSlots.CenterRect(new Vector2(500f, 400f), new Vector2(1000f, 800f), 0.16f, 0.2f);

            Assert.AreEqual(500f, rect.center.x, 1e-3, "переход по центру точки схода");
            Assert.AreEqual(400f, rect.yMin, 1e-3, "низ перехода — на линии горизонта");
            Assert.AreEqual(160f, rect.width, 1e-3, "ширина — долей сцены");
            Assert.AreEqual(160f, rect.height, 1e-3, "высота — долей сцены");
        }

        /// <summary>Два объекта стоят по разные стороны: справа ниже центра и слева выше.</summary>
        [Test]
        public void Slots_TwoObjects_OppositeSidesAndVerticalShift()
        {
            var layout = PlaySceneSlots.Layout(2, -1);

            CollectionAssert.AreEqual(
                new[] { PlaySceneSlot.RightLow, PlaySceneSlot.LeftHigh },
                layout);
        }

        /// <summary>При двух переходах продолжение пути стоит по центру, ответвление — справа.</summary>
        [Test]
        public void Slots_TwoObjectsWithForward_ForwardInCenter()
        {
            var layout = PlaySceneSlots.Layout(2, 0);

            CollectionAssert.AreEqual(
                new[] { PlaySceneSlot.Center, PlaySceneSlot.RightLow },
                layout);
        }

        /// <summary>Номер продолжения пути не важен: центр достаётся ему, ответвление — стороне.</summary>
        [Test]
        public void Slots_TwoObjectsWithSecondForward_ForwardInCenter()
        {
            var layout = PlaySceneSlots.Layout(2, 1);

            CollectionAssert.AreEqual(
                new[] { PlaySceneSlot.RightLow, PlaySceneSlot.Center },
                layout);
        }

        /// <summary>Продолжение пути занимает центр, ветки — стороны.</summary>
        [Test]
        public void Slots_ThreeWithForward_CenterHoldsForward()
        {
            var layout = PlaySceneSlots.Layout(3, 2);

            Assert.AreEqual(PlaySceneSlot.Center, layout[2], "продолжение пути — по центру");
            Assert.AreEqual(PlaySceneSlot.RightLow, layout[0], "первая ветка — справа ниже");
            Assert.AreEqual(PlaySceneSlot.LeftHigh, layout[1], "вторая ветка — слева выше");
        }

        /// <summary>Три ветки без продолжения пути: третья встаёт по центру.</summary>
        [Test]
        public void Slots_ThreeWithoutForward_ThirdGoesToCenter()
        {
            var layout = PlaySceneSlots.Layout(3, -1);

            CollectionAssert.AreEqual(
                new[] { PlaySceneSlot.RightLow, PlaySceneSlot.LeftHigh, PlaySceneSlot.Center },
                layout);
        }

        /// <summary>Небо — полоса сверху, под ней текстура дороги текущей локации.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Scene_OnPath_HasSkyBandAndRoadTexture()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            AssertSpriteOf("SceneSky", PlaySceneArt.SkyDay, "небо дня");
            var sky = GameObject.Find("SceneSky");
            Assert.IsNotNull(sky, "Нет полосы неба");
            var rect = (RectTransform)sky!.transform;
            Assert.AreEqual(1f - PlayMetrics.SkyHeight, rect.anchorMin.y, 1e-4, "Небо занимает верхнюю полосу");

            var screen = FindScreen();
            var world = screen.Session!.State.World;
            var road = screen.Locations.Get(world.CurrentLocation.TemplateId).RoadArt ?? PlaySceneArt.DefaultRoad;
            AssertSpriteOf("SceneRoad", road, "дорога локации");
        }

        /// <summary>С наступлением ночи небо сцены меняется на ночное.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Scene_Night_SwitchesToNightSky()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;
            var screen = FindScreen();
            var world = screen.Session!.State.World;

            world.Clock.Advance(12);
            screen.ShowGame(false);
            yield return null;

            Assert.AreEqual(TimePhase.Night, world.Clock.Phase, "часы должны показывать ночь");
            AssertSpriteOf("SceneSky", PlaySceneArt.SkyNight, "ночное небо");
        }

        /// <summary>На дороге переходы — объекты сцены, а не кнопки панели действий.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Transitions_OnPath_AreSceneObjects()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var transition = FindAnyTransition();
            Assert.IsNotNull(transition, "Нет объектов-переходов по маршруту");
            Assert.IsNotNull(transition!.GetComponent<Image>()!.sprite, "У перехода нет картинки");
            Assert.IsNull(GameObject.Find("Travel_"), "Переходы не должны дублироваться кнопками действий");
            Assert.IsNotNull(transition.GetComponent<HoverHighlight>(), "Нет подсветки объекта");
        }

        /// <summary>Переходы стоят в верхней половине: продолжение по центру, ветки по сторонам.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Transitions_PlacedInUpperHalf()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var half = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Assert.IsNotNull(FindAnyTransition(), "Нет объектов-переходов");

            var center = TransitionCenter("Center");
            if (center != null)
            {
                Assert.AreEqual(half.x, center.Value.x, 2f, "Переход по центру должен стоять по центру экрана");
                Assert.Greater(center.Value.y, half.y, "Переход по центру — в верхней половине");
            }

            var right = TransitionCenter("Right");
            if (right != null)
            {
                Assert.Greater(right.Value.x, half.x, "Правая ветка должна быть справа от центра");
                Assert.Greater(right.Value.y, half.y, "Правая ветка — в верхней половине");
            }

            var left = TransitionCenter("Left");
            if (left != null)
            {
                Assert.Less(left.Value.x, half.x, "Левая ветка должна быть слева от центра");
                Assert.Greater(left.Value.y, half.y, "Левая ветка — в верхней половине");
            }
        }

        /// <summary>При наведении объект подсвечивается, а после ухода подсветка снимается.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Transition_Hover_ShowsAndHidesGlow()
        {
            yield return StartGame();
            Click("LeaveHub");
            yield return null;

            var button = FindAnyTransition();
            Assert.IsNotNull(button, "Нет объекта-перехода");
            var highlight = button!.GetComponent<HoverHighlight>();
            Assert.IsNotNull(highlight, "У перехода нет подсветки");
            var glow = FindTransitionGlow(button);
            Assert.IsNotNull(glow, "У перехода нет свечения");
            Assert.IsFalse(glow!.activeSelf, "Свечение не должно гореть без наведения");

            var data = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerEnterHandler);
            Assert.IsTrue(highlight!.IsHighlighted, "Наведение не включает подсветку");
            Assert.IsTrue(glow.activeSelf, "Наведение не включает свечение");

            ExecuteEvents.Execute(button.gameObject, data, ExecuteEvents.pointerExitHandler);
            Assert.IsFalse(highlight.IsHighlighted, "Уход мыши не снимает подсветку");
            Assert.IsFalse(glow.activeSelf, "Уход мыши не гасит свечение");
        }

        /// <summary>В хабе объектов-переходов нет: маршрут строится действием «Покинуть хаб».</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Transitions_InHub_AreHidden()
        {
            yield return StartGame();

            Assert.IsNull(FindTransition("Right"), "В хабе нет перехода справа");
            Assert.IsNull(FindTransition("Left"), "В хабе нет перехода слева");
            Assert.IsNull(FindTransition("Center"), "В хабе нет перехода по центру");
        }

        /// <summary>На экране нет названий локаций: ни текущей, ни целей переходов.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Screen_HasNoLocationNames()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen.Session!.State.World;

            AssertNoLocationNames(screen, world);

            Click("LeaveHub");
            yield return null;
            AssertNoLocationNames(screen, world);
        }

        private static void AssertNoLocationNames(GameScreen screen, WorldState world)
        {
            var names = new List<string>
            {
                screen.Texts.Get(screen.Locations.Get(world.CurrentLocation.TemplateId).TitleKey)
            };
            foreach (var edge in screen.Sessions.AvailableEdges(screen.Session!))
            {
                var target = world.Map.GetLocation(edge.ToId);
                names.Add(screen.Texts.Get(screen.Locations.Get(target.TemplateId).TitleKey));
            }

            var panel = GamePanel();
            Assert.IsNotNull(panel, "Нет игровой панели");
            foreach (var label in panel!.GetComponentsInChildren<Text>(true))
            {
                foreach (var name in names)
                {
                    if (string.IsNullOrEmpty(name))
                    {
                        continue;
                    }

                    Assert.IsFalse(
                        label.text.Contains(name, StringComparison.Ordinal),
                        $"На экране название локации «{name}»: «{label.text}»");
                }
            }

            var scene = GameObject.Find("LocationScene");
            if (scene != null)
            {
                Assert.IsNull(
                    FindByPrefix(scene.transform, "TransitionLabel"),
                    "У переходов осталась подпись с названием");
            }
        }

        private static void AssertSpriteOf(string objectName, string id, string what)
        {
            var go = GameObject.Find(objectName);
            Assert.IsNotNull(go, $"Нет объекта «{objectName}»");
            var image = go!.GetComponent<Image>();
            Assert.IsNotNull(image, $"У «{objectName}» нет Image");
            Assert.IsNotNull(image!.sprite, $"Нет спрайта: {what}");
            Assert.AreEqual(id, image.sprite!.name, $"Не тот спрайт: {what}");
        }

        private static Vector2? TransitionCenter(string slot)
        {
            var go = GameObject.Find("Transition_" + slot);
            return go == null ? null : ScreenCenter((RectTransform)go.transform);
        }
    }
}
