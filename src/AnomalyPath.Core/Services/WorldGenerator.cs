#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Детерминированный генератор маршрута: хаб, звенья пути с ветками в здания и местности.
    /// Один seed и одно состояние мира дают один маршрут; звено строится из своего seed,
    /// поэтому достройка после загрузки продолжает тот же маршрут. Ветки распределяются по локациям
    /// звена: первый круг — с конца (последняя получает первую ветку), второй и следующие — с локации,
    /// выбранной по seed звена; с одной локации их не больше <c>maxTransitionsPerNode − 1</c>
    /// (одно место занято продолжением пути). Концы веток ведут в единственное следующее звено,
    /// а конец звена пути всегда имеет переход. Шаг считается внутри звена, поэтому цена возврата
    /// в хаб на первом шаге нового звена сбрасывается до базы.
    /// Квестовые цепочки встают в звено как дополнительная ветка, только пока их квест активен.
    /// </summary>
    public sealed partial class WorldGenerator : IWorldGenerator
    {
        private readonly ILocationRepository _locations;
        private readonly ITopologyRepository _topology;
        private readonly IQuestChainRepository? _chains;
        private readonly ISpawnRepository? _spawns;

        /// <summary>Создаёт генератор.</summary>
        /// <param name="locations">Шаблоны локаций.</param>
        /// <param name="topology">Топология мира.</param>
        /// <param name="chains">Квестовые цепочки: их локации включаются в маршрут; <c>null</c> — без квестов.</param>
        /// <param name="spawns">Записи спавна принудительных объектов цепочек; <c>null</c> — без объектов.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public WorldGenerator(
            ILocationRepository locations,
            ITopologyRepository topology,
            IQuestChainRepository? chains = null,
            ISpawnRepository? spawns = null)
        {
            _locations = locations ?? throw new ArgumentNullException(nameof(locations));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _chains = chains;
            _spawns = spawns;
        }

        /// <inheritdoc />
        public WorldMap Generate(int seed)
        {
            var topology = _topology.Topology;
            var hub = FirstHub();
            var random = new SeededWorldRandom(seed);
            var hubInstance = NewInstance(hub, random, routeStep: 0, routeEdge: 0, "loc_hub");
            var locations = new List<LocationInstance> { hubInstance };
            var edges = new List<WorldEdge>();

            var map = new WorldMap(
                seed,
                locations,
                edges,
                hubInstance.InstanceId,
                topology.InitialStamina,
                topology.DayStartHour,
                topology.NightStartHour);

            BuildForward(map, hubInstance, hub, topology.Route.LookaheadEdges, hubInstance, null);
            return map;
        }

        /// <inheritdoc />
        public void BeginRoute(WorldState world)
        {
            BeginRoute(world, null);
        }

        /// <inheritdoc />
        public void BeginRoute(WorldState world, IReadOnlyList<string>? activeChains)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var topology = _topology.Topology;
            var map = world.Map;
            if (world.CurrentLocation.Subtype != LocationSubtype.Hub)
            {
                throw new DataException(world.CurrentLocation.InstanceId, CoreErrors.WorldRouteNotFromHub);
            }

            var stale = new List<string>();
            foreach (var location in map.Locations)
            {
                if (location.RouteEdge >= 1)
                {
                    stale.Add(location.InstanceId);
                }
            }

            foreach (var id in stale)
            {
                map.RemoveLocation(id);
            }

            map.BeginRoute();
            var hubInstance = world.CurrentLocation;
            var hub = _locations.Get(hubInstance.TemplateId);
            BuildForward(map, hubInstance, hub, topology.Route.LookaheadEdges, hubInstance, activeChains);
        }

        /// <inheritdoc />
        public void Extend(WorldState world)
        {
            Extend(world, null);
        }

        /// <inheritdoc />
        public void Extend(WorldState world, IReadOnlyList<string>? activeChains)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            var topology = _topology.Topology;
            var needed = world.CurrentLocation.RouteEdge + topology.Route.LookaheadEdges;
            var built = world.RouteEdges;
            if (built >= needed)
            {
                return;
            }

            var tailId = world.RouteTailId;
            if (tailId == null)
            {
                if (world.CurrentLocation.Subtype != LocationSubtype.Hub &&
                    !world.Map.TryGetLocation(world.LastHubId, out _))
                {
                    throw new DataException("world", CoreErrors.WorldNoHub);
                }

                var hubInstance = world.CurrentLocation.Subtype == LocationSubtype.Hub
                    ? world.CurrentLocation
                    : world.Map.GetLocation(world.LastHubId);
                var hub = _locations.Get(hubInstance.TemplateId);
                BuildForward(world.Map, hubInstance, hub, needed, hubInstance, activeChains);
                return;
            }

            var tail = world.Map.GetLocation(tailId);
            var tailTemplate = _locations.Get(tail.TemplateId);
            BuildForward(world.Map, tail, tailTemplate, needed - built, ResolveHub(world), activeChains);
        }
    }
}
