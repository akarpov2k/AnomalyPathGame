#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Рисует сетку окна торговли: клетки, линии и предметы по габаритам. Инвентарь игрока
    /// ставит предметы на их места в контейнере, товары торговца раскладываются подряд и
    /// переносятся на следующую строку, когда в текущей не хватает ширины.
    /// </summary>
    public static class TradeGridBuilder
    {
        /// <summary>
        /// Строит сетку в области прокрутки и поднимает её высоту под содержимое.
        /// </summary>
        /// <param name="ui">Фабрика элементов интерфейса.</param>
        /// <param name="content">Контент области прокрутки, куда кладётся сетка.</param>
        /// <param name="viewportWidth">Ширина видимой области.</param>
        /// <param name="viewportHeight">Высота видимой области.</param>
        /// <param name="items">Предметы для показа.</param>
        /// <param name="columns">Число колонок сетки.</param>
        /// <param name="minRows">Минимальное число строк.</param>
        /// <param name="selectedKey">Ключ выбранного предмета или <c>null</c>.</param>
        /// <param name="onSelect">Что выполнить по нажатию; <c>null</c> — предметы не кликаются.</param>
        /// <param name="spriteOf">Как получить спрайт иконки по id.</param>
        /// <param name="namePrefix">Префикс имён объектов: <c>TradeItem</c> или <c>PlayerItem</c>.</param>
        /// <param name="pack">Раскладывать предметы подряд (товары торговца) или брать позиции из контейнера.</param>
        /// <param name="tooltip">Подсказка предмета при наведении; <c>null</c> — наведение не обрабатывается.</param>
        /// <param name="onMenu">
        /// Что выполнить по правому клику на предмете (позиция курсора и ключ предмета);
        /// <c>null</c> — контекстного меню у сетки нет.
        /// </param>
        /// <returns>Высота сетки: её нужно поставить контенту прокрутки.</returns>
        public static float Build(
            UiFactory ui,
            RectTransform content,
            float viewportWidth,
            float viewportHeight,
            IReadOnlyList<TradeGridItem> items,
            int columns,
            int minRows,
            string? selectedKey,
            Action<string>? onSelect,
            Func<string, Sprite?> spriteOf,
            string namePrefix,
            bool pack,
            ItemTooltip? tooltip = null,
            Action<string, Vector2>? onMenu = null)
        {
            Clear(content);

            var safeColumns = Mathf.Max(1, columns);
            var width = viewportWidth;
            var height = viewportHeight;
            if (width < 1f)
            {
                return 0f;
            }

            var side = Mathf.Max(
                TradeMetrics.MinCellSize,
                InventoryGridArt.CellSide(width, safeColumns, TradeMetrics.GridGap));
            var cell = new Vector2(side, side);
            var fitRows = InventoryGridArt.FitRows(height, side, TradeMetrics.GridGap);
            var placements = Place(items, safeColumns, pack, out var usedRows);
            var rows = Mathf.Max(Mathf.Max(fitRows, usedRows), Mathf.Max(1, minRows));
            var gridSize = InventoryGridArt.GridSize(safeColumns, rows, cell, TradeMetrics.GridGap);

            var root = new GameObject("Grid", typeof(RectTransform));
            root.transform.SetParent(content, false);
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.anchoredPosition = Vector2.zero;
            rootRect.sizeDelta = gridSize;

            DrawLines(ui, root.transform, safeColumns, rows, cell);
            for (var index = 0; index < items.Count; index++)
            {
                DrawItem(
                    ui,
                    root.transform,
                    items[index],
                    placements[index],
                    cell,
                    selectedKey,
                    onSelect,
                    spriteOf,
                    namePrefix,
                    tooltip,
                    onMenu);
            }

            return gridSize.y;
        }

        private static Vector2Int[] Place(
            IReadOnlyList<TradeGridItem> items,
            int columns,
            bool pack,
            out int usedRows)
        {
            var places = new Vector2Int[items.Count];
            usedRows = 0;
            if (!pack)
            {
                for (var index = 0; index < items.Count; index++)
                {
                    var item = items[index];
                    places[index] = new Vector2Int(item.X, item.Y);
                    usedRows = Mathf.Max(usedRows, item.Y + item.Height);
                }

                return places;
            }

            var taken = new HashSet<int>();
            var rows = 1;
            for (var index = 0; index < items.Count; index++)
            {
                var item = items[index];
                var itemWidth = Mathf.Min(item.Width, columns);
                var itemHeight = item.Height;
                var placed = false;
                for (var y = 0; y < rows && !placed; y++)
                {
                    for (var x = 0; x + itemWidth <= columns && !placed; x++)
                    {
                        if (!IsFree(taken, x, y, itemWidth, itemHeight, columns))
                        {
                            continue;
                        }

                        Mark(taken, x, y, itemWidth, itemHeight, columns);
                        places[index] = new Vector2Int(x, y);
                        rows = Mathf.Max(rows, y + itemHeight);
                        placed = true;
                    }
                }

                if (placed)
                {
                    continue;
                }

                places[index] = new Vector2Int(0, rows);
                Mark(taken, 0, rows, itemWidth, itemHeight, columns);
                rows += itemHeight;
            }

            usedRows = rows;
            return places;
        }

        private static bool IsFree(HashSet<int> taken, int x, int y, int width, int height, int columns)
        {
            for (var row = y; row < y + height; row++)
            {
                for (var column = x; column < x + width; column++)
                {
                    if (taken.Contains((row * columns) + column))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void Mark(HashSet<int> taken, int x, int y, int width, int height, int columns)
        {
            for (var row = y; row < y + height; row++)
            {
                for (var column = x; column < x + width; column++)
                {
                    taken.Add((row * columns) + column);
                }
            }
        }

        private static void DrawLines(UiFactory ui, Transform parent, int columns, int rows, Vector2 cell)
        {
            var size = InventoryGridArt.GridSize(columns, rows, cell, TradeMetrics.GridGap);
            var half = InventoryGridArt.LineThickness / 2f;
            for (var column = 1; column < columns; column++)
            {
                var x = (column * (cell.x + TradeMetrics.GridGap)) - (TradeMetrics.GridGap / 2f) - half;
                Line(ui, parent, "GridV" + column, x, 0f, InventoryGridArt.LineThickness, size.y);
            }

            for (var row = 1; row < rows; row++)
            {
                var y = (row * (cell.y + TradeMetrics.GridGap)) - (TradeMetrics.GridGap / 2f) - half;
                Line(ui, parent, "GridH" + row, 0f, y, size.x, InventoryGridArt.LineThickness);
            }
        }

        private static void Line(
            UiFactory ui,
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            var image = ui.CreateImage(name, parent, InventoryGridArt.LineColor);
            image.raycastTarget = false;
            UiWidgets.Place(image.rectTransform, x, y, width, height);
        }

        private static void DrawItem(
            UiFactory ui,
            Transform parent,
            TradeGridItem item,
            Vector2Int place,
            Vector2 cell,
            string? selectedKey,
            Action<string>? onSelect,
            Func<string, Sprite?> spriteOf,
            string namePrefix,
            ItemTooltip? tooltip,
            Action<string, Vector2>? onMenu)
        {
            var offset = InventoryGridArt.Offset(place.x, place.y, cell, TradeMetrics.GridGap);
            var size = InventoryGridArt.ItemSize(item.Width, item.Height, cell, TradeMetrics.GridGap);
            var selected = string.Equals(item.Key, selectedKey, StringComparison.Ordinal);
            var frame = ui.CreateImage(
                namePrefix + "_" + item.Key,
                parent,
                selected ? TradePalette.ItemSelected : TradePalette.Item);
            frame.raycastTarget = true;
            UiWidgets.Place(frame.rectTransform, offset.x, offset.y, size.x, size.y);

            if (onSelect != null)
            {
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                button.transition = Selectable.Transition.None;
                var key = item.Key;
                button.onClick.AddListener(() => onSelect(key));
            }

            var sprite = item.Icon != null ? spriteOf(item.Icon) : null;
            if (sprite != null)
            {
                var icon = ui.CreateImage("Icon", frame.transform, Color.white);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiWidgets.Place(icon.rectTransform, 4f, 4f, Mathf.Max(1f, size.x - 8f), Mathf.Max(1f, size.y - 26f));
            }

            const float labelHeight = 22f;
            var label = ui.CreateText(
                frame.transform,
                item.Label,
                11,
                sprite != null ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            if (sprite != null)
            {
                UiWidgets.Place(
                    label.rectTransform,
                    3f,
                    Mathf.Max(0f, size.y - labelHeight),
                    Mathf.Max(1f, size.x - 6f),
                    labelHeight);
            }
            else
            {
                UiWidgets.Place(label.rectTransform, 3f, 3f, Mathf.Max(1f, size.x - 6f), Mathf.Max(1f, size.y - 24f));
            }

            if (item.Count > 1)
            {
                var badge = ui.CreateText(
                    frame.transform,
                    "×" + item.Count,
                    12,
                    TextAnchor.UpperRight,
                    UiFactory.TextColor);
                badge.raycastTarget = false;
                UiWidgets.Place(
                    badge.rectTransform,
                    Mathf.Max(0f, size.x - 46f),
                    2f,
                    42f,
                    18f);
            }

            RarityFrame.Draw(ui, frame.rectTransform, item.Rarity);

            if (tooltip != null)
            {
                var hover = frame.gameObject.AddComponent<ItemHover>();
                if (item.Instance != null)
                {
                    hover.Configure(item.Instance, tooltip);
                }
                else if (item.Template != null)
                {
                    hover.Configure(item.Template, item.Count, tooltip);
                }
            }

            if (onMenu != null)
            {
                var key = item.Key;
                var trigger = frame.gameObject.AddComponent<ItemMenuTrigger>();
                trigger.Configure(position => onMenu(key, position));
            }
        }

        private static void Clear(RectTransform content)
        {
            for (var index = content.childCount - 1; index >= 0; index--)
            {
                var child = content.GetChild(index).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }
        }
    }
}
