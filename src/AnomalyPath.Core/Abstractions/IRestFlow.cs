#nullable enable
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Отдых: предложение по текущему состоянию и восстановление выносливости.</summary>
    internal interface IRestFlow
    {
        /// <summary>Предложение отдыха: скорость восстановления, предел часов и риск нападения.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Предложение отдыха.</returns>
        RestQuote GetRestQuote(GameSession session);

        /// <summary>Отдыхает выбранное число часов.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="hours">Сколько часов отдыхать.</param>
        /// <returns>Итог отдыха и возможное нападение.</returns>
        RestResult Rest(GameSession session, int hours);
    }
}
