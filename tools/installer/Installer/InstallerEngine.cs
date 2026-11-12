#nullable enable
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Распаковка поставки в выбранную папку: снимает архив из ресурса, пишет файлы
    /// с отчётом о прогрессе, кладёт <c>Uninstall.exe</c>, создаёт ярлыки и запись в реестре.
    /// Работает в фоновом потоке; окно читает свойства движка по таймеру.
    /// </summary>
    internal sealed class InstallerEngine
    {
        private readonly byte[] _payload;
        private readonly byte[] _uninstaller;
        private readonly string _installDir;
        private readonly bool _desktopShortcut;
        private readonly InstallInfo _info;

        private Thread? _thread;
        private volatile bool _cancel;
        private long _totalBytes = 1;
        private long _doneBytes;
        private int _fileNumber;
        private string _status = string.Empty;
        private string _currentFile = string.Empty;
        private Exception? _error;
        private string _warning = string.Empty;
        private bool _finished;
        private bool _canceled;
        private bool _createdDirectory;

        /// <summary>Создаёт движок установки.</summary>
        /// <param name="payload">Архив с файлами игры.</param>
        /// <param name="uninstaller">Исполняемый файл удаления.</param>
        /// <param name="installDir">Папка установки.</param>
        /// <param name="desktopShortcut">Создавать ярлык на рабочем столе.</param>
        /// <param name="info">Данные поставки.</param>
        public InstallerEngine(byte[] payload, byte[] uninstaller, string installDir, bool desktopShortcut, InstallInfo info)
        {
            _payload = payload;
            _uninstaller = uninstaller;
            _installDir = Path.GetFullPath(installDir);
            _desktopShortcut = desktopShortcut;
            _info = info;
        }

        /// <summary>Текущий этап установки.</summary>
        public string Status => _status;

        /// <summary>Файл, который распаковывается сейчас.</summary>
        public string CurrentFile => _currentFile;

        /// <summary>Всего байт к распаковке.</summary>
        public long TotalBytes => Interlocked.Read(ref _totalBytes);

        /// <summary>Сколько байт уже распаковано.</summary>
        public long DoneBytes => Interlocked.Read(ref _doneBytes);

        /// <summary>Номер текущего файла.</summary>
        public int FileNumber => Volatile.Read(ref _fileNumber);

        /// <summary>Работа завершена (успешно, с ошибкой или отменена).</summary>
        public bool Finished => Volatile.Read(ref _finished);

        /// <summary>Установка отменена пользователем.</summary>
        public bool Canceled => Volatile.Read(ref _canceled);

        /// <summary>Ошибка установки; <c>null</c>, если всё прошло успешно.</summary>
        public Exception? Error => _error;

        /// <summary>Некритичные замечания (ярлык, запись в реестре), собранные при установке.</summary>
        public string Warning => _warning;

        /// <summary>Папка установки.</summary>
        public string InstallDir => _installDir;

        /// <summary>Путь к исполняемому файлу игры.</summary>
        public string GameExecutable => Path.Combine(_installDir, _info.ExeName);

        /// <summary>Считает объём поставки и проверяет свободное место на диске.</summary>
        /// <exception cref="InvalidDataException">Архив пуст.</exception>
        /// <exception cref="IOException">На диске мало места.</exception>
        public void Prepare()
        {
            long total = 0;
            int files = 0;
            using (var archive = new ZipArchive(new MemoryStream(_payload, writable: false), ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    if (entry.Name.Length == 0)
                    {
                        continue;
                    }

                    total += entry.Length;
                    files++;
                }
            }

            if (files == 0)
            {
                throw new InvalidDataException("архив поставки пуст");
            }

            Interlocked.Exchange(ref _totalBytes, Math.Max(total, 1));

            var root = Path.GetPathRoot(_installDir);
            if (!string.IsNullOrEmpty(root))
            {
                var drive = new DriveInfo(root);
                long required = total + (64L * 1024 * 1024);
                if (drive.IsReady && drive.AvailableFreeSpace < required)
                {
                    throw new IOException("на диске " + root + " недостаточно места: нужно " +
                                          FormatSize(required) + ", свободно " + FormatSize(drive.AvailableFreeSpace));
                }
            }
        }

        /// <summary>Запускает установку в фоновом потоке.</summary>
        public void Start()
        {
            _thread = new Thread(Run) { IsBackground = true, Name = "AnomalyPathSetup" };
            _thread.Start();
        }

        /// <summary>Просит прервать установку; распаковка останавливается на ближайшем блоке.</summary>
        public void Cancel()
        {
            _cancel = true;
        }

        /// <summary>Форматирует размер для подписей установщика.</summary>
        /// <param name="bytes">Размер в байтах.</param>
        /// <returns>Строка вида <c>108,6 МБ</c>.</returns>
        public static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024)
            {
                return (bytes / (1024d * 1024 * 1024)).ToString("0.0") + " ГБ";
            }

            if (bytes >= 1024L * 1024)
            {
                return (bytes / (1024d * 1024)).ToString("0.0") + " МБ";
            }

            return Math.Max(1, bytes / 1024) + " КБ";
        }

        private void Run()
        {
            try
            {
                _status = "Распаковка файлов";
                Extract();
                if (_cancel)
                {
                    CleanupAfterCancel();
                    return;
                }

                _status = "Создание ярлыков";
                WriteUninstaller();
                _status = "Регистрация в системе";
                TryRegister();
                TryCreateShortcuts();
                _status = "Установка завершена";
            }
            catch (Exception exception)
            {
                _error = exception;
                CleanupAfterCancel();
            }
            finally
            {
                Volatile.Write(ref _finished, true);
            }
        }

        private const string UninstallerName = "Uninstall.exe";

        private void Extract()
        {
            if (!Directory.Exists(_installDir))
            {
                Directory.CreateDirectory(_installDir);
                _createdDirectory = true;
            }

            using var archive = new ZipArchive(new MemoryStream(_payload, writable: false), ZipArchiveMode.Read);
            var buffer = new byte[1024 * 1024];
            foreach (var entry in archive.Entries)
            {
                if (_cancel)
                {
                    return;
                }

                if (entry.Name.Length == 0)
                {
                    continue;
                }

                string relative = entry.FullName.Replace('/', Path.DirectorySeparatorChar);
                string destination = Path.GetFullPath(Path.Combine(_installDir, relative));
                if (!destination.StartsWith(_installDir + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException("недопустимый путь в архиве: " + entry.FullName);
                }

                _currentFile = entry.FullName;
                var directory = Path.GetDirectoryName(destination);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                using (var source = entry.Open())
                using (var target = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, buffer.Length))
                {
                    int read;
                    while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        if (_cancel)
                        {
                            return;
                        }

                        target.Write(buffer, 0, read);
                        Interlocked.Add(ref _doneBytes, read);
                    }
                }

                Interlocked.Increment(ref _fileNumber);
            }
        }

        private void WriteUninstaller()
        {
            File.WriteAllBytes(Path.Combine(_installDir, UninstallerName), _uninstaller);
        }

        private void TryCreateShortcuts()
        {
            string target = GameExecutable;
            string description = _info.DisplayName;
            try
            {
                Shortcuts.Create(Shortcuts.StartMenu(_info.App), target, _installDir, description);
                if (_desktopShortcut)
                {
                    Shortcuts.Create(Shortcuts.Desktop(_info.App), target, _installDir, description);
                }
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException ||
                                              exception is InvalidOperationException || exception is System.Runtime.InteropServices.COMException)
            {
                AddWarning("ярлыки не созданы: " + exception.Message);
            }
        }

        private void TryRegister()
        {
            try
            {
                InstallRegistry.Register(_info, _installDir, Path.Combine(_installDir, UninstallerName), GameExecutable);
            }
            catch (Exception exception) when (exception is UnauthorizedAccessException || exception is System.Security.SecurityException ||
                                              exception is InvalidOperationException)
            {
                AddWarning("запись в список программ не создана: " + exception.Message);
            }
        }

        private void AddWarning(string message)
        {
            _warning = string.IsNullOrEmpty(_warning) ? message : _warning + "; " + message;
        }

        private void CleanupAfterCancel()
        {
            bool canceled = _cancel;
            Volatile.Write(ref _canceled, canceled);
            if (!canceled)
            {
                return;
            }

            try
            {
                if (_createdDirectory && Directory.Exists(_installDir))
                {
                    Directory.Delete(_installDir, recursive: true);
                }
            }
            catch (IOException)
            {
                // Недописанные файлы останутся до ручной уборки; установка уже прервана.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
