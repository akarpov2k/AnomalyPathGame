#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Combat;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Ход противника: детерминированный выбор действия по текущему состоянию.</summary>
    public interface ICombatAi
    {
        /// <summary>Строит план хода противника, не изменяя бой.</summary>
        /// <param name="state">Состояние боя; ход должен принадлежать противнику.</param>
        /// <returns>Шаги плана по порядку; пустой список, если ходить нечем.</returns>
        IReadOnlyList<EnemyTurnStep> Plan(CombatState state);

        /// <summary>
        /// Выбирает действие заново по текущему состоянию и применяет его.
        /// Следующее действие выбирается только после того, как предыдущее изменило бой.
        /// </summary>
        /// <param name="state">Состояние боя; ход должен принадлежать противнику.</param>
        /// <param name="action">Выполненное действие; при <c>false</c> значение не определено.</param>
        /// <returns><c>true</c>, если действие выполнено; <c>false</c> — ходить нечем.</returns>
        bool TryExecuteNext(CombatState state, out EnemyTurnAction action);

        /// <summary>Исполняет ход целиком: после каждого действия план пересчитывается.</summary>
        /// <param name="state">Состояние боя; ход должен принадлежать противнику.</param>
        /// <returns>Выполненные действия и результат хода.</returns>
        EnemyTurnResult TakeTurn(CombatState state);
    }
}
