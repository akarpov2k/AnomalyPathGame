#nullable enable
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Точка входа установщика. Режимы: обычный (окно с постером), беззвучный
    /// (<c>/silent /dir &lt;папка&gt;</c>) и снимок окна (<c>/shot &lt;png&gt;</c>) для проверки вёрстки.
    /// </summary>
    internal static class SetupProgram
    {
        /// <summary>Запускает установщик.</summary>
        /// <param name="args">Аргументы командной строки.</param>
        /// <returns>Код возврата: 0 — успех, 1 — ошибка.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                if (HasFlag(args, "/selftest"))
                {
                    return RunSelfTest();
                }

                if (HasFlag(args, "/silent") || HasFlag(args, "-silent"))
                {
                    return RunSilent(args);
                }

                if (TryGetValue(args, "/shot", out var shotPath))
                {
                    return RunScreenshot(args, shotPath);
                }

                return RunUi(args);
            }
            catch (Exception exception)
            {
                WriteCrashLog(exception);
                if (args.Length == 0)
                {
                    MessageBox.Show(
                        "Установщик завершился с ошибкой:\n" + exception.Message,
                        "AnomalyPath",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }

                return 1;
            }
        }

        /// <summary>Проверяет встроенные ресурсы без окна и записывает отчёт.</summary>
        /// <returns>Код возврата.</returns>
        private static int RunSelfTest()
        {
            var report = new StringBuilder();
            try
            {
                var info = InstallInfo.Read(EmbeddedResources.Open(EmbeddedResources.Info));
                report.AppendLine("info=" + info.App + " " + info.Version + " " + info.Publisher + " files=" + info.FileCount);
                report.AppendLine("payload=" + EmbeddedResources.ReadAll(EmbeddedResources.Payload).Length);
                report.AppendLine("uninstaller=" + EmbeddedResources.ReadAll(EmbeddedResources.Uninstaller).Length);
                using var poster = PosterArt.FromStream(EmbeddedResources.Open(EmbeddedResources.Poster));
                using var probe = new Bitmap(64, 64);
                using (var graphics = Graphics.FromImage(probe))
                {
                    poster.DrawCover(graphics, new Rectangle(0, 0, 64, 64));
                }

                report.AppendLine("poster=ok");
                report.AppendLine("ok=true");
                File.WriteAllText(SelfTestLog, report.ToString(), new UTF8Encoding(false));
                return 0;
            }
            catch (Exception exception)
            {
                report.AppendLine("error=" + exception);
                File.WriteAllText(SelfTestLog, report.ToString(), new UTF8Encoding(false));
                return 1;
            }
        }

        private static string SelfTestLog => Path.Combine(Path.GetTempPath(), "AnomalyPath-setup-selftest.log");

        private static void WriteCrashLog(Exception exception)
        {
            try
            {
                File.WriteAllText(
                    Path.Combine(Path.GetTempPath(), "AnomalyPath-setup-error.log"),
                    exception.ToString(),
                    new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // Лог не записался — сообщение об ошибке всё равно выводится в окно.
            }
        }

        private static int RunUi(string[] args)
        {
            var info = InstallInfo.Read(EmbeddedResources.Open(EmbeddedResources.Info));
            using var poster = PosterArt.FromStream(EmbeddedResources.Open(EmbeddedResources.Poster));
            string defaultDir = TryGetValue(args, "/dir", out var requested) ? requested : DefaultDirectory(info);
            using var form = new SetupForm(poster, info, defaultDir);
            Application.Run(form);
            return 0;
        }

        private static int RunScreenshot(string[] args, string path)
        {
            var info = InstallInfo.Read(EmbeddedResources.Open(EmbeddedResources.Info));
            using var poster = PosterArt.FromStream(EmbeddedResources.Open(EmbeddedResources.Poster));
            using var form = new SetupForm(poster, info, DefaultDirectory(info))
            {
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.Manual,
                Location = new Point(-4000, -4000)
            };

            form.Show();
            Application.DoEvents();

            string phase = TryGetValue(args, "/phase", out var requested) ? requested : "welcome";
            if (phase.Equals("installing", StringComparison.OrdinalIgnoreCase))
            {
                float fraction = TryGetValue(args, "/progress", out var raw) &&
                                 float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                    ? parsed / 100f
                    : 0.37f;
                form.PreviewInstalling(fraction, "Распаковка файлов");
            }
            else if (phase.Equals("done", StringComparison.OrdinalIgnoreCase))
            {
                form.PreviewDone();
            }

            Application.DoEvents();
            var size = form.ClientSize;
            using var bitmap = new Bitmap(size.Width, size.Height);
            form.DrawToBitmap(bitmap, new Rectangle(0, 0, size.Width, size.Height));
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            bitmap.Save(path, ImageFormat.Png);
            form.Close();
            return 0;
        }

        private static int RunSilent(string[] args)
        {
            var info = InstallInfo.Read(EmbeddedResources.Open(EmbeddedResources.Info));
            string dir = TryGetValue(args, "/dir", out var requested) ? requested : DefaultDirectory(info);
            string logPath = TryGetValue(args, "/log", out var logFile) ? logFile : Path.Combine(Path.GetTempPath(), "AnomalyPath-setup.log");
            bool desktopShortcut = !HasFlag(args, "/noshortcut");

            var engine = new InstallerEngine(
                EmbeddedResources.ReadAll(EmbeddedResources.Payload),
                EmbeddedResources.ReadAll(EmbeddedResources.Uninstaller),
                dir,
                desktopShortcut,
                info);

            var log = new StringBuilder();
            log.AppendLine("installDir=" + engine.InstallDir);
            try
            {
                engine.Prepare();
                engine.Start();
                while (!engine.Finished)
                {
                    Thread.Sleep(50);
                }

                if (engine.Error != null)
                {
                    log.AppendLine("error=" + engine.Error.Message);
                    File.WriteAllText(logPath, log.ToString(), new UTF8Encoding(true));
                    return 1;
                }

                log.AppendLine("files=" + engine.FileNumber);
                if (!string.IsNullOrEmpty(engine.Warning))
                {
                    log.AppendLine("warning=" + engine.Warning);
                }

                log.AppendLine("ok=true");
                File.WriteAllText(logPath, log.ToString(), new UTF8Encoding(true));
                return 0;
            }
            catch (Exception exception)
            {
                log.AppendLine("error=" + exception.Message);
                File.WriteAllText(logPath, log.ToString(), new UTF8Encoding(true));
                return 1;
            }
        }

        private static string DefaultDirectory(InstallInfo info)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                info.App);
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
