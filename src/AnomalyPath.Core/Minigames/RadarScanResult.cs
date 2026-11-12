#nullable enable
using AnomalyPath.Core.Anomalies;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>Результат скана клетки: сила сигнала и остаток сканов.</summary>
    public sealed class RadarScanResult
    {
        /// <summary>Создаёт результат скана.</summary>
        /// <param name="status">Итог скана.</param>
        /// <param name="signal">Сила сигнала 0..4; 0 — артефактов нет.</param>
        /// <param name="scansLeft">Сколько сканов осталось.</param>
        public RadarScanResult(AnomalySearchStatus status, int signal, int scansLeft)
        {
            Status = status;
            Signal = signal;
            ScansLeft = scansLeft;
        }

        /// <summary>Итог скана.</summary>
        public AnomalySearchStatus Status { get; }

        /// <summary>Сила сигнала 0..4.</summary>
        public int Signal { get; }

        /// <summary>Сколько сканов осталось.</summary>
        public int ScansLeft { get; }

        /// <summary>Скан выполнен.</summary>
        /// <param name="signal">Сила сигнала.</param>
        /// <param name="scansLeft">Остаток сканов.</param>
        /// <returns>Результат скана.</returns>
        public static RadarScanResult Success(int signal, int scansLeft) =>
            new RadarScanResult(AnomalySearchStatus.Success, signal, scansLeft);

        /// <summary>Скан не выполнен.</summary>
        /// <param name="status">Причина отказа.</param>
        /// <returns>Результат без сигнала.</returns>
        public static RadarScanResult Failed(AnomalySearchStatus status) =>
            new RadarScanResult(status, 0, 0);
    }
}
