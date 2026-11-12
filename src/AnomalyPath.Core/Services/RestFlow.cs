#nullable enable
using System;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: отдых. В хабе час сна восстанавливает
    /// долю максимума выносливости, вне хаба — меньшую долю, а каждый час может принести нападение.
    /// </summary>
    internal sealed class RestFlow : IRestFlow
    {
        private readonly ITopologyRepository? _topology;
        private readonly ITalentService? _talents;
        private readonly IArtifactService? _artifacts;
        private readonly IRoadEncounterService _encounters;
        private readonly ITravelClock _clock;
        private readonly ICombatFlow _combat;

        internal RestFlow(
            ITopologyRepository? topology,
            ITalentService? talents,
            IArtifactService? artifacts,
            IRoadEncounterService encounters,
            ITravelClock clock,
            ICombatFlow combat)
        {
            _topology = topology;
            _talents = talents;
            _artifacts = artifacts;
            _encounters = encounters ?? throw new ArgumentNullException(nameof(encounters));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        /// <inheritdoc />
        public RestQuote GetRestQuote(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.State.World;
            var isHub = world.CurrentLocation.Subtype == LocationSubtype.Hub;
            var staminaMax = world.EffectiveStaminaMax(StaminaBonus(session));
            var missing = Math.Max(0, staminaMax - world.Stamina);
            if (session.InCombat)
            {
                return new RestQuote(
                    RestStatus.InCombat,
                    isHub,
                    world.Stamina,
                    staminaMax,
                    0,
                    0,
                    missing,
                    0);
            }

            var settings = _topology?.Topology.Rest;
            if (settings == null)
            {
                return new RestQuote(
                    RestStatus.Unavailable,
                    isHub,
                    world.Stamina,
                    staminaMax,
                    0,
                    0,
                    missing,
                    0);
            }

            if (missing <= 0)
            {
                return new RestQuote(
                    RestStatus.FullStamina,
                    isHub,
                    world.Stamina,
                    staminaMax,
                    0,
                    0,
                    0,
                    0);
            }

            var recovery = StaminaRecovery(session);
            var perHour = settings.StaminaPerHour(staminaMax, isHub) + recovery;
            var chance = isHub ? 0 : settings.RaidAttackChancePerHour;
            return new RestQuote(
                RestStatus.Success,
                isHub,
                world.Stamina,
                staminaMax,
                perHour,
                settings.MaxHours(missing, staminaMax, isHub, recovery),
                missing,
                chance);
        }

        /// <inheritdoc />
        public RestResult Rest(GameSession session, int hours)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var quote = GetRestQuote(session);
            if (!quote.Available)
            {
                return new RestResult(quote.Status, 0, 0, quote.Stamina, 0);
            }

            if (hours < 0 || hours > quote.MaxHours)
            {
                return new RestResult(RestStatus.InvalidHours, 0, 0, quote.Stamina, 0);
            }

            var world = session.State.World;
            if (hours == 0)
            {
                return new RestResult(RestStatus.Success, 0, 0, quote.Stamina, 0);
            }

            var restored = Math.Min(quote.Missing, hours * quote.StaminaPerHour);
            world.RestoreStamina(restored, StaminaBonus(session));

            var chance = quote.AttackChance(hours);
            var key = RestKey(world, hours);
            world.Clock.Advance(hours);
            _clock.Advance(session, hours);

            if (chance <= 0)
            {
                return new RestResult(RestStatus.Success, hours, restored, world.Stamina, 0);
            }

            var locationTier = world.CurrentLocation.Tier;
            var encounter = _encounters.Roll(locationTier, locationTier, locationTier, chance, key, world.Map.Seed);
            if (encounter == null)
            {
                return new RestResult(RestStatus.Success, hours, restored, world.Stamina, chance);
            }

            if (!_encounters.TryStart(session, encounter))
            {
                return new RestResult(RestStatus.Success, hours, restored, world.Stamina, chance);
            }

            return new RestResult(
                RestStatus.Encounter,
                hours,
                restored,
                world.Stamina,
                chance,
                encounter.ActorId,
                encounter.Tier);
        }

        /// <summary>Прибавка к максимуму выносливости от артефактов и талантов; без них 0.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Прибавка к максимуму выносливости.</returns>
        private int StaminaBonus(GameSession session)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return 0;
            }

            var artifacts = _artifacts == null
                ? 0
                : _artifacts.GetEquippedModifiers(player.Equipment).Stamina;
            var talents = _talents == null ? 0 : _talents.GetModifiers(player).StaminaMax;
            return artifacts + talents;
        }

        /// <summary>Прибавка к восстановлению выносливости за час отдыха от талантов; без них 0.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Сколько дополнительной выносливости даёт час отдыха.</returns>
        private int StaminaRecovery(GameSession session)
        {
            var player = session.State.Player;
            return player == null || _talents == null
                ? 0
                : Math.Max(0, _talents.GetModifiers(player).StaminaRecovery);
        }

        /// <summary>
        /// Ключ броска нападения: экземпляр локации, день, часы мира и выбранная длительность.
        /// Один seed и одно состояние дают один результат, поэтому отдых детерминирован.
        /// </summary>
        /// <param name="world">Состояние мира до отдыха.</param>
        /// <param name="hours">Выбранные часы отдыха.</param>
        /// <returns>Ключ для смешивания с seed мира.</returns>
        private static string RestKey(WorldState world, int hours) =>
            "rest:" + world.CurrentLocation.InstanceId
            + "#" + world.Clock.Day.ToString(CultureInfo.InvariantCulture)
            + "#" + world.Clock.Hours.ToString("R", CultureInfo.InvariantCulture)
            + "#" + hours.ToString(CultureInfo.InvariantCulture);
    }
}
