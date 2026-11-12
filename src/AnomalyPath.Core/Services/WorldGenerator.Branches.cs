#nullable enable
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="WorldGenerator"/>: ветки маршрута и выбор их локаций.</summary>
    public sealed partial class WorldGenerator
    {
        /// <summary>
        /// Локация звена, к которой крепится ветка: первый круг — с конца звена по одной
        /// на каждую локацию, второй и следующие — с локации, выбранной по seed звена.
        /// </summary>
        private static int BranchAttachIndex(int chainLength, int branchNumber, int extraStart)
        {
            if (branchNumber < chainLength)
            {
                return chainLength - 1 - branchNumber;
            }

            var extra = branchNumber - chainLength;
            var index = (extraStart - extra) % chainLength;
            return index < 0 ? index + chainLength : index;
        }

        private LocationInstance? BuildBranch(
            WorldMap map,
            LocationInstance attach,
            LocationTemplate attachTemplate,
            int edgeNumber,
            int branchNumber,
            TopologyTemplate topology,
            IWorldRandom random)
        {
            var current = attach;
            var currentTemplate = attachTemplate;
            var step = 0;
            for (var depth = 0; depth < topology.Route.BranchMaxChains; depth++)
            {
                var next = PickTarget(currentTemplate, topology, random, allowPath: false);
                if (next == null)
                {
                    break;
                }

                var (target, via) = next.Value;
                var count = topology.Route.BranchMaxChains <= 1
                    ? 1
                    : target.ChainLength.Sample(random);
                for (var index = 0; index < count; index++)
                {
                    var nodeVia = index == 0 ? via : PickVia(currentTemplate, target, topology, random);
                    var node = NewInstance(
                        target,
                        random,
                        attach.RouteStep,
                        edgeNumber,
                        InstanceId(edgeNumber, "b" + branchNumber.ToString(CultureInfo.InvariantCulture), step));
                    map.AddLocation(node);
                    AddPair(map, current.InstanceId, node.InstanceId, nodeVia, topology, EdgeId(edgeNumber, "b" + branchNumber.ToString(CultureInfo.InvariantCulture), step));
                    current = node;
                    currentTemplate = target;
                    step++;

                    if (!string.IsNullOrWhiteSpace(target.InteriorId) &&
                        !string.Equals(target.InteriorId, target.Id, System.StringComparison.Ordinal) &&
                        _locations.Contains(target.InteriorId))
                    {
                        var interiorTemplate = _locations.Get(target.InteriorId!);
                        var interior = NewInstance(
                            interiorTemplate,
                            random,
                            attach.RouteStep,
                            edgeNumber,
                            InstanceId(
                                edgeNumber,
                                "b" + branchNumber.ToString(CultureInfo.InvariantCulture) + "int",
                                step));
                        map.AddLocation(interior);
                        AddPair(
                            map,
                            current.InstanceId,
                            interior.InstanceId,
                            PickVia(target, interiorTemplate, topology, random),
                            topology,
                            EdgeId(
                                edgeNumber,
                                "b" + branchNumber.ToString(CultureInfo.InvariantCulture) + "int",
                                step));
                        current = interior;
                        currentTemplate = interiorTemplate;
                        step++;
                        break;
                    }
                }

                if (!CanLeave(currentTemplate, topology))
                {
                    break;
                }
            }

            if (current.InstanceId == attach.InstanceId)
            {
                return null;
            }

            return current;
        }

        /// <summary>Следующая локация ветки: любой подтип, кроме пути, если переход разрешён.</summary>
        private (LocationTemplate Template, string Via)? PickTarget(
            LocationTemplate from,
            TopologyTemplate topology,
            IWorldRandom random,
            bool allowPath)
        {
            var candidates = new List<(LocationTemplate Template, string Via, double Weight)>();
            foreach (var template in _locations.All)
            {
                if (template.Unique)
                {
                    continue;
                }

                if (!allowPath && template.Subtype == LocationSubtype.Path)
                {
                    continue;
                }

                if (template.Subtype == LocationSubtype.Hub)
                {
                    continue;
                }

                foreach (var rule in topology.Route.Rules(from, template))
                {
                    foreach (var via in rule.Via)
                    {
                        if (!ViaRules.Allows(via, from.Subtype, template.Subtype, from.Tags, template.Tags))
                        {
                            continue;
                        }

                        candidates.Add((template, via, rule.Weight));
                        break;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var index = WeightedPick.ChooseIndex(
                candidates.Count,
                position => candidates[position].Weight,
                random);
            if (index < 0)
            {
                return null;
            }

            var chosen = candidates[index];
            return (chosen.Template, chosen.Via);
        }

        /// <summary>Продолжается ли ветка дальше: есть ли переход из этой локации не на путь.</summary>
        private bool CanLeave(LocationTemplate from, TopologyTemplate topology)
        {
            foreach (var template in _locations.All)
            {
                if (template.Unique ||
                    template.Subtype == LocationSubtype.Hub ||
                    template.Subtype == LocationSubtype.Path)
                {
                    continue;
                }

                if (topology.Route.Rules(from, template).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static int BranchCount(int previous, int edgeNumber, RouteSettings route, IWorldRandom random)
        {
            if (edgeNumber == 1)
            {
                return route.BranchOddRange.Sample(random);
            }

            return edgeNumber % 2 == 0
                ? previous + route.BranchEvenGrowth
                : route.BranchOddRange.Sample(random);
        }
    }
}
