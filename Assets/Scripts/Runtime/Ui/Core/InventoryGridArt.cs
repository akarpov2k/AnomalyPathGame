#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Шаблон сетки инвентаря: окно инвентаря — квадратные клетки от ширины (<see cref="CellSide"/>)
    /// и число строк по высоте (<see cref="FitRows"/>), окно набора — растяжение на область целиком
    /// (<see cref="CellSize"/>). Разметка — только внутренние разделители: внешнего контура нет,
    /// линии тонкие и тусклые, чтобы главной оставалась текстура окна. Раскладку ведут одни и те же
    /// формулы, поэтому клетки, предметы, подсветка сброса и перетаскивание не разъезжаются.
    /// </summary>
    public static class InventoryGridArt
    {
        /// <summary>Толщина линии разметки в пикселях UI.</summary>
        public const float LineThickness = 1f;

        /// <summary>Цвет линий разметки: серый с прозрачностью — заметнее на светлой панели, но не сплошная линия.</summary>
        public static readonly Color LineColor = new Color(0.55f, 0.60f, 0.58f, 0.42f);

        /// <summary>
        /// Считает клетку так, чтобы сетка заняла область целиком: и по ширине, и по высоте.
        /// Клетка не обязана быть квадратной.
        /// </summary>
        /// <param name="area">Размер свободной области окна.</param>
        /// <param name="columns">Число колонок.</param>
        /// <param name="rows">Число строк.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <returns>Ширина и высота одной клетки.</returns>
        public static Vector2 CellSize(Vector2 area, int columns, int rows, float gap)
        {
            var safeColumns = Mathf.Max(1, columns);
            var safeRows = Mathf.Max(1, rows);
            var width = (area.x - ((safeColumns - 1) * gap)) / safeColumns;
            var height = (area.y - ((safeRows - 1) * gap)) / safeRows;
            return new Vector2(Mathf.Max(1f, width), Mathf.Max(1f, height));
        }

        /// <summary>
        /// Сторона квадратной клетки по ширине области: клетки окна инвентаря не растягиваются
        /// по высоте, лишние ряды уходят в прокрутку.
        /// </summary>
        /// <param name="areaWidth">Ширина свободной области окна.</param>
        /// <param name="columns">Число колонок.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <returns>Сторона клетки.</returns>
        public static float CellSide(float areaWidth, int columns, float gap)
        {
            var safeColumns = Mathf.Max(1, columns);
            return Mathf.Max(1f, (areaWidth - ((safeColumns - 1) * gap)) / safeColumns);
        }

        /// <summary>
        /// Сколько строк квадратных клеток помещается в высоту области без прокрутки.
        /// Округляет вниз: лишняя строка ушла бы в прокрутку.
        /// </summary>
        /// <param name="areaHeight">Высота свободной области окна.</param>
        /// <param name="cellSide">Сторона квадратной клетки.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <returns>Число строк, не меньше одной.</returns>
        public static int FitRows(float areaHeight, float cellSide, float gap)
        {
            var step = Mathf.Max(1f, cellSide + gap);
            return Mathf.Max(1, Mathf.FloorToInt((Mathf.Max(0f, areaHeight) + gap) / step));
        }

        /// <summary>Размер области, занятой сеткой из указанного числа клеток.</summary>
        /// <param name="columns">Число колонок.</param>
        /// <param name="rows">Число строк.</param>
        /// <param name="cell">Размер клетки.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <returns>Ширина и высота области сетки.</returns>
        public static Vector2 GridSize(int columns, int rows, Vector2 cell, float gap) => new Vector2(
            (columns * cell.x) + ((columns - 1) * gap),
            (rows * cell.y) + ((rows - 1) * gap));

        /// <summary>Смещение левого верхнего угла клетки от начала сетки.</summary>
        /// <param name="column">Колонка.</param>
        /// <param name="row">Строка.</param>
        /// <param name="cell">Размер клетки.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <returns>Смещение в пикселях.</returns>
        public static Vector2 Offset(int column, int row, Vector2 cell, float gap) =>
            new Vector2(column * (cell.x + gap), row * (cell.y + gap));

        /// <summary>Размер предмета, занимающего прямоугольник клеток.</summary>
        /// <param name="width">Ширина в клетках.</param>
        /// <param name="height">Высота в клетках.</param>
        /// <param name="cell">Размер клетки.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <returns>Размер в пикселях с учётом зазоров.</returns>
        public static Vector2 ItemSize(int width, int height, Vector2 cell, float gap) =>
            GridSize(width, height, cell, gap);

        /// <summary>
        /// Рисует разметку сетки: только внутренние разделители, без рамки по периметру.
        /// </summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="parent">Корень сетки.</param>
        /// <param name="columns">Число колонок.</param>
        /// <param name="rows">Число строк.</param>
        /// <param name="cell">Размер клетки.</param>
        /// <param name="gap">Зазор между клетками.</param>
        /// <param name="origin">Смещение сетки от левого верхнего угла родителя.</param>
        public static void DrawLines(
            IUiHost host,
            Transform parent,
            int columns,
            int rows,
            Vector2 cell,
            float gap,
            Vector2 origin)
        {
            var size = GridSize(columns, rows, cell, gap);
            var half = LineThickness / 2f;
            for (var column = 1; column < columns; column++)
            {
                var x = origin.x + (column * (cell.x + gap)) - (gap / 2f) - half;
                Line(host, parent, "GridV" + column, x, origin.y, LineThickness, size.y);
            }

            for (var row = 1; row < rows; row++)
            {
                var y = origin.y + (row * (cell.y + gap)) - (gap / 2f) - half;
                Line(host, parent, "GridH" + row, origin.x, y, size.x, LineThickness);
            }
        }

        private static void Line(
            IUiHost host,
            Transform parent,
            string name,
            float x,
            float y,
            float width,
            float height)
        {
            var image = UiWidgets.Box(host, parent, name, x, y, width, height, LineColor);
            image.raycastTarget = false;
        }
    }
}
