#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: переходы по маршруту. Каждый переход
    /// тратит часы и выносливость, сообщает миру о прибытии и снимает пройденное с карты.
    /// </summary>
    internal sealed class RouteTravel : IRouteTravel
    {
        private readonly ITravelService _travel;
        private readonly IWorldGenerator? _worldGenerator;
        private readonly ITopologyRepository? _topology;
        private readonly ITravelClock _clock;
        private readonly IStoryQuestHooks _story;
        private readonly ILocationEntry _entry;

        internal RouteTravel(
            ITravelService travel,
            IWorldGenerator? worldGenerator,
            ITopologyRepository? topology,
            ITravelClock clock,
            IStoryQuestHooks story,
            ILocationEntry entry)
        {
            _travel = travel ?? throw new ArgumentNullException(nameof(travel));
            _worldGenerator = worldGenerator;
            _topology = topology;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _story = story ?? throw new ArgumentNullException(nameof(story));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        /// <inheritdoc />
        public IReadOnlyList<WorldEdge> AvailableEdges(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return session.InCombat
                ? new List<WorldEdge>()
                : _travel.GetAvailableEdges(session.State.World);
        }

        /// <inheritdoc />
        public TravelResult Travel(GameSession session, string edgeId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.InCombat)
            {
                return new TravelResult(TravelStatus.InCombat);
            }

            var previous = session.State.World.CurrentLocation;
            var world = session.State.World;
            if (!world.Map.TryGetEdge(edgeId, out var edge) || edge == null)
            {
                return new TravelResult(TravelStatus.EdgeNotFound);
            }

            if (edge.FromId != world.CurrentLocationId)
            {
                return new TravelResult(TravelStatus.WrongLocation);
            }

            var staminaCost = _clock.GetTravelStaminaCost(session, edge.Cost.Stamina);
            if (world.Stamina < staminaCost)
            {
                return new TravelResult(TravelStatus.NotEnoughStamina);
            }

            if (staminaCost > 0)
            {
                world.TrySpendStamina(staminaCost);
            }

            var result = _travel.Travel(world, edgeId);
            if (result.Succeeded)
            {
                var hours = result.Edge?.Cost.Hours ?? 0;
                _clock.Advance(session, hours);
                _story.OnTravel(session, previous, world.CurrentLocation);
                if (world.CurrentLocation.RouteEdge != previous.RouteEdge)
                {
                    _worldGenerator?.Extend(world, _story.ActiveChains(session));
                }

                PrunePassed(world, previous);
                _entry.Ensure(session);
                var damage = _clock.Thirst(session);
                if (damage != null)
                {
                    return new TravelResult(TravelStatus.Success, result.Edge, damage);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public TravelResult LeaveHub(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.InCombat)
            {
                return new TravelResult(TravelStatus.InCombat);
            }

            var world = session.State.World;
            if (world.CurrentLocation.Subtype != LocationSubtype.Hub)
            {
                return new TravelResult(TravelStatus.WrongLocation);
            }

            if (_worldGenerator == null)
            {
                return new TravelResult(TravelStatus.EdgeNotFound);
            }

            _worldGenerator.BeginRoute(world, _story.ActiveChains(session));
            var edge = FirstRouteEdge(world);
            if (edge == null)
            {
                return new TravelResult(TravelStatus.EdgeNotFound);
            }

            var previous = world.CurrentLocation;
            var result = _travel.Travel(world, edge.EdgeId);
            if (result.Succeeded)
            {
                var hours = result.Edge?.Cost.Hours ?? 0;
                _clock.Advance(session, hours);
                _story.OnTravel(session, previous, world.CurrentLocation);
                PrunePassed(world, previous);
                _entry.Ensure(session);
                var damage = _clock.Thirst(session);
                if (damage != null)
                {
                    return new TravelResult(TravelStatus.Success, result.Edge, damage);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public TravelResult ReturnToHub(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.InCombat)
            {
                return new TravelResult(TravelStatus.InCombat);
            }

            var world = session.State.World;
            var location = world.CurrentLocation;
            if (location.Subtype == LocationSubtype.Hub)
            {
                return new TravelResult(TravelStatus.HubUnavailable);
            }

            var hours = _clock.ReturnMinutes / 60.0;
            var staminaCost = _clock.GetTravelStaminaCost(session, 0);
            if (world.Stamina < staminaCost)
            {
                return new TravelResult(TravelStatus.NotEnoughStaminaReturn);
            }

            if (staminaCost > 0)
            {
                world.TrySpendStamina(staminaCost);
            }

            world.Clock.Advance(hours);
            _clock.Advance(session, hours);
            var previous = location;
            world.MoveTo(world.LastHubId);
            _story.OnTravel(session, previous, world.CurrentLocation);
            PrunePassed(world, previous);
            _entry.Ensure(session);
            return new TravelResult(TravelStatus.Success, damage: _clock.Thirst(session));
        }

        /// <summary>
        /// Снимает с карты пройденное: покинутую локацию и всё, что осталось позади текущей.
        /// Хаб и цели квестов остаются, чтобы в хаб можно было вернуться.
        /// </summary>
        /// <param name="world">Состояние мира.</param>
        /// <param name="previous">Покинутая локация.</param>
        private static void PrunePassed(WorldState world, LocationInstance previous)
        {
            if (previous.InstanceId != world.CurrentLocationId)
            {
                world.Map.RemoveEdgesBetween(previous.InstanceId, world.CurrentLocationId);
            }

            if (previous.Subtype != LocationSubtype.Hub &&
                previous.InstanceId != world.Map.StartLocationId &&
                previous.InstanceId != world.CurrentLocationId)
            {
                world.Map.RemoveLocation(previous.InstanceId);
            }

            world.Map.PrunePassed(world.CurrentLocationId);
        }

        /// <summary>Первое ребро маршрута из хаба: в первую локацию пути звена 1.</summary>
        /// <param name="world">Состояние мира.</param>
        /// <returns>Ребро или <c>null</c>, если маршрут не построен.</returns>
        private static WorldEdge? FirstRouteEdge(WorldState world)
        {
            foreach (var edge in world.Map.EdgesFrom(world.CurrentLocationId))
            {
                var target = world.Map.GetLocation(edge.ToId);
                if (target.Subtype == LocationSubtype.Path && target.RouteEdge == 1 && target.RouteStep == 1)
                {
                    return edge;
                }
            }

            return null;
        }
    }
}
