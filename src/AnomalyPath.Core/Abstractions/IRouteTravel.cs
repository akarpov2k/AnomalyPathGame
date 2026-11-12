#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Переходы по маршруту: рёбра, переход, выход из хаба и возврат в хаб.</summary>
    internal interface IRouteTravel
    {
        /// <summary>Рёбра, видимые из текущей локации; в бою список пуст.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Видимые рёбра.</returns>
        IReadOnlyList<WorldEdge> AvailableEdges(GameSession session);

        /// <summary>Выполняет переход, если он разрешён и игрок не в бою.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="edgeId">Id ребра.</param>
        /// <returns>Результат перехода.</returns>
        TravelResult Travel(GameSession session, string edgeId);

        /// <summary>Покидает хаб: строит новый маршрут и ведёт в первую локацию пути.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Результат перехода.</returns>
        TravelResult LeaveHub(GameSession session);

        /// <summary>Возвращает игрока в последний хаб с пути.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Результат возврата.</returns>
        TravelResult ReturnToHub(GameSession session);
    }
}
