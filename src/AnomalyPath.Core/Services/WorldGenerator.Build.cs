#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="WorldGenerator"/>: построение звеньев, рёбер и квестовых цепочек.</summary>
    public sealed partial class WorldGenerator
    {

        private void BuildForward(
            WorldMap map,
            LocationInstance from,
            LocationTemplate fromTemplate,
            int edgesToBuild,
            LocationInstance? hub,
            IReadOnlyList<string>? activeChains)
        {
            var topology = _topology.Topology;
            var current = from;
            var currentTemplate = fromTemplate;
            for (var index = 0; index < edgesToBuild; index++)
            {
                var edgeNumber = map.RouteEdges + 1;
                current = BuildEdge(map, current, currentTemplate, edgeNumber, topology, hub, activeChains);
                currentTemplate = _locations.Get(current.TemplateId);
            }
        }

        /// <summary>Хаб, от которого строится маршрут: текущий, иначе последний посещённый.</summary>
        private static LocationInstance? ResolveHub(WorldState world)
        {
            if (world.CurrentLocation.Subtype == LocationSubtype.Hub)
            {
                return world.CurrentLocation;
            }

            return world.Map.TryGetLocation(world.LastHubId, out var hub) ? hub : null;
        }

        private LocationInstance BuildEdge(
            WorldMap map,
            LocationInstance from,
            LocationTemplate fromTemplate,
            int edgeNumber,
            TopologyTemplate topology,
            LocationInstance? hub,
            IReadOnlyList<string>? activeChains)
        {
            var paths = Select(LocationSubtype.Path);
            if (paths.Count == 0)
            {
                throw new DataException("locations", CoreErrors.WorldNoPath);
            }

            var random = new SeededWorldRandom(StableHash.Combine(map.RouteSeed, "edge:" + edgeNumber.ToString(CultureInfo.InvariantCulture)));
            var pathTemplate = paths[random.NextInt(0, paths.Count)];
            var count = pathTemplate.ChainLength.Sample(random);
            var previousFront = map.RouteFrontIds;
            var chain = new List<LocationInstance>(count);
            var previous = from;
            var previousTemplate = fromTemplate;

            for (var index = 0; index < count; index++)
            {
                var node = NewInstance(
                    pathTemplate,
                    random,
                    index + 1,
                    edgeNumber,
                    InstanceId(edgeNumber, "path", index));
                map.AddLocation(node);
                var via = PickVia(previousTemplate, pathTemplate, topology, random);
                AddPair(map, previous.InstanceId, node.InstanceId, via, topology, EdgeId(edgeNumber, "path", index));
                if (index == 0)
                {
                    ConnectFront(map, previousFront, node, pathTemplate, edgeNumber, topology, random);
                }

                previous = node;
                previousTemplate = pathTemplate;
                chain.Add(node);
            }

            // Квестовая цепочка занимает место одной ветки: с локации не бывает больше
            // переходов, чем задаёт maxTransitionsPerNode (места сцены — три), и она
            // видна сразу у входа в звено. Пока цепочка стоит в звене, обычных веток
            // на локацию не больше одной: второе место ветки держит цепочка.
            var questChains = edgeNumber == 1 && hub != null
                ? PlaceableChains(activeChains)
                : new List<QuestChainTemplate>();
            var branchCapacity = questChains.Count == 0
                ? chain.Count * (topology.Route.MaxTransitionsPerNode - 1)
                : chain.Count * (topology.Route.MaxTransitionsPerNode - 2);
            var branches = Math.Min(
                Math.Max(0, branchCapacity - questChains.Count),
                Math.Max(
                    0,
                    BranchCount(map.RouteLastBranches, edgeNumber, topology.Route, random) - questChains.Count));
            var extraStart = chain.Count == 1
                ? 0
                : StableHash.Combine(
                    map.RouteSeed,
                    "attach:" + edgeNumber.ToString(CultureInfo.InvariantCulture)) % chain.Count;
            var front = new List<string>(branches);
            for (var index = 0; index < branches; index++)
            {
                var attach = chain[BranchAttachIndex(chain.Count, index, extraStart)];
                var exit = BuildBranch(map, attach, pathTemplate, edgeNumber, index, topology, random);
                if (exit != null)
                {
                    front.Add(exit.InstanceId);
                }
            }

            for (var index = 0; index < questChains.Count; index++)
            {
                // Квестовая цепочка висит на первой локации звена: игрок видит её вход
                // сразу после выхода из хаба и решает сам, идти в неё или дальше по дороге.
                var questExit = PlaceQuestChain(
                    map,
                    questChains[index],
                    chain[0],
                    pathTemplate,
                    edgeNumber,
                    branches + index,
                    topology,
                    hub!,
                    random);
                if (questExit != null)
                {
                    front.Add(questExit.InstanceId);
                }
            }

            map.MarkBranchCount(edgeNumber, branches + questChains.Count);
            map.MarkRoute(edgeNumber, previous.InstanceId, branches + questChains.Count, front);
            return previous;
        }

        /// <summary>Цепочки из списка активных, которые есть в данных и непусты.</summary>
        private List<QuestChainTemplate> PlaceableChains(IReadOnlyList<string>? activeChains)
        {
            var result = new List<QuestChainTemplate>();
            if (activeChains == null || _chains == null)
            {
                return result;
            }

            foreach (var chainId in activeChains)
            {
                if (string.IsNullOrWhiteSpace(chainId) || !_chains.TryGet(chainId, out var chain))
                {
                    continue;
                }

                if (chain.Locations.Count == 0 || result.Contains(chain))
                {
                    continue;
                }

                result.Add(chain);
            }

            return result;
        }

        /// <summary>
        /// Ставит квестовую цепочку в звено как ветку: локации и принудительные объекты
        /// создаются здесь, вход идёт от локации пути, выход — обратно в хаб.
        /// </summary>
        private LocationInstance? PlaceQuestChain(
            WorldMap map,
            QuestChainTemplate chain,
            LocationInstance attach,
            LocationTemplate attachTemplate,
            int edgeNumber,
            int branchNumber,
            TopologyTemplate topology,
            LocationInstance hub,
            IWorldRandom random)
        {
            if (!QuestChainLocations.Ensure(map, chain, _locations, _spawns, map.Seed))
            {
                return null;
            }

            var previous = attach;
            var previousTemplate = attachTemplate;
            var prefix = EdgeId(
                edgeNumber,
                "quest" + branchNumber.ToString(CultureInfo.InvariantCulture),
                0);
            for (var index = 0; index < chain.Locations.Count; index++)
            {
                var instanceId = QuestChainInstanceIds.For(chain.Id, index);
                if (!map.TryGetLocation(instanceId, out var step) || step == null)
                {
                    return null;
                }

                var stepTemplate = _locations.Get(step.TemplateId);
                if (!HasEdge(map, previous.InstanceId, instanceId))
                {
                    // Вход в цепочку — с локации маршрута (способ перехода выбирается по
                    // подтипам и тегам, роли зданий тут не заданы), шаги внутри цепочки —
                    // по дороге: свои подтипы между шагами топология не описывает.
                    var via = index == 0
                        ? ChainVia(attachTemplate, stepTemplate)
                        : "road";
                    var pairEdge = index == 0
                        ? prefix
                        : EdgeId(
                            edgeNumber,
                            "quest" + branchNumber.ToString(CultureInfo.InvariantCulture),
                            index);
                    AddPair(map, previous.InstanceId, instanceId, via, topology, pairEdge);
                }

                previous = step;
                previousTemplate = stepTemplate;

                if (!string.IsNullOrWhiteSpace(stepTemplate.InteriorId) &&
                    !string.Equals(stepTemplate.InteriorId, stepTemplate.Id, StringComparison.Ordinal) &&
                    _locations.Contains(stepTemplate.InteriorId!))
                {
                    var interiorTemplate = _locations.Get(stepTemplate.InteriorId!);
                    var interiorId = instanceId + "int";
                    if (!map.TryGetLocation(interiorId, out _))
                    {
                        var interior = NewInstance(
                            interiorTemplate,
                            random,
                            attach.RouteStep,
                            edgeNumber,
                            interiorId);
                        map.AddLocation(interior);
                        AddPair(
                            map,
                            step.InstanceId,
                            interior.InstanceId,
                            PickVia(stepTemplate, interiorTemplate, topology, random),
                            topology,
                            EdgeId(
                                edgeNumber,
                                "quest" + branchNumber.ToString(CultureInfo.InvariantCulture) + "int",
                                index));
                    }
                }
            }

            if (!HasEdge(map, previous.InstanceId, hub.InstanceId))
            {
                AddPair(
                    map,
                    previous.InstanceId,
                    hub.InstanceId,
                    "road",
                    topology,
                    EdgeId(
                        edgeNumber,
                        "quest" + branchNumber.ToString(CultureInfo.InvariantCulture) + "exit",
                        0));
            }

            return previous;
        }

    }
}
