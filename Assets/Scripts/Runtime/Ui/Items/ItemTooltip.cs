#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>
    /// Всплывающая подсказка предмета у курсора: название, описание и характеристики.
    /// Плашка не перехватывает мышь, сама подстраивает высоту под текст и держится в границах экрана.
    /// </summary>
    public sealed class ItemTooltip
    {
        private readonly IUiHost _host;
        private readonly RectTransform _panel;
        private readonly Text _title;
        private readonly Text _description;
        private readonly Text _facts;

        /// <summary>Создаёт скрытую подсказку на экране.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="parent">Родитель, обычно корень содержимого экрана.</param>
        public ItemTooltip(IUiHost host, Transform parent)
        {
            _host = host;
            var background = host.Ui.CreateImage("ItemTooltip", parent, ItemPalette.Tooltip);
            background.raycastTarget = false;
            var outline = background.gameObject.AddComponent<Outline>();
            outline.effectColor = ItemPalette.TooltipBorder;
            outline.effectDistance = new Vector2(1f, -1f);

            _panel = background.rectTransform;
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0f, 1f);
            _panel.sizeDelta = new Vector2(ItemTooltipMetrics.Width, 0f);

            var layout = background.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                ItemTooltipMetrics.Padding,
                ItemTooltipMetrics.Padding,
                ItemTooltipMetrics.Padding,
                ItemTooltipMetrics.Padding);
            layout.spacing = ItemTooltipMetrics.Spacing;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = background.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _title = CreateLabel(host, _panel, "Title", ItemTooltipMetrics.TitleFontSize, UiFactory.TextColor, false);
            _title.font = UiFactory.FindBoldFont();
            _description = CreateLabel(
                host,
                _panel,
                "Description",
                ItemTooltipMetrics.BodyFontSize,
                UiFactory.MutedTextColor,
                true);
            _facts = CreateLabel(
                host,
                _panel,
                "Facts",
                ItemTooltipMetrics.BodyFontSize,
                UiFactory.TextColor,
                true);

            _panel.gameObject.SetActive(false);
        }

        /// <summary>Показана ли подсказка сейчас.</summary>
        public bool IsShown => _panel.gameObject.activeSelf;

        /// <summary>Видимый текст подсказки: заголовок, описание и характеристики через перевод строки.</summary>
        public string ShownText
        {
            get
            {
                if (!IsShown)
                {
                    return string.Empty;
                }

                var parts = new List<string> { _title.text };
                if (_description.gameObject.activeSelf && _description.text.Length > 0)
                {
                    parts.Add(_description.text);
                }

                if (_facts.gameObject.activeSelf && _facts.text.Length > 0)
                {
                    parts.Add(_facts.text);
                }

                return string.Join("\n", parts);
            }
        }

        /// <summary>Показывает подсказку предмета и ставит её у курсора.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="screenPosition">Позиция курсора в координатах экрана.</param>
        /// <param name="camera">Камера события указателя или <c>null</c> для оверлея.</param>
        /// <exception cref="ArgumentNullException">Экземпляр не передан.</exception>
        public void Show(ItemInstance item, Vector2 screenPosition, Camera? camera)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (!_host.Items.TryGet(item.ItemId, out var template) || template == null)
            {
                Hide();
                return;
            }

            Show(item, template, item.Stack, screenPosition, camera);
        }

        /// <summary>
        /// Показывает подсказку по данным шаблона: так выглядит товар торговца, у которого ещё
        /// нет экземпляра. Прочность, заряды и магазин в такой подсказке не показываются.
        /// </summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="stack">Количество в стопке или остаток товара у торговца.</param>
        /// <param name="screenPosition">Позиция курсора в координатах экрана.</param>
        /// <param name="camera">Камера события указателя или <c>null</c> для оверлея.</param>
        /// <exception cref="ArgumentNullException">Шаблон не передан.</exception>
        public void Show(ItemTemplate template, int stack, Vector2 screenPosition, Camera? camera)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            Show(null, template, Mathf.Max(1, stack), screenPosition, camera);
        }

        private void Show(
            ItemInstance? item,
            ItemTemplate template,
            int stack,
            Vector2 screenPosition,
            Camera? camera)
        {
            _title.text = ItemTooltipText.Title(_host, template, stack);
            var description = ItemTooltipText.Description(_host, template);
            _description.text = description;
            _description.gameObject.SetActive(description.Length > 0);

            var facts = ItemTooltipText.Facts(_host, item, template);
            _facts.text = facts;
            _facts.gameObject.SetActive(facts.Length > 0);

            _panel.gameObject.SetActive(true);
            _panel.SetAsLastSibling();
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_panel);
            Canvas.ForceUpdateCanvases();
            Move(screenPosition, camera);
        }

        /// <summary>Перемещает подсказку к курсору, не выходя за края экрана.</summary>
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
            var margin = ItemTooltipMetrics.Margin;
            var offset = ItemTooltipMetrics.Offset;

            var x = local.x + offset;
            if (x + size.x > half.x - margin)
            {
                x = local.x - size.x - offset;
            }

            var y = local.y - offset;
            if (y - size.y < -half.y + margin)
            {
                y = local.y + size.y + offset;
            }

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
                Mathf.Clamp(x, minX, maxX),
                Mathf.Clamp(y, minY, maxY));
        }

        /// <summary>Скрывает подсказку.</summary>
        public void Hide()
        {
            if (_panel != null)
            {
                _panel.gameObject.SetActive(false);
            }
        }

        private static Text CreateLabel(
            IUiHost host,
            Transform parent,
            string name,
            int size,
            Color color,
            bool flexibleHeight)
        {
            var label = host.Ui.CreateText(parent, string.Empty, size, TextAnchor.UpperLeft, color);
            label.gameObject.name = name;
            label.raycastTarget = false;
            if (flexibleHeight)
            {
                var element = label.GetComponent<LayoutElement>();
                if (element != null)
                {
                    element.preferredHeight = -1f;
                }
            }

            return label;
        }
    }
}
