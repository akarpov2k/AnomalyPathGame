#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: объекты локаций и их пулы.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateSpawns(LoadedData data, List<ValidationIssue> issues)
        {
            SpawnRepository spawns;
            LootRepository loot;
            ActorRepository actors;
            AnomalyRepository anomalies;
            try
            {
                spawns = SpawnRepository.FromLoadedData(data);
                loot = LootRepository.FromLoadedData(data);
                actors = ActorRepository.FromLoadedData(data);
                anomalies = AnomalyRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                return;
            }

            foreach (var spawn in spawns.All)
            {
                if (spawn.LootTableId != null && !loot.TryGet(spawn.LootTableId, out _))
                {
                    issues.Add(new ValidationIssue(
                        spawn.Id,
                        CoreErrors.LootTableNotFound,
                        CoreText.Format(CoreErrors.LootTableNotFound, spawn.LootTableId)));
                }

                if (spawn.ActorId == null)
                {
                    if (spawn.ObjectKind == LocationObjectKind.Anomaly &&
                        (spawn.AnomalyId == null || !anomalies.TryGet(spawn.AnomalyId, out _)))
                    {
                        issues.Add(new ValidationIssue(
                            spawn.Id,
                            CoreErrors.AnomalyNotFound,
                            CoreText.Format(CoreErrors.AnomalyNotFound, spawn.AnomalyId ?? "<null>")));
                    }

                    continue;
                }

                if (!actors.Contains(spawn.ActorId))
                {
                    issues.Add(new ValidationIssue(
                        spawn.Id,
                        CoreErrors.ActorNotFound,
                        CoreText.Format(CoreErrors.ActorNotFound, spawn.ActorId)));
                    continue;
                }

                var actor = actors.Get(spawn.ActorId);
                if (spawn.ObjectKind == LocationObjectKind.Enemy && actor.Type != ActorType.Enemy)
                {
                    issues.Add(new ValidationIssue(
                        spawn.Id,
                        CoreErrors.SpawnEnemyActor,
                        CoreText.Format(CoreErrors.SpawnEnemyActor, spawn.ActorId)));
                    continue;
                }

                if (spawn.ObjectKind == LocationObjectKind.Npc && actor.Type != ActorType.Npc)
                {
                    issues.Add(new ValidationIssue(
                        spawn.Id,
                        CoreErrors.SpawnActorType,
                        CoreText.Format(CoreErrors.SpawnActorType, spawn.ActorId)));
                }
            }

            foreach (var record in data.All("locations"))
            {
                LocationTemplate template;
                try
                {
                    template = LocationTemplate.FromJson(record, "locations");
                }
                catch (DataException)
                {
                    continue;
                }

                var pool = template.SpawnPool;

                if (template.ObjectCount.Max > LocationObjectService.MaxObjectsPerLocation)
                {
                    issues.Add(new ValidationIssue(
                        template.Id,
                        CoreErrors.WorldObjectCountMax,
                        CoreText.Format(CoreErrors.WorldObjectCountMax, template.Id)));
                }

                if (template.HubView != HubView.None && template.LocationKind != LocationKind.Hub)
                {
                    issues.Add(new ValidationIssue(
                        template.Id,
                        CoreErrors.WorldHubViewNotHub,
                        CoreText.Format(CoreErrors.WorldHubViewNotHub, template.Id)));
                }

                if (template.WorldMapSpot != null && template.LocationKind != LocationKind.Hub)
                {
                    issues.Add(new ValidationIssue(
                        template.Id,
                        CoreErrors.WorldMapSpotNotHub,
                        CoreText.Format(CoreErrors.WorldMapSpotNotHub, template.Id)));
                }

                if (template.LocationKind == LocationKind.Hub)
                {
                    if (template.ObjectCount.Min != 0 || template.ObjectCount.Max != 0)
                    {
                        issues.Add(new ValidationIssue(
                            template.Id,
                            CoreErrors.SpawnObjectCount,
                            CoreText.Format(CoreErrors.SpawnObjectCount, template.Id)));
                    }

                    foreach (var spawnId in pool)
                    {
                        if (!spawns.TryGet(spawnId, out var hubEntry) || hubEntry == null)
                        {
                            issues.Add(new ValidationIssue(
                                template.Id,
                                CoreErrors.SpawnPoolOnHub,
                                CoreText.Format(CoreErrors.SpawnPoolOnHub, template.Id)));
                            continue;
                        }

                        if (hubEntry.ObjectKind != LocationObjectKind.Npc || !hubEntry.Unique)
                        {
                            issues.Add(new ValidationIssue(
                                template.Id,
                                CoreErrors.SpawnPoolOnHub,
                                CoreText.Format(CoreErrors.SpawnPoolOnHub, template.Id)));
                        }
                    }

                    continue;
                }

                if (template.ObjectCount.Max > pool.Count)
                {
                    issues.Add(new ValidationIssue(
                        template.Id,
                        CoreErrors.SpawnObjectCount,
                        CoreText.Format(CoreErrors.SpawnObjectCount, template.Id)));
                }

                foreach (var spawnId in pool)
                {
                    if (!spawns.TryGet(spawnId, out var entry) || entry == null)
                    {
                        issues.Add(new ValidationIssue(
                            template.Id,
                            CoreErrors.SpawnNotFound,
                            CoreText.Format(CoreErrors.SpawnNotFound, spawnId)));
                        continue;
                    }

                    if (entry.ObjectKind == LocationObjectKind.Stash)
                    {
                        issues.Add(new ValidationIssue(
                            template.Id,
                            CoreErrors.SpawnPoolStash,
                            CoreText.Format(CoreErrors.SpawnPoolStash, spawnId)));
                        continue;
                    }

                    if (entry.ObjectKind == LocationObjectKind.Anomaly)
                    {
                        if (entry.AnomalyId == null ||
                            !anomalies.TryGet(entry.AnomalyId, out var anomaly) ||
                            anomaly == null)
                        {
                            issues.Add(new ValidationIssue(
                                template.Id,
                                CoreErrors.AnomalyNotFound,
                                CoreText.Format(CoreErrors.AnomalyNotFound, entry.AnomalyId ?? "<null>")));
                            continue;
                        }

                        if (!anomaly.AllowsTier(template.Tier))
                        {
                            issues.Add(new ValidationIssue(
                                template.Id,
                                CoreErrors.SpawnAnomalyTier,
                                CoreText.Format(CoreErrors.SpawnAnomalyTier, entry.AnomalyId, template.Id)));
                            continue;
                        }
                    }

                    if (!entry.Matches(template.Tags, template.Tier, template.Subtype, template.Unique))
                    {
                        issues.Add(new ValidationIssue(
                            template.Id,
                            CoreErrors.SpawnPoolIncompatible,
                            CoreText.Format(CoreErrors.SpawnPoolIncompatible, spawnId, template.Id)));
                    }
                }
            }
        }
    }
}
