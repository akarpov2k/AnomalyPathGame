#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Core.Quests
{
    /// <summary>Результат взятия или сдачи квеста.</summary>
    public sealed class QuestResult
    {
        private QuestResult(
            QuestResultStatus status,
            string? questId,
            int money,
            IReadOnlyList<ItemInstance>? loot)
        {
            Status = status;
            QuestId = questId;
            Money = money;
            Loot = loot ?? Array.Empty<ItemInstance>();
        }

        /// <summary>Статус.</summary>
        public QuestResultStatus Status { get; }

        /// <summary>Идентификатор квеста, если известен.</summary>
        public string? QuestId { get; }

        /// <summary>Полученные деньги: они зачисляются сразу при сдаче.</summary>
        public int Money { get; }

        /// <summary>
        /// Предметы награды, ещё не попавшие в инвентарь: их забирает экран наград,
        /// а оставшееся при закрытии пропадает.
        /// </summary>
        public IReadOnlyList<ItemInstance> Loot { get; }

        /// <summary><c>true</c>, если действие выполнено.</summary>
        public bool Succeeded => Status == QuestResultStatus.Ok;

        /// <summary>Успешный результат.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="money">Полученные деньги.</param>
        /// <param name="loot">Предметы награды, которые забирает экран наград.</param>
        /// <returns>Результат.</returns>
        public static QuestResult Ok(
            string questId,
            int money = 0,
            IReadOnlyList<ItemInstance>? loot = null) =>
            new QuestResult(QuestResultStatus.Ok, questId, money, loot);

        /// <summary>Ошибка.</summary>
        /// <param name="status">Статус ошибки.</param>
        /// <param name="questId">Идентификатор квеста, если известен.</param>
        /// <returns>Результат.</returns>
        public static QuestResult Error(QuestResultStatus status, string? questId = null) =>
            new QuestResult(status, questId, 0, null);
    }
}
