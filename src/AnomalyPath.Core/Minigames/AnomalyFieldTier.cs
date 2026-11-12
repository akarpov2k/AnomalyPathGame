#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Параметры радара для одного тира: число сканов и пороги силы сигнала.
    /// Размер поля общий для всех тиров и задан в <see cref="AnomalyFieldSettings"/>.
    /// </summary>
    public sealed class AnomalyFieldTier
    {
        /// <summary>Создаёт настройку поля.</summary>
        /// <param name="tier">Тир локации, не меньше нуля.</param>
        /// <param name="scans">Сколько клеток можно отсканировать, от 1 до 50.</param>
        /// <param name="bands">Три возрастающих порога расстояния для сигнала 4, 3 и 2.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        /// <exception cref="ArgumentNullException">Пороги не заданы.</exception>
        /// <exception cref="ArgumentException">Пороги не возрастают.</exception>
        public AnomalyFieldTier(int tier, int scans, IReadOnlyList<int> bands)
        {
            if (tier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tier));
            }

            if (scans < 1 || scans > 50)
            {
                throw new ArgumentOutOfRangeException(nameof(scans));
            }

            if (bands == null)
            {
                throw new ArgumentNullException(nameof(bands));
            }

            if (bands.Count != 3 || bands[0] < 0 || bands[0] >= bands[1] || bands[1] >= bands[2])
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RulesRange, "anomalyField.bands"),
                    nameof(bands));
            }

            Tier = tier;
            Scans = scans;
            Bands = bands;
        }

        /// <summary>Тир локации.</summary>
        public int Tier { get; }

        /// <summary>Сколько клеток можно отсканировать за аномалию.</summary>
        public int Scans { get; }

        /// <summary>Пороги расстояния: до первого — сигнал 4, до второго — 3, до третьего — 2.</summary>
        public IReadOnlyList<int> Bands { get; }
    }
}
