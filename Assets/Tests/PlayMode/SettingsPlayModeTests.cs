#nullable enable
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Настройки: экран из префаба, машинные значения, привязки клавиш и горячие клавиши.</summary>
    public sealed class SettingsPlayModeTests : PlayModeTestBase
    {
        /// <summary>Кнопка «Настройки» открывает экран, из него — экран клавиш, «Назад» возвращает по шагам.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Settings_MainMenuButton_OpensScreensAndBack()
        {
            yield return LoadMain();

            var settingsButton = GameObject.Find("Настройки");
            Assert.IsNotNull(settingsButton, "В стартовом меню нет кнопки «Настройки»");
            Assert.IsTrue(settingsButton!.GetComponent<Button>().interactable, "Кнопка «Настройки» неактивна");

            Click("Настройки");
            yield return null;

            var settings = GameObject.Find("Settings");
            Assert.IsNotNull(settings, "Экран настроек не открылся");
            Assert.IsNotNull(settings!.GetComponent<SettingsScreen>(), "Экран настроек собран не из префаба");
            var texts = FindScreen().Texts;
            var resolution = GameObject.Find("ResolutionLabel");
            Assert.IsNotNull(resolution, "На экране настроек нет подписи разрешения");
            Assert.AreEqual(
                UiText.Get(texts, UiTextKey.Settings_Resolution),
                resolution!.GetComponent<Text>().text,
                "Подпись разрешения не из каталога");

            Click("Настройка клавиш");
            yield return null;

            var keys = GameObject.Find("KeyBindings");
            Assert.IsNotNull(keys, "Экран настройки клавиш не открылся");
            Assert.IsNotNull(keys!.GetComponent<KeyBindingsScreen>(), "Экран клавиш собран не из префаба");
            var activeRows = 0;
            foreach (var row in keys.GetComponentsInChildren<KeyBindRow>(true))
            {
                if (row.gameObject.activeSelf)
                {
                    activeRows++;
                }
            }

            Assert.AreEqual(
                HotkeyDefaults.All.Count,
                activeRows,
                "На экране клавиш не по строке на каждое действие");
            Assert.IsNotNull(GameObject.Find("SectionTitle"), "Нет заголовка раздела «Игра»");

            Click("Назад");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Settings"), "«Назад» не вернул на экран настроек");

            Click("Назад");
            yield return null;
            Assert.IsNotNull(GameObject.Find("MainMenu"), "«Назад» не вернул в стартовое меню");
        }

        /// <summary>Ползунки настроек пишут значения в сервис и показывают их справа.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Settings_Sliders_StoreVolumeAndSensitivity()
        {
            yield return LoadMain();
            Click("Настройки");
            yield return null;

            var screen = FindScreen();
            var volume = FindAnywhere("Volume");
            Assert.IsNotNull(volume, "Нет ползунка громкости");
            volume!.GetComponent<Slider>().value = 42f;
            Assert.AreEqual(42, screen.Settings.Volume, "Громкость не записана");
            Assert.AreEqual("42", GameObject.Find("VolumeValue")!.GetComponent<Text>().text, "Громкость не показана");

            var sensitivity = FindAnywhere("Sensitivity");
            Assert.IsNotNull(sensitivity, "Нет ползунка чувствительности");
            sensitivity!.GetComponent<Slider>().value = 88f;
            Assert.AreEqual(88, screen.Settings.MouseSensitivity, "Чувствительность не записана");
            Assert.AreEqual(
                "88",
                GameObject.Find("SensitivityValue")!.GetComponent<Text>().text,
                "Чувствительность не показана");

            screen.Settings.SetVolume(GameSettingsService.DefaultVolume);
            screen.Settings.SetMouseSensitivity(GameSettingsService.DefaultMouseSensitivity);
        }

        /// <summary>Выпадающий список разрешений раскрывается, показывает варианты и применяет выбранный.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Settings_ResolutionDropdown_PicksResolution()
        {
            yield return LoadMain();
            Click("Настройки");
            yield return null;

            var toggle = GameObject.Find("ResolutionToggle");
            Assert.IsNotNull(toggle, "Нет кнопки выбора разрешения");
            toggle!.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var list = GameObject.Find("ResolutionOptions");
            Assert.IsNotNull(list, "Список разрешений не раскрылся");
            Button? option = null;
            foreach (var button in list!.GetComponentsInChildren<Button>(true))
            {
                if (button.gameObject.activeSelf && button.gameObject.name.StartsWith("Resolution_"))
                {
                    option = button;
                    break;
                }
            }

            Assert.IsNotNull(option, "В списке нет ни одного разрешения");
            var parts = option!.gameObject.name.Substring("Resolution_".Length).Split('×');
            var width = int.Parse(parts[0]);
            var height = int.Parse(parts[1]);
            option.onClick.Invoke();
            yield return null;

            var screen = FindScreen();
            Assert.AreEqual(width, screen.Settings.ResolutionWidth, "Ширина не применилась");
            Assert.AreEqual(height, screen.Settings.ResolutionHeight, "Высота не применилась");
            Assert.IsFalse(list.activeSelf, "Список не закрылся после выбора");
            screen.Settings.SetResolution(
                GameSettingsService.DefaultResolutionWidth,
                GameSettingsService.DefaultResolutionHeight);
        }

        /// <summary>Звёздочка несохранённого изменения стоит у названия и не двигает кнопку клавиши.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator KeyBindings_ChangedStar_DoesNotMoveKeyButton()
        {
            yield return LoadMain();
            Click("Настройки");
            yield return null;
            Click("Настройка клавиш");
            yield return null;

            var panel = GameObject.Find("KeyBindings");
            Assert.IsNotNull(panel, "Экран настройки клавиш не открылся");
            KeyBindRow? row = null;
            foreach (var candidate in panel!.GetComponentsInChildren<KeyBindRow>(true))
            {
                if (candidate.gameObject.activeSelf)
                {
                    row = candidate;
                    break;
                }
            }

            Assert.IsNotNull(row, "Нет ни одной строки привязки");
            var star = row!.transform.Find("ChangedStar");
            Assert.IsNotNull(star, "В строке нет звёздочки изменения");

            star!.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row.transform);
            var before = WorldRect((RectTransform)row.KeyButton.transform).center.x;

            star.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)row.transform);
            var after = WorldRect((RectTransform)row.KeyButton.transform).center.x;

            Assert.AreEqual(before, after, 0.5f, "Звёздочка сдвигает кнопку клавиши");
            var rowRect = WorldRect((RectTransform)row.transform);
            var keyLeft = WorldRect((RectTransform)row.KeyButton.transform).xMin;
            Assert.Less(star.position.x, keyLeft, "Звёздочка должна быть левее кнопки клавиши");
            Assert.Less(
                star.position.x - rowRect.xMin,
                500f,
                "Звёздочка должна стоять рядом с названием настройки");
        }

        /// <summary>Привязки по умолчанию, обмен занятой клавишей, сохранение и откат рабочей копии.</summary>
        [Test]
        public void Settings_Bindings_DefaultsSwapSaveAndRevert()
        {
            var storage = new MemorySettingsStorage();
            var settings = new GameSettingsService(storage);
            settings.Load();

            Assert.AreEqual(Key.F5, settings.GetBinding(HotkeyAction.QuickSave), "Быстрое сохранение не F5");
            Assert.AreEqual(Key.I, settings.GetBinding(HotkeyAction.Inventory), "Инвентарь не I");

            settings.SetBinding(HotkeyAction.Inventory, Key.M);

            Assert.AreEqual(Key.M, settings.GetBinding(HotkeyAction.Inventory), "Клавиша инвентаря не сменилась");
            Assert.AreEqual(Key.I, settings.GetBinding(HotkeyAction.Map), "Занятая клавиша не поменялась местами");
            Assert.IsTrue(settings.HasUnsavedBindings, "Изменение не отмечено как несохранённое");

            settings.RevertBindings();
            Assert.AreEqual(Key.I, settings.GetBinding(HotkeyAction.Inventory), "Откат не вернул клавишу");
            Assert.IsFalse(settings.HasUnsavedBindings, "После отката остались изменения");

            settings.SetBinding(HotkeyAction.QuickSave, Key.F6);
            settings.SaveBindings();

            var reloaded = new GameSettingsService(storage);
            reloaded.Load();
            Assert.AreEqual(Key.F6, reloaded.GetSavedBinding(HotkeyAction.QuickSave), "Сохранённая привязка не прочиталась");
            Assert.IsFalse(reloaded.HasUnsavedBindings, "После сохранения есть несохранённые изменения");
        }

        /// <summary>Громкость и чувствительность зажимаются в 0..100, настройки перечитываются из хранилища.</summary>
        [Test]
        public void Settings_VolumeAndSensitivity_AreClampedAndPersisted()
        {
            var storage = new MemorySettingsStorage();
            var settings = new GameSettingsService(storage);
            settings.Load();

            settings.SetVolume(150);
            settings.SetMouseSensitivity(-10);
            Assert.AreEqual(100, settings.Volume, "Громкость не зажата сверху");
            Assert.AreEqual(0, settings.MouseSensitivity, "Чувствительность не зажата снизу");
            Assert.GreaterOrEqual(settings.Resolutions.Count, 1, "Нет ни одного доступного разрешения");

            var reloaded = new GameSettingsService(storage);
            reloaded.Load();
            Assert.AreEqual(100, reloaded.Volume, "Громкость не сохранилась");
            Assert.AreEqual(0, reloaded.MouseSensitivity, "Чувствительность не сохранилась");
        }

        /// <summary>Быстрое сохранение пишет слот, быстрая загрузка возвращает состояние, вне игрового поля — отказ.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hotkeys_QuickSaveAndLoad_RestoreGame()
        {
            yield return LoadMain();
            var screen = FindScreen();
            Assert.IsFalse(
                screen.TryRunHotkey(HotkeyAction.QuickSave),
                "Горячая клавиша не должна работать в стартовом меню");

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;

            screen = FindScreen();
            Assert.IsNotNull(screen.Session, "Игра не начата");
            var money = screen.Session!.State.Player!.Money;

            Assert.IsTrue(screen.TryRunHotkey(HotkeyAction.QuickSave), "Быстрое сохранение не выполнилось");
            Assert.IsTrue(screen.Saves.Exists(SaveSlots.Quick), "Слот быстрого сохранения не появился");

            screen.Session!.State.Player!.AddMoney(100);
            Assert.IsTrue(screen.TryRunHotkey(HotkeyAction.QuickLoad), "Быстрая загрузка не выполнилась");
            yield return null;

            Assert.IsNotNull(screen.Session, "После быстрой загрузки нет сессии");
            Assert.AreEqual(money, screen.Session!.State.Player!.Money, "Быстрая загрузка не вернула деньги");
        }

        /// <summary>Горячие клавиши экранов открывают экран только с игрового поля.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hotkeys_OpenScreens_OnlyFromPlayScreen()
        {
            yield return LoadMain();
            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;

            var screen = FindScreen();
            Assert.IsTrue(screen.TryRunHotkey(HotkeyAction.Inventory), "Инвентарь не открылся по горячей клавише");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Inventory"), "Экран инвентаря не показан");
            Assert.IsFalse(
                screen.TryRunHotkey(HotkeyAction.Character),
                "С экрана инвентаря горячие клавиши работать не должны");
        }

        private sealed class MemorySettingsStorage : IGameSettingsStorage
        {
            private readonly Dictionary<string, int> _ints = new Dictionary<string, int>();
            private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();

            public int GetInt(string key, int fallback) =>
                _ints.TryGetValue(key, out var value) ? value : fallback;

            public void SetInt(string key, int value) => _ints[key] = value;

            public string GetString(string key, string fallback) =>
                _strings.TryGetValue(key, out var value) ? value : fallback;

            public void SetString(string key, string value) => _strings[key] = value;

            public void Save()
            {
            }
        }
    }
}
