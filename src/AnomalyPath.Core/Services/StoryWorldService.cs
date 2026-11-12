#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Квестовые локации: цепочка создаётся при взятии квеста (соединяет её с маршрутом
    /// генератор), одиночная локация ставится на карту с переходом от хаба.
    /// </summary>
    public sealed class StoryWorldService : IStoryWorldService
    {
        private readonly IQuestChainRepository _chains;
        private readonly ILocationRepository _locations;
        private readonly ISpawnRepository? _spawns;
        private readonly ITopologyRepository? _topology;

        /// <summary>Создаёт сервис квестовых локаций.</summary>
        /// <param name="chains">Цепочки локаций.</param>
        /// <param name="locations">Шаблоны локаций.</param>
        /// <param name="spawns">Записи спавна принудительных объектов; <c>null</c> — без объектов.</param>
        /// <param name="topology">Топология: цена перехода по дороге; <c>null</c> — бесплатно.</param>
        /// <exception cref="ArgumentNullException">Цепочки или локации не заданы.</exception>
        public StoryWorldService(
            IQuestChainRepository chains,
            ILocationRepository locations,
            ISpawnRepository? spawns = null,
            ITopologyRepository? topology = null)
        {
            _chains = chains ?? throw new ArgumentNullException(nameof(chains));
            _locations = locations ?? throw new ArgumentNullException(nameof(locations));
            _spawns = spawns;
            _topology = topology;
        }

        /// <inheritdoc />
        public bool EnsureChain(GameSession session, string chainId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!_chains.TryGet(chainId, out var chain))
            {
                return false;
            }

            return QuestChainLocations.Ensure(
                session.State.World.Map,
                chain,
                _locations,
                _spawns,
                session.State.World.Map.Seed);
        }

        /// <inheritdoc />
        public int RemoveChain(GameSession session, string chainId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return _chains.TryGet(chainId, out var chain)
                ? QuestChainLocations.Remove(session.State.World.Map, chain)
                : 0;
        }

        /// <inheritdoc />
        public bool EnsureLocation(GameSession session, string locationId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.State.World;
            var instanceId = LocationInstanceId(locationId);
            if (world.Map.ContainsLocation(instanceId))
            {
                return true;
            }

            if (!_locations.Contains(locationId))
            {
                return false;
            }

            var template = _locations.Get(locationId);
            if (!template.Unique)
            {
                return false;
            }

            var hubId = ResolveHub(world);
            if (hubId == null)
            {
                return false;
            }

            var location = new LocationInstance(
                instanceId,
                template.Id,
                StableHash.Combine(world.Map.Seed, "story:" + locationId),
                template.Backgrounds.Count > 0 ? template.Backgrounds[0] : string.Empty,
                template.Subtype,
                template.Tier,
                template.Danger);
            foreach (var spawnId in template.SpawnPool)
            {
                if (_spawns != null &&
                    _spawns.TryGet(spawnId, out var entry) &&
                    entry != null &&
                    entry.Unique)
                {
                    location.AddObject(entry.CreateObject());
                }
            }

            world.Map.AddLocation(location);
            AddEdge(world.Map, "story_edge_" + locationId, hubId, instanceId);
            return true;
        }

        /// <summary>Идентификатор экземпляра одиночной квестовой локации.</summary>
        /// <param name="locationId">Шаблон локации.</param>
        /// <returns>Идентификатор экземпляра.</returns>
        public static string LocationInstanceId(string locationId) => "story_" + locationId;

        /// <inheritdoc />
        public bool HasChain(GameSession session, string chainId)
        {
            if (session == null || !_chains.TryGet(chainId, out var chain))
            {
                return false;
            }

            return QuestChainLocations.HasAll(session.State.World.Map, chain);
        }

        /// <summary>Идентификатор экземпляра локации цепочки: цепочка и номер шага.</summary>
        /// <param name="chainId">Идентификатор цепочки.</param>
        /// <param name="index">Номер шага.</param>
        /// <returns>Идентификатор экземпляра.</returns>
        public static string InstanceId(string chainId, int index) =>
            QuestChainInstanceIds.For(chainId, index);

        private static string? ResolveHub(WorldState world)
        {
            if (world.CurrentLocation.Subtype == LocationSubtype.Hub)
            {
                return world.CurrentLocationId;
            }

            return string.IsNullOrWhiteSpace(world.LastHubId) ? null : world.LastHubId;
        }

        private void AddEdge(WorldMap map, string edgeId, string fromId, string toId)
        {
            var cost = _topology != null ? _topology.Topology.GetCost("road") : TravelCost.Zero;
            map.AddEdge(new WorldEdge(edgeId, fromId, toId, "road", 1, cost, oneWay: false, hidden: false));
            map.AddEdge(new WorldEdge(
                edgeId + "_back",
                toId,
                fromId,
                "road",
                1,
                cost,
                oneWay: false,
                hidden: false));
        }
    }
}
