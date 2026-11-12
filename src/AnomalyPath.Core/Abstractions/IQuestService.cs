#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Повторяемые квесты: доска объявлений хаба, слоты с таймерами, прогресс и сдача.
    /// </summary>
    public interface IQuestService
    {
        /// <summary>Возвращает слоты доски объявлений хаба.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="boardLocationId">Экземпляр хаба, в котором стоит доска.</param>
        /// <returns>Слоты в порядке настройки; пустой список, если доска недоступна.</returns>
        IReadOnlyList<QuestBoardSlot> GetBoard(GameSession session, string boardLocationId);

        /// <summary>Берёт квест из слота доски.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="boardLocationId">Экземпляр хаба.</param>
        /// <param name="slot">Номер слота.</param>
        /// <returns>Результат взятия.</returns>
        QuestResult Accept(GameSession session, string boardLocationId, int slot);

        /// <summary>Отказывается от квеста в слоте; слот уходит на таймер.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="boardLocationId">Экземпляр хаба.</param>
        /// <param name="slot">Номер слота.</param>
        /// <returns>Результат отказа.</returns>
        QuestResult Decline(GameSession session, string boardLocationId, int slot);

        /// <summary>Отменяет взятый квест.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <returns>Результат отмены.</returns>
        QuestResult CancelQuest(GameSession session, string questId);

        /// <summary>Сдаёт выполненный квест на доске, где он был взят.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="boardLocationId">Экземпляр хаба.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <returns>Результат сдачи с наградой.</returns>
        QuestResult TurnIn(GameSession session, string boardLocationId, string questId);

        /// <summary>Возвращает активные квесты игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Список активных квестов.</returns>
        IReadOnlyList<QuestState> GetActive(GameSession session);

        /// <summary>Ищет шаблон квеста для отображения.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="template">Найденный шаблон или <c>null</c>.</param>
        /// <returns><c>true</c>, если шаблон найден.</returns>
        bool TryGetTemplate(string questId, out QuestTemplate template);

        /// <summary>Считает текущий прогресс квеста по состоянию игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quest">Состояние квеста.</param>
        /// <returns>Прогресс; для неизвестного квеста — пустой.</returns>
        QuestProgress GetProgress(GameSession session, QuestState quest);

        /// <summary>Учитывает убийство актёра в активных квестах.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">Кого убили.</param>
        void NotifyActorKilled(GameSession session, string actorId);

        /// <summary>Учитывает зачистку локации в активных квестах.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="locationInstanceId">Экземпляр зачищенной локации.</param>
        void NotifyLocationCleared(GameSession session, string locationInstanceId);

        /// <summary>Проваливает активные квесты, у которых истёк срок.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Сколько квестов провалено этим вызовом.</returns>
        int Expire(GameSession session);

        /// <summary>Проваливает активные квесты «за жизнь» после смерти игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        void NotifyPlayerDeath(GameSession session);
    }
}
