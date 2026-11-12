#nullable enable
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AnomalyPath.Editor.DshBridge
{
    /// <summary>
    /// Описание запущенного моста: порт, токен, pid редактора. Пишется в
    /// <c>Library/DshBridge/endpoint.json</c> при старте и читается MCP-сервером.
    /// </summary>
    internal sealed class DshBridgeEndpoint
    {
        /// <summary>Идентификатор процесса редактора.</summary>
        [JsonProperty("pid")]
        public int Pid { get; set; }

        /// <summary>Порт локального HTTP-моста на 127.0.0.1.</summary>
        [JsonProperty("port")]
        public int Port { get; set; }

        /// <summary>Токен доступа; без него мост отвечает только на <c>/health</c>.</summary>
        [JsonProperty("token")]
        public string Token { get; set; } = string.Empty;

        /// <summary>Версия редактора.</summary>
        [JsonProperty("unityVersion")]
        public string UnityVersion { get; set; } = string.Empty;

        /// <summary>Путь к проекту.</summary>
        [JsonProperty("projectPath")]
        public string ProjectPath { get; set; } = string.Empty;

        /// <summary>Признак пакетного режима.</summary>
        [JsonProperty("batchMode")]
        public bool BatchMode { get; set; }

        /// <summary>Момент запуска моста (ISO 8601).</summary>
        [JsonProperty("startedAt")]
        public string StartedAt { get; set; } = string.Empty;

        /// <summary>Записывает описание на диск (перезапись целиком).</summary>
        /// <exception cref="IOException">Файл занят другим процессом.</exception>
        internal void Write()
        {
            DshBridgePaths.EnsureStore();
            var payload = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(DshBridgePaths.EndpointFile, payload, new UTF8Encoding(false));
        }

        /// <summary>Читает описание с диска.</summary>
        /// <returns>Описание моста или <c>null</c>, если файла нет или он испорчен.</returns>
        internal static DshBridgeEndpoint? TryRead()
        {
            var path = DshBridgePaths.EndpointFile;
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonConvert.DeserializeObject<DshBridgeEndpoint>(File.ReadAllText(path));
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>Удаляет файл описания, если он принадлежит этому процессу.</summary>
        internal static void RemoveIfOwned()
        {
            var current = TryRead();
            if (current is null || current.Pid != System.Diagnostics.Process.GetCurrentProcess().Id)
            {
                return;
            }

            try
            {
                File.Delete(DshBridgePaths.EndpointFile);
            }
            catch (IOException)
            {
                // Файл уже удалён или занят — на работу моста это не влияет.
            }
        }
    }
}
