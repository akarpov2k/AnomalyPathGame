#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Talents;
using AnomalyPath.Runtime.Screens.Talents;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Снимает кадр экрана талантов из собранного префаба: заголовок, панель уровня и очков,
    /// дерево карточек со связями и кнопки. Ранги на кадре — образец, чтобы видеть все состояния.
    /// </summary>
    public static class TalentScreenScreenshot
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/TalentScreen.prefab";
        private const string OutputFolder = "Logs/Screenshots";
        private const string DataRoot = "Assets/Data";
        private const string CatalogPath = "Assets/Art/SpriteCatalog.asset";
        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Пункт меню: снять кадр экрана талантов.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншот экрана талантов")]
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

        /// <summary>Снимает кадр экрана талантов в <c>Logs/Screenshots/talents.png</c>.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var services = result.Services;
            var texts = services.Resolve<IGameTexts>();
            var talents = services.Resolve<ITalentRepository>();
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(CatalogPath);
            Capture(panel => FillTalents(panel, texts, talents.Tree, catalog));
            Debug.Log($"AnomalyPath: кадр экрана талантов в {OutputFolder}");
        }

        private static void Capture(Action<GameObject> fill)
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
            FillBackdrop(instance);
            fill(instance);
            Canvas.ForceUpdateCanvases();
            SyncScrollbars(instance);
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

            var path = Path.Combine(OutputFolder, "talents.png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            Debug.Log($"AnomalyPath: скриншот {path}");
        }

        private static void FillTalents(
            GameObject panel,
            IGameTexts texts,
            TalentTreeTemplate tree,
            SpriteCatalog? catalog)
        {
            ApplyLabels(panel, texts);
            var level = FindDeep(panel.transform, "TalentLevel")?.GetComponent<Text>();
            if (level != null)
            {
                level.text = UiText.Format(texts, UiTextKey.Game_Level, 7);
            }

            var points = FindDeep(panel.transform, "TalentPoints")?.GetComponent<Text>();
            if (points != null)
            {
                points.text = UiText.Format(texts, UiTextKey.Talent_Points, 3);
            }

            var scroll = panel.GetComponentInChildren<ScrollArea>(true);
            var template = panel.GetComponentInChildren<TalentNodeView>(true);
            if (scroll == null || template == null)
            {
                throw new InvalidOperationException("В префабе талантов нет дерева или образца карточки");
            }

            var content = ReadContent(scroll);
            if (content == null)
            {
                throw new InvalidOperationException("В префабе талантов нет контента дерева");
            }

            var ranks = SampleRanks();
            var spent = 0;
            foreach (var node in tree.Nodes)
            {
                if (ranks.TryGetValue(node.Id, out var rank))
                {
                    spent += rank;
                }
            }

            foreach (var node in tree.Nodes)
            {
                foreach (var parent in TalentLayout.Parents(tree.Nodes, node))
                {
                    DrawLinks(content, parent, node);
                }
            }

            foreach (var node in tree.Nodes)
            {
                var view = UnityEngine.Object.Instantiate(template, content);
                var go = view.gameObject;
                go.SetActive(true);
                go.name = "TalentNode_" + node.Id;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = TalentLayout.NodeTopLeft(node);
                rect.sizeDelta = new Vector2(TalentMetrics.NodeWidth, TalentMetrics.NodeHeight);

                var rank = ranks.TryGetValue(node.Id, out var value) ? value : 0;
                var unlocked = spent >= node.RequiredPoints;
                view.Show(
                    node,
                    rank,
                    unlocked,
                    UiText.Format(texts, UiTextKey.Talent_Rank, rank, node.MaxRank),
                    catalog);
            }

            content.sizeDelta = new Vector2(0f, Mathf.Max(TalentMetrics.TreeHeight, TalentLayout.ContentHeight(tree.Nodes)));
        }

        private static RectTransform? ReadContent(ScrollArea scroll)
        {
            var serialized = new SerializedObject(scroll);
            var property = serialized.FindProperty("prefabContent");
            return property != null ? property.objectReferenceValue as RectTransform : null;
        }

        private static void DrawLinks(RectTransform content, TalentNodeTemplate parent, TalentNodeTemplate node)
        {
            foreach (var segment in TalentLayout.LinkSegments(parent, node))
            {
                var go = new GameObject("TalentLink", typeof(RectTransform), typeof(Image));
                go.transform.SetParent(content, false);
                var image = go.GetComponent<Image>();
                image.color = TalentPalette.Link;
                image.raycastTarget = false;
                var rect = (RectTransform)go.transform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = segment.Center;
                rect.sizeDelta = new Vector2(segment.Length, TalentMetrics.LinkWidth);
                rect.localRotation = Quaternion.Euler(0f, 0f, segment.Angle);
            }
        }

        private static Dictionary<string, int> SampleRanks() => new Dictionary<string, int>
        {
            { "tal_steady_hand", 5 },
            { "tal_second_wind", 2 },
            { "tal_toughness", 2 },
            { "tal_endurance", 1 }
        };

        /// <summary>
        /// Гасит полосу прокрутки: в batch-режиме ScrollRect не обновляет размер ползунка,
        /// и полоса рисуется во всю высоту, даже когда дерево помещается.
        /// </summary>
        private static void SyncScrollbars(GameObject panel)
        {
            foreach (var scroll in panel.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.content == null || scroll.viewport == null)
                {
                    continue;
                }

                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
                if (scroll.verticalScrollbar != null)
                {
                    scroll.verticalScrollbar.gameObject.SetActive(false);
                }
            }
        }

        private static void ApplyLabels(GameObject panel, IGameTexts? texts)
        {
            if (texts == null)
            {
                return;
            }

            foreach (var label in panel.GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }
        }

        private static void FillBackdrop(GameObject panel) => ScreenBackdropAssets.ApplyForScreenshot(panel);

        private static Transform? FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDeep(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }
    }
}
