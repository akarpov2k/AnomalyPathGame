#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Точка входа программы удаления. Запуск из папки игры копирует себя во временную папку
    /// и передаёт работу копии — иначе файл не даст удалить ту папку, в которой сам лежит.
    /// </summary>
    internal static class UninstallProgram
    {
        /// <summary>Запускает удаление.</summary>
        /// <param name="args">Аргументы: <c>/run</c> — уже копия во временной папке, <c>/dir</c> — папка игры.</param>
        /// <returns>Код возврата.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            CleanupTemporaryCopies();

            string installDir = TryGetValue(args, "/dir", out var requested)
                ? requested
                : AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
            bool silent = HasFlag(args, "/silent");

            if (!HasFlag(args, "/run"))
            {
                try
                {
                    string temporary = Path.Combine(
                        Path.GetTempPath(),
                        "AnomalyPath-uninstall-" + Guid.NewGuid().ToString("N") + ".exe");
                    File.Copy(Application.ExecutablePath, temporary, overwrite: true);
                    Process.Start(new ProcessStartInfo(temporary, "/run /dir \"" + installDir + "\"" + (silent ? " /silent" : string.Empty))
                    {
                        UseShellExecute = false,
                        WorkingDirectory = Path.GetTempPath()
                    });

                    return 0;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    MessageBox.Show(
                        "Не удалось запустить удаление: " + exception.Message,
                        "AnomalyPath",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                    return 1;
                }
            }

            if (silent)
            {
                try
                {
                    var quietInfo = InstallInfo.Read(EmbeddedResources.Open(EmbeddedResources.Info));
                    if (IsGameRunning(quietInfo))
                    {
                        return 1;
                    }

                    DeletionEngine.Execute(quietInfo, installDir, (_, _, _) => { });
                    return 0;
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    return 1;
                }
            }

            try
            {
                var info = InstallInfo.Read(EmbeddedResources.Open(EmbeddedResources.Info));
                if (IsGameRunning(info))
                {
                    MessageBox.Show(
                        "Игра запущена. Закройте её и повторите удаление.",
                        "AnomalyPath",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return 1;
                }

                using var poster = PosterArt.FromStream(EmbeddedResources.Open(EmbeddedResources.Poster));
                using var form = new UninstallForm(poster, info, installDir);
                Application.Run(form);
            }
            catch (Exception exception)
            {
                MessageBox.Show(
                    "Удаление завершилось с ошибкой:\n" + exception.Message,
                    "AnomalyPath",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Проверяет, запущена ли игра: занятые исполняемые файлы удалить нельзя,
        /// поэтому удаление в этот момент оставило бы половину папки.
        /// </summary>
        private static bool IsGameRunning(InstallInfo info)
        {
            string name = Path.GetFileNameWithoutExtension(info.ExeName);
            try
            {
                return Process.GetProcessesByName(name).Length > 0;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Убирает копии программы удаления, оставшиеся в %TEMP% от прошлых запусков:
        /// каждая копия доживает до конца удаления и сама себя стереть не может.
        /// </summary>
        private static void CleanupTemporaryCopies()
        {
            try
            {
                string pattern = "AnomalyPath-uninstall-*.exe";
                foreach (string file in Directory.GetFiles(Path.GetTempPath(), pattern))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (IOException)
                    {
                        // Копия ещё запущена — её уберёт следующий запуск.
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }
            catch (IOException)
            {
                // Каталог временных файлов недоступен — это не мешает удалению.
            }
        }

        private static bool HasFlag(string[] args, string name)
        {
            foreach (var arg in args)
            {
                if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetValue(string[] args, string name, out string value)
        {
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals(name, StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    value = args[i + 1];
                    return true;
                }
            }

            value = string.Empty;
            return false;
        }
    }
}
