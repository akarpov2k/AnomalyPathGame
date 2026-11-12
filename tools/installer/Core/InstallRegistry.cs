#nullable enable
using System;
using Microsoft.Win32;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Запись установленной игры в списке «Программы и компоненты» текущего пользователя
    /// (<c>HKCU\...\Uninstall\AnomalyPath</c>) — там же лежит строка запуска удаления.
    /// </summary>
    internal static class InstallRegistry
    {
        private const string UninstallKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

        /// <summary>Регистрирует установку.</summary>
        /// <param name="info">Данные поставки.</param>
        /// <param name="installDir">Папка установки.</param>
        /// <param name="uninstallerPath">Путь к <c>Uninstall.exe</c>.</param>
        /// <param name="gameExe">Путь к исполняемому файлу игры.</param>
        public static void Register(InstallInfo info, string installDir, string uninstallerPath, string gameExe)
        {
            using var key = Registry.CurrentUser.CreateSubKey(UninstallKey + "\\" + info.App, writable: true);
            if (key == null)
            {
                throw new InvalidOperationException("не удалось открыть ключ реестра установки");
            }

            key.SetValue("DisplayName", info.DisplayName, RegistryValueKind.String);
            key.SetValue("DisplayVersion", info.Version, RegistryValueKind.String);
            key.SetValue("Publisher", info.Publisher, RegistryValueKind.String);
            key.SetValue("InstallLocation", installDir, RegistryValueKind.String);
            key.SetValue("DisplayIcon", gameExe, RegistryValueKind.String);
            key.SetValue("UninstallString", "\"" + uninstallerPath + "\"", RegistryValueKind.String);
            key.SetValue("NoModify", 1, RegistryValueKind.DWord);
            key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
            key.SetValue("EstimatedSize", (int)Math.Max(1, info.Bytes / 1024), RegistryValueKind.DWord);
        }

        /// <summary>Удаляет запись установки.</summary>
        /// <param name="app">Короткое имя приложения.</param>
        public static void Unregister(string app)
        {
            Registry.CurrentUser.DeleteSubKeyTree(UninstallKey + "\\" + app, throwOnMissingSubKey: false);
        }
    }
}
