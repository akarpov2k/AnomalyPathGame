#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="WorldGenerator"/>: связи локаций, переходы веток и идентификаторы.</summary>
    public sealed partial class WorldGenerator
    {
        private static bool HasEdge(WorldMap map, string fromId, string toId)
        {
            foreach (var edge in map.EdgesFrom(fromId))
            {
                if (string.Equals(edge.ToId, toId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Связывает выходы предыдущего звена с первой локацией нового: из любой ветки можно перейти
        /// в путь следующего звена, а следующее звено строится одно.
        /// </summary>
        private void ConnectFront(
            WorldMap map,
            IReadOnlyList<string> front,
            LocationInstance target,
            LocationTemplate targetTemplate,
            int edgeNumber,
            TopologyTemplate topology,
            IWorldRandom random)
        {
            for (var index = 0; index < front.Count; index++)
            {
                if (!map.TryGetLocation(front[index], out var source) || source == null)
                {
                    continue;
                }

                var via = PickVia(_locations.Get(source.TemplateId), targetTemplate, topology, random);
                AddPair(map, source.InstanceId, target.InstanceId, via, topology, EdgeId(edgeNumber, "front", index));
            }
        }

        private static string PickVia(
            LocationTemplate from,
            LocationTemplate to,
            TopologyTemplate topology,
            IWorldRandom random)
        {
            var vias = RouteRules.Vias(from, to, topology);
            if (vias.Count == 0)
            {
                throw new DataException(
                    from.Id,
                    CoreErrors.WorldTransitionForbidden,
                    from.Id,
                    to.Id,
                    "нет");
            }

            return vias[random.NextInt(0, vias.Count)];
        }

        /// <summary>
        /// Способ входа в квестовую цепочку: обычный переход, разрешённый её подтипами
        /// и тегами; если такого нет — дорога. Роли зданий у цепочек не заданы, поэтому
        /// правило маршрута «путь → здание» к ним не подходит.
        /// </summary>
        /// <param name="from">Локация маршрута.</param>
        /// <param name="to">Первая локация цепочки.</param>
        /// <returns>Код способа перехода.</returns>
        private static string ChainVia(LocationTemplate from, LocationTemplate to) =>
            ViaRules.Allows("path", from.Subtype, to.Subtype, from.Tags, to.Tags) ? "path" : "road";

        private static void AddPair(
            WorldMap map,
            string fromId,
            string toId,
            string via,
            TopologyTemplate topology,
            string edgeId)
        {
            var cost = topology.GetCost(via);
            map.AddEdge(new WorldEdge(edgeId + ".out", fromId, toId, via, 1, cost, false, false));
            map.AddEdge(new WorldEdge(edgeId + ".in", toId, fromId, via, 1, cost, false, false));
        }

        /// <summary>Стартовый хаб: хаб с наименьшим тиром — он открыт первым на карте.</summary>
        /// <returns>Шаблон стартового хаба.</returns>
        /// <exception cref="DataException">В данных нет ни одного хаба.</exception>
        private LocationTemplate FirstHub()
        {
            var hubs = Select(LocationSubtype.Hub);
            if (hubs.Count == 0)
            {
                throw new DataException("world", CoreErrors.WorldNoHub);
            }

            var best = hubs[0];
            foreach (var hub in hubs)
            {
                if (hub.Tier < best.Tier)
                {
                    best = hub;
                }
            }

            return best;
        }

        private static LocationInstance NewInstance(
            LocationTemplate template,
            IWorldRandom random,
            int routeStep,
            int routeEdge,
            string instanceId)
        {
            var seed = random.NextInt(1, int.MaxValue);
            var background = template.Backgrounds[random.NextInt(0, template.Backgrounds.Count)];
            return new LocationInstance(
                instanceId,
                template.Id,
                seed,
                background,
                template.Subtype,
                template.Tier,
                template.Danger,
                routeStep,
                routeEdge);
        }

        private static string InstanceId(int edgeNumber, string part, int index) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "loc_e{0}_{1}_{2}",
                edgeNumber,
                part,
                index);

        private static string EdgeId(int edgeNumber, string part, int index) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "edge_e{0}_{1}_{2}",
                edgeNumber,
                part,
                index);

        private List<LocationTemplate> Select(LocationSubtype subtype)
        {
            var result = new List<LocationTemplate>();
            foreach (var template in _locations.All)
            {
                if (template.Subtype == subtype)
                {
                    result.Add(template);
                }
            }

            result.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            return result;
        }
    }
}
