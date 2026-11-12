#nullable enable
namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>Числа сетки окна торговли: зазор между клетками и минимальная сторона клетки.</summary>
    public static class TradeMetrics
    {
        /// <summary>Зазор между клетками сетки в единицах канваса.</summary>
        public const float GridGap = 8f;

        /// <summary>Минимальная сторона клетки: меньше некуда ставить иконку предмета.</summary>
        public const float MinCellSize = 24f;
    }
}
