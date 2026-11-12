#nullable enable
using System;
using System.IO;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Ярлыки Windows: создаются через COM-объект <c>WScript.Shell</c>, чтобы не тянуть
    /// в установщик интеропы оболочки. Ярлык один на меню «Пуск» и, по выбору, на рабочем столе.
    /// </summary>
    internal static class Shortcuts
    {
        /// <summary>Путь ярлыка в меню «Пуск».</summary>
        /// <param name="app">Короткое имя приложения.</param>
        /// <returns>Полный путь файла <c>.lnk</c>.</returns>
        public static string StartMenu(string app)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), app + ".lnk");
        }

        /// <summary>Путь ярлыка на рабочем столе.</summary>
        /// <param name="app">Короткое имя приложения.</param>
        /// <returns>Полный путь файла <c>.lnk</c>.</returns>
        public static string Desktop(string app)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), app + ".lnk");
        }

        /// <summary>Создаёт или перезаписывает ярлык на исполняемый файл игры.</summary>
        /// <param name="linkPath">Куда положить <c>.lnk</c>.</param>
        /// <param name="target">Исполняемый файл игры.</param>
        /// <param name="workingDirectory">Рабочая папка ярлыка.</param>
        /// <param name="description">Описание во всплывающей подсказке.</param>
        /// <exception cref="InvalidOperationException">Оболочка недоступна.</exception>
        public static void Create(string linkPath, string target, string workingDirectory, string description)
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell")
                            ?? throw new InvalidOperationException("WScript.Shell недоступен");
            dynamic shell = Activator.CreateInstance(shellType);
            dynamic link = shell.CreateShortcut(linkPath);
            link.TargetPath = target;
            link.WorkingDirectory = workingDirectory;
            link.IconLocation = target + ",0";
            link.Description = description;
            link.WindowStyle = 1;
            link.Save();
        }

        /// <summary>Удаляет ярлык, если он существует.</summary>
        /// <param name="linkPath">Путь файла <c>.lnk</c>.</param>
        public static void Delete(string linkPath)
        {
            try
            {
                if (File.Exists(linkPath))
                {
                    File.Delete(linkPath);
                }
            }
            catch (IOException)
            {
                // Занятый ярлык не мешает удалению игры: остаётся только ручная уборка.
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
