#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Итог хода противника: действия по порядку, потраченные ОД и результат.</summary>
    public sealed class EnemyTurnResult
    {
        /// <summary>Пустой ход: противник ничего не сделал.</summary>
        public static EnemyTurnResult Empty { get; } = new EnemyTurnResult(Array.Empty<EnemyTurnAction>(), false);

        /// <summary>Создаёт итог хода.</summary>
        /// <param name="actions">Выполненные действия по порядку.</param>
        /// <param name="playerKilled"><c>true</c>, если игрок погиб за этот ход.</param>
        /// <exception cref="ArgumentNullException">Список действий не передан.</exception>
        public EnemyTurnResult(IReadOnlyList<EnemyTurnAction> actions, bool playerKilled)
        {
            Actions = actions ?? throw new ArgumentNullException(nameof(actions));
            PlayerKilled = playerKilled;
            var ap = 0;
            var damage = 0;
            for (var i = 0; i < actions.Count; i++)
            {
                ap += actions[i].Step.ApCost;
                damage += actions[i].Damage;
            }

            ApSpent = ap;
            Damage = damage;
        }

        /// <summary>Выполненные действия по порядку.</summary>
        public IReadOnlyList<EnemyTurnAction> Actions { get; }

        /// <summary>Сколько ОД потрачено.</summary>
        public int ApSpent { get; }

        /// <summary>Фактический суммарный урон.</summary>
        public int Damage { get; }

        /// <summary><c>true</c>, если игрок погиб за этот ход.</summary>
        public bool PlayerKilled { get; }
    }
}
