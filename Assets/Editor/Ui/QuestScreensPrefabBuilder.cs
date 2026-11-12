#nullable enable
using System;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префабы двух экранов заданий по прототипам: доску объявлений хаба
    /// (сетка карточек с картинкой, описанием и кнопками «Принять»/«Отказаться») и журнал
    /// заданий (категории, список и панель описания с кнопкой действия). Оба префаба
    /// назначаются на <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static partial class QuestScreensPrefabBuilder
    {
        private const string BoardPrefabPath = "Assets/Prefabs/Screens/QuestBoardScreen.prefab";
        private const string JournalPrefabPath = "Assets/Prefabs/Screens/QuestJournalScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string CloseIconPath = "Assets/Art/Icons/ui_icon_close.png";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        /// <summary>Ширина окна доски в единицах канваса.</summary>
        private const float BoardWidth = 1161f;

        /// <summary>Высота окна доски.</summary>
        private const float BoardHeight = 631f;

        /// <summary>Ширина окна журнала.</summary>
        private const float JournalWidth = 923f;

        /// <summary>Высота окна журнала.</summary>
        private const float JournalHeight = 854f;

        /// <summary>Пункт меню: собрать префабы доски и журнала и привязать их к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префабы «Доска объявлений» и «Журнал заданий»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима: каталог спрайтов и оба префаба за один запуск.</summary>
        public static void BuildFromBatch()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Точка входа batch-режима: префабы, каталог спрайтов и кадры за один запуск.</summary>
        public static void BuildAndCaptureFromBatch()
        {
            try
            {
                Build();
                QuestScreensScreenshot.CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает оба префаба и назначает их на <c>GameScreen</c>.</summary>
        public static void Build()
        {
            AnomalyPath.Editor.Art.SpriteCatalogBuilder.Rebuild();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var board = SavePrefab(BuildBoard(), BoardPrefabPath);
            var journal = SavePrefab(BuildJournal(), JournalPrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(board, journal);
            Debug.Log("AnomalyPath: префабы доски объявлений и журнала заданий собраны");
        }

        private static void AssignToScene(GameObject board, GameObject journal)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "questBoardScreenPrefab", board);
            SetObject(screen, "questJournalScreenPrefab", journal);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildRoot<T>(string name, string spriteId, out RectTransform content)
            where T : MonoBehaviour, IScreen
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<UnityEngine.UI.Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            ScreenBackdropAssets.Apply(backdrop, spriteId);
            content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);
            var screen = root.AddComponent<T>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }
    }
}
