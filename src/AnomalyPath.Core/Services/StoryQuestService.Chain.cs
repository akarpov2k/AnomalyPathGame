#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="StoryQuestService"/>: цепочки локаций и области прогресса.
    /// Выход из хаба начинает рейд, смерть сбрасывает «за жизнь» и рейд; полная зачистка
    /// цепочки выдаёт системный предмет.
    /// </summary>
    public sealed partial class StoryQuestService
    {
        /// <inheritdoc />
        public void OnLeaveHub(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            session.State.Raid.Begin();
            ResetScopes(session, QuestProgressScope.Raid);
        }

        /// <inheritdoc />
        public IReadOnlyList<string> GetActiveChains(GameSession session)
        {
            var result = new List<string>();
            var player = session?.State.Player;
            if (player == null)
            {
                return result;
            }

            foreach (var state in player.StoryQuests)
            {
                if (state.Status != QuestStatus.Active ||
                    !_quests.TryGet(state.QuestId, out var template) ||
                    !template.TryGetVariant(state.VariantId, out var variant))
                {
                    continue;
                }

                foreach (var objective in variant.Objectives)
                {
                    if (objective.Type != QuestObjectiveType.ClearChain ||
                        string.IsNullOrWhiteSpace(objective.ChainId) ||
                        result.Contains(objective.ChainId!))
                    {
                        continue;
                    }

                    result.Add(objective.ChainId!);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public void OnEnterHub(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            // Прогресс рейда не сбрасывается: квесты области Raid сдаются в хабе, и счёт
            // должен пережить возврат. Новый рейд начинается при выходе из хаба.
            session.State.Raid.Reset();
        }

        /// <inheritdoc />
        public void OnDeath(GameSession session)
        {
            if (session == null)
            {
                return;
            }

            session.State.Raid.Reset();
            ResetScopes(session, QuestProgressScope.Raid, QuestProgressScope.Life);
        }

        private void ResetScopes(GameSession session, params QuestProgressScope[] scopes)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return;
            }

            foreach (var quest in player.StoryQuests)
            {
                if (quest.Status != QuestStatus.Active)
                {
                    continue;
                }

                foreach (var scope in scopes)
                {
                    if (quest.Scope == scope)
                    {
                        quest.ResetProgress();
                        break;
                    }
                }
            }
        }

        private void GrantCompletionItem(PlayerState player, string itemId)
        {
            if (!_items.TryGet(itemId, out var template))
            {
                return;
            }

            // Предмет за полную зачистку цепочки — системная награда: он не теряется из-за
            // перегруза, а ложится в сетку или в переполнение. Новые действия игрока после
            // этого снова упираются в лимит веса.
            var limit = player.Inventory.WeightLimit;
            try
            {
                player.Inventory.SetWeightLimit(double.PositiveInfinity);
                _inventory.TryAdd(
                    player.Inventory,
                    itemId,
                    1,
                    ItemInstances.NewDurability(template),
                    null,
                    out _);
            }
            finally
            {
                player.Inventory.SetWeightLimit(limit);
            }
        }
    }
}
