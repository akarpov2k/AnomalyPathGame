#nullable enable
using System;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Game.Hub;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры экрана хаба из собранного префаба: общий вид с торговцем за стойкой
    /// и кадр с дверью под наведением. Кадр собирается из самого префаба — подписи,
    /// картинка интерьера, торговец, дверь со свечением и часы подставляются теми же
    /// спрайтами и текстами, что и в игре.
    /// </summary>
    public static class HubScreenScreenshot
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/HubScreen.prefab";
        private const string BigPrefabPath = "Assets/Prefabs/Screens/BigHubScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const string InteriorPath = "Assets/Art/Backgrounds/bg_hub_house.png";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Пункт меню: снять кадры экрана хаба.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты экрана хаба")]
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

        /// <summary>Снимает общий вид и кадр с дверью под наведением.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var texts = LoadTexts();
            Capture("hub", texts, null);
            Capture("hub-door-hover", texts, "LeaveHub");
            Capture("big-hub", texts, null, BigPrefabPath);
            Debug.Log($"AnomalyPath: кадры экрана хаба в {OutputFolder}");
        }

        private static IGameTexts LoadTexts()
        {
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            return result.Services.Resolve<IGameTexts>();
        }

        private static void Capture(
            string fileName,
            IGameTexts texts,
            string? hoverZone,
            string prefabPath = PrefabPath)
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
            Fill(instance, texts, hoverZone);
            Rebuild(instance);

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

            var path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void Fill(GameObject panel, IGameTexts texts, string? hoverZone)
        {
            var stage = panel.transform.Find("Content/Stage");
            if (stage == null)
            {
                throw new InvalidOperationException("В префабе нет Content/Stage");
            }

            var interior = stage.Find("Interior")?.GetComponent<Image>();
            if (interior != null)
            {
                interior.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InteriorPath);
                interior.color = Color.white;
            }

            var hand = stage.Find("Clock/ClockFace/ClockHand") as RectTransform;
            if (hand != null)
            {
                hand.localEulerAngles = new Vector3(0f, 0f, 108f);
            }

            var time = stage.Find("ClockTime")?.GetComponent<Text>();
            if (time != null)
            {
                time.text = string.Format(texts.Get("ui.game_clock"), 3, 10, 30);
            }

            HoverZone(stage, hoverZone);
        }

        /// <summary>Включает наведение на объект хаба: кадр показывает его картинку со свечением.</summary>
        private static void HoverZone(Transform stage, string? name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            var zone = stage.Find(name!)?.GetComponent<HubZone>();
            if (zone == null)
            {
                throw new InvalidOperationException($"В префабе нет объекта {name}");
            }

            zone.OnPointerEnter(new PointerEventData(null));
        }

        private static void Rebuild(GameObject panel)
        {
            foreach (var group in panel.GetComponentsInChildren<LayoutGroup>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)group.transform);
            }

            foreach (var fitter in panel.GetComponentsInChildren<ContentSizeFitter>(true))
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)fitter.transform);
            }

            Canvas.ForceUpdateCanvases();
        }
    }
}
