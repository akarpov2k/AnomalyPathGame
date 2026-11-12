#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Плашки иконочных кнопок: у кнопки, которая раньше была только иконкой, появляется
    /// металлическая плашка <c>ui_plate_normal</c> позади иконки и подсвечивается при наведении.
    /// </summary>
    public sealed class IconButtonPlatePlayModeTests : PlayModeTestBase
    {
        private const string PlateSpriteName = "ui_plate_normal";
        private const string TraderId = "trd_hub_smuggler";

        /// <summary>Значки игрового экрана стоят на плашках, иконка остаётся сверху.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Plate_PlayScreenIcons_PlacedBehindArt()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.Sessions.LeaveHub(screen.Session!);
            screen.ShowGame(false);
            yield return null;
            Assert.IsNotNull(GameObject.Find("InGame"), "Экран локации не открылся");

            foreach (var name in new[] { "Gear", "Rest", "Backpack" })
            {
                AssertPlateBehind(name);
            }
        }

        /// <summary>Крестик карты мира стоит на плашке с уменьшенным для мелкой кнопки кантом.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Plate_MapClose_HasPlate()
        {
            yield return StartGame();

            Click("Map");
            yield return null;

            AssertPlateBehind("Close");
        }

        /// <summary>Крестики доски объявлений и журнала заданий стоят на плашках.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Plate_QuestCloses_HavePlates()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.ShowQuests(screen.Session!.State.World.CurrentLocation.InstanceId);
            yield return null;
            AssertPlateBehind("BoardClose");

            Click("BoardClose");
            yield return null;
            screen.ShowQuests(null);
            yield return null;
            AssertPlateBehind("JournalClose");
        }

        /// <summary>
        /// Объекты на стене хаба (карта, дверь, доска) — вырезки без металлических рамок:
        /// плашка вокруг них читалась как лишний квадрат, а при наведении давала рамку-подсветку.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_WallObjects_HaveNoPlateFrames()
        {
            yield return StartGame();
            Assert.IsNotNull(GameObject.Find("BigHub"), "Большой хаб не открылся");

            foreach (var name in new[] { "Map", "QuestBoard", "LeaveHub" })
            {
                AssertNoPlate(name);
            }

            Click("Traders");
            yield return null;
            Click("TraderCard_" + TraderId);
            yield return null;

            AssertNoPlate("LeaveHub");
        }

        /// <summary>При наведении на объект стены подменяется только его картинка, без рамки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_OnHover_SwapsObjectArtToGlow()
        {
            yield return StartGame();

            var button = GameObject.Find("Map")!;
            var art = button.transform.Find("Object");
            Assert.IsNotNull(art, "У объекта «Map» нет картинки Object");
            var image = art!.GetComponent<Image>();
            Assert.IsNotNull(image, "У картинки объекта «Map» нет Image");
            var normal = image!.sprite;
            Assert.IsNotNull(normal, "У объекта «Map» не задана картинка");

            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button, pointer, ExecuteEvents.pointerEnterHandler);
            yield return null;
            Assert.AreNotEqual(normal, image.sprite, "при наведении картинка объекта не подсветилась");

            ExecuteEvents.Execute(button, pointer, ExecuteEvents.pointerExitHandler);
            yield return null;
            Assert.AreEqual(normal, image.sprite, "после ухода курсора картинка не вернулась");
        }

        /// <summary>Проверяет, что у объекта нет плашки: рамка вокруг него не нужна.</summary>
        /// <param name="buttonName">Имя объекта хаба.</param>
        private static void AssertNoPlate(string buttonName)
        {
            Assert.IsNotNull(GameObject.Find(buttonName), $"Объект «{buttonName}» не найден");
            Assert.IsNull(
                GameObject.Find(buttonName + "Plate"),
                $"У объекта «{buttonName}» осталась плашка-рамка");
        }

        /// <summary>Проверяет, что у кнопки есть плашка с нужным спрайтом, и она лежит позади иконки.</summary>
        /// <param name="buttonName">Имя иконочной кнопки.</param>
        private static void AssertPlateBehind(string buttonName)
        {
            var button = GameObject.Find(buttonName);
            Assert.IsNotNull(button, $"Кнопка «{buttonName}» не найдена");
            var image = PlateOf(buttonName);
            Assert.AreEqual(PlateSpriteName, image.sprite!.name, $"У кнопки «{buttonName}» не тот кадр плашки");
            Assert.AreEqual(
                Image.Type.Sliced,
                image.type,
                $"Плашка кнопки «{buttonName}» должна растягиваться 9-slice");
            Assert.Less(
                image.transform.GetSiblingIndex(),
                button!.transform.GetSiblingIndex(),
                $"Плашка кнопки «{buttonName}» должна лежать позади иконки");
            Assert.IsNotNull(
                button.GetComponent<ButtonArt>(),
                $"У кнопки «{buttonName}» нет подсветки ButtonArt");
        }

        /// <summary>Изображение плашки иконочной кнопки по её имени.</summary>
        /// <param name="buttonName">Имя иконочной кнопки.</param>
        /// <returns>Изображение плашки.</returns>
        private static Image PlateOf(string buttonName)
        {
            var plate = GameObject.Find(buttonName + "Plate");
            Assert.IsNotNull(plate, $"У кнопки «{buttonName}» нет плашки");
            var image = plate!.GetComponent<Image>();
            Assert.IsNotNull(image, $"У плашки кнопки «{buttonName}» нет Image");
            Assert.IsNotNull(image!.sprite, $"У плашки кнопки «{buttonName}» не задан спрайт");
            return image;
        }
    }
}
