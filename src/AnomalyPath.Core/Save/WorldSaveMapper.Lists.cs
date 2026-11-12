#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Save
{
    /// <summary>Часть класса <see cref="WorldSaveMapper"/>: восстановление списков — торговцы, квесты, доски, рейд.</summary>
    public static partial class WorldSaveMapper
    {

        /// <summary>Восстанавливает состояние торговцев из снимка.</summary>
        /// <param name="traders">Снимки торговцев.</param>
        /// <returns>Состояния торговцев в порядке снимка.</returns>
        public static IReadOnlyList<TraderState> LoadTraders(IReadOnlyList<TraderSaveData> traders)
        {
            var result = new List<TraderState>();
            foreach (var data in traders)
            {
                var state = new TraderState(data.ActorId);
                foreach (var item in data.Stock)
                {
                    state.SetStock(item.ItemId, item.Count);
                }

                state.MarkRestocked(data.LastRestockAt);
                result.Add(state);
            }

            return result;
        }

        /// <summary>Восстанавливает состояние квестов из снимка.</summary>
        /// <param name="quests">Снимки квестов.</param>
        /// <returns>Состояния квестов в порядке снимка.</returns>
        public static IReadOnlyList<QuestState> LoadQuests(IReadOnlyList<QuestSaveData> quests)
        {
            var result = new List<QuestState>();
            foreach (var data in quests)
            {
                var items = new List<QuestItemTarget>();
                foreach (var item in data.Items)
                {
                    items.Add(new QuestItemTarget(item.ItemId, item.Count));
                }

                result.Add(new QuestState(
                    data.QuestId,
                    data.BoardLocationId,
                    data.Status,
                    data.AcceptedAt,
                    data.CooldownUntil,
                    data.CompletedAt,
                    data.LocationTemplateId,
                    data.LocationInstanceId,
                    data.ActorIds,
                    items,
                    data.KillCount,
                    data.Cleared,
                    data.RequiredCount,
                    data.Slot));
            }

            return result;
        }

        /// <summary>Восстанавливает состояние досок объявлений из снимка.</summary>
        /// <param name="boards">Снимки досок.</param>
        /// <returns>Состояния досок в порядке снимка.</returns>
        public static IReadOnlyList<QuestBoardState> LoadQuestBoards(
            IReadOnlyList<QuestBoardSaveData> boards)
        {
            var result = new List<QuestBoardState>();
            foreach (var data in boards)
            {
                var slots = new List<QuestBoardSlotState>();
                foreach (var slot in data.Slots)
                {
                    slots.Add(new QuestBoardSlotState(slot.QuestId, slot.AvailableAt));
                }

                result.Add(new QuestBoardState(data.LocationInstanceId, slots));
            }

            return result;
        }

        /// <summary>Восстанавливает состояние рейда из снимка.</summary>
        /// <param name="raid">Снимок рейда; <c>null</c> — рейд не начат.</param>
        /// <returns>Состояние рейда.</returns>
        public static RaidState LoadRaid(RaidSaveData? raid)
        {
            return raid == null
                ? new RaidState()
                : new RaidState(raid.Active, raid.ClearedLocationIds);
        }
    }
}
