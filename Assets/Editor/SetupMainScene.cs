#nullable enable
using System.IO;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnomalyPath.Editor
{
    /// <summary>Создаёт стартовую сцену с загрузчиком и экраном меню.</summary>
    public static class SetupMainScene
    {
        /// <summary>Создаёт сцену Main и добавляет её в список сборки.</summary>
        [MenuItem("Tools/AnomalyPath/Setup Main Scene")]
        public static void Setup()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var game = new GameObject("Game");
            game.AddComponent<GameBootstrap>();
            game.AddComponent<GameScreen>();
            EnsureAudio(game);
            CreateCamera();

            if (!Directory.Exists("Assets/Scenes"))
            {
                Directory.CreateDirectory("Assets/Scenes");
            }

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/Main.unity");
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Main.unity", true)
            };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("AnomalyPath: сцена Assets/Scenes/Main.unity создана");
        }

        /// <summary>Добавляет в сцену Main камеру, чтобы Game view не показывал «No cameras rendering».</summary>
        [MenuItem("Tools/AnomalyPath/Добавить камеру в Main")]
        public static void EnsureCameraInMainScene()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Main.unity", OpenSceneMode.Single);
            if (Object.FindAnyObjectByType<Camera>() == null)
            {
                CreateCamera();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log("AnomalyPath: в Assets/Scenes/Main.unity добавлена камера");
                return;
            }

            Debug.Log("AnomalyPath: в Assets/Scenes/Main.unity уже есть камера");
        }

        /// <summary>Добавляет в сцену Main слой звука: слушателя, источники эмбиента и музыки.</summary>
        [MenuItem("Tools/AnomalyPath/Добавить звук в Main")]
        public static void EnsureAudioInMainScene()
        {
            var scene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
            var screen = Object.FindAnyObjectByType<GameScreen>();
            if (screen == null)
            {
                Debug.LogWarning("AnomalyPath: на сцене Main нет GameScreen");
                return;
            }

            EnsureAudio(screen.gameObject);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("AnomalyPath: в Assets/Scenes/Main.unity добавлен слой звука");
        }

        /// <summary>Путь к стартовой сцене.</summary>
        internal const string MainScenePath = "Assets/Scenes/Main.unity";

        /// <summary>
        /// Добавляет на объект игры <see cref="AudioDirector"/> и двух слушателей-источников,
        /// если их ещё нет, и связывает источники с полями директора.
        /// </summary>
        /// <param name="game">Объект с <c>GameScreen</c>.</param>
        /// <returns>Слой звука на объекте игры.</returns>
        internal static AudioDirector EnsureAudio(GameObject game)
        {
            if (game.GetComponent<AudioListener>() == null)
            {
                game.AddComponent<AudioListener>();
            }

            var director = game.GetComponent<AudioDirector>();
            if (director == null)
            {
                director = game.AddComponent<AudioDirector>();
            }

            EnsureSource(game.transform, "Ambience", director, "ambienceSource");
            EnsureSource(game.transform, "Music", director, "musicSource");
            return director;
        }

        private static void EnsureSource(Transform parent, string name, AudioDirector director, string field)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                var host = new GameObject(name);
                host.transform.SetParent(parent, false);
                child = host.transform;
            }

            var source = child.GetComponent<AudioSource>();
            if (source == null)
            {
                source = child.gameObject.AddComponent<AudioSource>();
            }

            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.volume = 0f;

            var serialized = new SerializedObject(director);
            var property = serialized.FindProperty(field);
            if (property != null && property.objectReferenceValue != source)
            {
                property.objectReferenceValue = source;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.cullingMask = 0;
            camera.depth = -1f;
            camera.useOcclusionCulling = false;
        }
    }
}
