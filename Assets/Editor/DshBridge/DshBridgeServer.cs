#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace AnomalyPath.Editor.DshBridge
{
    /// <summary>
    /// Локальный HTTP-мост редактора: слушает <c>127.0.0.1</c> на случайном порту,
    /// принимает <c>POST /command</c> с JSON <c>{ "command": "...", "args": { } }</c>
    /// и выполняет команду в главном потоке. Включается маркером
    /// <c>Library/DshBridge/enable</c> (его создаёт MCP-сервер) или переменной окружения
    /// <c>DSH_BRIDGE=1</c>; без них редактор порт не открывает.
    /// </summary>
    [InitializeOnLoad]
    internal static class DshBridgeServer
    {
        /// <summary>Сколько ждать команду, прежде чем ответить таймаутом.</summary>
        private const int RequestTimeoutSeconds = 180;

        private static readonly ConcurrentQueue<Action> MainThread = new ConcurrentQueue<Action>();
        private static TcpListener? listener;
        private static CancellationTokenSource? cancellation;
        private static Thread? acceptThread;
        private static string token = string.Empty;
        private static int port;
        private static double quitAt = -1;
        private static bool quitSaveScenes;

        static DshBridgeServer()
        {
            DshBridgeLogBuffer.Install();
            if (!DshBridgePaths.IsEnabled || Application.isBatchMode)
            {
                return;
            }

            AssemblyReloadEvents.beforeAssemblyReload += Stop;
            EditorApplication.quitting += Stop;
            EditorApplication.update += Pump;
            Start();
        }

        /// <summary>Планирует закрытие редактора после ответа на текущую команду.</summary>
        /// <param name="saveScenes">Сохранить ли открытые сцены перед выходом.</param>
        internal static void ScheduleQuit(bool saveScenes)
        {
            quitSaveScenes = saveScenes;
            quitAt = EditorApplication.timeSinceStartup + 0.75;
        }

        private static void Start()
        {
            try
            {
                DshBridgePaths.EnsureStore();
                token = Guid.NewGuid().ToString("N");

                var probe = new TcpListener(IPAddress.Loopback, 0);
                probe.Start();
                port = ((IPEndPoint)probe.LocalEndpoint).Port;
                probe.Stop();

                cancellation = new CancellationTokenSource();
                listener = new TcpListener(IPAddress.Loopback, port);
                listener.Start();
                acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "DshBridge",
                };
                acceptThread.Start();

                var endpoint = new DshBridgeEndpoint
                {
                    Pid = Process.GetCurrentProcess().Id,
                    Port = port,
                    Token = token,
                    UnityVersion = Application.unityVersion,
                    ProjectPath = DshBridgePaths.ProjectRoot,
                    BatchMode = Application.isBatchMode,
                    StartedAt = DateTime.Now.ToString("O"),
                };
                endpoint.Write();
                Debug.Log($"[dsh] мост слушает 127.0.0.1:{port}");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[dsh] мост не запустился: {exception.Message}");
                Stop();
            }
        }

        private static void Stop()
        {
            try
            {
                cancellation?.Cancel();
                listener?.Stop();
            }
            catch (Exception)
            {
                // Остановка уже идущего моста — не ошибка.
            }
            finally
            {
                listener = null;
                cancellation = null;
                DshBridgeEndpoint.RemoveIfOwned();
            }
        }

        private static void AcceptLoop()
        {
            while (cancellation is not null && !cancellation.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = listener!.AcceptTcpClient();
                }
                catch (Exception)
                {
                    if (cancellation is null || cancellation.IsCancellationRequested)
                    {
                        break;
                    }

                    continue;
                }

                ThreadPool.QueueUserWorkItem(_ => Handle(client));
            }
        }

        private static void Handle(TcpClient client)
        {
            using (client)
            {
                try
                {
                    client.ReceiveTimeout = 10000;
                    using var stream = client.GetStream();
                    var request = ReadRequest(stream);
                    if (request is null)
                    {
                        Write(stream, 400, Failure("bad request"));
                        return;
                    }

                    if (string.Equals(request.Path, "/health", StringComparison.Ordinal))
                    {
                        Write(stream, 200, JsonConvert.SerializeObject(new
                        {
                            ok = true,
                            pid = Process.GetCurrentProcess().Id,
                            port,
                        }));
                        return;
                    }

                    if (!request.Headers.TryGetValue("x-dsh-token", out var provided) ||
                        !string.Equals(provided, token, StringComparison.Ordinal))
                    {
                        Write(stream, 403, Failure("forbidden"));
                        return;
                    }

                    Write(stream, 200, Dispatch(request.Body));
                }
                catch (Exception exception)
                {
                    try
                    {
                        using var stream = client.GetStream();
                        Write(stream, 500, Failure(exception.GetType().Name + ": " + exception.Message));
                    }
                    catch (Exception)
                    {
                        // Клиент уже отключился — отвечать некому.
                    }
                }
            }
        }

        private static string Dispatch(string body)
        {
            JObject envelope;
            try
            {
                envelope = JObject.Parse(body);
            }
            catch (Exception exception)
            {
                return Failure("bad json: " + exception.Message);
            }

            var command = envelope.Value<string>("command");
            if (string.IsNullOrWhiteSpace(command))
            {
                return Failure("command is required");
            }

            var args = envelope["args"] as JObject;
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            MainThread.Enqueue(() =>
            {
                try
                {
                    var result = DshBridgeCommands.Execute(command!, args);
                    completion.TrySetResult(JsonConvert.SerializeObject(new { ok = true, result }));
                }
                catch (Exception exception)
                {
                    completion.TrySetResult(Failure(exception.GetType().Name + ": " + exception.Message));
                }
            });

            return completion.Task.Wait(TimeSpan.FromSeconds(RequestTimeoutSeconds))
                ? completion.Task.Result
                : Failure("timeout: команда не завершилась за " + RequestTimeoutSeconds + " с");
        }

        private static Request? ReadRequest(Stream stream)
        {
            var buffer = new MemoryStream();
            var chunk = new byte[4096];
            var headerEnd = -1;
            while (headerEnd < 0)
            {
                var read = stream.Read(chunk, 0, chunk.Length);
                if (read <= 0)
                {
                    return null;
                }

                buffer.Write(chunk, 0, read);
                var text = Encoding.ASCII.GetString(buffer.GetBuffer(), 0, (int)buffer.Length);
                headerEnd = text.IndexOf("\r\n\r\n", StringComparison.Ordinal);
            }

            var headerText = Encoding.ASCII.GetString(buffer.GetBuffer(), 0, headerEnd);
            var lines = headerText.Split(new[] { "\r\n" }, StringSplitOptions.None);
            var requestLine = lines[0].Split(' ');
            if (requestLine.Length < 2)
            {
                return null;
            }

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 1; i < lines.Length; i++)
            {
                var separator = lines[i].IndexOf(':');
                if (separator <= 0)
                {
                    continue;
                }

                headers[lines[i].Substring(0, separator).Trim()] = lines[i].Substring(separator + 1).Trim();
            }

            var contentLength = headers.TryGetValue("Content-Length", out var raw) && int.TryParse(raw, out var parsed)
                ? parsed
                : 0;
            var bodyStart = headerEnd + 4;
            var body = new byte[contentLength];
            var available = (int)buffer.Length - bodyStart;
            if (available > 0)
            {
                Array.Copy(buffer.GetBuffer(), bodyStart, body, 0, Math.Min(available, contentLength));
            }

            var offset = Math.Max(available, 0);
            while (offset < contentLength)
            {
                var read = stream.Read(body, offset, contentLength - offset);
                if (read <= 0)
                {
                    break;
                }

                offset += read;
            }

            return new Request
            {
                Path = requestLine[1],
                Headers = headers,
                Body = Encoding.UTF8.GetString(body),
            };
        }

        private static void Write(Stream stream, int status, string payload)
        {
            var body = Encoding.UTF8.GetBytes(payload ?? string.Empty);
            var statusText = status == 200 ? "OK" : "ERROR";
            var head = "HTTP/1.1 " + status + " " + statusText + "\r\n" +
                       "Content-Type: application/json; charset=utf-8\r\n" +
                       "Content-Length: " + body.Length + "\r\n" +
                       "Connection: close\r\n\r\n";
            var headBytes = Encoding.ASCII.GetBytes(head);
            stream.Write(headBytes, 0, headBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private static string Failure(string message) => JsonConvert.SerializeObject(new { ok = false, error = message });

        private static void Pump()
        {
            while (MainThread.TryDequeue(out var action))
            {
                action();
            }

            if (quitAt <= 0 || EditorApplication.timeSinceStartup < quitAt)
            {
                return;
            }

            quitAt = -1;
            var saveScenes = quitSaveScenes;
            quitSaveScenes = false;
            try
            {
                if (saveScenes)
                {
                    EditorSceneManager.SaveOpenScenes();
                }

                AssetDatabase.SaveAssets();
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[dsh] не удалось сохранить перед выходом: {exception.Message}");
            }

            Debug.Log("[dsh] закрываю редактор по команде моста");
            EditorApplication.Exit(0);
        }

        /// <summary>Разобранный HTTP-запрос.</summary>
        private sealed class Request
        {
            /// <summary>Путь запроса, например <c>/command</c>.</summary>
            public string Path { get; set; } = string.Empty;

            /// <summary>Заголовки запроса (имена без учёта регистра).</summary>
            public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Тело запроса.</summary>
            public string Body { get; set; } = string.Empty;
        }
    }
}
