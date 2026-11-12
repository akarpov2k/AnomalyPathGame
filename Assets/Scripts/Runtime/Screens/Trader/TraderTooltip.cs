#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Всплывающая подсказка с именем торговца у курсора: появляется при наведении на портрет
    /// в выпадашке и не перехватывает мышь. Подсказка держится в границах экрана.
    /// </summary>
    public sealed class TraderTooltip
    {
        private readonly RectTransform _panel;
        private readonly Text _label;

        /// <summary>Создаёт скрытую подсказку на экране.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="parent">Родитель, обычно контент экрана.</param>
        public TraderTooltip(IUiHost host, Transform parent)
        {
            var background = host.Ui.CreateImage("TraderTooltip", parent, TraderPalette.Tooltip);
            background.raycastTarget = false;
            _panel = background.rectTransform;
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = new Vector2(TraderMetrics.TooltipWidth, TraderMetrics.TooltipHeight);

            _label = host.Ui.CreateText(
                _panel,
                string.Empty,
                TraderMetrics.TooltipFontSize,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            _label.gameObject.name = "Label";
            _label.raycastTarget = false;
            var labelRect = _label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            background.gameObject.SetActive(false);
        }

        /// <summary>Показана ли подсказка сейчас.</summary>
        public bool IsShown => _panel.gameObject.activeSelf;

        /// <summary>Текст подсказки; пусто, пока она скрыта.</summary>
        public string ShownText => IsShown ? _label.text : string.Empty;

        /// <summary>Показывает имя торговца у курсора.</summary>
        /// <param name="text">Имя торговца; пусто — подсказка скрывается.</param>
        /// <param name="screenPosition">Позиция курсора в координатах экрана.</param>
        /// <param name="camera">Камера события указателя или <c>null</c> для оверлея.</param>
        public void Show(string text, Vector2 screenPosition, Camera? camera)
        {
            if (string.IsNullOrEmpty(text))
            {
                Hide();
                return;
            }

            _label.text = text;
            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            Move(screenPosition, camera);
        }

        /// <summary>Двигает подсказку за курсором, не выходя за края экрана.</summary>
        /// <param name="screenPosition">Позиция курсора в координатах экрана.</param>
        /// <param name="camera">Камера события указателя или <c>null</c> для оверлея.</param>
        public void Move(Vector2 screenPosition, Camera? camera)
        {
            if (!_panel.gameObject.activeInHierarchy || !(_panel.parent is RectTransform area))
            {
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screenPosition, camera, out var local))
            {
                return;
            }

            var size = _panel.rect.size;
            if (size.x <= 0f || size.y <= 0f)
            {
                return;
            }

            var half = area.rect.size * 0.5f;
            var margin = TraderMetrics.TooltipMargin;
            var offset = TraderMetrics.TooltipOffset;

            var minX = -half.x + margin;
            var maxX = half.x - size.x - margin;
            if (minX > maxX)
            {
                minX = maxX;
            }

            var minY = -half.y + size.y + margin;
            var maxY = half.y - margin;
            if (minY > maxY)
            {
                minY = maxY;
            }

            _panel.anchoredPosition = new Vector2(
                Mathf.Clamp(local.x + offset, minX, maxX),
                Mathf.Clamp(local.y - offset, minY, maxY));
        }

        /// <summary>Скрывает подсказку.</summary>
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }
    }
}
