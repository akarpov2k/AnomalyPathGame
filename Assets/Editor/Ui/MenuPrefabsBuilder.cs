#nullable enable
using System;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префабы стартового меню, экранов загрузки, сохранения, настроек и настройки клавиш
    /// по прототипам и привязывает их к сцене Main. Префаб после сборки — источник истины: дальше
    /// разметка и кнопки правятся в инспекторе, а не здесь.
    /// </summary>
    public static partial class MenuPrefabsBuilder
    {
        private const string MainMenuPrefabPath = "Assets/Prefabs/Screens/MainMenuScreen.prefab";
        private const string LoadPrefabPath = "Assets/Prefabs/Screens/LoadGameScreen.prefab";
        private const string SavePrefabPath = "Assets/Prefabs/Screens/SaveGameScreen.prefab";
        private const string SettingsPrefabPath = "Assets/Prefabs/Screens/SettingsScreen.prefab";
        private const string KeyBindingsPrefabPath = "Assets/Prefabs/Screens/KeyBindingsScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string DataRoot = "Assets/Data";

        /// <summary>Пункт меню: собрать префабы меню, загрузки, сохранения и настроек и привязать их к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префабы «Меню, Загрузка, Сохранение, Настройки»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима.</summary>
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

        /// <summary>Собирает префабы пяти экранов и назначает их в сцене Main.</summary>
        public static void Build()
        {
            var services = LoadServices();
            var texts = services.Resolve<IGameTexts>();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var ui = new UiFactory(UiFactory.FindFont(), ButtonArtAssets.Normal(), ButtonArtAssets.Highlighted());
            var prefabs = new[]
            {
                SavePrefab(BuildMainMenu(ui, texts), MainMenuPrefabPath),
                SavePrefab(BuildLoadScreen(ui, texts), LoadPrefabPath),
                SavePrefab(BuildSaveScreen(ui, texts), SavePrefabPath),
                SavePrefab(BuildSettingsScreen(ui, texts), SettingsPrefabPath),
                SavePrefab(BuildKeyBindingsScreen(ui, texts), KeyBindingsPrefabPath)
            };

            AssetDatabase.SaveAssets();
            AssignToScene(prefabs);
            Debug.Log("AnomalyPath: префабы меню, загрузки, сохранения и настроек собраны");
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

        private static GameObject SavePrefab(GameObject root, string path)
        {
            ButtonArtAssets.ApplyPlatesToFlatButtons(root);
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Не удалось сохранить префаб {path}");
            }

            return prefab;
        }

        private static void AssignToScene(GameObject[] prefabs)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "mainMenuScreenPrefab", prefabs[0]);
            SetObject(screen, "loadScreenPrefab", prefabs[1]);
            SetObject(screen, "saveScreenPrefab", prefabs[2]);
            SetObject(screen, "settingsScreenPrefab", prefabs[3]);
            SetObject(screen, "keyBindingsScreenPrefab", prefabs[4]);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
