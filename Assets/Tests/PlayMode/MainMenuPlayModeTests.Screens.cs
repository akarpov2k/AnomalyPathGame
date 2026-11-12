#nullable enable
using System;
using System.Collections;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Game;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="MainMenuPlayModeTests"/>: экраны и фон.</summary>
    public sealed partial class MainMenuPlayModeTests
    {
        /// <summary>
        /// У всех окон есть тематический задник; у окна инвентаря — свой фон-сундук,
        /// а не картинка локации.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Screens_HaveThemedBackdrop()
        {
            yield return LoadMain();

            var menu = BackdropOf("MainMenu");
            Assert.IsNotNull(menu.sprite, "У стартового меню нет тематического задника");
            Assert.AreEqual(1f, menu.color.a, 0.01f, "Задник меню должен быть полностью непрозрачным");

            Click("Авторы");
            yield return null;
            Assert.IsNotNull(BackdropOf("Authors").sprite, "У экрана авторов нет задника");
            Click("AuthorsBack");
            yield return null;

            Click("Новая игра");
            yield return null;
            Assert.IsNotNull(BackdropOf("NewGame").sprite, "У экрана новой игры нет задника");

            Click("Начать");
            yield return null;
            var bigHub = GameObject.Find("BigHub");
            Assert.IsNotNull(bigHub, "Большой хаб не открылся");
            var interior = bigHub!.transform.Find("Content/Stage/Interior")?.GetComponent<Image>();
            Assert.IsNotNull(interior, "У большого хаба нет интерьера");
            Assert.IsNotNull(interior!.sprite, "У большого хаба нет задника");

            Click("Backpack");
            yield return null;
            var character = BackdropOf("Inventory");
            var characterBackdrop = character.GetComponent<AnomalyPath.Runtime.Ui.Core.ScreenBackdrop>();
            Assert.IsNotNull(characterBackdrop, "У окна инвентаря нет компонента задника");
            Assert.AreEqual(
                AnomalyPath.Runtime.Ui.Core.ScreenBackdropIds.Inventory,
                characterBackdrop!.SpriteId,
                "У окна инвентаря должен быть свой фон-сундук, а не локация");
            Assert.IsNotNull(character.sprite, "У окна инвентаря нет картинки фона");
            Assert.AreEqual(1f, character.color.a, 0.01f, "Фон инвентаря должен быть во всю яркость");
        }

        /// <summary>
        /// Стартовое меню показывает свой задник из префаба, а вырезка сталкера лежит поверх него
        /// без растяжения: пропорции фигуры сохраняются при любом окне.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_Backdrop_OwnPictureAndUnstretchedArtwork()
        {
            yield return LoadMain();

            var menu = BackdropOf("MainMenu");
            Assert.IsNotNull(menu.sprite, "У стартового меню нет задника");
            var host = FindScreen();
            Assert.IsTrue(
                host.Sprites.TryGet("backdrops/screen", out var shared) && shared != null,
                "В каталоге нет общего задника окон");
            Assert.AreNotSame(shared, menu.sprite, "Стартовое меню должно брать свой задник из префаба, а не общий");

            var artwork = GameObject.Find("Artwork");
            Assert.IsNotNull(artwork, "В стартовом меню нет картинки сталкера");
            var image = artwork!.GetComponent<Image>();
            Assert.IsNotNull(image, "У картинки сталкера нет изображения");
            Assert.IsNotNull(image!.sprite, "У картинки сталкера не задан спрайт");
            Assert.IsTrue(image.preserveAspect, "Вырезка сталкера должна сохранять пропорции фигуры");
        }

        /// <summary>В правом нижнем углу меню версия: номер из сборки и подпись демо из каталога.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_VersionLabel_FromCatalogAndBuild()
        {
            yield return LoadMain();

            var label = GameObject.Find("VersionLabel");
            Assert.IsNotNull(label, "В стартовом меню нет подписи версии");
            var text = label!.GetComponent<Text>();
            Assert.IsNotNull(text, "У подписи версии нет Text");
            var expected = AnomalyPath.Runtime.Ui.Core.UiText.Format(
                FindScreen().Texts,
                AnomalyPath.Runtime.Ui.Core.UiTextKey.MainMenu_Version,
                Application.version);
            Assert.AreEqual(expected, text!.text, "Версия в меню не из каталога строк");
            StringAssert.Contains("Alpha Demo", text.text, "В версии нет подписи демо");
        }

        /// <summary>Esc возвращает из настроек новой игры и из «Авторов», но не из игровой панели.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Escape_ReturnsToMenu_WhereDeclared()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");

            Click("Авторы");
            yield return null;
            Assert.IsTrue(screen.HandleEscape(), "Esc из сведений не обработан");
            yield return null;
            Assert.IsTrue(GameObject.Find("MainMenu").activeSelf, "Esc не вернул в меню из «Авторов»");

            Click("Новая игра");
            yield return null;
            Assert.IsTrue(screen.HandleEscape(), "Esc из настроек новой игры не обработан");
            yield return null;
            Assert.IsTrue(GameObject.Find("MainMenu").activeSelf, "Esc не вернул в меню из новой игры");

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Assert.IsTrue(screen.HandleEscape(), "Esc на игровой панели не открыл меню");
            yield return null;
            Assert.IsTrue(GameObject.Find("GameMenu").activeSelf, "Esc не открыл меню игры");
            Assert.IsNull(GamePanel(), "Игровая панель осталась поверх меню");

            Assert.IsTrue(screen.HandleEscape(), "Esc в меню игры не обработан");
            yield return null;
            Assert.IsTrue(GamePanel()!.activeSelf, "Esc не вернул из меню в игру");
        }

        /// <summary>Меню игры показывает счётчик смертей текущего прохождения.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator GameMenu_ShowsDeathsCounter()
        {
            yield return StartGame();

            FindScreen().ShowGameMenu();
            yield return null;

            var menu = GameObject.Find("GameMenu");
            Assert.IsNotNull(menu, "Меню игры не открылось");
            var label = FindByPrefix(menu!.transform, "GameMenuDeaths");
            Assert.IsNotNull(label, "В меню нет строки счётчика смертей");
            Assert.IsTrue(
                label!.GetComponent<Text>().text.Contains("0", StringComparison.Ordinal),
                "Счётчик смертей на старте не ноль");
        }

        /// <summary>Esc на экране персонажа возвращает в игровое поле.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Escape_Character_ReturnsToGame()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;
            Assert.IsTrue(GameObject.Find("Inventory").activeSelf, "Окно инвентаря не открылось");

            Assert.IsTrue(screen.HandleEscape(), "Esc в окне инвентаря не обработан");
            yield return null;
            Assert.IsTrue(GamePanel()!.activeSelf, "Esc не вернул из инвентаря в игру");
            Assert.IsNull(GameObject.Find("Inventory"), "Окно инвентаря осталось активным");
        }

        /// <summary>Стрелка часов идёт по дневной и ночной половинам и совпадает с временем.</summary>
        [Test]
        public void PlayClock_AngleFollowsDayAndNight()
        {
            Assert.AreEqual(0f, PlayClock.Angle(new WorldClock(1, 10, 10, 22)), 0.01f, "начало дня");
            Assert.AreEqual(300f, PlayClock.Angle(new WorldClock(1, 14, 10, 22)), 0.01f, "2 часа дня");
            Assert.AreEqual(180f, PlayClock.Angle(new WorldClock(1, 22, 10, 22)), 0.01f, "конец дня, начало ночи");
            Assert.AreEqual(120f, PlayClock.Angle(new WorldClock(1, 2, 10, 22)), 0.01f, "2 часа ночи");
        }
    }
}
