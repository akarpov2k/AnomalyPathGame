#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Запуск автопрогона сюжета главы 1 на настоящей сцене Main: открывает сцену,
    /// включает Play Mode, запускает запись MP4 и ведёт автопилот. Запускается из меню
    /// <c>Tools/AnomalyPath/Автопрогон сюжета (запись видео)</c> или из командной строки
    /// флагом <c>-autoplayStory</c> (без <c>-batchmode</c>: запись снимает Game View).
    /// </summary>
    [InitializeOnLoad]
    public static class AutoPlaythrough
    {
        /// <summary>Флаг командной строки для автоматического запуска.</summary>
        public const string CommandLineFlag = "-autoplayStory";

        private const string PendingKey = "AnomalyPath.AutoPlay.Pending";
        private const string OutputKey = "AnomalyPath.AutoPlay.Output";
        private const string StartedKey = "AnomalyPath.AutoPlay.Started";
        private const string CommandLineKey = "AnomalyPath.AutoPlay.CommandLine";
        private const string LogPath = "Logs/AutoPlay/story-run.log";

        private static AutoPlayDriver? _driver;
        private static AutoPlayRecorder? _recorder;
        private static AutoPlayLog? _log;
        private static bool _started;
        private static bool _fromCommandLine;
        private static double _nextStartAttempt;

        static AutoPlaythrough()
        {
            EditorApplication.update += OnUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            if (FromCommandLine() && !SessionState.GetBool(AutoStartedKey, false))
            {
                EditorApplication.delayCall += TryAutoStart;
            }
        }

        private const string AutoStartedKey = "AnomalyPath.AutoPlay.AutoStarted";

        private static void TryAutoStart()
        {
            if (SessionState.GetBool(AutoStartedKey, false) ||
                SessionState.GetBool(PendingKey, false) ||
                Application.isPlaying ||
                !FromCommandLine())
            {
                return;
            }

            if (EditorApplication.isCompiling ||
                EditorApplication.isUpdating ||
                EditorApplication.timeSinceStartup < 10)
            {
                EditorApplication.delayCall += TryAutoStart;
                return;
            }

            SessionState.SetBool(AutoStartedKey, true);
            Begin(fromCommandLine: true);
        }

        /// <summary>
        /// Точка входа командной строки: запускает прогон после загрузки проекта.
        /// Вызывается флагом <c>-executeMethod AnomalyPath.Editor.AutoPlay.AutoPlaythrough.RunFromCommandLine</c>.
        /// </summary>
        public static void RunFromCommandLine()
        {
            Begin(fromCommandLine: true);
        }

        /// <summary>Пункт меню: запустить прогон из редактора.</summary>
        [MenuItem("Tools/AnomalyPath/Автопрогон сюжета (запись видео)")]
        public static void RunFromMenu()
        {
            Begin(fromCommandLine: false);
        }

        private static void OnUpdate()
        {
            if (_started)
            {
                if (_driver == null)
                {
                    return;
                }

                _driver.Tick();
                if (_driver.IsFinished)
                {
                    CompleteRun();
                }

                return;
            }

            if (!SessionState.GetBool(PendingKey, false) || !Application.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup < _nextStartAttempt)
            {
                return;
            }

            _nextStartAttempt = EditorApplication.timeSinceStartup + 0.5;
            StartRun();
        }

        private static void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.EnteredPlayMode &&
                SessionState.GetBool(PendingKey, false) &&
                !SessionState.GetBool(StartedKey, false))
            {
                _nextStartAttempt = EditorApplication.timeSinceStartup + 1.0;
            }
        }

        private static bool FromCommandLine() =>
            Environment.GetCommandLineArgs().Any(argument =>
                string.Equals(argument, CommandLineFlag, StringComparison.OrdinalIgnoreCase));

        private static void Begin(bool fromCommandLine)
        {
            _fromCommandLine = fromCommandLine;
            SessionState.SetBool(CommandLineKey, fromCommandLine);
            if (Application.isPlaying)
            {
                Debug.LogWarning("[AutoPlay] Сначала выйдите из Play Mode.");
                return;
            }

            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += () => Begin(fromCommandLine);
                return;
            }

            if (!File.Exists(AutoPlayIds.MainScenePath))
            {
                Debug.LogError("[AutoPlay] Нет сцены " + AutoPlayIds.MainScenePath);
                ExitEditor(1);
                return;
            }

            if (!string.Equals(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene().path,
                    AutoPlayIds.MainScenePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                EditorSceneManager.OpenScene(AutoPlayIds.MainScenePath, OpenSceneMode.Single);
            }

            // Окно Game View должно быть открыто: Recorder снимает именно его.
            EditorApplication.ExecuteMenuItem("Window/General/Game");

            Directory.CreateDirectory("Logs/AutoPlay");
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var output = "Logs/AutoPlay/story-" + stamp;
            File.WriteAllText(LogPath, string.Empty);

            SessionState.SetBool(PendingKey, true);
            SessionState.SetBool(StartedKey, false);
            SessionState.SetString(OutputKey, output);
            _log = new AutoPlayLog(LogPath);
            _log.Step("открыта сцена " + AutoPlayIds.MainScenePath + ", включён Play Mode");
            _started = false;
            _driver = null;
            EditorApplication.EnterPlaymode();
        }

        private static void StartRun()
        {
            if (SessionState.GetBool(StartedKey, false))
            {
                return;
            }

            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null || screen.Rewards == null || GameObject.Find("MainMenu") == null)
            {
                return;
            }

            if (!Application.isPlaying)
            {
                return;
            }

            SessionState.SetBool(StartedKey, true);
            _started = true;
            EditorApplication.LockReloadAssemblies();
            Application.runInBackground = true;
            Time.timeScale = 1f;
            _log ??= new AutoPlayLog(LogPath);

            var output = SessionState.GetString(OutputKey, "Logs/AutoPlay/story-" + DateTime.Now.ToString("yyyyMMdd-HHmmss"));
            _recorder = new AutoPlayRecorder(_log);
            _recorder.Start(output);
            _driver = new AutoPlayDriver(_log, OnDriverFinished);
        }

        private static void OnDriverFinished(string result)
        {
            // Завершение обрабатывается в OnUpdate, чтобы не останавливать запись из тика.
            _finishReason = result;
        }

        private static string? _finishReason;

        private static void CompleteRun()
        {
            if (!_started || _driver == null)
            {
                return;
            }

            _started = false;
            _driver = null;
            var result = _finishReason ?? "прогон остановлен";
            long size = 0;
            var hasVideo = _recorder != null && _recorder.TryGetFileSize(out size);
            _recorder?.Stop();
            var stoppedSize = 0L;
            if (_recorder != null && _recorder.TryGetFileSize(out stoppedSize))
            {
                size = stoppedSize;
                hasVideo = true;
            }

            _recorder = null;
            SessionState.SetBool(PendingKey, false);
            SessionState.SetBool(StartedKey, false);
            EditorApplication.UnlockReloadAssemblies();

            _log?.Step("итог: " + result + (hasVideo ? ", видео " + size + " Б" : ", видео нет"));
            if (SessionState.GetBool(CommandLineKey, false))
            {
                WriteResult(result, hasVideo, size);
                EditorApplication.Exit(hasVideo && result == "глава 1 пройдена" ? 0 : 1);
                return;
            }

            EditorApplication.ExitPlaymode();
            Debug.Log("[AutoPlay] Прогон завершён: " + result + (hasVideo ? ", видео есть" : ", видео нет"));
        }

        private static void WriteResult(string result, bool hasVideo, long size)
        {
            try
            {
                var report = "result=" + result + Environment.NewLine +
                             "video=" + (hasVideo ? size.ToString() : "none") + Environment.NewLine;
                File.WriteAllText("Logs/AutoPlay/result.txt", report);
            }
            catch (Exception error)
            {
                Debug.LogWarning("[AutoPlay] Отчёт не записан: " + error.Message);
            }
        }

        private static void ExitEditor(int code)
        {
            if (_fromCommandLine)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
