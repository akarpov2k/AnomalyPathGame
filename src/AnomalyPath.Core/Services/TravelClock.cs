#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: часы мира. Переход и отдых тратят
    /// травмы, еду и воду, проваливают просроченные квесты и наносят урон жажды.
    /// </summary>
    internal sealed class TravelClock : ITravelClock
    {
        private readonly INeedsService? _needs;
        private readonly IQuestService? _quests;
        private readonly IStoryQuestHooks _story;
        private readonly IGameRulesRepository? _rules;

        internal TravelClock(
            INeedsService? needs,
            IQuestService? quests,
            IStoryQuestHooks story,
            IGameRulesRepository? rules)
        {
            _needs = needs;
            _quests = quests;
            _story = story ?? throw new ArgumentNullException(nameof(story));
            _rules = rules;
        }

        /// <inheritdoc />
        public int ReturnMinutes => (int)Math.Round(_rules?.Rules?.Walk.ReturnMinutes ?? 30);

        /// <inheritdoc />
        public int GetTravelStaminaCost(GameSession session, int baseStamina)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (_rules?.Rules?.Walk.UseStamina == false)
            {
                return 0;
            }

            return _needs?.TravelStaminaCost(session.State, baseStamina) ?? baseStamina;
        }

        /// <inheritdoc />
        public void Advance(GameSession session, double hours)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            TickInjuries(session, hours);
            _needs?.Consume(session.State, hours);
            _quests?.Expire(session);
        }

        /// <inheritdoc />
        public PlayerDamageResult? Thirst(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (_needs == null)
            {
                return null;
            }

            var damage = _needs.ApplyThirstDamage(session.State);
            if (damage.Died)
            {
                _story.OnPlayerDeath(session);
            }

            return damage.Damage > 0 || damage.Died ? damage : null;
        }

        /// <inheritdoc />
        public int ApCostBonus(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return _needs?.ApCostBonus(session.State) ?? 0;
        }

        /// <inheritdoc />
        public int TurnDamage(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return _needs?.TurnDamage(session.State) ?? 0;
        }

        /// <summary>
        /// Тратит часовую длительность травм игрока. Часы мира идут переходами и возвратом в хаб,
        /// поэтому неполный час не тратит единицу длительности.
        /// </summary>
        /// <param name="session">Сессия.</param>
        /// <param name="hours">Сколько часов прошло.</param>
        private static void TickInjuries(GameSession session, double hours)
        {
            var player = session.State.Player;
            if (player == null || hours <= 0)
            {
                return;
            }

            var world = session.State.World;
            var total = world.HourCarry + hours;
            var whole = (int)Math.Floor(total);
            world.SetHourCarry(total - whole);
            if (whole <= 0)
            {
                return;
            }

            player.TickInjuries(InjuryDurationKind.Hours, whole);
        }
    }
}
