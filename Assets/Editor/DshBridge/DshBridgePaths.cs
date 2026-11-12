#nullable enable
using System;
using System.IO;
using UnityEngine;

namespace AnomalyPath.Editor.DshBridge
{
    /// <summary>
    /// Пути моста: служебные файлы живут в <c>Library/DshBridge</c> и в git не попадают.
    /// </summary>
    internal static class DshBridgePaths
    {
        /// <summary>Корень проекта (родитель каталога <c>Assets</c>).</summary>
        internal static string ProjectRoot => Path.GetDirectoryName(Application.dataPath) ?? string.Empty;

        /// <summary>Каталог служебных файлов моста.</summary>
        internal static string StoreDirectory => Path.Combine(ProjectRoot, "Library", "DshBridge");

        /// <summary>Маркер включения: файл создаёт MCP-сервер перед запуском редактора.</summary>
        internal static string EnableMarker => Path.Combine(StoreDirectory, "enable");

        /// <summary>Файл с портом и токеном текущего процесса редактора.</summary>
        internal static string EndpointFile => Path.Combine(StoreDirectory, "endpoint.json");

        /// <summary>Каталог скриншотов (в git не попадает).</summary>
        internal static string ScreenshotDirectory => Path.Combine(ProjectRoot, "Logs", "Screenshots");

        /// <summary>
        /// Включён ли мост: маркер <c>Library/DshBridge/enable</c> или переменная окружения
        /// <c>DSH_BRIDGE=1</c>. Без этого редактор не открывает порт.
        /// </summary>
        internal static bool IsEnabled =>
            File.Exists(EnableMarker) ||
            string.Equals(Environment.GetEnvironmentVariable("DSH_BRIDGE"), "1", StringComparison.Ordinal);

        /// <summary>Создаёт служебный каталог, если его нет.</summary>
        internal static void EnsureStore()
        {
            Directory.CreateDirectory(StoreDirectory);
        }
    }
}
