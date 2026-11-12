#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MapScreenPrefabBuilder"/>: плашка перехода, кнопки режимов и крестик.</summary>
    public static partial class MapScreenPrefabBuilder
    {
        private static Button BuildTravelButton(
            UiFactory ui,
            IGameTexts texts,
            RectTransform panel,
            UiTextKey key,
            string name,
            float bottom,
            out Text price)
        {
            var button = ui.CreateActionButton(panel, UiText.Get(texts, key));
            button.gameObject.name = name;
            var rect = (RectTransform)button.transform;
            Place(
                rect,
                Vector2.zero,
                Vector2.zero,
                new Vector2(0f, bottom),
                new Vector2(TravelButtonWidth, TravelButtonHeight));

            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                throw new InvalidOperationException("У кнопки перехода нет подписи");
            }

            // Подпись стоит на металлической табличке кадра так же, как в меню; цена — под ней,
            // на тёмной кромке кадра, тем же песочным цветом с обводкой.
            ActionButtonMetrics.PlaceLabel(label, TravelModeFontSize);

            var localized = label.gameObject.AddComponent<LocalizedText>();
            SetObject(localized, "label", label);
            SetString(localized, "key", UiText.Code(key));

            price = CreateText(
                rect,
                "Price",
                string.Empty,
                TravelPriceFontSize,
                TextAnchor.MiddleCenter,
                ActionButtonPalette.Label);
            Place(
                price.rectTransform,
                Vector2.zero,
                Vector2.zero,
                new Vector2(TravelLabelPadding, TravelTextLift),
                new Vector2(TravelButtonWidth - (2f * TravelLabelPadding), TravelPriceHeight));
            price.resizeTextForBestFit = true;
            price.resizeTextMinSize = TravelPriceFontMin;
            price.resizeTextMaxSize = TravelPriceFontSize;
            price.horizontalOverflow = HorizontalWrapMode.Wrap;
            price.verticalOverflow = VerticalWrapMode.Truncate;
            ActionButtonMetrics.ApplyStyle(price);
            return button;
        }

        private static Button BuildClose(Transform content)
        {
            var go = new GameObject("Close", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(content, false);
            var rect = (RectTransform)go.transform;
            Place(
                rect,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-CloseInset, -CloseInset),
                new Vector2(CloseSize, CloseSize));

            var hit = go.GetComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.82f, 1f, 0.78f, 1f);
            colors.pressedColor = new Color(0.7f, 0.9f, 0.66f, 1f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var icon = CreateImage(rect, "Icon", Color.white);
            var iconRect = icon.rectTransform;
            Stretch(iconRect);
            iconRect.offsetMin = new Vector2(CloseIconInset, CloseIconInset);
            iconRect.offsetMax = new Vector2(-CloseIconInset, -CloseIconInset);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CloseIconPath);
            if (sprite != null)
            {
                icon.sprite = sprite;
                icon.preserveAspect = true;
            }
            else
            {
                Debug.LogWarning($"AnomalyPath: нет значка закрытия {CloseIconPath}");
            }

            button.targetGraphic = icon;
            ButtonArtAssets.ApplyPlateBehind(button, 0f);
            return button;
        }

    }
}
