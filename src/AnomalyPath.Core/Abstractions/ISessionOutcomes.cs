#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Death;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Итоги сессии: смерть, результат боя и побег с учётом начатого перехода между хабами.</summary>
    internal interface ISessionOutcomes
    {
        /// <summary>Применяет последствия смерти к вещам игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Что потеряно, что осталось и что спасла страховка.</returns>
        DeathResult ApplyDeath(GameSession session);

        /// <summary>Применяет итог боя: опыт и лут, либо смерть и респавн.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Результат боя.</returns>
        CombatOutcome ApplyCombatOutcome(GameSession session);

        /// <summary>Выполняет побег; при побеге с дороги завершает начатый переход в хаб.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="result">Исход побега.</param>
        /// <returns><c>true</c>, если побег выполнен.</returns>
        bool TryFlee(GameSession session, out FleeResult result);
    }
}
