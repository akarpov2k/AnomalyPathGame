#nullable enable
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Поиск артефакта в аномалии: радар, скан и обыск клетки.</summary>
    internal interface IAnomalyFlow
    {
        /// <summary>Открывает радар аномалии и при первом обращении раскладывает артефакты.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия на текущей локации.</param>
        /// <returns>Состояние радара или <c>null</c>, если радар не настроен.</returns>
        RadarState? OpenRadar(GameSession session, AnomalyLocationObject anomaly);

        /// <summary>Сканирует клетку радара: бесплатно, показывает силу сигнала.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия.</param>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Сигнал и остаток сканов.</returns>
        RadarScanResult Scan(GameSession session, AnomalyLocationObject anomaly, int cell);

        /// <summary>Обыскивает клетку аномалии.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия.</param>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Урон, находка и остаток попыток.</returns>
        AnomalySearchResult SearchCell(GameSession session, AnomalyLocationObject anomaly, int cell);
    }
}
