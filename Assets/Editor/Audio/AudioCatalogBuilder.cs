#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Editor.Art;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnomalyPath.Editor.Audio
{
    /// <summary>
    /// Собирает каталог звуков из <c>Assets/Audio</c>, кладёт его ассетом и назначает на <c>GameScreen</c>
    /// в сцене Main. Id записи — имя файла без расширения; эмбиенты и музыка лежат в подпапках.
    /// </summary>
    public static class AudioCatalogBuilder
    {
        /// <summary>Корень звуков.</summary>
        public const string AudioRoot = "Assets/Audio";

        /// <summary>Папка эмбиентов локаций.</summary>
        public const string AmbienceFolder = "Assets/Audio/Ambience";

        /// <summary>Папка музыки: меню и бой.</summary>
        public const string MusicFolder = "Assets/Audio/Music";

        /// <summary>Путь к ассету каталога.</summary>
        public const string CatalogPath = "Assets/Audio/AudioCatalog.asset";

        private const string GameScreenCatalogField = "audioCatalog";
        private const string GameScreenDirectorField = "audioDirector";

        private static readonly string[] Extensions = { ".mp3", ".wav", ".ogg" };

        /// <summary>Пересобирает каталог, настраивает импорт звуков и назначает каталог в сцену.</summary>
        [MenuItem("Tools/AnomalyPath/Собрать каталог звуков")]
        public static void Rebuild()
        {
            EnsureFolders();
            EnsureImporters();

            var entries = CollectEntries();
            var catalog = LoadOrCreateCatalog();
            catalog.SetEntries(entries);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            WireScene();
            ReportMissing(entries);

            AssetDatabase.Refresh();
            Debug.Log($"AnomalyPath: каталог звуков собран — {entries.Count} записей: {CatalogPath}");
        }

        /// <summary>Создаёт папки звуков, если их нет.</summary>
        internal static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder(AudioRoot))
            {
                AssetDatabase.CreateFolder("Assets", "Audio");
            }

            EnsureSubfolder("Ambience");
            EnsureSubfolder("Music");
        }

        /// <summary>Переводит звуки в потоковый импорт: длинные треки не держатся в памяти целиком.</summary>
        internal static void EnsureImporters()
        {
            if (!AssetDatabase.IsValidFolder(AudioRoot))
            {
                return;
            }

            foreach (var file in Directory.EnumerateFiles(AudioRoot, "*.*", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                if (Array.IndexOf(Extensions, Path.GetExtension(path).ToLowerInvariant()) < 0)
                {
                    continue;
                }

                if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
                {
                    continue;
                }

                var changed = false;
                var settings = importer.defaultSampleSettings;
                if (settings.loadType != AudioClipLoadType.Streaming)
                {
                    settings.loadType = AudioClipLoadType.Streaming;
                    changed = true;
                }

                if (settings.compressionFormat != AudioCompressionFormat.Vorbis)
                {
                    settings.compressionFormat = AudioCompressionFormat.Vorbis;
                    changed = true;
                }

                if (settings.preloadAudioData)
                {
                    settings.preloadAudioData = false;
                    changed = true;
                }

                if (importer.loadInBackground)
                {
                    importer.loadInBackground = false;
                    changed = true;
                }

                if (importer.forceToMono)
                {
                    importer.forceToMono = false;
                    changed = true;
                }

                if (changed)
                {
                    importer.defaultSampleSettings = settings;
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>Собирает записи каталога: все звуки из <c>Assets/Audio</c> по имени файла.</summary>
        /// <returns>Записи в порядке id.</returns>
        internal static List<AudioCatalog.Entry> CollectEntries()
        {
            var entries = new List<AudioCatalog.Entry>();
            if (!AssetDatabase.IsValidFolder(AudioRoot))
            {
                return entries;
            }

            foreach (var file in Directory.EnumerateFiles(AudioRoot, "*.*", SearchOption.AllDirectories))
            {
                var path = file.Replace('\\', '/');
                if (Array.IndexOf(Extensions, Path.GetExtension(path).ToLowerInvariant()) < 0)
                {
                    continue;
                }

                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip == null)
                {
                    continue;
                }

                entries.Add(new AudioCatalog.Entry(Path.GetFileNameWithoutExtension(path), clip));
            }

            entries.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return entries;
        }

        private static void EnsureSubfolder(string name)
        {
            var path = AudioRoot + "/" + name;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(AudioRoot, name);
            }
        }

        private static AudioCatalog LoadOrCreateCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog != null)
            {
                return catalog;
            }

            catalog = ScriptableObject.CreateInstance<AudioCatalog>();
            AssetDatabase.CreateAsset(catalog, CatalogPath);
            return catalog;
        }

        private static void WireScene()
        {
            var scenePath = Path.GetFullPath(SetupMainScene.MainScenePath);
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"AnomalyPath: сцена {scenePath} не найдена, каталог звуков не назначен");
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                Debug.LogWarning("AnomalyPath: сцена не сохранена, каталог звуков не назначен");
                return;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            if (screen == null)
            {
                Debug.LogWarning("AnomalyPath: на сцене Main нет GameScreen");
                return;
            }

            var catalog = AssetDatabase.LoadAssetAtPath<AudioCatalog>(CatalogPath);
            if (catalog == null)
            {
                Debug.LogWarning($"AnomalyPath: каталог звуков не загружен из {CatalogPath}");
                return;
            }

            var guidAvailable = AssetDatabase.TryGetGUIDAndLocalFileIdentifier(catalog, out var guid, out _);
            var director = SetupMainScene.EnsureAudio(screen.gameObject);
            var serialized = new SerializedObject(screen);
            var catalogProperty = serialized.FindProperty(GameScreenCatalogField);
            var directorProperty = serialized.FindProperty(GameScreenDirectorField);
            if (catalogProperty == null || directorProperty == null)
            {
                Debug.LogWarning("AnomalyPath: у GameScreen нет полей каталога звуков и слоя звука");
                return;
            }

            catalogProperty.objectReferenceValue = catalog;
            directorProperty.objectReferenceValue = director;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            var saved = EditorSceneManager.SaveScene(scene, scenePath);
            var written = saved && guidAvailable && File.ReadAllText(scenePath).Contains(guid, StringComparison.Ordinal);
            if (written)
            {
                Debug.Log($"AnomalyPath: каталог звуков назначен на GameScreen в {SetupMainScene.MainScenePath}");
            }
            else
            {
                Debug.LogWarning(
                    $"AnomalyPath: каталог звуков не записался в сцену {SetupMainScene.MainScenePath} — " +
                    $"saved={saved}, guid={guidAvailable}, " +
                    "назначьте поле audioCatalog на GameScreen вручную");
            }
        }

        private static void ReportMissing(List<AudioCatalog.Entry> entries)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                ids.Add(entry.Id);
            }

            var missing = new List<string>();
            var reported = new HashSet<string>(StringComparer.Ordinal);
            void Report(string id)
            {
                if (!ids.Contains(id) && reported.Add(id))
                {
                    missing.Add(id);
                }
            }

            foreach (var ambience in DataArtIds.Ambience())
            {
                Report(ambience);
            }

            foreach (var music in DataArtIds.Music())
            {
                Report(music);
            }

            if (missing.Count > 0)
            {
                Debug.LogWarning(
                    "AnomalyPath: нет звуков для id: " + string.Join(", ", missing) +
                    ". Каталог собирается только из файлов Assets/Audio — положите mp3 и пересоберите каталог.");
            }
        }
    }
}
