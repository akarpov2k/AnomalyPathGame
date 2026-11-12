#nullable enable
using AnomalyPath.Core.Dialog;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Диалоги: начало разговора, показ узла, выбор варианта и последствия выбора.
    /// Состояние разговора — это пара «диалог + узел», сервис его не хранит.
    /// </summary>
    public interface IDialogService
    {
        /// <summary>Проверяет, можно ли начать диалог.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="dialogId">Идентификатор диалога.</param>
        /// <returns><c>true</c>, если условия диалога выполнены.</returns>
        bool CanStart(GameSession session, string dialogId);

        /// <summary>Начинает диалог и отдаёт начальный узел.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="dialogId">Идентификатор диалога.</param>
        /// <param name="actorId">С кем говорят; засчитывается цель «поговорить».</param>
        /// <returns>Начальный узел или <c>null</c>, если диалог недоступен.</returns>
        DialogView? Start(GameSession session, string dialogId, string? actorId = null);

        /// <summary>Возвращает узел диалога, если его условия выполнены.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="dialogId">Идентификатор диалога.</param>
        /// <param name="nodeId">Идентификатор узла.</param>
        /// <returns>Узел или <c>null</c>, если он недоступен.</returns>
        DialogView? Node(GameSession session, string dialogId, string nodeId);

        /// <summary>Выбирает вариант ответа, применяет последствия и переходит дальше.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="dialogId">Идентификатор диалога.</param>
        /// <param name="nodeId">Идентификатор текущего узла.</param>
        /// <param name="optionId">Идентификатор варианта.</param>
        /// <returns>Следующий узел, конец разговора или ошибка.</returns>
        DialogResult Choose(
            GameSession session,
            string dialogId,
            string nodeId,
            string optionId);
    }
}
