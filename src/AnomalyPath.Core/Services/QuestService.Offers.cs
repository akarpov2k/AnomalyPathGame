#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="QuestService"/>: слоты доски, взятие, отказ и сдача.</summary>
    public sealed partial class QuestService
    {
        /// <inheritdoc />
        public IReadOnlyList<QuestBoardSlot> GetBoard(GameSession session, string boardLocationId)
        {
            var result = new List<QuestBoardSlot>();
            if (!TryGetBoard(session, boardLocationId, out var board))
            {
                return result;
            }

            var state = session!.State;
            Expire(session);
            var player = state.Player!;
            var now = state.World.Clock.AbsoluteHours;
            var boardState = state.GetOrCreateQuestBoard(boardLocationId);
            boardState.EnsureSlots(board!.Slots.Count);
            for (var index = 0; index < board.Slots.Count; index++)
            {
                var config = board.Slots[index];
                var slotState = boardState.GetSlot(index);
                var offer = FindOffer(session, boardLocationId, index);
                if (offer == null && slotState.QuestId != null)
                {
                    slotState.Close(0);
                }

                if (offer == null && slotState.IsReady(now))
                {
                    offer = GenerateOffer(session, board, boardLocationId, index, player, now);
                    if (offer != null)
                    {
                        slotState.SetOffer(offer.QuestId);
                    }
                }

                result.Add(new QuestBoardSlot(
                    index,
                    config.Difficulty,
                    config.RefreshHours,
                    slotState.AvailableAt,
                    offer));
            }

            return result;
        }

        /// <inheritdoc />
        public QuestResult Accept(GameSession session, string boardLocationId, int slot)
        {
            if (!TryGetBoard(session, boardLocationId, out var board))
            {
                return QuestResult.Error(QuestResultStatus.Unavailable);
            }

            if (slot < 0 || slot >= board!.Slots.Count)
            {
                return QuestResult.Error(QuestResultStatus.SlotEmpty);
            }

            var state = session!.State;
            Expire(session);
            var now = state.World.Clock.AbsoluteHours;
            var boardState = state.GetOrCreateQuestBoard(boardLocationId);
            boardState.EnsureSlots(board.Slots.Count);
            var slotState = boardState.GetSlot(slot);
            if (slotState.IsLocked(now))
            {
                return QuestResult.Error(QuestResultStatus.SlotLocked);
            }

            var offered = FindOfferedQuest(session, boardLocationId, slot);
            if (offered == null)
            {
                return QuestResult.Error(QuestResultStatus.SlotEmpty);
            }

            if (!_quests.TryGet(offered.QuestId, out var template))
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, offered.QuestId);
            }

            if (template.QuestKind != QuestKind.Repeatable)
            {
                return QuestResult.Error(QuestResultStatus.NotRepeatable, offered.QuestId);
            }

            var player = state.Player!;
            if (!template.Requirements.IsMet(player))
            {
                return QuestResult.Error(QuestResultStatus.RequirementsNotMet, offered.QuestId);
            }

            if (IsActive(session, offered.QuestId))
            {
                return QuestResult.Error(QuestResultStatus.AlreadyActive, offered.QuestId);
            }

            if (CountActive(session) >= _rules.Require().MaxActiveQuests)
            {
                return QuestResult.Error(QuestResultStatus.ActiveLimit, offered.QuestId);
            }

            if (template.Type == QuestType.ClearLocation)
            {
                if (offered.LocationTemplateId == null ||
                    !_locations.Contains(offered.LocationTemplateId))
                {
                    return QuestResult.Error(QuestResultStatus.NoTargets, offered.QuestId);
                }

                var serial = state.NextQuestSerial();
                var random = new SeededWorldRandom(StableHash.Combine(
                    state.World.Map.Seed,
                    offered.QuestId + "#" + serial));
                if (!EnsureQuestLocation(
                        session,
                        _locations.Get(offered.LocationTemplateId),
                        offered,
                        random,
                        serial))
                {
                    return QuestResult.Error(QuestResultStatus.NoTargets, offered.QuestId);
                }
            }

            offered.Activate(now);
            slotState.Close(now + board.Slots[slot].RefreshHours);
            return QuestResult.Ok(offered.QuestId);
        }

        /// <inheritdoc />
        public QuestResult Decline(GameSession session, string boardLocationId, int slot)
        {
            if (!TryGetBoard(session, boardLocationId, out var board))
            {
                return QuestResult.Error(QuestResultStatus.Unavailable);
            }

            if (slot < 0 || slot >= board!.Slots.Count)
            {
                return QuestResult.Error(QuestResultStatus.SlotEmpty);
            }

            var state = session!.State;
            var now = state.World.Clock.AbsoluteHours;
            var boardState = state.GetOrCreateQuestBoard(boardLocationId);
            boardState.EnsureSlots(board.Slots.Count);
            var slotState = boardState.GetSlot(slot);
            if (slotState.IsLocked(now))
            {
                return QuestResult.Error(QuestResultStatus.SlotLocked);
            }

            var offered = FindOfferedQuest(session, boardLocationId, slot);
            if (offered == null)
            {
                return QuestResult.Error(QuestResultStatus.SlotEmpty);
            }

            offered.MarkCancelled(now);
            slotState.Close(now + board.Slots[slot].RefreshHours);
            return QuestResult.Ok(offered.QuestId);
        }

        /// <inheritdoc />
        public QuestResult CancelQuest(GameSession session, string questId)
        {
            if (session == null || string.IsNullOrWhiteSpace(questId))
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, questId);
            }

            Expire(session);
            if (!session.State.TryGetActiveQuest(questId, out var quest) || quest == null)
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, questId);
            }

            quest.MarkCancelled(session.State.World.Clock.AbsoluteHours);
            return QuestResult.Ok(questId);
        }

        /// <inheritdoc />
        public QuestResult TurnIn(GameSession session, string boardLocationId, string questId)
        {
            if (!TryGetBoard(session, boardLocationId, out _))
            {
                return QuestResult.Error(QuestResultStatus.WrongLocation, questId);
            }

            Expire(session!);
            if (!session!.State.TryGetActiveQuest(questId, out var quest) || quest == null)
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, questId);
            }

            if (!string.Equals(quest.BoardLocationId, boardLocationId, StringComparison.Ordinal))
            {
                return QuestResult.Error(QuestResultStatus.WrongBoard, questId);
            }

            if (!_quests.TryGet(questId, out var template))
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, questId);
            }

            var progress = GetProgress(session, quest);
            if (!progress.Ready)
            {
                return QuestResult.Error(QuestResultStatus.NotReady, questId);
            }

            var player = session.State.Player!;
            if (!RemoveRequiredItems(player, template, quest))
            {
                return QuestResult.Error(QuestResultStatus.NotReady, questId);
            }

            if (template.Rewards.Money > 0)
            {
                player.AddMoney(template.Rewards.Money);
            }

            foreach (var unlock in template.Rewards.Unlocks)
            {
                player.AddUnlock(unlock);
            }

            // Предметы награды не кладутся в инвентарь: их забирает экран наград.
            var loot = QuestRewardLoot.Build("quest-reward:" + questId, template.Rewards, _items);

            var cooldownHours = CooldownHours(template);
            quest.MarkCompleted(session.State.World.Clock.AbsoluteHours, cooldownHours);
            player.CompleteQuest(questId);
            if (template.Type == QuestType.ClearLocation && quest.LocationInstanceId != null &&
                session.State.World.Map.TryGetLocation(quest.LocationInstanceId, out var location) &&
                location != null)
            {
                location.MarkCleared();
            }

            return QuestResult.Ok(questId, template.Rewards.Money, loot);
        }
    }
}
