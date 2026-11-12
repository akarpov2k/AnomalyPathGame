#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: переходы между хабами по карте мира.
    /// Пешком списываются часы и выносливость и бросается встреча; с проводником — часы
    /// и деньги без встречи.
    /// </summary>
    internal sealed class HubTravelFlow : IHubTravelFlow
    {
        private readonly ITopologyRepository? _topology;
        private readonly IRoadEncounterService _encounters;
        private readonly ICombatFlow _combat;
        private readonly ITravelClock _clock;
        private readonly IStoryQuestHooks _story;
        private readonly ILocationRepository? _locations;

        internal HubTravelFlow(
            ITopologyRepository? topology,
            IRoadEncounterService encounters,
            ICombatFlow combat,
            ITravelClock clock,
            IStoryQuestHooks story,
            ILocationRepository? locations)
        {
            _topology = topology;
            _encounters = encounters ?? throw new ArgumentNullException(nameof(encounters));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _story = story ?? throw new ArgumentNullException(nameof(story));
            _locations = locations;
        }

        /// <inheritdoc />
        public HubTravelQuote GetHubTravelQuote(GameSession session, string targetHubId, HubTravelMode mode)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var zero = HubTravelCost.Zero;
            if (session.InCombat)
            {
                return new HubTravelQuote(HubTravelStatus.InCombat, 0, zero, 0);
            }

            var world = session.State.World;
            if (world.CurrentLocation.Subtype != LocationSubtype.Hub)
            {
                return new HubTravelQuote(HubTravelStatus.NotFromHub, 0, zero, 0);
            }

            if (!world.Map.TryGetLocation(targetHubId, out var target) || target == null)
            {
                return new HubTravelQuote(HubTravelStatus.TargetNotFound, 0, zero, 0);
            }

            if (target.Subtype != LocationSubtype.Hub)
            {
                return new HubTravelQuote(HubTravelStatus.TargetNotHub, 0, zero, 0);
            }

            if (string.Equals(target.InstanceId, world.CurrentLocationId, StringComparison.Ordinal))
            {
                return new HubTravelQuote(HubTravelStatus.SameHub, 0, zero, 0);
            }

            var settings = _topology?.Topology.Travel;
            if (settings == null)
            {
                return new HubTravelQuote(HubTravelStatus.Unavailable, 0, zero, 0);
            }

            var steps = Math.Abs(target.Tier - world.CurrentLocation.Tier);
            if (steps <= 0)
            {
                return new HubTravelQuote(HubTravelStatus.SameHub, 0, zero, 0);
            }

            if (mode == HubTravelMode.Guide)
            {
                var guideCost = new HubTravelCost(
                    settings.Guide.HoursPerStep * steps,
                    0,
                    settings.Guide.MoneyPerStep * steps);
                var player = session.State.Player;
                var status = guideCost.Money > 0 && (player == null || player.Money < guideCost.Money)
                    ? HubTravelStatus.NotEnoughMoney
                    : HubTravelStatus.Success;
                return QuoteFor(session, target, status, steps, guideCost, 0);
            }

            var baseStamina = settings.OnFoot.StaminaPerStep * steps;
            var footCost = new HubTravelCost(
                settings.OnFoot.HoursPerStep * steps,
                _clock.GetTravelStaminaCost(session, baseStamina),
                0);
            var chance = Math.Min(1, settings.OnFoot.EncounterChancePerStep * steps);
            var footStatus = world.Stamina < footCost.Stamina
                ? HubTravelStatus.NotEnoughStamina
                : HubTravelStatus.Success;
            return QuoteFor(session, target, footStatus, steps, footCost, chance);
        }

        /// <inheritdoc />
        public HubTravelResult TravelToHub(GameSession session, string targetHubId, HubTravelMode mode)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var quote = GetHubTravelQuote(session, targetHubId, mode);
            if (!quote.Available)
            {
                return new HubTravelResult(quote.Status, quote.Steps, quote.Cost);
            }

            var world = session.State.World;
            var player = session.State.Player;
            if (quote.Cost.Stamina > 0)
            {
                world.TrySpendStamina(quote.Cost.Stamina);
            }

            if (quote.Cost.Money > 0 && player != null)
            {
                player.TrySpendMoney(quote.Cost.Money);
            }

            world.Clock.Advance(quote.Cost.Hours);
            _clock.Advance(session, quote.Cost.Hours);
            _story.OnLeaveHub(session);

            var damage = _clock.Thirst(session);
            if (damage?.Died == true)
            {
                return new HubTravelResult(
                    HubTravelStatus.Success,
                    quote.Steps,
                    quote.Cost,
                    damage: damage);
            }

            var encounter = mode == HubTravelMode.OnFoot
                ? RollEncounter(session, targetHubId, quote.EncounterChance)
                : null;
            if (encounter == null)
            {
                MoveToHub(session, targetHubId);
                return new HubTravelResult(
                    HubTravelStatus.Success,
                    quote.Steps,
                    quote.Cost,
                    damage: damage);
            }

            session.SetPendingHubTravel(new PendingHubTravel(world.CurrentLocationId, targetHubId, mode));
            if (!_encounters.TryStart(session, encounter))
            {
                CompletePending(session);
                return new HubTravelResult(
                    HubTravelStatus.Success,
                    quote.Steps,
                    quote.Cost,
                    damage: damage);
            }

            return new HubTravelResult(
                HubTravelStatus.Encounter,
                quote.Steps,
                quote.Cost,
                encounter.ActorId,
                encounter.Tier,
                damage);
        }

        /// <inheritdoc />
        public void CompletePending(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var pending = session.PendingHubTravel;
            if (pending == null)
            {
                return;
            }

            session.SetPendingHubTravel(null);
            MoveToHub(session, pending.TargetHubId);
        }

        /// <inheritdoc />
        public void CancelPending(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            session.SetPendingHubTravel(null);
        }

        /// <summary>
        /// Оборачивает предложение перехода: у локации, которая после своего квеста стала
        /// заглушкой, статус всегда <see cref="HubTravelStatus.InDevelopment"/>, а цена остаётся
        /// посчитанной для показа.
        /// </summary>
        /// <param name="session">Игровая сессия: проверка завершённых квестов.</param>
        /// <param name="target">Целевой хаб.</param>
        /// <param name="status">Обычный статус предложения.</param>
        /// <param name="steps">Число шагов.</param>
        /// <param name="cost">Цена перехода.</param>
        /// <param name="encounterChance">Шанс встречи.</param>
        /// <returns>Предложение перехода.</returns>
        private HubTravelQuote QuoteFor(
            GameSession session,
            LocationInstance target,
            HubTravelStatus status,
            int steps,
            HubTravelCost cost,
            double encounterChance) =>
            new HubTravelQuote(
                IsStub(session, target) ? HubTravelStatus.InDevelopment : status,
                steps,
                cost,
                encounterChance);

        /// <summary>
        /// Локация стала заглушкой: её квест завершён, а нового контента в ней ещё нет.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="target">Целевой хаб.</param>
        /// <returns><c>true</c>, если переход в этот хаб не выполняется.</returns>
        private bool IsStub(GameSession session, LocationInstance target)
        {
            if (_locations == null || !_locations.Contains(target.TemplateId))
            {
                return false;
            }

            var quest = _locations.Get(target.TemplateId).StubAfterQuest;
            var player = session.State.Player;
            return quest != null && player != null && player.HasCompletedQuest(quest);
        }

        /// <summary>Бросает встречу на дороге: один бросок на переход, пул пересекаемых тиров.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        /// <param name="chance">Шанс встречи за весь переход, 0..1.</param>
        /// <returns>Найденный противник или <c>null</c>, если встреча не случилась.</returns>
        private RoadEncounter? RollEncounter(GameSession session, string targetHubId, double chance)
        {
            if (chance <= 0)
            {
                return null;
            }

            var world = session.State.World;
            var from = world.CurrentLocation;
            if (!world.Map.TryGetLocation(targetHubId, out var target) || target == null)
            {
                return null;
            }

            var key = EncounterKey(from.InstanceId, target.InstanceId, world.Clock);
            return _encounters.Roll(from.Tier, target.Tier, Math.Min(from.Tier, target.Tier), chance, key, world.Map.Seed);
        }

        /// <summary>Переносит игрока в хаб и сообщает об этом сюжетным квестам.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        private void MoveToHub(GameSession session, string targetHubId)
        {
            var world = session.State.World;
            if (!world.Map.TryGetLocation(targetHubId, out var target) || target == null)
            {
                return;
            }

            world.MoveTo(targetHubId);
            _story.OnLocationReached(session, target.InstanceId, target.TemplateId);
            _story.OnEnterHub(session);
        }

        private static string EncounterKey(string fromHubId, string targetHubId, WorldClock clock) =>
            "road:" + fromHubId + ">" + targetHubId
            + "#" + clock.Day.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "#" + clock.Hours.ToString("R", System.Globalization.CultureInfo.InvariantCulture);
    }
}
