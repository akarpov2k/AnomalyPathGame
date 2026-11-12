#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: аномалии и их пулы артефактов.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateAnomalies(LoadedData data, List<ValidationIssue> issues)
        {
            AnomalyRepository anomalies;
            LootRepository loot;
            ItemRepository items;
            DamageTypeRepository damageTypes;
            try
            {
                anomalies = AnomalyRepository.FromLoadedData(data);
                loot = LootRepository.FromLoadedData(data);
                items = ItemRepository.FromLoadedData(data);
                damageTypes = DamageTypeRepository.FromLoadedData(data);
            }
            catch (DataException)
            {
                // Ошибки разбора уже собраны другими проходами.
                return;
            }

            foreach (var anomaly in anomalies.All)
            {
                if (!damageTypes.Contains(anomaly.DamageTypeId))
                {
                    issues.Add(new ValidationIssue(
                        anomaly.Id,
                        CoreErrors.UnknownDamageType,
                        CoreText.Format(CoreErrors.UnknownDamageType, anomaly.DamageTypeId)));
                }

                if (!loot.TryGet(anomaly.ArtifactTableId, out var table) || table == null)
                {
                    issues.Add(new ValidationIssue(
                        anomaly.Id,
                        CoreErrors.LootTableNotFound,
                        CoreText.Format(CoreErrors.LootTableNotFound, anomaly.ArtifactTableId)));
                    continue;
                }

                var entries = new List<LootEntry>();
                CollectEntries(table, loot, entries, new HashSet<string>(StringComparer.Ordinal));
                if (entries.Count == 0)
                {
                    issues.Add(new ValidationIssue(
                        anomaly.Id,
                        CoreErrors.AnomalyPoolEmpty,
                        CoreText.Format(CoreErrors.AnomalyPoolEmpty, anomaly.ArtifactTableId)));
                    continue;
                }

                foreach (var entry in entries)
                {
                    if (!items.TryGet(entry.ItemId, out var item) || item == null ||
                        item.Category != ItemCategory.Artifact)
                    {
                        issues.Add(new ValidationIssue(
                            anomaly.Id,
                            CoreErrors.AnomalyPoolItem,
                            CoreText.Format(CoreErrors.AnomalyPoolItem, entry.ItemId)));
                        continue;
                    }

                    if (!string.Equals(item.ArtifactType, anomaly.ArtifactType, StringComparison.Ordinal))
                    {
                        issues.Add(new ValidationIssue(
                            anomaly.Id,
                            CoreErrors.AnomalyArtifactType,
                            CoreText.Format(CoreErrors.AnomalyArtifactType, item.Id, anomaly.Id)));
                    }
                }
            }
        }

        private static void CollectEntries(
            LootTable table,
            LootRepository loot,
            List<LootEntry> entries,
            HashSet<string> visited)
        {
            if (!visited.Add(table.Id))
            {
                return;
            }

            foreach (var sharedId in table.SharedTableIds)
            {
                if (loot.TryGet(sharedId, out var shared) && shared != null)
                {
                    CollectEntries(shared, loot, entries, visited);
                }
            }

            entries.AddRange(table.Entries);
        }
    }
}
