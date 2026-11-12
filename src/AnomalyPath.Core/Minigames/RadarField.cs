#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Поле радара: клетки с артефактами и сила сигнала по манхэттенскому расстоянию.
    /// Клетки нумеруются построчно: <c>row * Width + column</c>.
    /// </summary>
    public sealed class RadarField
    {
        private readonly IReadOnlyList<int> _artifacts;
        private readonly IReadOnlyList<int> _bands;

        /// <summary>Создаёт поле радара.</summary>
        /// <param name="width">Ширина поля, от 2 до 10.</param>
        /// <param name="height">Высота поля, от 2 до 10.</param>
        /// <param name="artifacts">Клетки с артефактами; могут повторяться только в тестах.</param>
        /// <param name="bands">Три возрастающих порога расстояния для сигнала 4, 3 и 2.</param>
        /// <exception cref="ArgumentOutOfRangeException">Размер поля вне границ.</exception>
        /// <exception cref="ArgumentNullException">Списки не заданы.</exception>
        /// <exception cref="ArgumentException">Пороги не возрастают или клетка вне поля.</exception>
        public RadarField(int width, int height, IReadOnlyList<int> artifacts, IReadOnlyList<int> bands)
        {
            if (width < 2 || width > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 2 || height > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _bands = bands ?? throw new ArgumentNullException(nameof(bands));
            if (_bands.Count != 3 || _bands[0] < 0 || _bands[0] >= _bands[1] || _bands[1] >= _bands[2])
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RulesRange, "radar.bands"),
                    nameof(bands));
            }

            foreach (var cell in _artifacts)
            {
                if (cell < 0 || cell >= CellCount)
                {
                    throw new ArgumentOutOfRangeException(nameof(artifacts));
                }
            }
        }

        /// <summary>Ширина поля в клетках.</summary>
        public int Width { get; }

        /// <summary>Высота поля в клетках.</summary>
        public int Height { get; }

        /// <summary>Всего клеток.</summary>
        public int CellCount => Width * Height;

        /// <summary>Клетки с артефактами.</summary>
        public IReadOnlyList<int> Artifacts => _artifacts;

        /// <summary>
        /// Сила сигнала в клетке: 4 — ближе всего, 3 и 2 — дальше, 1 — далеко, 0 — артефактов нет.
        /// </summary>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Сигнал от 0 до 4.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Клетка вне поля.</exception>
        public int Signal(int cell)
        {
            CheckCell(cell);
            if (_artifacts.Count == 0)
            {
                return 0;
            }

            var nearest = int.MaxValue;
            foreach (var artifact in _artifacts)
            {
                var distance = Distance(cell, artifact);
                if (distance < nearest)
                {
                    nearest = distance;
                }
            }

            if (nearest <= _bands[0])
            {
                return 4;
            }

            if (nearest <= _bands[1])
            {
                return 3;
            }

            return nearest <= _bands[2] ? 2 : 1;
        }

        /// <summary>Манхэттенское расстояние между клетками.</summary>
        /// <param name="first">Первая клетка.</param>
        /// <param name="second">Вторая клетка.</param>
        /// <returns>Сумма разниц строк и столбцов.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Клетка вне поля.</exception>
        public int Distance(int first, int second)
        {
            CheckCell(first);
            CheckCell(second);
            return Math.Abs(first / Width - second / Width) + Math.Abs(first % Width - second % Width);
        }

        /// <summary>
        /// Раскладывает артефакты по разным клеткам детерминированно.
        /// </summary>
        /// <param name="seed">Seed раскладки.</param>
        /// <param name="width">Ширина поля.</param>
        /// <param name="height">Высота поля.</param>
        /// <param name="count">Сколько артефактов разложить.</param>
        /// <returns>Номера клеток без повторов.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Размер, число или счётчик вне границ.</exception>
        public static IReadOnlyList<int> Place(int seed, int width, int height, int count)
        {
            if (width < 2 || width > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 2 || height > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            if (count < 0 || count > width * height)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var result = new List<int>(count);
            var used = new HashSet<int>();
            var random = new SeededWorldRandom(seed);
            while (result.Count < count)
            {
                var cell = random.NextInt(0, width * height);
                if (used.Add(cell))
                {
                    result.Add(cell);
                }
            }

            return result;
        }

        private void CheckCell(int cell)
        {
            if (cell < 0 || cell >= CellCount)
            {
                throw new ArgumentOutOfRangeException(nameof(cell));
            }
        }
    }
}
