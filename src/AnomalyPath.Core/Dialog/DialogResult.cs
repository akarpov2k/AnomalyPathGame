#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Результат выбора варианта ответа: следующий узел, конец диалога и выданное.</summary>
    public sealed class DialogResult
    {
        private DialogResult(
            DialogResultStatus status,
            DialogView? view,
            bool ended,
            string? questId,
            int money,
            IReadOnlyList<ItemInstance>? loot)
        {
            Status = status;
            View = view;
            Ended = ended;
            QuestId = questId;
            Money = money;
            Loot = loot ?? Array.Empty<ItemInstance>();
        }

        /// <summary>Статус шага.</summary>
        public DialogResultStatus Status { get; }

        /// <summary>Следующий узел; <c>null</c>, если диалог закрыт.</summary>
        public DialogView? View { get; }

        /// <summary>Закрылся ли диалог.</summary>
        public bool Ended { get; }

        /// <summary>Квест, выданный или принятый шагом, или <c>null</c>.</summary>
        public string? QuestId { get; }

        /// <summary>Выданные деньги.</summary>
        public int Money { get; }

        /// <summary>
        /// Предметы награды за сданный квест, ещё не попавшие в инвентарь:
        /// их забирает экран наград после разговора.
        /// </summary>
        public IReadOnlyList<ItemInstance> Loot { get; }

        /// <summary><c>true</c>, если шаг выполнен.</summary>
        public bool Succeeded => Status == DialogResultStatus.Ok;

        /// <summary>Успешный шаг с переходом на следующий узел.</summary>
        /// <param name="view">Следующий узел.</param>
        /// <param name="questId">Квест, выданный шагом, или <c>null</c>.</param>
        /// <param name="money">Выданные деньги.</param>
        /// <param name="loot">Предметы награды для экрана наград.</param>
        /// <returns>Результат.</returns>
        public static DialogResult Next(
            DialogView view,
            string? questId = null,
            int money = 0,
            IReadOnlyList<ItemInstance>? loot = null) =>
            new DialogResult(DialogResultStatus.Ok, view, false, questId, money, loot);

        /// <summary>Успешный шаг, закрывающий диалог.</summary>
        /// <param name="questId">Квест, выданный шагом, или <c>null</c>.</param>
        /// <param name="money">Выданные деньги.</param>
        /// <param name="loot">Предметы награды для экрана наград.</param>
        /// <returns>Результат.</returns>
        public static DialogResult Closed(
            string? questId = null,
            int money = 0,
            IReadOnlyList<ItemInstance>? loot = null) =>
            new DialogResult(DialogResultStatus.Ok, null, true, questId, money, loot);

        /// <summary>Ошибка шага.</summary>
        /// <param name="status">Статус ошибки.</param>
        /// <param name="questId">Квест, на котором произошла ошибка, или <c>null</c>.</param>
        /// <returns>Результат.</returns>
        public static DialogResult Error(DialogResultStatus status, string? questId = null) =>
            new DialogResult(status, null, false, questId, 0, null);
    }
}
