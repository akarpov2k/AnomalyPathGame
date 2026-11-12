#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.Save
{
    /// <summary>Часть класса <see cref="WorldSaveMapper"/>: восстановление объектов локации и их предметов.</summary>
    public static partial class WorldSaveMapper
    {

        private static LocationObject? RestoreObject(LocationObjectSaveData saved, ISpawnRepository? spawns)
        {
            if (saved.SpawnId == StashLocationObject.DefaultSpawnId)
            {
                return StashLocationObject.CreateDefault();
            }

            if (spawns == null || !spawns.TryGet(saved.SpawnId, out var entry) || entry == null)
            {
                return null;
            }

            var restored = entry.CreateObject();
            if (restored is LootLocationObject lootObject)
            {
                lootObject.SetLoot(ToInstances(saved.Items));
            }
            else if (restored is AnomalyLocationObject anomalyObject)
            {
                anomalyObject.Restore(ToInstances(saved.Items), saved.AttemptsUsed);
                anomalyObject.RestoreRadar(
                    saved.RadarPlaced,
                    saved.ArtifactCells,
                    saved.ArtifactAttempts,
                    saved.ScannedCells,
                    saved.SearchedCells);
            }

            if (saved.Looted)
            {
                restored.MarkLooted();
            }

            return restored;
        }

        private static List<ItemInstance> ToInstances(IReadOnlyList<ItemSaveData> saved)
        {
            var items = new List<ItemInstance>();
            foreach (var item in saved)
            {
                items.Add(new ItemInstance(
                    item.InstanceId,
                    item.ItemId,
                    item.Stack,
                    item.Durability,
                    item.DurabilityMax,
                    item.VariantKey,
                    item.Magazine,
                    item.Charges));
            }

            return items;
        }
    }
}
