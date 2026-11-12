#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Stage
{
    /// <summary>
    /// Точка земли на сцене локации: где фигура стоит и каким будет её размер.
    /// Координаты — доли картинки дороги (0 — левый или нижний край, 1 — правый или
    /// верхний), масштаб — пиксели картинки на метр высоты предмета. Поэтому точка
    /// не зависит ни от разрешения экрана, ни от того, как вписана картинка.
    /// </summary>
    public sealed class StageAnchor
    {
        /// <summary>Создаёт точку земли.</summary>
        /// <param name="slot">Слот сцены, который занимает точка.</param>
        /// <param name="x">Положение по ширине картинки, 0..1.</param>
        /// <param name="y">Положение по высоте картинки, 0..1 — низ фигуры.</param>
        /// <param name="pixelsPerMeter">Сколько пикселей картинки приходится на метр высоты.</param>
        /// <exception cref="ArgumentException">Слот не определён.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Координаты вне 0..1 или масштаб не положителен.</exception>
        public StageAnchor(StageSlot slot, double x, double y, double pixelsPerMeter)
        {
            if (!Enum.IsDefined(typeof(StageSlot), slot))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.StageUnknownSlot, slot),
                    nameof(slot));
            }

            if (x < 0 || x > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(x));
            }

            if (y < 0 || y > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(y));
            }

            if (pixelsPerMeter <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pixelsPerMeter));
            }

            Slot = slot;
            X = x;
            Y = y;
            PixelsPerMeter = pixelsPerMeter;
        }

        /// <summary>Слот сцены, который занимает точка.</summary>
        public StageSlot Slot { get; }

        /// <summary>Положение по ширине картинки, 0..1.</summary>
        public double X { get; }

        /// <summary>Положение по высоте картинки, 0..1 — низ фигуры.</summary>
        public double Y { get; }

        /// <summary>Сколько пикселей картинки приходится на метр высоты предмета.</summary>
        public double PixelsPerMeter { get; }

        /// <summary>
        /// Высота фигуры в долях высоты картинки дороги: из неё экран считает размер
        /// на любой ширине и разрешении.
        /// </summary>
        /// <param name="meters">Высота фигуры в метрах из записи спавна.</param>
        /// <param name="artHeight">Высота картинки дороги в пикселях.</param>
        /// <returns>Высота в долях высоты картинки, больше нуля.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Высота фигуры или картинки не положительна.</exception>
        public double HeightFraction(double meters, double artHeight)
        {
            if (meters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(meters));
            }

            if (artHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(artHeight));
            }

            return meters * PixelsPerMeter / artHeight;
        }

        /// <summary>Читает точку земли из элемента массива <c>anchors</c>.</summary>
        /// <param name="value">Объект точки.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Точка земли.</returns>
        /// <exception cref="DataException">Поля отсутствуют, неизвестны или вне границ.</exception>
        public static StageAnchor FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var slotCode = DataMap.RequireString(value, "slot", sourcePath);
            if (!SchemaEnum.TryParse(slotCode, out StageSlot slot))
            {
                throw new DataException(sourcePath, CoreErrors.StageUnknownSlot, slotCode);
            }

            var x = DataMap.RequireDouble(value, "x", sourcePath);
            if (x < 0 || x > 1)
            {
                throw new DataException(sourcePath, CoreErrors.StageRange, "x");
            }

            var y = DataMap.RequireDouble(value, "y", sourcePath);
            if (y < 0 || y > 1)
            {
                throw new DataException(sourcePath, CoreErrors.StageRange, "y");
            }

            var pixelsPerMeter = DataMap.RequireDouble(value, "ppm", sourcePath);
            if (pixelsPerMeter <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.StageRange, "ppm");
            }

            return new StageAnchor(slot, x, y, pixelsPerMeter);
        }
    }
}
