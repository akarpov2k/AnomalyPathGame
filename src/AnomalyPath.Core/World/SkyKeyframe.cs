#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>Ключевой кадр неба: час и цвета света, оттенка и дымки в этот час.</summary>
    public readonly struct SkyKeyframe
    {
        /// <summary>Создаёт ключевой кадр.</summary>
        /// <param name="hour">Час суток, 0..24.</param>
        /// <param name="light">Цвет светила.</param>
        /// <param name="tint">Цвет оттенка сцены.</param>
        /// <param name="tintAlpha">Сила оттенка, 0..1.</param>
        /// <param name="glow">Яркость светила, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public SkyKeyframe(double hour, SkyColor light, SkyColor tint, double tintAlpha, double glow)
        {
            if (double.IsNaN(hour) || hour < 0 || hour > 24)
            {
                throw new ArgumentOutOfRangeException(nameof(hour));
            }

            ValidateUnit(tintAlpha, nameof(tintAlpha));
            ValidateUnit(glow, nameof(glow));

            Hour = hour;
            Light = light;
            Tint = tint;
            TintAlpha = (float)tintAlpha;
            Glow = (float)glow;
        }

        /// <summary>Час суток, 0..24.</summary>
        public double Hour { get; }

        /// <summary>Цвет светила в этот час.</summary>
        public SkyColor Light { get; }

        /// <summary>Цвет оттенка сцены.</summary>
        public SkyColor Tint { get; }

        /// <summary>Сила оттенка сцены, 0..1.</summary>
        public float TintAlpha { get; }

        /// <summary>Яркость светила, 0..1.</summary>
        public float Glow { get; }

        private static void ValidateUnit(double value, string name)
        {
            if (double.IsNaN(value) || value < 0 || value > 1)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
