#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Состояние радара: размер поля, остаток сканов и попыток, отмеченные клетки.
    /// </summary>
    public sealed class RadarState
    {
        /// <summary>Создаёт снимок состояния радара.</summary>
        /// <param name="width">Ширина поля.</param>
        /// <param name="height">Высота поля.</param>
        /// <param name="scansTotal">Всего сканов.</param>
        /// <param name="attemptsTotal">Всего попыток обыска.</param>
        /// <param name="attemptsUsed">Потрачено попыток.</param>
        /// <param name="scannedCells">Отсканированные клетки.</param>
        /// <param name="searchedCells">Обысканные клетки.</param>
        /// <param name="bands">Пороги расстояния для силы сигнала.</param>
        /// <exception cref="ArgumentNullException">Списки не заданы.</exception>
        public RadarState(
            int width,
            int height,
            int scansTotal,
            int attemptsTotal,
            int attemptsUsed,
            IReadOnlyList<int> scannedCells,
            IReadOnlyList<int> searchedCells,
            IReadOnlyList<int> bands)
        {
            Width = width;
            Height = height;
            ScansTotal = scansTotal;
            AttemptsTotal = attemptsTotal;
            AttemptsUsed = attemptsUsed;
            ScannedCells = scannedCells ?? throw new ArgumentNullException(nameof(scannedCells));
            SearchedCells = searchedCells ?? throw new ArgumentNullException(nameof(searchedCells));
            Bands = bands ?? throw new ArgumentNullException(nameof(bands));
        }

        /// <summary>Ширина поля в клетках.</summary>
        public int Width { get; }

        /// <summary>Высота поля в клетках.</summary>
        public int Height { get; }

        /// <summary>Всего клеток.</summary>
        public int CellCount => Width * Height;

        /// <summary>Всего сканов.</summary>
        public int ScansTotal { get; }

        /// <summary>Сколько сканов осталось.</summary>
        public int ScansLeft => Math.Max(0, ScansTotal - ScannedCells.Count);

        /// <summary>Всего попыток обыска.</summary>
        public int AttemptsTotal { get; }

        /// <summary>Потрачено попыток обыска.</summary>
        public int AttemptsUsed { get; }

        /// <summary>Сколько попыток осталось.</summary>
        public int AttemptsLeft => Math.Max(0, AttemptsTotal - AttemptsUsed);

        /// <summary>Отсканированные клетки.</summary>
        public IReadOnlyList<int> ScannedCells { get; }

        /// <summary>Обысканные клетки.</summary>
        public IReadOnlyList<int> SearchedCells { get; }

        /// <summary>Пороги расстояния для силы сигнала.</summary>
        public IReadOnlyList<int> Bands { get; }
    }
}
