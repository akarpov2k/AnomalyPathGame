#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: редкости.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateRarities(LoadedData data, List<ValidationIssue> issues)
        {
            var records = data.All("rarities");
            if (records.Count != RarityIds.Count)
            {
                issues.Add(new ValidationIssue(
                    "rarities",
                    CoreErrors.RarityRecordCount,
                    CoreText.Format(CoreErrors.RarityRecordCount, RarityIds.Count, records.Count)));
            }

            var orders = new HashSet<int>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var record in records)
            {
                if (!record.TryGet("id", out var idValue) || idValue.Kind != JsonKind.String)
                {
                    continue;
                }

                var id = idValue.Text;
                ids.Add(id);
                if (!record.TryGet("order", out var orderValue) || orderValue.Kind != JsonKind.Number)
                {
                    continue;
                }

                var order = (int)Math.Round(orderValue.Number);
                if (!orders.Add(order))
                {
                    issues.Add(new ValidationIssue(
                        id,
                        CoreErrors.RarityOrderDuplicate,
                        CoreText.Format(CoreErrors.RarityOrderDuplicate, order)));
                }
            }

            foreach (var rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
            {
                var id = RarityIds.Id(rarity);
                if (!ids.Contains(id))
                {
                    issues.Add(new ValidationIssue(
                        "rarities",
                        CoreErrors.RarityMissing,
                        CoreText.Format(CoreErrors.RarityMissing, id)));
                }
            }
        }
    }
}
