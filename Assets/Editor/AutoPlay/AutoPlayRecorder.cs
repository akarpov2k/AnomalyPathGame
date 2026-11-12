#nullable enable
using System;
using System.IO;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Encoder;
using UnityEditor.Recorder.Input;
using UnityEngine;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Запись прогона в MP4 через Unity Recorder: кадры Game View, 1280×720, 30 кадров
    /// в секунду, умеренный битрейт. Ошибка запуска не роняет прогон: он продолжается
    /// без видео, а причина пишется в журнал.
    /// </summary>
    internal sealed class AutoPlayRecorder
    {
        private readonly AutoPlayLog _log;
        private RecorderController? _controller;
        private MovieRecorderSettings? _movie;

        /// <summary>Создаёт обёртку записи.</summary>
        /// <param name="log">Журнал прогона.</param>
        public AutoPlayRecorder(AutoPlayLog log)
        {
            _log = log;
        }

        /// <summary>Путь итогового видео без расширения.</summary>
        public string OutputPath { get; private set; } = string.Empty;

        /// <summary>Идёт ли запись.</summary>
        public bool IsRecording => _controller != null && _controller.IsRecording();

        /// <summary>Запускает запись в указанный файл.</summary>
        /// <param name="pathWithoutExtension">Путь видео без расширения.</param>
        /// <returns><c>true</c>, если запись началась.</returns>
        public bool Start(string pathWithoutExtension)
        {
            OutputPath = pathWithoutExtension;
            try
            {
                var settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
                settings.SetRecordModeToManual();
                settings.FrameRate = 30f;
                settings.CapFrameRate = true;

                _movie = ScriptableObject.CreateInstance<MovieRecorderSettings>();
                _movie.name = "AutoPlayStory";
                _movie.Enabled = true;
                _movie.OutputFile = pathWithoutExtension;
                _movie.ImageInputSettings = new GameViewInputSettings
                {
                    OutputWidth = 1280,
                    OutputHeight = 720
                };
                _movie.CaptureAudio = false;
                _movie.EncoderSettings = new CoreEncoderSettings
                {
                    TargetBitRate = 3f,
                    EncodingQuality = CoreEncoderSettings.VideoEncodingQuality.Medium
                };

                settings.AddRecorderSettings(_movie);
                _controller = new RecorderController(settings);
                _controller.PrepareRecording();
                if (!_controller.StartRecording())
                {
                    _log.Warn("Recorder не начал запись; прогон продолжается без видео");
                    return false;
                }

                _log.Step("запись видео: " + pathWithoutExtension + ".mp4");
                return true;
            }
            catch (Exception error)
            {
                _log.Warn("видео не записывается: " + error.GetType().Name + ": " + error.Message);
                _controller = null;
                return false;
            }
        }

        /// <summary>Останавливает запись и дожидается записи файла на диск.</summary>
        public void Stop()
        {
            if (_controller == null)
            {
                return;
            }

            try
            {
                _controller.StopRecording();
                var deadline = DateTime.UtcNow.AddSeconds(60);
                while (_controller.IsRecording() && DateTime.UtcNow < deadline)
                {
                    System.Threading.Thread.Sleep(100);
                }

                _log.Step("запись видео остановлена");
            }
            catch (Exception error)
            {
                _log.Warn("не удалось корректно остановить запись: " + error.Message);
            }
            finally
            {
                _controller = null;
                _movie = null;
            }
        }

        /// <summary>Проверяет, что файл видео появился и не пуст.</summary>
        /// <param name="size">Размер файла в байтах.</param>
        /// <returns><c>true</c>, если файл записан.</returns>
        public bool TryGetFileSize(out long size)
        {
            size = 0;
            var file = OutputPath + ".mp4";
            if (!File.Exists(file))
            {
                return false;
            }

            size = new FileInfo(file).Length;
            return size > 0;
        }
    }
}
