#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Repositories;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: сюжетные квесты.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateStoryQuests(LoadedData data, List<ValidationIssue> issues)
        {
            StoryQuestRepository quests;
            ActorRepository actors;
            LocationRepository locations;
            ItemRepository items;
            QuestRepository regularQuests;
            try
            {
                quests = StoryQuestRepository.FromLoadedData(data);
                actors = ActorRepository.FromLoadedData(data);
                locations = LocationRepository.FromLoadedData(data);
                items = ItemRepository.FromLoadedData(data);
                regularQuests = QuestRepository.FromLoadedData(data);
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
                return;
            }

            var predecessors = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var quest in quests.All)
            {
                ValidateStoryCharacters(quest, actors, issues);
                ValidateStoryNext(quest, quests, predecessors, issues);
                ValidateStoryObjectives(quest, actors, locations, issues);
                ValidateStoryRewards(quest, items, issues);
                ValidateStoryVariants(quest, issues);
            }

            ValidateStoryCycles(quests, issues);
            ValidateStubLocations(locations, quests, regularQuests, issues);
        }

        /// <summary>
        /// Проверяет ссылки локаций-заглушек: квест, после которого локация закрывается,
        /// обязан существовать среди сюжетных или обычных квестов.
        /// </summary>
        /// <param name="locations">Шаблоны локаций.</param>
        /// <param name="storyQuests">Сюжетные квесты.</param>
        /// <param name="regularQuests">Обычные квесты.</param>
        /// <param name="issues">Список замечаний.</param>
        private static void ValidateStubLocations(
            LocationRepository locations,
            StoryQuestRepository storyQuests,
            QuestRepository regularQuests,
            List<ValidationIssue> issues)
        {
            foreach (var location in locations.All)
            {
                var quest = location.StubAfterQuest;
                if (quest == null || storyQuests.Contains(quest) || regularQuests.Contains(quest))
                {
                    continue;
                }

                issues.Add(new ValidationIssue(
                    location.Id,
                    CoreErrors.StubQuestMissing,
                    CoreText.Format(CoreErrors.StubQuestMissing, quest)));
            }
        }

        private static void ValidateStoryCharacters(
            StoryQuestTemplate quest,
            ActorRepository actors,
            List<ValidationIssue> issues)
        {
            if (!IsFriend(actors, quest.GiverActorId))
            {
                AddStoryIssue(
                    issues,
                    quest,
                    CoreErrors.StoryGiverNotNpc,
                    quest.GiverActorId);
            }

            if (quest.TurnInActorId != null && !IsFriend(actors, quest.TurnInActorId))
            {
                AddStoryIssue(
                    issues,
                    quest,
                    CoreErrors.StoryTurnInNotNpc,
                    quest.TurnInActorId);
            }
        }

        private static bool IsFriend(ActorRepository actors, string actorId) =>
            actors.Contains(actorId) && actors.Get(actorId).Type != ActorType.Enemy;

        private static void ValidateStoryNext(
            StoryQuestTemplate quest,
            StoryQuestRepository quests,
            Dictionary<string, string> predecessors,
            List<ValidationIssue> issues)
        {
            if (quest.NextQuestId == null)
            {
                return;
            }

            if (!quests.TryGet(quest.NextQuestId, out var next))
            {
                AddStoryIssue(
                    issues,
                    quest,
                    CoreErrors.StoryNextQuestMissing,
                    quest.NextQuestId);
                return;
            }

            if (!string.Equals(next.StoryLine, quest.StoryLine, StringComparison.Ordinal))            {
                AddStoryIssue(
                    issues,
                    quest,
                    CoreErrors.StoryNextQuestLine,
                    quest.NextQuestId);
            }

            if (predecessors.TryGetValue(quest.NextQuestId, out var first))
            {
                AddStoryIssue(
                    issues,
                    quest,
                    CoreErrors.StoryNextQuestDuplicate,
                    first,
                    quest.NextQuestId);
            }
            else
            {
                predecessors[quest.NextQuestId] = quest.Id;
            }
        }

        private static void ValidateStoryObjectives(
            StoryQuestTemplate quest,
            ActorRepository actors,
            LocationRepository locations,
            List<ValidationIssue> issues)
        {
            foreach (var objective in quest.Objectives)
            {
                ValidateStoryObjective(quest, objective, actors, locations, issues);
            }
        }

        private static void ValidateStoryObjective(
            StoryQuestTemplate quest,
            QuestObjective objective,
            ActorRepository actors,
            LocationRepository locations,
            List<ValidationIssue> issues)
        {
            switch (objective.Type)
            {
                case QuestObjectiveType.KillActors:
                    foreach (var actorId in objective.ActorPool)
                    {
                        if (!actors.Contains(actorId) || actors.Get(actorId).Type != ActorType.Enemy)
                        {
                            AddStoryIssue(issues, quest, CoreErrors.StoryObjectiveActor, actorId);
                        }
                    }

                    break;

                case QuestObjectiveType.TalkToActor:
                    if (objective.ActorId == null || !IsFriend(actors, objective.ActorId))
                    {
                        AddStoryIssue(
                            issues,
                            quest,
                            CoreErrors.StoryObjectiveActor,
                            objective.ActorId ?? string.Empty);
                    }

                    break;

                case QuestObjectiveType.ReachLocation:
                    if (objective.LocationId == null || !locations.Contains(objective.LocationId))
                    {
                        AddStoryIssue(
                            issues,
                            quest,
                            CoreErrors.StoryObjectiveLocation,
                            objective.LocationId ?? string.Empty);
                    }

                    break;
            }
        }

        private static void ValidateStoryRewards(
            StoryQuestTemplate quest,
            ItemRepository items,
            List<ValidationIssue> issues)
        {
            ValidateRewardItems(quest, quest.Rewards, items, issues);
            foreach (var variant in quest.Variants)
            {
                ValidateRewardItems(quest, variant.Rewards, items, issues);
            }

            foreach (var item in quest.TurnInItems)
            {
                if (!items.TryGet(item.ItemId, out _))
                {
                    issues.Add(new ValidationIssue(
                        quest.Id,
                        CoreErrors.QuestRewardItemMissing,
                        CoreText.Format(CoreErrors.QuestRewardItemMissing, item.ItemId)));
                }
            }
        }

        private static void ValidateRewardItems(
            StoryQuestTemplate quest,
            QuestRewardTemplate rewards,
            ItemRepository items,
            List<ValidationIssue> issues)
        {
            foreach (var reward in rewards.Items)
            {
                if (!items.TryGet(reward.ItemId, out _))
                {
                    issues.Add(new ValidationIssue(
                        quest.Id,
                        CoreErrors.QuestRewardItemMissing,
                        CoreText.Format(CoreErrors.QuestRewardItemMissing, reward.ItemId)));
                }
            }
        }

        private static void ValidateStoryVariants(
            StoryQuestTemplate quest,
            List<ValidationIssue> issues)
        {
            foreach (var variant in quest.Variants)
            {
                if (!string.Equals(
                        variant.Id,
                        StoryQuestTemplate.HardVariantId,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                if (variant.Objectives.Count != quest.Objectives.Count)
                {
                    AddStoryIssue(issues, quest, CoreErrors.StoryHardObjectives);
                    continue;
                }

                for (var index = 0; index < variant.Objectives.Count; index++)
                {
                    var hard = variant.Objectives[index];
                    var standard = quest.Objectives[index];
                    if (hard.Type != standard.Type || hard.Count < standard.Count)
                    {
                        AddStoryIssue(issues, quest, CoreErrors.StoryHardObjectives);
                        break;
                    }
                }

                if (variant.Rewards.Money > quest.Rewards.Money ||
                    TotalItems(variant.Rewards) > TotalItems(quest.Rewards))
                {
                    AddStoryIssue(issues, quest, CoreErrors.StoryHardReward);
                }
            }
        }

        private static int TotalItems(QuestRewardTemplate rewards)
        {
            var total = 0;
            foreach (var item in rewards.Items)
            {
                total += item.Count;
            }

            return total;
        }

        private static void ValidateStoryCycles(
            StoryQuestRepository quests,
            List<ValidationIssue> issues)
        {
            foreach (var quest in quests.All)
            {
                var visited = new HashSet<string>(StringComparer.Ordinal) { quest.Id };
                var current = quest.NextQuestId;
                while (current != null)
                {
                    if (!visited.Add(current))
                    {
                        AddStoryIssue(issues, quest, CoreErrors.StoryNextQuestCycle);
                        break;
                    }

                    current = quests.TryGet(current, out var next) ? next.NextQuestId : null;
                }
            }
        }

        private static void AddStoryIssue(
            List<ValidationIssue> issues,
            StoryQuestTemplate quest,
            string code,
            params object[] args)
        {
            var values = new object[args.Length + 1];
            values[0] = quest.Id;
            Array.Copy(args, 0, values, 1, args.Length);
            issues.Add(new ValidationIssue(quest.Id, code, CoreText.Format(code, values)));
        }
    }
}
