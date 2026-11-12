#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="WorldTests"/>: фикстуры и помощники.</summary>
    public sealed partial class WorldTests
    {
        private Dictionary<string, string> BaseWorldFixture() =>
            new Dictionary<string, string>
            {
                ["catalog.json"] = Catalog("topology.json", "locations/hub.json", "locations/route.json", "locations/site.json"),
                ["topology.json"] = WorldTopologyJson(),
                ["locations/hub.json"] = LocationJson("loc_hub", "hub", "hub", "[\"hub\",\"outdoor\",\"road\"]"),
                ["locations/route.json"] = LocationJson("loc_route", "wild", "path", "[\"road\",\"outdoor\"]"),
                ["locations/site.json"] = LocationJson("loc_site", "wild", "building", "[\"indoor\",\"outdoor\"]")
            };

        private static string WorldTopologyJson() =>
            "{\"schemaVersion\":1,\"kind\":\"topology\",\"id\":\"topology\",\"route\":{\"transitions\":[" +
            "{\"from\":\"hub\",\"to\":\"path\",\"via\":[\"road\",\"path\"]}," +
            "{\"from\":\"path\",\"to\":\"path\",\"via\":[\"road\",\"path\"]}," +
            "{\"from\":\"path\",\"to\":\"building\",\"via\":[\"path\"]}," +
            "{\"from\":\"building\",\"to\":\"building\",\"via\":[\"corridor\"]}," +
            "{\"from\":\"building\",\"to\":\"path\",\"via\":[\"path\"]}]," +
            "\"lookaheadEdges\":2,\"maxTransitionsPerNode\":3,\"hubReturn\":{\"stamina\":2,\"hours\":0.5}," +
            "\"branches\":{\"oddRange\":[1,2],\"evenGrowth\":1,\"maxChains\":2}},\"costs\":{" +
            "\"road\":{\"stamina\":2,\"hours\":1},\"path\":{\"stamina\":3,\"hours\":1.5}," +
            "\"corridor\":{\"stamina\":0,\"hours\":0}}," +
            "\"travel\":{\"onFoot\":{\"hoursPerStep\":4,\"staminaPerStep\":2,\"encounterChancePerStep\":0.2}," +
            "\"guide\":{\"hoursPerStep\":4,\"moneyPerStep\":200}}," +
            "\"rest\":{\"hubStaminaPercent\":0.2,\"raidStaminaPercent\":0.1,\"raidAttackChancePerHour\":0.1}," +
            "\"initialStamina\":10,\"dayStartHour\":6,\"nightStartHour\":22}";

        private static string LocationJson(
            string id,
            string locationKind,
            string subtype,
            string tags,
            string extra = "")
        {
            var suffix = string.IsNullOrWhiteSpace(extra) ? string.Empty : "," + extra.TrimStart(',');
            var tier = string.Equals(locationKind, "hub", StringComparison.Ordinal) ? 1 : 0;
            return "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"" + id + "\",\"titleKey\":\"t\"," +
                   "\"locationKind\":\"" + locationKind + "\",\"subtype\":\"" + subtype + "\"," +
                   "\"tags\":" + tags + ",\"tier\":" + tier + ",\"backgrounds\":[\"bg\"],\"objectCount\":[0,0]," +
                   "\"danger\":0,\"chainLength\":[1,1],\"rarity\":\"common\"," +
                   "\"spawnChance\":1,\"unique\":false" + suffix + "}";
        }

        /// <summary>Шаблон локации для прямого прогона генератора без загрузки данных.</summary>
        private static LocationTemplate WorldTemplate(
            string id,
            LocationKind kind,
            LocationSubtype subtype,
            LocationTag[] tags,
            IntRange chainLength) =>
            new LocationTemplate(
                id,
                "t",
                kind,
                subtype,
                null,
                tags,
                0,
                new[] { "bg" },
                new IntRange(0, 0),
                0,
                new WeightedRange(chainLength),
                Array.Empty<string>(),
                LocationRarity.Common,
                1,
                false);

        /// <summary>Топология для прямого прогона генератора: хаб → путь, ветки в здания.</summary>
        private static TopologyTemplate CustomTopology(
            int maxTransitionsPerNode,
            IntRange branchOddRange,
            int lookaheadEdges = 2)
        {
            var transitions = new[]
            {
                new TransitionRule(LocationSubtype.Hub, LocationSubtype.Path, new[] { "road" }),
                new TransitionRule(LocationSubtype.Path, LocationSubtype.Path, new[] { "road" }),
                new TransitionRule(LocationSubtype.Path, LocationSubtype.Building, new[] { "path" }),
                new TransitionRule(LocationSubtype.Building, LocationSubtype.Path, new[] { "path" })
            };
            var costs = new Dictionary<string, TravelCost>
            {
                ["road"] = new TravelCost(2, 1),
                ["path"] = new TravelCost(3, 1.5)
            };
            return new TopologyTemplate(
                "topology",
                new RouteSettings(
                    transitions,
                    lookaheadEdges,
                    maxTransitionsPerNode,
                    new TravelCost(2, 0.5),
                    branchOddRange,
                    1,
                    2),
                costs,
                10,
                6,
                22,
                new HubTravelSettings(
                    new OnFootTravelSettings(4, 2, 0.2),
                    new GuideTravelSettings(4, 200)),
                new RestSettings(2, 0.1));
        }

        /// <summary>Генератор на заданных шаблонах: без загрузки данных и без сервисов.</summary>
        private static WorldGenerator CustomGenerator(
            TopologyTemplate topology,
            int pathChainLength)
        {
            var repository = new LocationRepository(
                new[]
                {
                    WorldTemplate(
                        "loc_hub",
                        LocationKind.Hub,
                        LocationSubtype.Hub,
                        new[] { LocationTag.Hub, LocationTag.Road, LocationTag.Outdoor },
                        new IntRange(1, 1)),
                    WorldTemplate(
                        "loc_path",
                        LocationKind.Wild,
                        LocationSubtype.Path,
                        new[] { LocationTag.Road, LocationTag.Outdoor },
                        new IntRange(pathChainLength, pathChainLength)),
                    WorldTemplate(
                        "loc_house",
                        LocationKind.Wild,
                        LocationSubtype.Building,
                        new[] { LocationTag.Outdoor },
                        new IntRange(1, 1))
                });
            return new WorldGenerator(repository, new TopologyRepository(topology));
        }

        /// <summary>Локации пути звена по порядку шага.</summary>
        private static List<LocationInstance> PathNodes(WorldMap map, int edgeNumber)
        {
            var result = new List<LocationInstance>();
            foreach (var location in map.Locations)
            {
                if (location.Subtype == LocationSubtype.Path && location.RouteEdge == edgeNumber)
                {
                    result.Add(location);
                }
            }

            result.Sort((left, right) => left.RouteStep.CompareTo(right.RouteStep));
            return result;
        }

        /// <summary>Сколько веток выходит из локации пути: цели того же звена, кроме пути и хаба.</summary>
        private static int BranchCountAt(WorldMap map, LocationInstance pathNode)
        {
            var count = 0;
            foreach (var edge in map.EdgesFrom(pathNode.InstanceId))
            {
                var target = map.GetLocation(edge.ToId);
                if (target.RouteEdge == pathNode.RouteEdge &&
                    target.Subtype != LocationSubtype.Path &&
                    target.Subtype != LocationSubtype.Hub)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Сколько переходов вперёд, кроме веток, выходит из локации пути.</summary>
        private static int ForwardPathCount(WorldMap map, LocationInstance pathNode)
        {
            var count = 0;
            foreach (var edge in map.EdgesFrom(pathNode.InstanceId))
            {
                var target = map.GetLocation(edge.ToId);
                if (target.Subtype != LocationSubtype.Path)
                {
                    continue;
                }

                var ahead = target.RouteEdge > pathNode.RouteEdge ||
                            (target.RouteEdge == pathNode.RouteEdge && target.RouteStep > pathNode.RouteStep);
                if (ahead)
                {
                    count++;
                }
            }

            return count;
        }

        private static string Signature(WorldMap map)
        {
            var parts = new List<string>();
            foreach (var location in map.Locations)
            {
                parts.Add($"{location.InstanceId}:{location.TemplateId}:{location.Seed}:{location.Background}");
            }

            foreach (var edge in map.Edges)
            {
                parts.Add($"{edge.FromId}>{edge.ToId}:{edge.Via}:{edge.Length}");
            }

            return string.Join("|", parts);
        }

        private static List<LocationInstance> OfTemplate(WorldMap map, string templateId)
        {
            var result = new List<LocationInstance>();
            foreach (var location in map.Locations)
            {
                if (location.TemplateId == templateId)
                {
                    result.Add(location);
                }
            }

            return result;
        }

        private static WorldEdge? FindEdge(WorldMap map, string fromId, string toId)
        {
            foreach (var edge in map.EdgesFrom(fromId))
            {
                if (edge.ToId == toId)
                {
                    return edge;
                }
            }

            return null;
        }

        private static WorldEdge FirstPaidEdge(WorldMap map, string fromId)
        {
            foreach (var edge in map.EdgesFrom(fromId))
            {
                if (edge.Cost.Stamina > 0)
                {
                    return edge;
                }
            }

            throw new InvalidOperationException("Нет платного ребра из " + fromId);
        }
    }
}
