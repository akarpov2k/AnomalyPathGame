#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AnomalyPath.Editor.DshBridge
{
    /// <summary>
    /// Команды моста. Выполняются в главном потоке редактора: сюда приходит то, что
    /// агент вызвал через MCP-инструмент. Никакого произвольного C# — только перечисленные
    /// ниже операции, чтобы мост не стал дырой в песочнице.
    /// </summary>
    internal static class DshBridgeCommands
    {
        /// <summary>Выполняет команду моста.</summary>
        /// <param name="command">Имя команды.</param>
        /// <param name="args">Аргументы команды.</param>
        /// <returns>Результат для сериализации в JSON.</returns>
        /// <exception cref="InvalidOperationException">Команда неизвестна или неприменима сейчас.</exception>
        internal static object Execute(string command, JObject? args)
        {
            switch (command)
            {
                case "ping":
                    return new { pong = true, time = DateTime.Now.ToString("O") };
                case "status":
                    return Status();
                case "log":
                    return Log(args);
                case "menu":
                    return Menu(args);
                case "asset-refresh":
                    AssetDatabase.Refresh();
                    return new { refreshed = true };
                case "asset-save":
                    AssetDatabase.SaveAssets();
                    return new { saved = true };
                case "scene-open":
                    return OpenScene(args);
                case "scene-save":
                    return SaveScene(args);
                case "play":
                    EditorApplication.isPlaying = true;
                    return new { playing = true };
                case "stop":
                    EditorApplication.isPlaying = false;
                    return new { playing = false };
                case "screenshot":
                    return Screenshot(args);
                case "compile":
                    CompilationPipeline.RequestScriptCompilation();
                    return new { requested = true };
                case "quit":
                    DshBridgeServer.ScheduleQuit(args?.Value<bool?>("saveScenes") ?? false);
                    return new { closing = true };
                default:
                    throw new InvalidOperationException($"Неизвестная команда моста: {command}");
            }
        }

        private static object Status()
        {
            var scenes = new List<string>();
            var dirty = new List<string>();
            for (var i = 0; i < EditorSceneManager.sceneCount; i++)
            {
                var scene = EditorSceneManager.GetSceneAt(i);
                var name = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
                scenes.Add(name);
                if (scene.isDirty)
                {
                    dirty.Add(name);
                }
            }

            return new
            {
                pid = Process.GetCurrentProcess().Id,
                unityVersion = Application.unityVersion,
                projectPath = DshBridgePaths.ProjectRoot,
                batchMode = Application.isBatchMode,
                isPlaying = EditorApplication.isPlaying,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                activeScene = EditorSceneManager.GetActiveScene().path,
                scenes,
                dirtyScenes = dirty,
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
            };
        }

        private static object Log(JObject? args)
        {
            var limit = args?.Value<int?>("limit") ?? 60;
            var level = args?.Value<string>("level") ?? "all";
            var entries = DshBridgeLogBuffer.Tail(Mathf.Clamp(limit, 1, 400), level);
            var simplified = new List<object>();
            for (var i = 0; i < entries.Count; i++)
            {
                simplified.Add(new { entries[i].Time, type = entries[i].Type.ToString(), entries[i].Message });
            }

            return simplified;
        }

        private static object Menu(JObject? args)
        {
            var path = args?.Value<string>("path");
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("menu: нужен аргумент path (путь пункта меню).");
            }

            var executed = EditorApplication.ExecuteMenuItem(path);
            return new { path, executed };
        }

        private static object OpenScene(JObject? args)
        {
            var path = args?.Value<string>("path");
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new InvalidOperationException("scene-open: нужен аргумент path.");
            }

            if (!File.Exists(Path.Combine(DshBridgePaths.ProjectRoot, path)))
            {
                throw new InvalidOperationException($"scene-open: файл не найден: {path}");
            }

            var additive = args?.Value<bool?>("additive") ?? false;
            var mode = additive ? OpenSceneMode.Additive : OpenSceneMode.Single;
            var scene = EditorSceneManager.OpenScene(path, mode);
            return new { opened = scene.path, additive };
        }

        private static object SaveScene(JObject? args)
        {
            var path = args?.Value<string>("path");
            if (!string.IsNullOrWhiteSpace(path))
            {
                var scene = EditorSceneManager.GetSceneByPath(path);
                if (!scene.IsValid())
                {
                    throw new InvalidOperationException($"scene-save: сцена не открыта: {path}");
                }

                var saved = EditorSceneManager.SaveScene(scene);
                return new { saved, path };
            }

            var result = EditorSceneManager.SaveOpenScenes();
            return new { saved = result };
        }

        private static object Screenshot(JObject? args)
        {
            var width = Mathf.Clamp(args?.Value<int?>("width") ?? 1280, 64, 4096);
            var path = ScreenshotPath(args?.Value<string>("name"));

            if (Application.isPlaying)
            {
                // Camera.Render не захватывает Screen Space Overlay UI, поэтому в Play Mode
                // снимаем кадр целиком — на нём видно и мир, и интерфейс.
                var frame = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    File.WriteAllBytes(path, frame.EncodeToPNG());
                    return new { path, width = frame.width, height = frame.height, mode = "game" };
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(frame);
                }
            }

            var camera = SceneView.lastActiveSceneView is null ? Camera.main : SceneView.lastActiveSceneView.camera;
            if (camera is null)
            {
                throw new InvalidOperationException(
                    "screenshot: в редакторе нет камеры — откройте сцену или снимайте в Play Mode (mode: game).");
            }

            var aspect = 16f / 9f;
            if (camera.pixelHeight > 0)
            {
                aspect = (float)camera.pixelWidth / camera.pixelHeight;
            }
            else if (Screen.height > 0)
            {
                aspect = (float)Screen.width / Screen.height;
            }

            var height = Mathf.Max(64, Mathf.RoundToInt(width / Mathf.Max(0.1f, aspect)));
            var render = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var previousTarget = camera.targetTexture;
            var previousActive = RenderTexture.active;
            try
            {
                camera.targetTexture = render;
                camera.Render();
                RenderTexture.active = render;
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                try
                {
                    texture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    texture.Apply();
                    File.WriteAllBytes(path, texture.EncodeToPNG());
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                render.Release();
                UnityEngine.Object.DestroyImmediate(render);
            }

            return new { path, width, height, mode = "camera" };
        }

        private static string ScreenshotPath(string? requestedName)
        {
            Directory.CreateDirectory(DshBridgePaths.ScreenshotDirectory);
            var name = requestedName ?? string.Empty;
            var fileName = string.IsNullOrWhiteSpace(name)
                ? $"dsh_{DateTime.Now:yyyyMMdd_HHmmss}.png"
                : (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? name : name + ".png");
            return Path.Combine(DshBridgePaths.ScreenshotDirectory, fileName);
        }
    }
}
