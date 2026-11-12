#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.World
{
    /// <summary>
    /// Точка хаба на карте мира: прямоугольник в долях картинки карты, где 0 — левый или
    /// нижний край, а 1 — правый или верхний. По ней экран карты строит кликабельную вырезку,
    /// поэтому подпись на картинке не нужна.
    /// </summary>
    public sealed class WorldMapSpot
    {
        /// <summary>Создаёт точку карты.</summary>
        /// <param name="left">Левая граница в долях ширины картинки, 0..1.</param>
        /// <param name="bottom">Нижняя граница в долях высоты картинки, 0..1.</param>
        /// <param name="right">Правая граница в долях ширины картинки, 0..1.</param>
        /// <param name="top">Верхняя граница в долях высоты картинки, 0..1.</param>
        public WorldMapSpot(double left, double bottom, double right, double top)
        {
            Left = left;
            Bottom = bottom;
            Right = right;
            Top = top;
        }

        /// <summary>Левая граница в долях ширины картинки.</summary>
        public double Left { get; }

        /// <summary>Нижняя граница в долях высоты картинки.</summary>
        public double Bottom { get; }

        /// <summary>Правая граница в долях ширины картинки.</summary>
        public double Right { get; }

        /// <summary>Верхняя граница в долях высоты картинки.</summary>
        public double Top { get; }

        /// <summary>Читает точку карты из блока данных локации.</summary>
        /// <param name="value">Объект блока <c>worldMap</c>.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Точка карты.</returns>
        /// <exception cref="DataException">Поле отсутствует, не число или вне границ.</exception>
        public static WorldMapSpot FromJson(JsonValue value, string sourcePath)
        {
            var left = Read(value, "left", sourcePath);
            var bottom = Read(value, "bottom", sourcePath);
            var right = Read(value, "right", sourcePath);
            var top = Read(value, "top", sourcePath);

            if (left >= right)
            {
                throw new DataException(sourcePath, CoreErrors.WorldMapSpotOrder, "left/right");
            }

            if (bottom >= top)
            {
                throw new DataException(sourcePath, CoreErrors.WorldMapSpotOrder, "bottom/top");
            }

            return new WorldMapSpot(left, bottom, right, top);
        }

        private static double Read(JsonValue value, string field, string sourcePath)
        {
            var number = DataMap.RequireDouble(value, field, sourcePath);
            if (number < 0 || number > 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldMapSpotRange, field);
            }

            return number;
        }
    }
}
