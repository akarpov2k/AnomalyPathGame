#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Постановка фигуры на сцену локации: прямоугольник фигуры от точки земли и высоты
    /// в долях картинки дороги, контактная тень под ней и свечение вокруг. Считается
    /// в единицах канваса, поэтому от разрешения экрана не зависит.
    /// </summary>
    public static class StagePlacement
    {
        /// <summary>
        /// Прямоугольник фигуры: низ стоит на точке земли, ширина — по пропорции вырезки.
        /// </summary>
        /// <param name="height">Высота фигуры в единицах сцены.</param>
        /// <param name="aspect">Отношение ширины вырезки к её высоте; не положительно — квадрат.</param>
        /// <param name="ground">Точка земли в единицах сцены.</param>
        /// <returns>Прямоугольник фигуры.</returns>
        public static Rect Figure(float height, float aspect, Vector2 ground)
        {
            var safeHeight = Mathf.Max(1f, height);
            var safeAspect = aspect > 0f ? aspect : 1f;
            var width = safeHeight * safeAspect;
            return new Rect(ground.x - (width * 0.5f), ground.y, width, safeHeight);
        }

        /// <summary>
        /// Прямоугольник контактной тени: эллипс под фигурой, чуть шире её и низко.
        /// </summary>
        /// <param name="figure">Прямоугольник фигуры.</param>
        /// <param name="widthFactor">Насколько тень шире фигуры.</param>
        /// <param name="heightFactor">Высота тени относительно ширины фигуры.</param>
        /// <param name="sink">Насколько низ тени уходит ниже точки земли.</param>
        /// <returns>Прямоугольник тени.</returns>
        public static Rect Shadow(Rect figure, float widthFactor, float heightFactor, float sink)
        {
            var width = figure.width * Mathf.Max(0.1f, widthFactor);
            var height = figure.width * Mathf.Max(0.01f, heightFactor);
            return new Rect(
                figure.center.x - (width * 0.5f),
                figure.y - sink,
                width,
                height);
        }

        /// <summary>Прямоугольник свечения: фигура, расширенная на долю своего размера.</summary>
        /// <param name="figure">Прямоугольник фигуры.</param>
        /// <param name="padding">Доля размера фигуры, на которую свечение больше.</param>
        /// <returns>Прямоугольник свечения.</returns>
        public static Rect Glow(Rect figure, float padding)
        {
            var grow = Mathf.Max(0f, padding);
            var width = figure.width * (1f + (grow * 2f));
            var height = figure.height * (1f + (grow * 2f));
            return new Rect(
                figure.center.x - (width * 0.5f),
                figure.center.y - (height * 0.5f),
                width,
                height);
        }
    }
}
