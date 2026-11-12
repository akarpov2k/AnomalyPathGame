#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Сюжетные квесты: выдача и сдача по диалогу, прогресс целей, награды и сброс
    /// областей прогресса при возврате в хаб и смерти. Все числа — из данных.
    /// </summary>
    public sealed partial class StoryQuestService : IStoryQuestService
    {
        private readonly IStoryQuestRepository _quests;
        private readonly IItemRepository _items;
        private readonly IInventoryService _inventory;
        private readonly IQuestChainRepository? _chains;
        private readonly IStoryWorldService? _world;

        /// <summary>Создаёт сервис сюжетных квестов.</summary>
        /// <param name="quests">Шаблоны сюжетных квестов.</param>
        /// <param name="items">Шаблоны предметов: награды и прочность.</param>
        /// <param name="inventory">Инвентарь: проверка места и выдача награды.</param>
        /// <param name="chains">Цепочки локаций: прогресс цели зачистки; <c>null</c> — без цепочек.</param>
        /// <param name="world">Квестовые локации: создание цепочки при взятии; <c>null</c> — не создавать.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public StoryQuestService(
            IStoryQuestRepository quests,
            IItemRepository items,
            IInventoryService inventory,
            IQuestChainRepository? chains = null,
            IStoryWorldService? world = null)
        {
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _chains = chains;
            _world = world;
        }

        /// <inheritdoc />
        public IReadOnlyList<StoryQuestTemplate> GetAvailable(GameSession session, string actorId)
        {
            var result = new List<StoryQuestTemplate>();
            var player = session?.State.Player;
            if (session == null || player == null || string.IsNullOrWhiteSpace(actorId))
            {
                return result;
            }

            foreach (var template in _quests.All)
            {
                if (!string.Equals(template.GiverActorId, actorId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (IsAvailable(session, template.Id, actorId))
                {
                    result.Add(template);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public bool IsAvailable(GameSession session, string questId, string actorId)
        {
            var player = session?.State.Player;
            if (player == null || string.IsNullOrWhiteSpace(actorId))
            {
                return false;
            }

            if (!_quests.TryGet(questId, out var template) ||
                !string.Equals(template.GiverActorId, actorId, StringComparison.Ordinal))
            {
                return false;
            }

            if (player.HasCompletedQuest(questId) || player.TryGetStoryQuest(questId, out _))
            {
                return false;
            }

            return !IsLineBusy(player, template.StoryLine) && template.Requirements.IsMet(player);
        }

        /// <inheritdoc />
        public IReadOnlyList<StoryQuestState> GetActive(GameSession session)
        {
            var result = new List<StoryQuestState>();
            var player = session?.State.Player;
            if (player == null)
            {
                return result;
            }

            foreach (var quest in player.StoryQuests)
            {
                if (quest.Status == QuestStatus.Active)
                {
                    result.Add(quest);
                }
            }

            return result;
        }

        /// <inheritdoc />
        public bool TryGetTemplate(string questId, out StoryQuestTemplate template) =>
            _quests.TryGet(questId, out template!);

        /// <inheritdoc />
        public bool TryGetActive(GameSession session, string questId, out StoryQuestState quest)
        {
            quest = null!;
            var player = session?.State.Player;
            if (player != null &&
                player.TryGetStoryQuest(questId, out var state) &&
                state != null &&
                state.Status == QuestStatus.Active)
            {
                quest = state;
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public bool IsReady(GameSession session, StoryQuestState quest)
        {
            if (quest == null || !_quests.TryGet(quest.QuestId, out var template) ||
                !template.TryGetVariant(quest.VariantId, out var variant))
            {
                return false;
            }

            return quest.IsReady(variant.Objectives);
        }

        private static PlayerState? RequirePlayer(GameSession session) => session?.State.Player;

        private static bool IsLineBusy(PlayerState player, string storyLine)
        {
            foreach (var quest in player.StoryQuests)
            {
                if (quest.Status == QuestStatus.Active &&
                    string.Equals(quest.StoryLine, storyLine, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Обходит цели активных квестов: сам решает, какие квесты и цели брать в работу.
        /// Шаблон для всех <c>Notify*</c>: конкретное событие задаёт только тип цели,
        /// условие совпадения и ключ прогресса.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="type">Тип цели, которую продвигает событие.</param>
        /// <param name="match">Условие совпадения цели с событием.</param>
        /// <param name="advance">Что сделать с прогрессом цели.</param>
        private void ForEachObjective(
            GameSession? session,
            QuestObjectiveType type,
            Func<QuestObjective, bool> match,
            Action<StoryQuestState, int, QuestObjective> advance)
        {
            ForEachActiveQuest(session, (state, objectives) =>
            {
                for (var index = 0; index < objectives.Count; index++)
                {
                    var objective = objectives[index];
                    if (objective.Type != type || !match(objective))
                    {
                        continue;
                    }

                    advance(state, index, objective);
                }
            });
        }

        private void ForEachActiveQuest(
            GameSession? session,
            Action<StoryQuestState, IReadOnlyList<QuestObjective>> action)
        {
            var player = session?.State.Player;
            if (player == null)
            {
                return;
            }

            foreach (var state in player.StoryQuests)
            {
                if (state.Status != QuestStatus.Active ||
                    !_quests.TryGet(state.QuestId, out var template) ||
                    !template.TryGetVariant(state.VariantId, out var variant) ||
                    variant.Objectives.Count != state.Progress.Count)
                {
                    continue;
                }

                action(state, variant.Objectives);
            }
        }
    }
}
