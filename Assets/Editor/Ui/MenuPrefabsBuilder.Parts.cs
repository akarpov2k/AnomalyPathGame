#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsBuilder"/>: общие элементы префабов.</summary>
    public static partial class MenuPrefabsBuilder
    {
        /// <summary>Цвет действия в строке сохранения.</summary>
        private static readonly Color ActionColor = new Color(0.24f, 0.48f, 0.30f, 1f);

        /// <summary>Цвет удаления в строке сохранения.</summary>
        private static readonly Color DangerColor = new Color(0.42f, 0.20f, 0.18f, 1f);

        private static GameObject CreatePanel(string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)go.transform);
            go.GetComponent<Image>().color = UiFactory.BackgroundColor;
            return go;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateLabel(Transform parent, string name, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = UiFactory.FindFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button CreateLocalizedButton(
            UiFactory ui,
            IGameTexts texts,
            Transform parent,
            UiTextKey key,
            Color color,
            bool action = false)
        {
            var button = action
                ? ui.CreateActionButton(parent, UiText.Get(texts, key))
                : ui.CreateButton(parent, UiText.Get(texts, key));
            if (!action)
            {
                button.GetComponent<Image>().color = color;
            }

            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                throw new InvalidOperationException("У созданной кнопки нет подписи");
            }

            Localize(label, key);
            return button;
        }

        private static Button CreateSymbolButton(UiFactory ui, Transform parent, string name, string caption)
        {
            var button = ui.CreateButton(parent, caption);
            button.gameObject.name = name;
            return button;
        }

        private static void Localize(Text label, UiTextKey key)
        {
            var localized = label.gameObject.AddComponent<LocalizedText>();
            SetObject(localized, "label", label);
            SetString(localized, "key", UiText.Code(key));
        }

        private static void Place(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchTopBand(RectTransform rect, float left, float top, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(left, -top - height);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void StretchBottomBand(RectTransform rect, float left, float bottom, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, bottom + height);
        }

        /// <summary>Растягивает по ширине и центрирует по высоте родителя.</summary>
        /// <param name="rect">Прямоугольник.</param>
        /// <param name="left">Отступ слева.</param>
        /// <param name="right">Отступ справа.</param>
        /// <param name="height">Высота полосы.</param>
        private static void StretchCenterBand(RectTransform rect, float left, float right, float height)
        {
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(left, -height / 2f);
            rect.offsetMax = new Vector2(-right, height / 2f);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path)!;
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
