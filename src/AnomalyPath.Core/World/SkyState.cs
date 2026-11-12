#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>Состояние неба в текущий час: положение светила, его цвет и оттенок сцены.</summary>
    public readonly struct SkyState
    {
        /// <summary>Создаёт состояние неба.</summary>
        /// <param name="night">Ночью светит луна, днём — солнце.</param>
        /// <param name="elevation">Высота светила над горизонтом, 0..1.</param>
        /// <param name="azimuth">Путь светила по небу от восхода к закату, 0..1.</param>
        /// <param name="light">Цвет светила.</param>
        /// <param name="tint">Цвет оттенка сцены.</param>
        /// <param name="tintAlpha">Сила оттенка сцены, 0..1.</param>
        /// <param name="glow">Яркость светила, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public SkyState(
            bool night,
            float elevation,
            float azimuth,
            SkyColor light,
            SkyColor tint,
            float tintAlpha,
            float glow)
        {
            ValidateUnit(elevation, nameof(elevation));
            ValidateUnit(azimuth, nameof(azimuth));
            ValidateUnit(tintAlpha, nameof(tintAlpha));
            ValidateUnit(glow, nameof(glow));

            Night = night;
            Elevation = elevation;
            Azimuth = azimuth;
            Light = light;
            Tint = tint;
            TintAlpha = tintAlpha;
            Glow = glow;
        }

        /// <summary>Ночью светит луна, днём — солнце.</summary>
        public bool Night { get; }

        /// <summary>Высота светила над горизонтом, 0..1: 0 — на горизонте, 1 — в зените.</summary>
        public float Elevation { get; }

        /// <summary>Путь светила по небу от восхода к закату, 0..1.</summary>
        public float Azimuth { get; }

        /// <summary>Цвет светила.</summary>
        public SkyColor Light { get; }

        /// <summary>Цвет оттенка сцены.</summary>
        public SkyColor Tint { get; }

        /// <summary>Сила оттенка сцены, 0..1.</summary>
        public float TintAlpha { get; }

        /// <summary>Яркость светила, 0..1.</summary>
        public float Glow { get; }

        private static void ValidateUnit(float value, string name)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f || value > 1f)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }
    }
}
