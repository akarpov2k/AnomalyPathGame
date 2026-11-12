#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Сюжетные квесты: выдача и сдача, прогресс целей, сброс области прогресса.
    /// Квест выдаётся и сдаётся в диалоге, поэтому сервис не знает о доске объявлений.
    /// </summary>
    public interface IStoryQuestService
    {
        /// <summary>Возвращает сюжетные квесты, которые персонаж может предложить сейчас.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">Кто предлагает квест.</param>
        /// <returns>Доступные квесты в порядке данных.</returns>
        IReadOnlyList<StoryQuestTemplate> GetAvailable(GameSession session, string actorId);

        /// <summary>Проверяет, доступен ли квест у персонажа.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="actorId">Кто предлагает квест.</param>
        /// <returns><c>true</c>, если квест можно взять.</returns>
        bool IsAvailable(GameSession session, string questId, string actorId);

        /// <summary>Берёт сюжетный квест в выбранном варианте.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="variantId">Вариант условий; пусто — стандартный.</param>
        /// <returns>Результат взятия.</returns>
        QuestResult Accept(GameSession session, string questId, string? variantId = null);

        /// <summary>Сдаёт выполненный сюжетный квест и выдаёт награду.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <returns>Результат сдачи с наградой.</returns>
        QuestResult TurnIn(GameSession session, string questId);

        /// <summary>Возвращает активные сюжетные квесты.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Активные квесты в порядке взятия.</returns>
        IReadOnlyList<StoryQuestState> GetActive(GameSession session);

        /// <summary>
        /// Цепочки локаций, нужные активным квестам: генератор включает их в маршрут,
        /// пока цель зачистки цепочки не выполнена.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Идентификаторы цепочек без повторов; пусто — активных целей цепочки нет.</returns>
        IReadOnlyList<string> GetActiveChains(GameSession session);

        /// <summary>Ищет шаблон сюжетного квеста для отображения.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="template">Найденный шаблон или <c>null</c>.</param>
        /// <returns><c>true</c>, если шаблон найден.</returns>
        bool TryGetTemplate(string questId, out StoryQuestTemplate template);

        /// <summary>Ищет активный сюжетный квест по идентификатору.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="quest">Найденное состояние или <c>null</c>.</param>
        /// <returns><c>true</c>, если квест взят и ещё не сдан.</returns>
        bool TryGetActive(GameSession session, string questId, out StoryQuestState quest);

        /// <summary>Проверяет, выполнены ли все цели квеста.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quest">Состояние квеста.</param>
        /// <returns><c>true</c>, если квест готов к сдаче.</returns>
        bool IsReady(GameSession session, StoryQuestState quest);

        /// <summary>Учитывает убийство актёра.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">Кого убили.</param>
        void NotifyActorKilled(GameSession session, string actorId);

        /// <summary>Учитывает выход с зачищенной локации.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="instanceId">Экземпляр локации.</param>
        /// <param name="templateId">Шаблон локации.</param>
        /// <param name="subtype">Подтип локации.</param>
        void NotifyLocationCleared(
            GameSession session,
            string instanceId,
            string templateId,
            LocationSubtype subtype);

        /// <summary>Учитывает прибытие на локацию.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="instanceId">Экземпляр локации.</param>
        /// <param name="templateId">Шаблон локации.</param>
        void NotifyLocationReached(GameSession session, string instanceId, string templateId);

        /// <summary>Учитывает вскрытие тайника.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="instanceId">Экземпляр локации.</param>
        void NotifyStashOpened(GameSession session, string instanceId);

        /// <summary>Учитывает осмотр объекта локации.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="instanceId">Экземпляр локации.</param>
        /// <param name="kind">Вид объекта.</param>
        /// <param name="objectKey">Идентификатор объекта для дедупликации.</param>
        void NotifyObjectInspected(
            GameSession session,
            string instanceId,
            LocationObjectKind kind,
            string objectKey);

        /// <summary>Учитывает разговор с персонажем.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">С кем поговорили.</param>
        void NotifyTalkedToActor(GameSession session, string actorId);

        /// <summary>Начинает рейд при выходе из хаба.</summary>
        /// <param name="session">Игровая сессия.</param>
        void OnLeaveHub(GameSession session);

        /// <summary>
        /// Завершает рейд при входе в хаб. Прогресс целей области «рейд» сохраняется:
        /// такой квест сдаётся в хабе, и счёт рейда должен пережить возврат.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        void OnEnterHub(GameSession session);

        /// <summary>Сбрасывает прогресс областей «рейд» и «жизнь» при смерти.</summary>
        /// <param name="session">Игровая сессия.</param>
        void OnDeath(GameSession session);
    }
}
