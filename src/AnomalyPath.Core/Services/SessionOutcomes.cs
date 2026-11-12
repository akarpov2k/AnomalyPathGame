#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Death;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: итоги сессии. Смерть и результат боя
    /// применяются здесь же, а побег с дороги завершает начатый переход между хабами.
    /// </summary>
    internal sealed class SessionOutcomes : ISessionOutcomes
    {
        private readonly IDeathService _death;
        private readonly ICombatOutcomeService _outcomes;
        private readonly ICombatFlow _combat;
        private readonly IHubTravelFlow _hubTravel;
        private readonly IStoryQuestHooks _story;

        internal SessionOutcomes(
            IDeathService death,
            ICombatOutcomeService outcomes,
            ICombatFlow combat,
            IHubTravelFlow hubTravel,
            IStoryQuestHooks story)
        {
            _death = death ?? throw new ArgumentNullException(nameof(death));
            _outcomes = outcomes ?? throw new ArgumentNullException(nameof(outcomes));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _hubTravel = hubTravel ?? throw new ArgumentNullException(nameof(hubTravel));
            _story = story ?? throw new ArgumentNullException(nameof(story));
        }

        /// <inheritdoc />
        public DeathResult ApplyDeath(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            _story.OnPlayerDeath(session);
            return _death.Apply(session.State);
        }

        /// <inheritdoc />
        public CombatOutcome ApplyCombatOutcome(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var outcome = _outcomes.Resolve(session);
            if (session.PendingHubTravel != null)
            {
                if (outcome.Status == CombatOutcomeStatus.Victory)
                {
                    _hubTravel.CompletePending(session);
                }
                else if (outcome.Status == CombatOutcomeStatus.Defeat)
                {
                    _hubTravel.CancelPending(session);
                }
            }

            if (outcome.Status == CombatOutcomeStatus.Defeat)
            {
                _story.OnPlayerDeath(session);
            }

            return outcome;
        }

        /// <inheritdoc />
        public bool TryFlee(GameSession session, out FleeResult result)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var onRoad = session.Combat?.Context?.OnRoad == true;
            if (!_combat.TryFlee(session, out result))
            {
                return false;
            }

            if (result.Died)
            {
                return true;
            }

            if (onRoad)
            {
                _hubTravel.CompletePending(session);
            }

            return true;
        }
    }
}
