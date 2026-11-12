#nullable enable
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Поиск в аномалии: радар показывает силу сигнала по клеткам, обыск клетки тратит
    /// попытку и наносит урон, а совпадение с клеткой артефакта отдаёт находку.
    /// </summary>
    public interface IAnomalyService
    {
        /// <summary>
        /// Открывает радар: при первом обращении раскладывает артефакты по клеткам
        /// предварительными бросками на каждую попытку.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия на текущей локации.</param>
        /// <returns>Состояние радара или <c>null</c>, если радар не настроен.</returns>
        /// <exception cref="System.ArgumentNullException">Сессия или аномалия не заданы.</exception>
        RadarState? OpenRadar(GameSession session, AnomalyLocationObject anomaly);

        /// <summary>Сканирует клетку: бесплатно, не тратит попытку, показывает силу сигнала.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия.</param>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Сигнал и остаток сканов.</returns>
        RadarScanResult Scan(GameSession session, AnomalyLocationObject anomaly, int cell);

        /// <summary>
        /// Обыскивает клетку: тратит попытку, наносит урон, при совпадении отдаёт артефакт.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="anomaly">Объект-аномалия.</param>
        /// <param name="cell">Номер клетки.</param>
        /// <returns>Урон, находка и остаток попыток.</returns>
        AnomalySearchResult SearchCell(GameSession session, AnomalyLocationObject anomaly, int cell);
    }
}