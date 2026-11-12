#nullable enable
using System;
using System.IO;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Уборка установленной игры: ярлыки, запись в реестре и файлы папки.
    /// Используется и окном удаления, и беззвучным режимом <c>/silent</c>.
    /// </summary>
    internal static class DeletionEngine
    {
        /// <summary>Удаляет установку целиком.</summary>
        /// <param name="info">Данные поставки.</param>
        /// <param name="installDir">Папка установки.</param>
        /// <param name="report">Сообщает число удалённых файлов из общего и текущий файл.</param>
        public static void Execute(InstallInfo info, string installDir, Action<int, int, string> report)
        {
            Shortcuts.Delete(Shortcuts.StartMenu(info.App));
            Shortcuts.Delete(Shortcuts.Desktop(info.App));
            InstallRegistry.Unregister(info.App);

            if (!Directory.Exists(installDir))
            {
                report(0, 0, installDir);
                return;
            }

            var files = Directory.GetFiles(installDir, "*", SearchOption.AllDirectories);
            int total = files.Length;
            int done = 0;
            report(done, total, installDir);

            foreach (string file in files)
            {
                DeleteFile(file);
                done++;
                report(done, total, file);
            }

            DeleteFolder(installDir);
        }

        private static void DeleteFile(string file)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                try
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                    return;
                }
                catch (IOException)
                {
                    // Файл может быть занят программой удаления, которая только что запустила копию.
                }
                catch (UnauthorizedAccessException)
                {
                    return;
                }

                System.Threading.Thread.Sleep(150);
            }
        }

        private static void DeleteFolder(string folder)
        {
            for (int attempt = 0; attempt < 8; attempt++)
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                    return;
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                    return;
                }

                System.Threading.Thread.Sleep(200);
            }
        }
    }
}
