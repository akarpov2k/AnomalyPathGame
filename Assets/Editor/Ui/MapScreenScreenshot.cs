#nullable enable
using System;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Runtime.Screens.Map;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадры экрана карты мира из собранного префаба: общий вид и открытую плашку
    /// с двумя способами перехода. Кадр собирается из самого префаба — картинка, вырезки
    /// по точкам из данных, подписи и крестик те же, что и в игре.
    /// </summary>
    public static class MapScreenScreenshot
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/MapScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Пункт меню: снять кадры экрана карты мира.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты экрана карты мира")]
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

        /// <summary>Снимает общий вид карты и открытую плашку перехода.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var services = LoadServices();
            var texts = services.Resolve<IGameTexts>();
            var locations = services.Resolve<ILocationRepository>();
            Capture("map", texts, locations, travel: false);
            Capture("map-travel", texts, locations, travel: true);
            Debug.Log($"AnomalyPath: кадры экрана карты мира в {OutputFolder}");
        }

        private static ServiceContainer LoadServices()
        {
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            return result.Services;
        }

        private static void Capture(string fileName, IGameTexts texts, ILocationRepository locations, bool travel)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Нет префаба {PrefabPath}");
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
            Fill(instance, texts, locations, travel);
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

            var path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void Fill(GameObject panel, IGameTexts texts, ILocationRepository locations, bool travel)
        {
            var stage = panel.transform.Find("Content/Stage") as RectTransform;
            if (stage == null)
            {
                throw new InvalidOperationException("В префабе нет Content/Stage");
            }

            var spots = stage.Find("Spots") as RectTransform;
            var template = spots?.Find("SpotTemplate") as RectTransform;
            if (spots != null && template != null)
            {
                AddSpot(spots, template, locations, "loc_hub_settlement", false);
                AddSpot(spots, template, locations, "loc_hub_depot", travel);
            }

            var travelPanel = stage.Find("TravelPanel") as RectTransform;
            if (travelPanel == null)
            {
                throw new InvalidOperationException("В префабе нет TravelPanel");
            }

            travelPanel.gameObject.SetActive(travel);
            SetPrice(travelPanel, texts, "Walk", "ui.map_walk_price", 240, 2, 20);
            SetPrice(travelPanel, texts, "Guide", "ui.map_guide_price", 240, 200);
            if (travel)
            {
                PlacePanel(travelPanel, stage, locations, "loc_hub_depot");
            }
        }

        private static void SetPrice(
            RectTransform panel,
            IGameTexts texts,
            string button,
            string key,
            params object[] values)
        {
            var price = panel.Find(button + "/Price")?.GetComponent<Text>();
            if (price != null)
            {
                price.text = string.Format(texts.Get(key), values);
            }
        }

        /// <summary>Прямоугольник игры ставит пару кнопок рядом с хабом: повторяем это в кадре.</summary>
        private static void PlacePanel(
            RectTransform panel,
            RectTransform stage,
            ILocationRepository locations,
            string templateId)
        {
            if (!locations.Contains(templateId))
            {
                return;
            }

            var spot = locations.Get(templateId).WorldMapSpot;
            if (spot == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var area = stage.rect.size;
            var size = panel.rect.size;
            var middle = (float)((spot.Bottom + spot.Top) * 0.5);
            var left = ((float)spot.Right * area.x) + 16f;
            var top = Mathf.Clamp(
                (middle * area.y) - (size.y * 0.5f),
                24f,
                Mathf.Max(24f, area.y - size.y - 24f));

            panel.anchorMin = Vector2.zero;
            panel.anchorMax = Vector2.zero;
            panel.pivot = Vector2.zero;
            panel.anchoredPosition = new Vector2(left, top);
        }

        private static void AddSpot(
            RectTransform parent,
            RectTransform template,
            ILocationRepository locations,
            string templateId,
            bool selected)
        {
            if (!locations.Contains(templateId))
            {
                return;
            }

            var spot = locations.Get(templateId).WorldMapSpot;
            if (spot == null)
            {
                return;
            }

            var clone = UnityEngine.Object.Instantiate(template, parent);
            clone.name = "Spot_" + templateId;
            clone.anchorMin = new Vector2((float)spot.Left, (float)spot.Bottom);
            clone.anchorMax = new Vector2((float)spot.Right, (float)spot.Top);
            clone.pivot = new Vector2(0.5f, 0.5f);
            clone.offsetMin = Vector2.zero;
            clone.offsetMax = Vector2.zero;
            clone.SetAsLastSibling();
            clone.gameObject.SetActive(true);

            var zone = clone.GetComponent<MapSpot>();
            if (zone != null)
            {
                zone.SetSelected(selected);
            }
        }
    }
}
