#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.Trade;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Save
{
    /// <summary>Часть класса <see cref="WorldSaveMapper"/>: снимки локаций, рёбер, торговцев, квестов и досок.</summary>
    public static partial class WorldSaveMapper
    {

        private static IReadOnlyList<LocationSaveData> ToLocationData(WorldMap map)
        {
            var result = new List<LocationSaveData>();
            foreach (var location in map.Locations)
            {
                var objects = new List<LocationObjectSaveData>();
                foreach (var locationObject in location.Objects)
                {
                    var items = new List<ItemSaveData>();
                    if (locationObject is LootLocationObject lootObject)
                    {
                        foreach (var item in lootObject.Loot)
                        {
                            items.Add(new ItemSaveData(
                                item.InstanceId,
                                item.ItemId,
                                item.Stack,
                                item.Durability,
                                item.DurabilityMax,
                                item.VariantKey,
                                item.Magazine,
                                item.Charges,
                                item.AmmoItemId));
                        }
                    }
                    else if (locationObject is AnomalyLocationObject anomalyObject)
                    {
                        foreach (var item in anomalyObject.Loot)
                        {
                            items.Add(new ItemSaveData(
                                item.InstanceId,
                                item.ItemId,
                                item.Stack,
                                item.Durability,
                                item.DurabilityMax,
                                item.VariantKey,
                                item.Magazine,
                                item.Charges,
                                item.AmmoItemId));
                        }
                    }

                    var attemptsUsed = locationObject is AnomalyLocationObject anomaly
                        ? anomaly.AttemptsUsed
                        : 0;
                    var radarPlaced = locationObject is AnomalyLocationObject radar && radar.RadarPlaced;
                    var artifactCells = locationObject is AnomalyLocationObject radarSource
                        ? new List<int>(radarSource.ArtifactCells)
                        : new List<int>();
                    var artifactAttempts = locationObject is AnomalyLocationObject radarAttempts
                        ? new List<int>(radarAttempts.ArtifactAttempts)
                        : new List<int>();
                    var scannedCells = locationObject is AnomalyLocationObject scanned
                        ? new List<int>(scanned.ScannedCells)
                        : new List<int>();
                    var searchedCells = locationObject is AnomalyLocationObject searched
                        ? new List<int>(searched.SearchedCells)
                        : new List<int>();
                    objects.Add(new LocationObjectSaveData(
                        locationObject.SpawnId,
                        locationObject.Looted,
                        items,
                        attemptsUsed,
                        radarPlaced,
                        artifactCells,
                        artifactAttempts,
                        scannedCells,
                        searchedCells));
                }

                result.Add(new LocationSaveData(
                    location.InstanceId,
                    location.TemplateId,
                    location.Seed,
                    location.Background,
                    location.Subtype,
                    location.Tier,
                    location.Danger,
                    location.Visited,
                    location.Cleared,
                    location.EnemyGone,
                    location.StashRolled,
                    location.StashPresent,
                    location.StashOpened,
                    location.Spawned,
                    objects,
                    location.RouteStep,
                    location.RouteEdge,
                    location.StashLockAttemptsUsed,
                    location.StashLockSolved,
                    location.StashLockFailed,
                    new List<string>(location.StashLockGuesses)));
            }

            return result;
        }

        private static IReadOnlyList<EdgeSaveData> ToEdgeData(WorldMap map)
        {
            var result = new List<EdgeSaveData>();
            foreach (var edge in map.Edges)
            {
                result.Add(new EdgeSaveData(
                    edge.EdgeId,
                    edge.FromId,
                    edge.ToId,
                    edge.Via,
                    edge.Length,
                    edge.Cost.Stamina,
                    edge.Cost.Hours,
                    edge.OneWay,
                    edge.Hidden));
            }

            return result;
        }

        private static IReadOnlyList<TraderSaveData> ToTraderData(IReadOnlyList<TraderState> traders)
        {
            var result = new List<TraderSaveData>();
            foreach (var trader in traders)
            {
                var stock = new List<TraderStockSaveData>();
                foreach (var pair in trader.Stock)
                {
                    stock.Add(new TraderStockSaveData(pair.Key, pair.Value));
                }

                stock.Sort((left, right) => string.CompareOrdinal(left.ItemId, right.ItemId));
                result.Add(new TraderSaveData(
                    trader.ActorId,
                    trader.LastRestockAt,
                    stock));
            }

            result.Sort((left, right) => string.CompareOrdinal(left.ActorId, right.ActorId));
            return result;
        }

        private static IReadOnlyList<QuestBoardSaveData> ToQuestBoardData(
            IReadOnlyList<QuestBoardState> boards)
        {
            var result = new List<QuestBoardSaveData>();
            foreach (var board in boards)
            {
                var slots = new List<QuestBoardSlotSaveData>();
                foreach (var slot in board.Slots)
                {
                    slots.Add(new QuestBoardSlotSaveData(slot.QuestId, slot.AvailableAt));
                }

                result.Add(new QuestBoardSaveData(board.LocationInstanceId, slots));
            }

            result.Sort((left, right) => string.CompareOrdinal(left.LocationInstanceId, right.LocationInstanceId));
            return result;
        }

        private static IReadOnlyList<QuestSaveData> ToQuestData(IReadOnlyList<QuestState> quests)
        {
            var result = new List<QuestSaveData>();
            foreach (var quest in quests)
            {
                var items = new List<QuestItemSaveData>();
                foreach (var item in quest.Items)
                {
                    items.Add(new QuestItemSaveData(item.ItemId, item.Count));
                }

                result.Add(new QuestSaveData(
                    quest.QuestId,
                    quest.BoardLocationId,
                    quest.Status,
                    quest.AcceptedAt,
                    quest.CooldownUntil,
                    quest.CompletedAt,
                    quest.LocationTemplateId,
                    quest.LocationInstanceId,
                    quest.ActorIds,
                    items,
                    quest.KillCount,
                    quest.Cleared,
                    quest.RequiredCount,
                    quest.Slot));
            }

            result.Sort((left, right) => string.CompareOrdinal(left.QuestId, right.QuestId));
            return result;
        }
    }
}
