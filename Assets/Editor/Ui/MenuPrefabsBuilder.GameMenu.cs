#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Часть класса <see cref="MenuPrefabsBuilder"/>: меню игры (Esc и шестерёнка).
    /// Раскладка снята с прежнего кодового экрана: колонка по центру, сводка состояния,
    /// счётчик смертей и четыре кнопки прежней плашкой — пользователь просил сохранить расположение.
    /// </summary>
    public static partial class MenuPrefabsBuilder
    {
        private const string GameMenuPrefabPath = "Assets/Prefabs/Screens/GameMenuScreen.prefab";

        /// <summary>Пункт меню: собрать префаб меню игры и привязать его к сцене Main.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Меню игры»")]
        public static void BuildGameMenuFromMenu() => BuildGameMenu();

        /// <summary>Точка входа batch-режима для префаба меню игры.</summary>
        public static void BuildGameMenuFromBatch()
        {
            try
            {
                BuildGameMenu();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает префаб меню игры и назначает его полю <c>gameMenuScreenPrefab</c> в сцене Main.</summary>
        public static void BuildGameMenu()
        {
            var services = LoadServices();
            var texts = services.Resolve<IGameTexts>();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var ui = new UiFactory(UiFactory.FindFont(), ButtonArtAssets.Normal(), ButtonArtAssets.Highlighted());
            var prefab = SavePrefab(BuildGameMenuScreen(ui, texts), GameMenuPrefabPath);
            AssetDatabase.SaveAssets();
            AssignGameMenuToScene(prefab);
            Debug.Log("AnomalyPath: префаб меню игры собран");
        }

        private static GameObject BuildGameMenuScreen(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("GameMenu");
            var backdrop = AddBackdrop(root.transform);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            var column = BuildGameMenuColumn(content);

            var title = CreateLabel(
                column,
                "Title",
                UiText.Get(texts, UiTextKey.GameMenu_Title),
                GameMenuMetrics.TitleSize,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            Localize(title, UiTextKey.GameMenu_Title);
            SetPreferredHeight(title.gameObject, GameMenuMetrics.TitleHeight);

            var status = CreateLabel(
                column,
                "Status",
                string.Empty,
                GameMenuMetrics.TextSize,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            SetPreferredHeight(status.gameObject, GameMenuMetrics.StatusHeight);

            var deaths = CreateLabel(
                column,
                "GameMenuDeaths",
                string.Empty,
                GameMenuMetrics.TextSize,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            SetPreferredHeight(deaths.gameObject, GameMenuMetrics.DeathsHeight);

            var continueButton = CreatePlaqueButton(ui, texts, column, UiTextKey.GameMenu_Continue);
            var saveButton = CreatePlaqueButton(ui, texts, column, UiTextKey.Game_Save);
            var loadButton = CreatePlaqueButton(ui, texts, column, UiTextKey.Game_Load);
            var exitButton = CreatePlaqueButton(ui, texts, column, UiTextKey.Game_Exit);

            var screen = root.AddComponent<GameMenuScreen>();
            SetObject(screen, "status", status);
            SetObject(screen, "deaths", deaths);
            SetObject(screen, "continueButton", continueButton);
            SetObject(screen, "saveButton", saveButton);
            SetObject(screen, "loadButton", loadButton);
            SetObject(screen, "exitButton", exitButton);
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static RectTransform BuildGameMenuColumn(Transform parent)
        {
            var column = CreateRect(parent, "Column");
            Place(
                column,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(GameMenuMetrics.ColumnWidth, 0f));

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                GameMenuMetrics.ColumnPadding,
                GameMenuMetrics.ColumnPadding,
                GameMenuMetrics.ColumnPadding,
                GameMenuMetrics.ColumnPadding);
            layout.spacing = GameMenuMetrics.ColumnSpacing;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        /// <summary>
        /// Кнопка меню прежней плашкой: пользователь просил сохранить нынешнее расположение,
        /// поэтому кадр-табличка 2:1 здесь не применяется. Подпись берётся из каталога строк,
        /// имя объекта — та же подпись, по ней ищут кнопку PlayMode-тесты.
        /// </summary>
        /// <param name="ui">Фабрика элементов интерфейса.</param>
        /// <param name="texts">Каталог текстов игры.</param>
        /// <param name="parent">Родитель кнопки — колонка меню.</param>
        /// <param name="key">Ключ подписи в каталоге.</param>
        /// <returns>Готовая кнопка.</returns>
        private static Button CreatePlaqueButton(
            UiFactory ui,
            IGameTexts texts,
            Transform parent,
            UiTextKey key)
        {
            var button = ui.CreateButton(parent, UiText.Get(texts, key));
            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                throw new InvalidOperationException("У кнопки меню игры нет подписи");
            }

            label.fontSize = GameMenuMetrics.ButtonLabelSize;
            Localize(label, key);

            var element = button.GetComponent<LayoutElement>();
            element.preferredHeight = GameMenuMetrics.ButtonHeight;
            element.minHeight = GameMenuMetrics.ButtonHeight;
            element.flexibleHeight = 0f;
            return button;
        }

        private static void SetPreferredHeight(GameObject target, float height)
        {
            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.AddComponent<LayoutElement>();
            }

            element.preferredHeight = height;
            element.minHeight = height;
            element.flexibleHeight = 0f;
        }

        private static void AssignGameMenuToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "gameMenuScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
