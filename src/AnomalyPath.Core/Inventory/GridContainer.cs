#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Сетка предметов: прямоугольник клеток, в котором экземпляры занимают позиции.
    /// Сама сетка не решает про стаки и автоподбор — это работа сервиса инвентаря.
    /// </summary>
    public sealed class GridContainer
    {
        private readonly List<ItemInstance> _items = new List<ItemInstance>();

        /// <summary>Создаёт пустую сетку.</summary>
        /// <param name="id">Идентификатор контейнера.</param>
        /// <param name="width">Ширина в клетках.</param>
        /// <param name="rows">Высота в клетках.</param>
        public GridContainer(string id, int width, int rows)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            if (width < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    CoreText.Format(CoreErrors.MinimumOne, nameof(width)));
            }

            if (rows < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    CoreText.Format(CoreErrors.MinimumOne, nameof(rows)));
            }

            Id = id;
            Width = width;
            Rows = rows;
            MaxRows = rows;
            WeightLimit = double.PositiveInfinity;
        }

        /// <summary>Идентификатор контейнера.</summary>
        public string Id { get; }

        /// <summary>Ширина сетки в клетках.</summary>
        public int Width { get; }

        /// <summary>Высота сетки в клетках.</summary>
        public int Rows { get; private set; }

        /// <summary>
        /// Предел высоты, до которого сетка добавляет ряды при нехватке места.
        /// Равен высоте сетки, пока рост не разрешён явно.
        /// </summary>
        public int MaxRows { get; private set; }

        /// <summary>
        /// Строки, занятые предметами: высота самой нижней занятой клетки.
        /// Пустая сетка возвращает 0, переполнение не считается.
        /// </summary>
        public int UsedRows
        {
            get
            {
                var used = 0;
                foreach (var item in _items)
                {
                    if (item.Overflow || item.PlacedHeight <= 0)
                    {
                        continue;
                    }

                    var bottom = item.Y + item.PlacedHeight;
                    if (bottom > used)
                    {
                        used = bottom;
                    }
                }

                return used;
            }
        }

        /// <summary>
        /// Предел веса содержимого в килограммах; <see cref="double.PositiveInfinity"/> —
        /// без ограничения (так живут склады, торговцы и окна показа).
        /// </summary>
        public double WeightLimit { get; private set; }

        /// <summary>Задаёт предел веса содержимого.</summary>
        /// <param name="weightLimit">Предел в килограммах; неотрицательный, иначе бесконечность.</param>
        /// <exception cref="ArgumentOutOfRangeException">Предел отрицательный или не число.</exception>
        public void SetWeightLimit(double weightLimit)
        {
            if (double.IsNaN(weightLimit) || weightLimit < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(weightLimit),
                    CoreText.Format(CoreErrors.NonNegative, nameof(weightLimit)));
            }

            WeightLimit = weightLimit;
        }

        /// <summary>
        /// Разрешает сетке добавлять ряды, когда предмет не помещается в текущую высоту.
        /// Предел не может быть меньше текущей высоты.
        /// </summary>
        /// <param name="maxRows">Предел высоты в клетках.</param>
        /// <exception cref="ArgumentOutOfRangeException">Предел меньше текущей высоты.</exception>
        public void AllowGrowth(int maxRows)
        {
            if (maxRows < Rows)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxRows),
                    CoreText.Format(CoreErrors.RangeOrder, nameof(maxRows)));
            }

            MaxRows = maxRows;
        }

        /// <summary>Все экземпляры, включая лежащие в переполнении.</summary>
        public IReadOnlyList<ItemInstance> Items => _items;

        /// <summary>
        /// Меняет высоту сетки: не поместившиеся предметы уходят в переполнение.
        /// Предел авторасширения поднимается до новой высоты, чтобы он не отставал от сетки.
        /// </summary>
        /// <param name="rows">Новая высота в клетках.</param>
        /// <exception cref="ArgumentOutOfRangeException">Высота меньше одной клетки.</exception>
        public void Resize(int rows)
        {
            if (rows < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rows),
                    CoreText.Format(CoreErrors.MinimumOne, nameof(rows)));
            }

            Rows = rows;
            if (MaxRows < rows)
            {
                MaxRows = rows;
            }
            foreach (var item in _items)
            {
                if (!item.Overflow && item.Y + item.PlacedHeight > rows)
                {
                    AddOverflow(item);
                }
            }
        }

        /// <summary>Занятые клетки сетки.</summary>
        public int UsedCells
        {
            get
            {
                var cells = 0;
                foreach (var item in _items)
                {
                    if (!item.Overflow)
                    {
                        cells += item.PlacedWidth * item.PlacedHeight;
                    }
                }

                return cells;
            }
        }

        /// <summary>Свободные клетки сетки.</summary>
        public int FreeCells => (Width * Rows) - UsedCells;

        /// <summary>Проверяет, можно ли поставить предмет в указанную позицию.</summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="x">Колонка левого верхнего угла.</param>
        /// <param name="y">Строка левого верхнего угла.</param>
        /// <param name="rotated">Повёрнут ли предмет.</param>
        /// <param name="exceptInstanceId">Экземпляр, который не учитывается при проверке.</param>
        /// <returns><c>true</c>, если предмет помещается.</returns>
        public bool CanPlace(ItemTemplate template, int x, int y, bool rotated, string? exceptInstanceId = null)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            var useRotation = rotated && template.Rotatable;
            var width = useRotation ? template.FootprintHeight : template.FootprintWidth;
            var height = useRotation ? template.FootprintWidth : template.FootprintHeight;

            if (x < 0 || y < 0 || x + width > Width || y + height > Rows)
            {
                return false;
            }

            foreach (var other in _items)
            {
                if (other.Overflow || other.InstanceId == exceptInstanceId)
                {
                    continue;
                }

                var overlaps = x < other.X + other.PlacedWidth &&
                               x + width > other.X &&
                               y < other.Y + other.PlacedHeight &&
                               y + height > other.Y;
                if (overlaps)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Ставит предмет в сетку.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="x">Колонка.</param>
        /// <param name="y">Строка.</param>
        /// <param name="rotated">Поворот.</param>
        /// <exception cref="Data.DataException">Предмет не помещается.</exception>
        public void Place(ItemInstance item, ItemTemplate template, int x, int y, bool rotated)
        {
            if (!CanPlace(template, x, y, rotated, item.InstanceId))
            {
                throw new Data.DataException(item.ItemId, CoreErrors.ItemDoesNotFit);
            }

            var useRotation = rotated && template.Rotatable;
            item.X = x;
            item.Y = y;
            item.Rotated = useRotation;
            item.PlacedWidth = useRotation ? template.FootprintHeight : template.FootprintWidth;
            item.PlacedHeight = useRotation ? template.FootprintWidth : template.FootprintHeight;
            item.Overflow = false;

            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        /// <summary>Добавляет экземпляр в переполнение.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        public void AddOverflow(ItemInstance item)
        {
            item.X = -1;
            item.Y = -1;
            item.Rotated = false;
            item.PlacedWidth = 0;
            item.PlacedHeight = 0;
            item.Overflow = true;

            if (!_items.Contains(item))
            {
                _items.Add(item);
            }
        }

        /// <summary>Глубокая копия сетки для проверок «поместится ли» без изменения настоящей.</summary>
        /// <returns>Копия с теми же размерами и размещёнными предметами.</returns>
        public GridContainer Clone()
        {
            var clone = new GridContainer(Id + "#probe", Width, Rows);
            clone.MaxRows = MaxRows;
            clone.WeightLimit = WeightLimit;
            foreach (var item in _items)
            {
                if (item.Overflow)
                {
                    continue;
                }

                var copy = new ItemInstance(
                    item.InstanceId,
                    item.ItemId,
                    item.Stack,
                    item.Durability,
                    item.DurabilityMax,
                    item.VariantKey,
                    item.Magazine,
                    item.Charges,
                    item.AmmoItemId);
                copy.X = item.X;
                copy.Y = item.Y;
                copy.Rotated = item.Rotated;
                copy.PlacedWidth = item.PlacedWidth;
                copy.PlacedHeight = item.PlacedHeight;
                clone._items.Add(copy);
            }

            return clone;
        }

        /// <summary>Убирает экземпляр из контейнера.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        /// <returns><c>true</c>, если экземпляр был в контейнере.</returns>
        public bool Remove(ItemInstance item) => _items.Remove(item);

        /// <summary>Ищет экземпляр по id.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <returns>Экземпляр или <c>null</c>.</returns>
        public ItemInstance? Find(string instanceId)
        {
            foreach (var item in _items)
            {
                if (item.InstanceId == instanceId)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>Возвращает экземпляр, занимающий клетку.</summary>
        /// <param name="x">Колонка.</param>
        /// <param name="y">Строка.</param>
        /// <returns>Экземпляр или <c>null</c>.</returns>
        public ItemInstance? ItemAt(int x, int y)
        {
            foreach (var item in _items)
            {
                if (item.Overflow)
                {
                    continue;
                }

                if (x >= item.X && x < item.X + item.PlacedWidth &&
                    y >= item.Y && y < item.Y + item.PlacedHeight)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
