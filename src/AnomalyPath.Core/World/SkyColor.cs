#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>Цвет в линейной шкале 0..1 без привязки к движку: светило, оттенок, тон.</summary>
    public readonly struct SkyColor
    {
        /// <summary>Создаёт цвет.</summary>
        /// <param name="r">Красный канал, 0..1.</param>
        /// <param name="g">Зелёный канал, 0..1.</param>
        /// <param name="b">Синий канал, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Канал вне 0..1 или не число.</exception>
        public SkyColor(float r, float g, float b)
        {
            Validate(r, nameof(r));
            Validate(g, nameof(g));
            Validate(b, nameof(b));
            R = r;
            G = g;
            B = b;
        }

        /// <summary>Красный канал, 0..1.</summary>
        public float R { get; }

        /// <summary>Зелёный канал, 0..1.</summary>
        public float G { get; }

        /// <summary>Синий канал, 0..1.</summary>
        public float B { get; }

        /// <summary>Смешивает два цвета линейно.</summary>
        /// <param name="from">Цвет при <paramref name="t"/> = 0.</param>
        /// <param name="to">Цвет при <paramref name="t"/> = 1.</param>
        /// <param name="t">Доля смешивания; значения вне 0..1 обрезаются.</param>
        /// <returns>Смешанный цвет.</returns>
        public static SkyColor Lerp(SkyColor from, SkyColor to, float t)
        {
            var value = Math.Clamp(t, 0f, 1f);
            return new SkyColor(
                from.R + ((to.R - from.R) * value),
                from.G + ((to.G - from.G) * value),
                from.B + ((to.B - from.B) * value));
        }

        /// <summary>Строка вида <c>SkyColor(1, 0.5, 0.25)</c> для тестов и логов.</summary>
        /// <returns>Текстовое представление.</returns>
        public override string ToString() => $"SkyColor({R:0.###}, {G:0.###}, {B:0.###})";

        private static void Validate(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
