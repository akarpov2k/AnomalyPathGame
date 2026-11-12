#nullable enable
using System;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает экраны меню, загрузки, сохранения и настроек с их префабов в PNG: карточки сохранений
    /// заполняются образцами, подписи берутся из каталога строк. Файлы — `Logs/Screenshots`.
    /// </summary>
    public static partial class MenuPrefabsScreenshot
    {
        private const int Width = 1920;
        private const int Height = 1080;
        private const int WideWidth = 2560;
        private const int WideHeight = 1440;
        private const string MainMenuPrefabPath = "Assets/Prefabs/Screens/MainMenuScreen.prefab";
        private const string AuthorsPrefabPath = "Assets/Prefabs/Screens/AuthorsScreen.prefab";
        private const string NewGamePrefabPath = "Assets/Prefabs/Screens/NewGameScreen.prefab";
        private const string LoadPrefabPath = "Assets/Prefabs/Screens/LoadGameScreen.prefab";
        private const string SavePrefabPath = "Assets/Prefabs/Screens/SaveGameScreen.prefab";
        private const string SettingsPrefabPath = "Assets/Prefabs/Screens/SettingsScreen.prefab";
        private const string KeyBindingsPrefabPath = "Assets/Prefabs/Screens/KeyBindingsScreen.prefab";
        private const string InventoryPrefabPath = "Assets/Prefabs/Screens/InventoryScreen.prefab";
        private const string GameMenuPrefabPath = "Assets/Prefabs/Screens/GameMenuScreen.prefab";
        private const string DevelopmentStubPrefabPath = "Assets/Prefabs/Screens/DevelopmentStubScreen.prefab";
        private const string DataRoot = "Assets/Data";
        private const string OutputFolder = "Logs/Screenshots";

        /// <summary>Пункт меню: снять скриншоты экранов меню, загрузки, сохранения и настроек.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты экранов меню, загрузки, сохранения и настроек")]
        public static void CaptureFromMenu() => CaptureAll();

        /// <summary>Точка входа batch-режима.</summary>
        public static void CaptureFromBatch()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает все экраны в нужных разрешениях.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var services = LoadServices();
            var texts = services.Resolve<IGameTexts>();
            var pageSize = services.Resolve<IGameRulesRepository>().Require().SavePageSize;

            Capture(MainMenuPrefabPath, "main-menu", Width, Height, texts, pageSize, FillMainMenu);
            Capture(MainMenuPrefabPath, "main-menu-hover", Width, Height, texts, pageSize, FillMainMenuHover);
            Capture(AuthorsPrefabPath, "authors", Width, Height, texts, pageSize, FillAuthors);
            Capture(NewGamePrefabPath, "new-game", Width, Height, texts, pageSize, FillNewGame);
            Capture(LoadPrefabPath, "load-game", Width, Height, texts, pageSize, FillLoadScreen);
            Capture(SavePrefabPath, "save-game", Width, Height, texts, pageSize, FillSaveScreen);
            Capture(SavePrefabPath, "save-game-2k", WideWidth, WideHeight, texts, pageSize, FillSaveScreen);
            Capture(SettingsPrefabPath, "settings", Width, Height, texts, pageSize, FillSettingsScreen);
            Capture(KeyBindingsPrefabPath, "key-bindings", Width, Height, texts, pageSize, FillKeyBindingsScreen);
            Capture(InventoryPrefabPath, "inventory", Width, Height, texts, pageSize, FillInventory);
            Capture(GameMenuPrefabPath, "game-menu", Width, Height, texts, pageSize, FillGameMenu);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(DevelopmentStubPrefabPath) != null)
            {
                Capture(DevelopmentStubPrefabPath, "development-stub", Width, Height, texts, pageSize, FillDevelopmentStub);
            }
        }

        /// <summary>Снимает только экран-заглушку: кадр нужен сборщику её префаба.</summary>
        public static void CaptureDevelopmentStub()
        {
            Directory.CreateDirectory(OutputFolder);
            var services = LoadServices();
            var texts = services.Resolve<IGameTexts>();
            var pageSize = services.Resolve<IGameRulesRepository>().Require().SavePageSize;
            Capture(DevelopmentStubPrefabPath, "development-stub", Width, Height, texts, pageSize, FillDevelopmentStub);
        }

        /// <summary>У заглушки нет динамики: все подписи берутся из каталога по ключам префаба.</summary>
        private static void FillDevelopmentStub(GameObject panel, IGameTexts texts, int pageSize)
        {
        }

        private static ServiceContainer LoadServices()
        {
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            return result.Services;
        }

        private static void Capture(
            string prefabPath,
            string fileName,
            int width,
            int height,
            IGameTexts texts,
            int pageSize,
            Action<GameObject, IGameTexts, int> fill)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {prefabPath}");
            }

            var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var canvasObject = new GameObject("CaptureCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(width, height);
            canvasRect.position = Vector3.zero;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.SetActive(true);
            ApplyLabels(instance, texts);
            ApplyBackdrop(instance);
            fill(instance, texts, pageSize);
            Rebuild(instance);

            var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            var path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void ApplyLabels(GameObject panel, IGameTexts texts)
        {
            foreach (var label in panel.GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }
        }

        /// <summary>
        /// Ставит задник окна: прямой спрайт из префаба, затем спрайт по id каталога, а без картинки —
        /// плоскую тему. В игре картинку из каталога подставляет хост.
        /// </summary>
        private static void ApplyBackdrop(GameObject panel) => ScreenBackdropAssets.ApplyForScreenshot(panel);

        private static void Rebuild(GameObject panel)
        {
            foreach (var group in panel.GetComponentsInChildren<LayoutGroup>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }

            foreach (var fitter in panel.GetComponentsInChildren<ContentSizeFitter>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)fitter.transform);
            }

            Canvas.ForceUpdateCanvases();
            ActionButtonLabel.FitAll(panel.transform);
            Canvas.ForceUpdateCanvases();
        }

        private static void FillMainMenu(GameObject panel, IGameTexts texts, int pageSize)
        {
            var version = panel.transform.Find("Content/VersionLabel");
            if (version != null)
            {
                var label = version.GetComponent<Text>();
                if (label != null)
                {
                    label.text = UiText.Format(texts, UiTextKey.MainMenu_Version, Application.version);
                }
            }
        }

        private static void FillMainMenuHover(GameObject panel, IGameTexts texts, int pageSize)
        {
            FillMainMenu(panel, texts, pageSize);
            var button = FindDeep(panel.transform, "Новая игра");
            var image = button != null ? button.GetComponent<Image>() : null;
            if (image != null)
            {
                image.sprite = ButtonArtAssets.Highlighted();
            }
        }

        private static void FillNewGame(GameObject panel, IGameTexts texts, int pageSize)
        {
            var nickname = FindDeep(panel.transform, "NicknameInput");
            var input = nickname != null ? nickname.GetComponent<TextInputField>() : null;
            input?.SetText(UiText.Get(texts, "ui.newgame_name_placeholder"));
        }

        private static void FillLoadScreen(GameObject panel, IGameTexts texts, int pageSize)
        {
            AddRows(panel, texts, pageSize, new[]
            {
                Sample.Readable("save_3", "Засада на блокпосту", 5, 4820, 3, 14.5),
                Sample.Readable("save_2", "Ночной рейд", 3, 2150, 2, 22.0),
                Sample.Corrupted("save_1"),
                Sample.Readable(SaveSlots.Exit, "Выход из игры", 2, 900, 1, 23.5)
            });
        }

        private static void FillSaveScreen(GameObject panel, IGameTexts texts, int pageSize)
        {
            AddRows(panel, texts, pageSize, new[]
            {
                Sample.Readable("save_3", "Засада на блокпосту", 5, 4820, 3, 14.5),
                Sample.Readable("save_2", "Ночной рейд", 3, 2150, 2, 22.0),
                Sample.Readable("save_1", "Первый выход", 2, 1400, 1, 9.5),
                Sample.Corrupted("save_4"),
                Sample.Free(SaveSlots.Slot(5), "Новое сохранение 3")
            });
        }

        private static void FillSettingsScreen(GameObject panel, IGameTexts texts, int pageSize)
        {
            var toggle = FindDeep(panel.transform, "ResolutionToggle");
            var resolution = toggle != null ? toggle.GetComponentInChildren<Text>(true) : null;
            if (resolution != null)
            {
                resolution.text = "1920×1080";
            }

            SetSlider(panel, "Volume", 70f, "VolumeValue");
            SetSlider(panel, "Sensitivity", 50f, "SensitivityValue");
        }

        private static void FillKeyBindingsScreen(GameObject panel, IGameTexts texts, int pageSize)
        {
            var rows = panel.GetComponentInChildren<ScrollArea>(true);
            var template = panel.GetComponentInChildren<KeyBindRow>(true);
            var content = rows != null ? ReadContent(rows) : null;
            if (rows == null || template == null || content == null)
            {
                throw new InvalidOperationException("В префабе настройки клавиш нет списка или образца строки");
            }

            var first = true;
            foreach (var action in HotkeyDefaults.All)
            {
                var row = UnityEngine.Object.Instantiate(template, content);
                row.gameObject.SetActive(true);
                row.Bind(texts, action, HotkeyDefaults.For(action), first);
                first = false;
            }

            Rebuild(panel);
        }

        private static void SetSlider(GameObject panel, string name, float value, string valueLabel)
        {
            var sliderObject = FindDeep(panel.transform, name);
            var slider = sliderObject != null ? sliderObject.GetComponent<Slider>() : null;
            if (slider != null)
            {
                slider.value = value;
            }

            var labelObject = FindDeep(panel.transform, valueLabel);
            var label = labelObject != null ? labelObject.GetComponent<Text>() : null;
            if (label != null)
            {
                label.text = Mathf.RoundToInt(value).ToString();
            }
        }

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

        private static void AddRows(GameObject panel, IGameTexts texts, int pageSize, Sample[] samples)
        {
            var rows = panel.GetComponentInChildren<ScrollArea>(true);
            if (rows == null)
            {
                throw new InvalidOperationException("В префабе нет ScrollArea");
            }

            var content = ReadContent(rows);
            var template = panel.GetComponentInChildren<SaveSlotRow>(true);
            if (content == null || template == null)
            {
                throw new InvalidOperationException("В префабе нет списка или образца карточки");
            }

            var layout = content.GetComponent<VerticalLayoutGroup>();
            var spacing = layout != null ? layout.spacing : 0f;
            var viewport = ((RectTransform)rows.transform).rect.height;
            var height = SaveRowLayout.Height(viewport, spacing, pageSize);
            foreach (var sample in samples)
            {
                var row = UnityEngine.Object.Instantiate(template, content);
                row.gameObject.SetActive(true);
                row.Bind(texts, sample.Slot, sample.Number, sample.Info, sample.IsNew, sample.NewName);
                SetHeight(row.gameObject, height);
            }

            Rebuild(panel);
        }

        private static RectTransform? ReadContent(ScrollArea rows)
        {
            var serialized = new SerializedObject(rows);
            var property = serialized.FindProperty("prefabContent");
            return property != null ? property.objectReferenceValue as RectTransform : null;
        }

        private static void SetHeight(GameObject row, float height)
        {
            var element = row.GetComponent<LayoutElement>();
            if (element != null)
            {
                element.preferredHeight = height;
                element.minHeight = height;
            }
        }

        private sealed class Sample
        {
            private Sample(string slot, int number, SaveSlotInfo? info, bool isNew, string newName)
            {
                Slot = slot;
                Number = number;
                Info = info;
                IsNew = isNew;
                NewName = newName;
            }

            public string Slot { get; }

            public int Number { get; }

            public SaveSlotInfo? Info { get; }

            public bool IsNew { get; }

            public string NewName { get; }

            public static Sample Readable(
                string slot,
                string title,
                int level,
                int money,
                int day,
                double hours) => new Sample(
                slot,
                NumberOf(slot),
                new SaveSlotInfo(
                    slot,
                    null,
                    0,
                    0,
                    null,
                    day,
                    hours,
                    true,
                    title,
                    level,
                    money),
                false,
                string.Empty);

            public static Sample Corrupted(string slot) => new Sample(
                slot,
                NumberOf(slot),
                new SaveSlotInfo(slot, null, 0, 0, null, 0, 0, false),
                false,
                string.Empty);

            public static Sample Free(string slot, string newName) =>
                new Sample(slot, NumberOf(slot), null, true, newName);

            private static int NumberOf(string slot)
            {
                if (!slot.StartsWith(SaveSlots.Prefix, StringComparison.Ordinal))
                {
                    return 0;
                }

                return int.TryParse(slot.Substring(SaveSlots.Prefix.Length), out var number) && number > 0
                    ? number
                    : 0;
            }
        }
    }
}
