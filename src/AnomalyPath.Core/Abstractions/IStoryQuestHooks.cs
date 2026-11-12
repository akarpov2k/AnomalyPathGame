#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Сообщения миру о событиях сессии: переходы, убийства, смерть, тайники.</summary>
    internal interface IStoryQuestHooks
    {
        /// <summary>Сообщает о переходе: зачистка покинутой локации, прибытие, вход и выход из хаба.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="previous">Покинутая локация.</param>
        /// <param name="current">Текущая локация.</param>
        void OnTravel(GameSession session, LocationInstance previous, LocationInstance current);

        /// <summary>Сообщает о достижении локации без проверки зачистки предыдущей.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="instanceId">Экземпляр локации.</param>
        /// <param name="templateId">Шаблон локации.</param>
        void OnLocationReached(GameSession session, string instanceId, string templateId);

        /// <summary>Сообщает о выходе из хаба: сюжетный рейд начинается.</summary>
        /// <param name="session">Игровая сессия.</param>
        void OnLeaveHub(GameSession session);

        /// <summary>Сообщает о входе в хаб: сюжетный рейд завершается.</summary>
        /// <param name="session">Игровая сессия.</param>
        void OnEnterHub(GameSession session);

        /// <summary>Сообщает о смерти игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        void OnPlayerDeath(GameSession session);

        /// <summary>Сообщает об убийстве актёра.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">Id убитого актёра.</param>
        void OnActorKilled(GameSession session, string actorId);

        /// <summary>Сообщает о вскрытом тайнике.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="instanceId">Экземпляр локации.</param>
        void OnStashOpened(GameSession session, string instanceId);

        /// <summary>Цепочки локаций активных сюжетных квестов для генератора маршрута.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Активные цепочки; <c>null</c> — сюжетные квесты не подключены.</returns>
        IReadOnlyList<string>? ActiveChains(GameSession session);
    }
}
