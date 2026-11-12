#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="WorldTests"/>: ветки маршрута — предел, распределение, вместимость.</summary>
    public sealed partial class WorldTests
    {
        private void BranchTests(IWorldGenerator generator, TopologyTemplate topology)
        {
            Check("мир: с локации не больше предела переходов", () =>
            {
                var max = topology.Route.MaxTransitionsPerNode;
                var map = generator.Generate(42);
                var checkedNodes = 0;
                foreach (var location in map.Locations)
                {
                    if (location.Subtype != LocationSubtype.Path)
                    {
                        continue;
                    }

                    var branches = BranchCountAt(map, location);
                    var total = branches + ForwardPathCount(map, location);
                    True(branches <= max - 1, $"{location.InstanceId}: {branches} веток при пределе {max}");
                    True(total <= max, $"{location.InstanceId}: {total} переходов при пределе {max}");
                    checkedNodes++;
                }

                True(checkedNodes > 0, "локации пути есть");
            });

            Check("мир: несколько веток с одной локации допустимы", () =>
            {
                var max = topology.Route.MaxTransitionsPerNode;
                var found = false;
                for (var seed = 1; seed <= 80 && !found; seed++)
                {
                    var map = generator.Generate(seed);
                    for (var edge = 1; edge <= map.RouteEdges && !found; edge++)
                    {
                        foreach (var node in PathNodes(map, edge))
                        {
                            var count = BranchCountAt(map, node);
                            True(count <= max - 1, $"seed {seed}, звено {edge}: {count} веток с локации");
                            if (count >= 2)
                            {
                                found = true;
                            }
                        }
                    }
                }

                True(found, "нашлась локация с несколькими ветками");
            });

            Check("мир: ветки распределяются по локациям звена и не превышают вместимость", () =>
            {
                var max = topology.Route.MaxTransitionsPerNode;
                var map = generator.Generate(42);
                for (var edge = 1; edge <= map.RouteEdges; edge++)
                {
                    var pathNodes = PathNodes(map, edge);
                    var branches = map.BranchCount(edge);
                    var capacity = pathNodes.Count * (max - 1);
                    True(branches <= capacity, $"звено {edge}: {branches} веток при вместимости {capacity}");
                    if (branches < 2 || pathNodes.Count < 2)
                    {
                        continue;
                    }

                    var withBranches = 0;
                    foreach (var node in pathNodes)
                    {
                        if (BranchCountAt(map, node) > 0)
                        {
                            withBranches++;
                        }
                    }

                    True(withBranches >= 2, $"звено {edge}: ветки только на {withBranches} локациях");
                }
            });

            Check("мир: второй круг веток распределяется по seed, на каждой локации есть ветка", () =>
            {
                var custom = CustomGenerator(CustomTopology(3, new IntRange(3, 3)), pathChainLength: 2);
                var map = custom.Generate(5);
                var nodes = PathNodes(map, 1);
                Equal(2, nodes.Count, "локации звена");
                Equal(3, map.BranchCount(1), "ветки звена");
                var first = BranchCountAt(map, nodes[0]);
                var last = BranchCountAt(map, nodes[1]);
                Equal(1, Math.Min(first, last), "на каждой локации хотя бы одна ветка");
                Equal(2, Math.Max(first, last), "вторая ветка на одной из локаций");
                Equal(3, first + last, "все ветки размещены");
            });

            Check("мир: вторая ветка может быть не только на последней локации", () =>
            {
                var custom = CustomGenerator(CustomTopology(3, new IntRange(4, 4)), pathChainLength: 3);
                var found = false;
                for (var seed = 1; seed <= 40 && !found; seed++)
                {
                    var map = custom.Generate(seed);
                    var nodes = PathNodes(map, 1);
                    Equal(3, nodes.Count, "локации звена");
                    var last = nodes[nodes.Count - 1];
                    True(BranchCountAt(map, last) >= 1, $"seed {seed}: последняя локация без ветки");
                    foreach (var node in nodes)
                    {
                        if (node.InstanceId != last.InstanceId && BranchCountAt(map, node) >= 2)
                        {
                            found = true;
                        }
                    }
                }

                True(found, "нашлась вторая ветка не на последней локации");
            });

            Check("мир: ветки сверх вместимости сокращаются", () =>
            {
                var custom = CustomGenerator(CustomTopology(2, new IntRange(3, 3)), pathChainLength: 1);
                var map = custom.Generate(5);
                var nodes = PathNodes(map, 1);
                Equal(1, nodes.Count, "локации звена");
                Equal(1, map.BranchCount(1), "ветки сокращены до вместимости");
                Equal(1, BranchCountAt(map, nodes[0]), "ветка на локации");
            });
        }
    }
}
