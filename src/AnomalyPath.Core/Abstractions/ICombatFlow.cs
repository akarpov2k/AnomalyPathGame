#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Бой в сессии: старт, действия игрока, ход ИИ, побег и завершение.</summary>
    internal interface ICombatFlow
    {
        /// <summary>Начинает бой с противником.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="playerActor">Актёр игрока.</param>
        /// <param name="enemy">Противник.</param>
        /// <param name="scenario">Сценарий боя; <c>null</c> — обычный бой.</param>
        /// <param name="context">Контекст боя вне локации; <c>null</c> — обычный бой.</param>
        /// <returns><c>true</c>, если бой начат.</returns>
        bool StartCombat(
            GameSession session,
            ActorTemplate playerActor,
            ActorTemplate enemy,
            CombatScenario? scenario = null,
            CombatEncounterContext? context = null);

        /// <summary>Выполняет атаку текущим участником.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="weapon">Оружие атакующего.</param>
        /// <param name="result">Результат атаки.</param>
        /// <returns><c>true</c>, если атака выполнена.</returns>
        bool TryAttack(GameSession session, ItemInstance weapon, out CombatAttackResult result);

        /// <summary>Действия игрока в бою.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Действия в порядке показа; пустой список, если боя нет.</returns>
        IReadOnlyList<CombatAction> GetCombatActions(GameSession session);

        /// <summary>Применяет скилл активного оружия игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="skillId">Идентификатор скилла.</param>
        /// <param name="result">Выстрелы, урон и смерть цели.</param>
        /// <returns><c>true</c>, если скилл выполнен.</returns>
        bool TryUseCombatSkill(GameSession session, string skillId, out CombatSkillResult result);

        /// <summary>Перезаряжает оружие из инвентаря игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="weapon">Оружие.</param>
        /// <returns><c>true</c>, если перезарядка выполнена.</returns>
        bool TryReload(GameSession session, ItemInstance weapon);

        /// <summary>Завершает ход и передаёт его противнику.</summary>
        /// <param name="session">Игровая сессия.</param>
        void EndTurn(GameSession session);

        /// <summary>Выполняет одно действие хода противника.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="action">Выполненное действие.</param>
        /// <returns><c>true</c>, если действие выполнено.</returns>
        bool TryRunEnemyTurnStep(GameSession session, out EnemyTurnAction action);

        /// <summary>Ход противника целиком.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Действия противника.</returns>
        EnemyTurnResult RunEnemyTurn(GameSession session);

        /// <summary>Меняет активное оружие игрока за ОД.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns><c>true</c>, если оружие сменено.</returns>
        bool TrySwapWeapon(GameSession session);

        /// <summary>
        /// Достаёт оружие из быстрого слота: оно встаёт в активный слот, прежнее активное оружие —
        /// в этот быстрый слот.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quickIndex">Индекс быстрого слота.</param>
        /// <returns><c>true</c>, если оружие сменено.</returns>
        bool TryUseQuickWeapon(GameSession session, int quickIndex);

        /// <summary>Проверяет, можно ли применить медицину из быстрого слота.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="item">Экземпляр расходника.</param>
        /// <returns><c>true</c>, если применение пройдёт.</returns>
        bool CanUseMedicine(GameSession session, ItemInstance item);

        /// <summary>Применяет медицину из быстрого слота в бою.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="item">Экземпляр расходника.</param>
        /// <param name="result">Сколько вылечено, восстановлено ОД, зарядов и травм.</param>
        /// <returns><c>true</c>, если применение состоялось.</returns>
        bool TryUseMedicine(GameSession session, ItemInstance item, out ConsumableUseResult result);

        /// <summary>Побег из боя.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="result">Исход побега.</param>
        /// <returns><c>true</c>, если побег выполнен.</returns>
        bool TryFlee(GameSession session, out FleeResult result);

        /// <summary>Завершает бой без победителя.</summary>
        /// <param name="session">Игровая сессия.</param>
        void EndCombat(GameSession session);
    }
}
