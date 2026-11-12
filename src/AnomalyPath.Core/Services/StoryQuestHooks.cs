#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: сообщения миру о том, что произошло
    /// в сессии — переход, убийство, смерть, вскрытый тайник и смена хаба.
    /// </summary>
    internal sealed class StoryQuestHooks : IStoryQuestHooks
    {
        private readonly IStoryQuestService? _storyQuests;
        private readonly IQuestService? _quests;

        internal StoryQuestHooks(IStoryQuestService? storyQuests, IQuestService? quests)
        {
            _storyQuests = storyQuests;
            _quests = quests;
        }

        /// <inheritdoc />
        public void OnTravel(GameSession session, LocationInstance previous, LocationInstance current)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (WasCleared(previous))
            {
                _storyQuests?.NotifyLocationCleared(
                    session,
                    previous.InstanceId,
                    previous.TemplateId,
                    previous.Subtype);
                _quests?.NotifyLocationCleared(session, previous.InstanceId);
            }

            if (_storyQuests == null)
            {
                return;
            }

            _storyQuests.NotifyLocationReached(session, current.InstanceId, current.TemplateId);
            if (previous.Subtype == LocationSubtype.Hub)
            {
                _storyQuests.OnLeaveHub(session);
            }

            if (current.Subtype == LocationSubtype.Hub)
            {
                _storyQuests.OnEnterHub(session);
            }
        }

        /// <inheritdoc />
        public void OnLocationReached(GameSession session, string instanceId, string templateId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _storyQuests?.NotifyLocationReached(session, instanceId, templateId);
        }

        /// <inheritdoc />
        public void OnLeaveHub(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _storyQuests?.OnLeaveHub(session);
        }

        /// <inheritdoc />
        public void OnEnterHub(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _storyQuests?.OnEnterHub(session);
        }

        /// <inheritdoc />
        public void OnPlayerDeath(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _storyQuests?.OnDeath(session);
            _quests?.NotifyPlayerDeath(session);
        }

        /// <inheritdoc />
        public void OnActorKilled(GameSession session, string actorId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _storyQuests?.NotifyActorKilled(session, actorId);
            _quests?.NotifyActorKilled(session, actorId);
        }

        /// <inheritdoc />
        public void OnStashOpened(GameSession session, string instanceId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _storyQuests?.NotifyStashOpened(session, instanceId);
        }

        /// <inheritdoc />
        public IReadOnlyList<string>? ActiveChains(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return _storyQuests?.GetActiveChains(session);
        }

        /// <summary>
        /// Считается ли покинутая локация зачищенной: противник убит или сбежал, а если
        /// противника не было — достаточно выйти. Хабы и дороги не считаются.
        /// </summary>
        /// <param name="location">Покинутая локация.</param>
        /// <returns><c>true</c>, если локация зачищена.</returns>
        private static bool WasCleared(LocationInstance location)
        {
            if (location.Subtype == LocationSubtype.Hub ||
                location.Subtype == LocationSubtype.Path)
            {
                return false;
            }

            var hasEnemy = false;
            var enemyGone = true;
            foreach (var locationObject in location.Objects)
            {
                if (locationObject is EnemyLocationObject enemy)
                {
                    hasEnemy = true;
                    if (!enemy.Looted)
                    {
                        enemyGone = false;
                    }
                }
            }

            return !hasEnemy || enemyGone || location.EnemyGone;
        }
    }
}
