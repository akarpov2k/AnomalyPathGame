#nullable enable
using System.Collections;
using System.Reflection;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Экран новой игры собран префабом: панель, задник и подписи приходят из ассета.</summary>
    public sealed partial class PrefabScreenPlayModeTests : PlayModeTestBase
    {
        /// <summary>Панель новой игры — экземпляр префаба с привязанным задником и локализованными подписями.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NewGame_PrefabScreen_BindsBackdropAndLocalizedTexts()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;

            var panel = GameObject.Find("NewGame");
            Assert.IsNotNull(panel, "Панель новой игры не создана");
            Assert.IsNotNull(panel.GetComponent<NewGameScreen>(), "Панель собрана не из префаба экрана");

            var backdrop = panel.transform.Find("ScreenBackdrop");
            Assert.IsNotNull(backdrop, "У панели нет задника из префаба");
            Assert.IsNotNull(backdrop.GetComponent<ScreenBackdrop>(), "У задника нет компонента ScreenBackdrop");
            Assert.IsNotNull(backdrop.GetComponent<Image>().sprite, "Задник не применил изображение темы");

            var start = GameObject.Find("Начать");
            Assert.IsNotNull(start, "Нет кнопки «Начать игру»");
            var screen = FindScreen();
            var label = start.GetComponentInChildren<Text>();
            Assert.IsNotNull(label, "У кнопки «Начать игру» нет подписи");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.NewGame_Start),
                label.text,
                "Подпись кнопки не взята из каталога текстов");
        }

        /// <summary>Каждая локализованная подпись префаба хранит код ключа строкой и берёт текст из каталога.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NewGame_Prefab_LocalizedTextsUseCatalogCodes()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;

            var panel = GameObject.Find("NewGame");
            Assert.IsNotNull(panel, "Панель новой игры не создана");
            var screen = FindScreen();

            var localized = panel.GetComponentsInChildren<LocalizedText>(true);
            Assert.Greater(localized.Length, 0, "В префабе нет локализованных подписей");
            foreach (var item in localized)
            {
                Assert.IsFalse(string.IsNullOrEmpty(item.Key), $"У «{item.name}» не задан код ключа");
                Assert.IsTrue(
                    item.Key.StartsWith("ui.", System.StringComparison.Ordinal),
                    $"Код «{item.Key}» у «{item.name}» не похож на ключ каталога");
                var label = item.GetComponent<Text>() ?? item.GetComponentInChildren<Text>(true);
                Assert.IsNotNull(label, $"У «{item.name}» нет подписи");
                Assert.AreEqual(
                    UiText.Get(screen!.Texts, item.Key),
                    label!.text,
                    $"Подпись «{item.name}» не из каталога");
            }

            var back = GameObject.Find("Назад");
            Assert.IsNotNull(back, "Нет кнопки «Назад»");
            Assert.AreEqual(
                UiText.Get(screen!.Texts, UiTextKey.NewGame_Back),
                back!.GetComponentInChildren<Text>(true).text,
                "Подпись «Назад» не из каталога текстов");
        }

        /// <summary>Интерфейс собран на шрифте игры из Resources, а не на встроенном шрифте Unity.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator UiFonts_ProjectFont_UsedByScreens()
        {
            yield return LoadMain();

            var regular = UiFactory.FindFont();
            Assert.IsNotNull(regular, "Шрифт интерфейса не найден");
            Assert.AreEqual("Play-Regular", regular.name, "Интерфейс остался на встроенном шрифте Unity");

            var bold = UiFactory.FindBoldFont();
            Assert.AreEqual("Play-Bold", bold.name, "Жирное начертание не из проекта");

            var button = GameObject.Find("Новая игра");
            Assert.IsNotNull(button, "В меню нет кнопки «Новая игра»");
            var label = button!.GetComponentInChildren<Text>(true);
            Assert.IsNotNull(label, "У кнопки «Новая игра» нет подписи");
            Assert.AreSame(regular, label!.font, "Подпись кнопки не на шрифте игры");

            var title = GameObject.Find("GameTitle");
            Assert.IsNotNull(title, "В меню нет названия игры");
            Assert.AreSame(bold, title!.GetComponent<Text>().font, "Название игры не на жирном начертании");
        }

        /// <summary>Спрайт, заданный в инспекторе, перекрывает картинку темы.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator ScreenBackdrop_InspectorSprite_OverridesThemeImage()
        {
            yield return LoadMain();

            var host = FindScreen();

            var go = new GameObject("BackdropTest", typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            var backdrop = go.AddComponent<ScreenBackdrop>();
            backdrop.Initialize(host, image);

            var texture = new Texture2D(2, 2);
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
            var field = typeof(ScreenBackdrop).GetField("sprite", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "У ScreenBackdrop нет поля sprite");
            field.SetValue(backdrop, sprite);

            backdrop.Apply(ScreenTheme.Default);

            Assert.AreSame(sprite, image.sprite, "Спрайт из инспектора не применился");
            Object.Destroy(go);
            Object.Destroy(texture);
            yield return null;
        }

        /// <summary>
        /// Стартовое меню собрано префабом по прототипу: пять кнопок — объекты префаба,
        /// «Настройки» активна, версия игры подставлена из сборки.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_Prefab_ButtonsAreObjects_SettingsActive()
        {
            yield return LoadMain();

            var panel = GameObject.Find("MainMenu");
            Assert.IsNotNull(panel, "Стартовое меню не создано");
            Assert.IsNotNull(panel!.GetComponent<MainMenuScreen>(), "Меню собрано не из префаба");

            foreach (var name in new[] { "Новая игра", "Загрузить игру", "Настройки", "Авторы", "Выход" })
            {
                var button = GameObject.Find(name);
                Assert.IsNotNull(button, $"В меню нет кнопки «{name}»");
                Assert.IsNotNull(button!.GetComponent<Button>(), $"«{name}» — не кнопка префаба");
            }

            Assert.IsTrue(
                GameObject.Find("Настройки")!.GetComponent<Button>().interactable,
                "Кнопка «Настройки» должна быть активна: экран настроек собран");

            var version = GameObject.Find("VersionLabel");
            Assert.IsNotNull(version, "В меню нет подписи версии игры");
            var versionText = UiText.Format(FindScreen().Texts, UiTextKey.MainMenu_Version, Application.version);
            Assert.AreEqual(versionText, version!.GetComponent<Text>().text, "Версия не взята из сборки");

            var screen = FindScreen();
            var localized = panel.GetComponentsInChildren<LocalizedText>(true);
            Assert.Greater(localized.Length, 0, "В префабе меню нет локализованных подписей");
            foreach (var item in localized)
            {
                Assert.IsTrue(
                    item.Key.StartsWith("ui.", System.StringComparison.Ordinal),
                    $"Код «{item.Key}» у «{item.name}» не похож на ключ каталога");
                var label = item.GetComponent<Text>() ?? item.GetComponentInChildren<Text>(true);
                Assert.IsNotNull(label, $"У «{item.name}» нет подписи");
                Assert.AreEqual(
                    UiText.Get(screen.Texts, item.Key),
                    label!.text,
                    $"Подпись «{item.name}» не из каталога");
            }
        }

        /// <summary>Кнопка меню показывает пустой индикатор, а при наведении — зелёный, и возвращается назад.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_ButtonArt_SwitchesGreenOnHover()
        {
            yield return LoadMain();

            var button = GameObject.Find("Новая игра");
            Assert.IsNotNull(button, "В меню нет кнопки «Новая игра»");
            var art = button!.GetComponent<ButtonArt>();
            Assert.IsNotNull(art, "У кнопки меню нет подсветки индикатора");
            var image = button.GetComponent<Image>();
            Assert.IsNotNull(image, "У кнопки меню нет изображения");
            Assert.IsNotNull(image!.sprite, "У кнопки меню не задан кадр");
            var normal = image.sprite;

            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(button, pointer, ExecuteEvents.pointerEnterHandler);
            yield return null;

            Assert.AreNotEqual(normal, image.sprite, "При наведении кадр кнопки не сменился на зелёный");

            ExecuteEvents.Execute(button, pointer, ExecuteEvents.pointerExitHandler);
            yield return null;

            Assert.AreEqual(normal, image.sprite, "После ухода курсора кадр кнопки не вернулся");
        }

        /// <summary>
        /// Подпись кнопки меню стоит по центру металлической таблички кадра, а не всего прямоугольника:
        /// на картинке табличка смещена вниз (над ней проходят трубы), поэтому центр кадра ей не подходит.
        /// Подпись остаётся внутри кадра кнопки.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_ButtonLabels_CenteredOnPlateInsideFrame()
        {
            yield return LoadMain();

            foreach (var name in new[] { "Новая игра", "Загрузить игру", "Настройки", "Авторы", "Выход" })
            {
                var button = GameObject.Find(name);
                Assert.IsNotNull(button, $"В меню нет кнопки «{name}»");
                var label = button!.GetComponentInChildren<Text>(true);
                Assert.IsNotNull(label, $"У кнопки «{name}» нет подписи");

                var frame = WorldRect((RectTransform)button.transform);
                var caption = WorldRect(label!.rectTransform);
                var plate = ActionButtonMetrics.PlateRect(frame.width, frame.height, 0f);
                var plateWorld = Rect.MinMaxRect(
                    frame.xMin + plate.xMin,
                    frame.yMin + plate.yMin,
                    frame.xMin + plate.xMax,
                    frame.yMin + plate.yMax);

                Assert.IsTrue(
                    frame.Contains(caption.min) && frame.Contains(caption.max),
                    $"Подпись «{name}» выходит за кадр кнопки");
                Assert.AreEqual(
                    plateWorld.center.x,
                    caption.center.x,
                    0.01f,
                    $"Подпись «{name}» сдвинута от центра таблички по горизонтали");
                Assert.AreEqual(
                    plateWorld.center.y,
                    caption.center.y,
                    0.01f,
                    $"Подпись «{name}» стоит не по центру таблички по вертикали");
            }
        }

        /// <summary>Название игры стоит над кнопкой «Новая игра» и берётся из каталога строк.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainMenu_Prefab_GameTitleAboveNewGame()
        {
            yield return LoadMain();

            var title = GameObject.Find("GameTitle");
            Assert.IsNotNull(title, "В стартовом меню нет названия игры");

            var label = title!.GetComponent<Text>();
            Assert.IsNotNull(label, "У названия игры нет подписи");
            Assert.AreEqual(
                UiText.Get(FindScreen().Texts, UiTextKey.MainMenu_Game_Title),
                label!.text,
                "Название игры не взято из каталога строк");

            var newGame = GameObject.Find("Новая игра");
            Assert.IsNotNull(newGame, "В меню нет кнопки «Новая игра»");
            var titleRect = WorldRect((RectTransform)title.transform);
            var buttonRect = WorldRect((RectTransform)newGame!.transform);
            Assert.Greater(
                titleRect.yMin,
                buttonRect.yMax,
                "Название игры должно стоять над кнопкой «Новая игра»");
        }

        /// <summary>Экраны загрузки и сохранения — экземпляры префабов: листалка, «Назад», поле названия — объекты.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator LoadAndSaveScreens_Prefab_PartsAreObjects()
        {
            yield return LoadMain();

            var screen = FindScreen();
            for (var number = 1; number <= 20; number++)
            {
                screen.Saves.Delete(SaveSlots.Slot(number));
            }

            Click("Загрузить игру");
            yield return null;
            var load = GameObject.Find("LoadGame");
            Assert.IsNotNull(load, "Экран загрузки не открылся");
            Assert.IsNotNull(load!.GetComponent<LoadGameScreen>(), "Экран загрузки собран не из префаба");
            Assert.IsNotNull(GameObject.Find("Назад"), "На экране загрузки нет кнопки «Назад»");
            var pager = FindAnywhere("SlotPager");
            Assert.IsNotNull(pager, "На экране загрузки нет листалки страниц");
            Assert.IsNotNull(FindAnywhere("RowTemplate"), "В префабе загрузки нет образца строки");

            Click("Назад");
            yield return null;
            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;

            var save = GameObject.Find("SaveGame");
            Assert.IsNotNull(save, "Экран сохранения не открылся");
            Assert.IsNotNull(save!.GetComponent<SaveGameScreen>(), "Экран сохранения собран не из префаба");

            var rows = save.GetComponentsInChildren<SaveSlotRow>(true);
            Assert.Greater(rows.Length, 0, "В префабе сохранения нет образца строки");
            SaveSlotRow? newRow = null;
            foreach (var row in rows)
            {
                if (!row.gameObject.activeSelf)
                {
                    continue;
                }

                Assert.IsNotNull(row.Save, "В строке сохранения нет кнопки «Сохранить»");
                if (row.NameField != null && row.NameField.gameObject.activeSelf)
                {
                    newRow = row;
                }
            }

            Assert.IsNotNull(newRow, "В свободной карточке нет поля названия");
            Assert.IsTrue(
                newRow!.NameField!.transform.IsChildOf(newRow.transform),
                "Поле названия должно быть частью свободной карточки");
        }

        /// <summary>
        /// Разметка префабов попадает в панель: кнопки меню внутри и достаточной ширины,
        /// версия справа, листалка по центру, «Назад» справа внизу.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MenuPrefabs_PartsInsidePanel()
        {
            yield return LoadMain();

            var menu = GameObject.Find("MainMenu");
            Assert.IsNotNull(menu, "Стартовое меню не создано");
            var menuRect = WorldRect((RectTransform)menu!.transform);
            Assert.Greater(menuRect.width, 100f, "Панель меню не растянута на окно");

            foreach (var name in new[] { "Новая игра", "Загрузить игру", "Настройки", "Авторы", "Выход" })
            {
                var button = GameObject.Find(name);
                Assert.IsNotNull(button, $"В меню нет кнопки «{name}»");
                var rect = WorldRect((RectTransform)button!.transform);
                Assert.Greater(rect.width, menuRect.width * 0.1f, $"Кнопка «{name}» слишком узкая");
                Assert.IsTrue(menuRect.Contains(rect.center), $"Кнопка «{name}» вне панели меню");
            }

            var version = GameObject.Find("VersionLabel");
            Assert.IsNotNull(version, "В меню нет подписи версии");
            Assert.Greater(
                WorldRect((RectTransform)version!.transform).center.x,
                menuRect.center.x,
                "Версия игры должна стоять справа");

            Click("Загрузить игру");
            yield return null;

            var load = GameObject.Find("LoadGame");
            Assert.IsNotNull(load, "Экран загрузки не открылся");
            var loadRect = WorldRect((RectTransform)load!.transform);
            var pagerObject = FindAnywhere("SlotPager");
            Assert.IsNotNull(pagerObject, "На экране загрузки нет листалки");
            var pagerRect = WorldRect((RectTransform)pagerObject!.transform);
            Assert.AreEqual(
                loadRect.center.x,
                pagerRect.center.x,
                loadRect.width * 0.01f,
                "Листалка не по центру экрана");
            Assert.Less(pagerRect.center.y, loadRect.center.y, "Листалка должна быть внизу");

            var back = GameObject.Find("Назад");
            Assert.IsNotNull(back, "Нет кнопки «Назад»");
            var backRect = WorldRect((RectTransform)back!.transform);
            Assert.Greater(backRect.center.x, loadRect.center.x, "«Назад» должна быть справа");
            Assert.AreEqual(
                pagerRect.center.y,
                backRect.center.y,
                loadRect.height * 0.02f,
                "«Назад» должна быть на уровне листалки");
        }
    }
}
