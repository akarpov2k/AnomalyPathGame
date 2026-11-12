#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Сетка инвентаря внутри окна из префаба: клетки квадратные и считаются от ширины окна,
    /// число строк — сколько помещается в высоту (лишние уходят в прокрутку) и сколько занято
    /// предметами; предметы лежат по габаритам, непоместившиеся собираются в строку переполнения.
    /// </summary>
    public sealed class CharacterInventoryFrame
    {
        private readonly IUiHost _host;
        private readonly ICharacterFrameHost _frameHost;
        private readonly ItemTooltip _tooltip;

        /// <summary>Создаёт фрейм инвентаря.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="frameHost">Экран с сеткой и слотами.</param>
        /// <param name="tooltip">Подсказка предмета при наведении.</param>
        public CharacterInventoryFrame(IUiHost host, ICharacterFrameHost frameHost, ItemTooltip tooltip)
        {
            _host = host;
            _frameHost = frameHost;
            _tooltip = tooltip;
        }

        /// <summary>
        /// Рисует сетку и предметы в области прокрутки из префаба, подгоняя высоту сетки под окно
        /// и под содержимое. Сетка не сжимается меньше занятых строк.
        /// </summary>
        /// <param name="player">Состояние игрока.</param>
        /// <param name="scroll">Область прокрутки сетки из префаба.</param>
        /// <param name="filter">Какие предметы показывать; <c>null</c> — все.</param>
        /// <returns>Список непоместившихся предметов, разделённый запятыми; пустая строка — переполнения нет.</returns>
        public string Build(PlayerState player, ScrollArea scroll, Func<ItemInstance, bool>? filter)
        {
            var area = scroll.Content;
            foreach (Transform child in area)
            {
                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }

            var grid = player.Inventory;
            var width = scroll.ViewportWidth;
            var height = scroll.ViewportHeight;
            if (width < 1f)
            {
                return string.Empty;
            }

            // Клетка квадратная и считается от ширины окна: столько строк, сколько помещается,
            // лишние уходят в прокрутку. Сетка растёт под вещи, а не наоборот.
            var side = Mathf.Max(
                CharacterMetrics.MinCellSize,
                InventoryGridArt.CellSide(width, grid.Width, CharacterMetrics.GridGap));
            var cell = new Vector2(side, side);
            var fitRows = InventoryGridArt.FitRows(height, side, CharacterMetrics.GridGap);
            var minRows = _host.Rules.Rules?.BaseRows ?? 1;
            var rows = Math.Max(Math.Max(fitRows, grid.UsedRows), minRows);
            if (grid.Rows != rows)
            {
                grid.Resize(rows);
            }

            var gridSize = InventoryGridArt.GridSize(grid.Width, rows, cell, CharacterMetrics.GridGap);

            var gridRoot = new GameObject("InventoryGrid", typeof(RectTransform), typeof(Image));
            gridRoot.transform.SetParent(area, false);
            var gridRect = (RectTransform)gridRoot.transform;
            CharacterWidgets.PlaceRect(gridRect, 0f, 0f, gridSize.x, gridSize.y);
            var gridImage = gridRoot.GetComponent<Image>();
            gridImage.color = Color.clear;
            gridImage.raycastTarget = true;
            var gridZone = gridRoot.AddComponent<ItemGridDropZone>();
            gridZone.Configure(
                _frameHost.Drag,
                grid.Width,
                rows,
                cell,
                CharacterMetrics.GridGap,
                (instanceId, x, y, rotated) => CanPlace(grid, instanceId, x, y, rotated),
                _frameHost.DropOnInventory);
            scroll.SetContentHeight(gridSize.y);

            for (var row = 0; row < rows; row++)
            {
                for (var column = 0; column < grid.Width; column++)
                {
                    var offset = InventoryGridArt.Offset(column, row, cell, CharacterMetrics.GridGap);
                    var cellBox = CharacterWidgets.Box(_host, gridRoot.transform, $"Cell_{column}_{row}",
                        offset.x,
                        offset.y,
                        cell.x,
                        cell.y,
                        Color.clear);
                    cellBox.raycastTarget = false;
                }
            }

            InventoryGridArt.DrawLines(
                _host,
                gridRoot.transform,
                grid.Width,
                rows,
                cell,
                CharacterMetrics.GridGap,
                Vector2.zero);

            var overflow = new List<string>();
            foreach (var item in grid.Items)
            {
                if (filter != null && !filter(item))
                {
                    continue;
                }

                if (item.Overflow || item.PlacedWidth <= 0 || item.PlacedHeight <= 0)
                {
                    overflow.Add(CharacterWidgets.Describe(_host, item));
                    continue;
                }

                var itemSize = InventoryGridArt.ItemSize(
                    item.PlacedWidth,
                    item.PlacedHeight,
                    cell,
                    CharacterMetrics.GridGap);
                var offset = InventoryGridArt.Offset(item.X, item.Y, cell, CharacterMetrics.GridGap);
                ItemBox(item, gridRoot.transform, offset.x, offset.y, itemSize.x, itemSize.y);
            }

            return string.Join(", ", overflow);
        }

        private bool CanPlace(GridContainer grid, string instanceId, int x, int y, bool rotated)
        {
            var item = grid.Find(instanceId);
            if (item == null || item.Overflow)
            {
                return false;
            }

            var template = CharacterWidgets.Template(_host, item);
            return template != null && grid.CanPlace(template, x, y, rotated, instanceId);
        }

        private void ItemBox(ItemInstance item, Transform parent, float x, float y, float width, float height)
        {
            var template = CharacterWidgets.Template(_host, item);
            var isArtifact = template?.Category == ItemCategory.Artifact;
            Sprite? sprite = null;
            var hasIcon = template != null && _host.Sprites.TryGet(template.Icon, out sprite) && sprite != null;
            var frame = CharacterWidgets.Box(_host, parent, "Item_" + item.InstanceId, x, y, width, height,
                hasIcon ? Color.clear : CharacterWidgets.ColorFor(_host, item));
            frame.raycastTarget = true;
            CharacterWidgets.AttachItem(_host, _frameHost, frame, item, null, -1, DragOrigin.Inventory, _tooltip);

            // Подпись занимает долю клетки, а не фиксированные 30 px: иначе в клетке 1×1
            // на иконку остаётся 12–14 px и мелкие предметы (артефакты, патроны) теряются.
            // У артефакта подписи нет — иконка занимает почти всю клетку, имя есть в подсказке.
            var labelHeight = isArtifact ? 0f : Mathf.Clamp(height * 0.3f, 14f, 30f);
            if (hasIcon)
            {
                var iconHeight = height - labelHeight - (isArtifact ? 4f : 6f);
                if (iconHeight > 8f)
                {
                    UiWidgets.ItemIcon(_host, parent, "ItemIcon_" + item.InstanceId, sprite!,
                        x, y, width, iconHeight, 4f);
                }
            }

            if (!isArtifact)
            {
                var text = hasIcon && template != null && template.MaxStack > 1
                    ? "×" + item.Stack
                    : CharacterWidgets.Describe(_host, item);
                var label = CharacterWidgets.Label(_host, parent, "ItemLabel_" + item.InstanceId,
                    text, 11,
                    hasIcon ? TextAnchor.LowerCenter : TextAnchor.MiddleCenter, UiFactory.TextColor,
                    x + 4f,
                    hasIcon ? y + height - labelHeight - 2f : y + 4f,
                    width - 8f,
                    hasIcon ? labelHeight : height - 8f);
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }

            RarityFrame.Draw(_host.Ui, frame.rectTransform, ItemRarity.Resolve(item, template));
        }
    }
}
