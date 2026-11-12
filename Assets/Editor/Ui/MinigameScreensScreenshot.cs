#nullable enable
using System;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Minigames;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры префабов мини-игр: «Взлом тайника» и «Поиск артефакта».
    /// Кадры нужны для проверки вёрстки без запуска игры.
    /// </summary>
    public static class MinigameScreensScreenshot
    {
        private const string LockPath = "Assets/Prefabs/Screens/LockMinigameScreen.prefab";
        private const string RadarPath = "Assets/Prefabs/Screens/RadarMinigameScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Пункт меню: снять кадры мини-игр.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты мини-игр")]
        public static void CaptureFromMenu() => CaptureAll();

        /// <summary>Точка входа batch-режима.</summary>
        public static void CaptureFromBatch()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает кадры в <c>Logs/Screenshots/minigame-*.png</c>.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var texts = result.Services.Resolve<IGameTexts>();
            Capture("minigame-lock.png", LockPath, panel => FillLock(panel, texts));
            Capture("minigame-radar.png", RadarPath, panel => FillRadar(panel, texts));
            Debug.Log($"AnomalyPath: кадры мини-игр в {OutputFolder}");
        }

        private static void FillLock(GameObject panel, IGameTexts texts)
        {
            ApplyLabels(panel, texts);
            SetText(panel, "Content/Window/Title", UiText.Get(texts, UiTextKey.Lock_Title));
            SetText(panel, "Content/Window/Attempts", UiText.Format(texts, UiTextKey.Lock_Attempts, 5, 7));
            SetText(panel, "Content/Window/CodeLabel", UiText.Format(texts, UiTextKey.Lock_Code, 3, 5));
            SetText(panel, "Content/Window/History/HistoryTitle", "Попытки");
            for (var index = 0; index < 3; index++)
            {
                SetText(panel, "Content/Window/Slots/Slot" + index + "/Label", "352"[index].ToString());
            }

            for (var index = 3; index < 8; index++)
            {
                var hiddenSlot = FindDeep(panel.transform, "Content/Window/Slots/Slot" + index);
                if (hiddenSlot != null)
                {
                    hiddenSlot.gameObject.SetActive(false);
                }
            }

            for (var index = 0; index < 5; index++)
            {
                SetText(panel, "Content/Window/Symbols/Symbol" + index + "/Label", (index + 1).ToString());
            }

            for (var index = 5; index < 9; index++)
            {
                var hiddenSymbol = FindDeep(panel.transform, "Content/Window/Symbols/Symbol" + index);
                if (hiddenSymbol != null)
                {
                    hiddenSymbol.gameObject.SetActive(false);
                }
            }

            var green = ColorUtility.ToHtmlStringRGB(MinigamePalette.Success);
            var yellow = ColorUtility.ToHtmlStringRGB(MinigamePalette.WrongPlace);
            SetRow(
                panel,
                0,
                "<color=#" + green + ">3</color><color=#" + yellow + ">5</color><color=#" + green + ">2</color>",
                "2",
                "1");
            SetRow(panel, 1, "<color=#" + yellow + ">2</color>81", "0", "1");
            for (var index = 2; index < 8; index++)
            {
                var hiddenRow = FindDeep(panel.transform, "Content/Window/History/Rows/HistoryRow" + index);
                if (hiddenRow != null)
                {
                    hiddenRow.gameObject.SetActive(false);
                }
            }

            SetText(panel, "Content/Window/Status", UiText.Format(texts, UiTextKey.Lock_Wrong, 2, 1));
        }

        private static void FillRadar(GameObject panel, IGameTexts texts)
        {
            ApplyLabels(panel, texts);
            SetText(panel, "Content/Window/Title", UiText.Get(texts, UiTextKey.Radar_Title));
            SetText(panel, "Content/Window/Attempts", UiText.Format(texts, UiTextKey.Radar_Attempts, 1, 2));
            SetText(panel, "Content/Window/Health", UiText.Format(texts, UiTextKey.Radar_Health, 84, 100));
            SetText(panel, "Content/Window/Scans", UiText.Format(texts, UiTextKey.Radar_Scans, 3, 5));
            SetText(panel, "Content/Window/Status", UiText.Format(texts, UiTextKey.Radar_Selected, "C2"));
            SetText(panel, "Content/Window/Panel/Log", "A1 — сигнал 4\nB1 — сигнал 2\nC2 — сигнал 1");
            var values = new[] { "4", "2", "?", "?", "1", "?" };
            for (var index = 0; index < values.Length; index++)
            {
                SetText(panel, "Content/Window/Grid/Cell" + index + "/Label", values[index]);
            }

            for (var index = values.Length; index < 36; index++)
            {
                var hidden = FindDeep(panel.transform, "Content/Window/Grid/Cell" + index);
                if (hidden != null)
                {
                    hidden.gameObject.SetActive(false);
                }
            }
        }

        private static void SetRow(GameObject panel, int row, string code, string exact, string partial)
        {
            var prefix = "Content/Window/History/Rows/HistoryRow" + row + "/";
            SetText(panel, prefix + "Code", code);
            SetText(panel, prefix + "Exact", exact);
            SetText(panel, prefix + "Partial", partial);
        }

        private static void Capture(string file, string prefabPath, Action<GameObject> fill)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {prefabPath}");
            }

            var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var canvasObject = new GameObject("CaptureCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(Width, Height);
            canvasRect.position = Vector3.zero;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvasObject.transform);
            instance.SetActive(true);
            FillBackdrop(instance);
            fill(instance);
            Canvas.ForceUpdateCanvases();
            ActionButtonLabel.FitAll(instance.transform);
            Canvas.ForceUpdateCanvases();

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            var path = Path.Combine(OutputFolder, file);
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void ApplyLabels(GameObject panel, IGameTexts texts)
        {
            foreach (var label in panel.GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }
        }

        private static void FillBackdrop(GameObject panel) => ScreenBackdropAssets.ApplyForScreenshot(panel);

        private static void SetText(GameObject panel, string path, string text)
        {
            var target = FindDeep(panel.transform, path);
            var label = target != null ? target.GetComponent<Text>() : null;
            if (label != null)
            {
                label.text = text;
            }
        }

        private static Transform? FindDeep(Transform root, string path)
        {
            var parts = path.Split('/');
            var current = root;
            foreach (var part in parts)
            {
                Transform? next = null;
                foreach (Transform child in current)
                {
                    if (child.name == part)
                    {
                        next = child;
                        break;
                    }
                }

                if (next == null)
                {
                    return null;
                }

                current = next;
            }

            return current;
        }
    }
}
