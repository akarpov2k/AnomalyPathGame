#nullable enable
using System;
using System.IO;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="CombatScreenScreenshot"/>: съёмка кадра и поиск объектов префаба.</summary>
    public static partial class CombatScreenScreenshot
    {
        private static void Capture(string file, Action<GameObject> fill)
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
            FitLabels(instance);
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

        /// <summary>
        /// Вписывает подписи кнопок в табличку после раскладки: в batch-режиме нет кадров,
        /// поэтому <see cref="ActionButtonLabel.LateUpdate"/> не успевает подобрать кегль.
        /// </summary>
        /// <param name="panel">Собранный экран.</param>
        private static void FitLabels(GameObject panel) => ActionButtonLabel.FitAll(panel.transform);

        private static void FillBackdrop(GameObject panel)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BattleBackdropPath);
            foreach (var backdrop in panel.GetComponentsInChildren<ScreenBackdrop>(true))
            {
                var image = backdrop.GetComponent<Image>();
                if (image == null)
                {
                    continue;
                }

                if (sprite != null)
                {
                    image.sprite = sprite;
                    image.color = Color.white;
                    image.preserveAspect = false;
                }
                else
                {
                    image.color = UiFactory.BackgroundColor;
                }
            }
        }

        private static void SetText(GameObject panel, string path, string text)
        {
            var target = FindPath(panel.transform, path);
            var label = target != null ? target.GetComponent<Text>() : null;
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void SetFill(GameObject panel, string path, float amount)
        {
            var target = FindPath(panel.transform, path);
            var image = target != null ? target.GetComponent<Image>() : null;
            if (image != null)
            {
                image.fillAmount = amount;
            }
        }

        /// <summary>Ищет объект по пути из имён: <c>Panel/Child</c>.</summary>
        /// <param name="root">Корень поиска.</param>
        /// <param name="path">Путь из имён через косую черту.</param>
        /// <returns>Найденный объект или <c>null</c>.</returns>
        private static Transform? FindPath(Transform root, string path)
        {
            var parts = path.Split('/');
            var current = FindDeep(root, parts[0]);
            for (var index = 1; index < parts.Length && current != null; index++)
            {
                Transform? next = null;
                foreach (Transform child in current)
                {
                    if (child.name == parts[index])
                    {
                        next = child;
                        break;
                    }
                }

                current = next;
            }

            return current;
        }

        private static float Fraction(double value, double max) =>
            max <= 0 ? 0f : Mathf.Clamp01((float)(value / max));

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

        /// <summary>Сервисы, нужные заполнению выпадашек и подсказок.</summary>
        private sealed class MenuContext
        {
            public MenuContext(ICombatService combat, ISkillRepository skills, ICombatSettings settings)
            {
                Combat = combat;
                Skills = skills;
                Settings = settings;
            }

            public ICombatService Combat { get; }

            public ISkillRepository Skills { get; }

            public ICombatSettings Settings { get; }
        }
    }
}
