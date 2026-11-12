#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="StoryQuestService"/>: сдача квеста — проверка предметов,
    /// награда, снятие цепочек с карты и завершение состояния.
    /// </summary>
    public sealed partial class StoryQuestService
    {
        /// <inheritdoc />
        public QuestResult TurnIn(GameSession session, string questId)
        {
            var player = RequirePlayer(session);
            if (player == null)
            {
                return QuestResult.Error(QuestResultStatus.Invalid, questId);
            }

            if (!player.TryGetStoryQuest(questId, out var state) || state == null)
            {
                return QuestResult.Error(
                    player.HasCompletedQuest(questId)
                        ? QuestResultStatus.AlreadyCompleted
                        : QuestResultStatus.NotAvailable,
                    questId);
            }

            if (state.Status == QuestStatus.Completed)
            {
                return QuestResult.Error(QuestResultStatus.AlreadyCompleted, questId);
            }

            if (!_quests.TryGet(questId, out var template) ||
                !template.TryGetVariant(state.VariantId, out var variant))
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, questId);
            }

            if (!state.IsReady(variant.Objectives))
            {
                return QuestResult.Error(QuestResultStatus.NotReady, questId);
            }

            if (!HasTurnInItems(player, template.TurnInItems))
            {
                return QuestResult.Error(QuestResultStatus.NotReady, questId);
            }

            RemoveTurnInItems(player, template.TurnInItems);
            RemoveQuestChains(session!, template, variant);

            // Предметы награды не кладутся в инвентарь: их забирает экран наград.
            var loot = QuestRewardLoot.Build("story-reward:" + questId, variant.Rewards, _items);
            Grant(player, variant.Rewards);
            state.MarkCompleted(session!.State.World.Clock.AbsoluteHours);
            player.CompleteQuest(questId);
            return QuestResult.Ok(questId, variant.Rewards.Money, loot);
        }

        /// <summary>Снимает с карты цепочки сданного квеста: квест пройден, локации не нужны.</summary>
        private void RemoveQuestChains(
            GameSession session,
            StoryQuestTemplate template,
            StoryQuestVariant variant)
        {
            if (_world == null)
            {
                return;
            }

            foreach (var objective in variant.Objectives)
            {
                if (objective.Type == QuestObjectiveType.ClearChain &&
                    !string.IsNullOrWhiteSpace(objective.ChainId))
                {
                    _world.RemoveChain(session, objective.ChainId!);
                }
            }
        }

        private static bool HasTurnInItems(
            PlayerState player,
            IReadOnlyList<QuestItemTarget> items)
        {
            foreach (var item in items)
            {
                if (RequirementsTemplate.CountItem(player, item.ItemId) < item.Count)
                {
                    return false;
                }
            }

            return true;
        }

        private static void RemoveTurnInItems(
            PlayerState player,
            IReadOnlyList<QuestItemTarget> items)
        {
            foreach (var target in items)
            {
                var remaining = target.Count;
                var stacks = player.Inventory.Items;
                for (var index = stacks.Count - 1; index >= 0 && remaining > 0; index--)
                {
                    var item = stacks[index];
                    if (!string.Equals(item.ItemId, target.ItemId, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var take = Math.Min(item.Stack, remaining);
                    item.Stack -= take;
                    remaining -= take;
                    if (item.Stack <= 0)
                    {
                        player.Inventory.Remove(item);
                    }
                }
            }
        }

        private void Grant(PlayerState player, QuestRewardTemplate rewards)
        {
            if (rewards.Money > 0)
            {
                player.AddMoney(rewards.Money);
            }

            foreach (var unlock in rewards.Unlocks)
            {
                player.AddUnlock(unlock);
            }
        }
    }
}
