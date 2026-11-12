#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Talents;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: квесты и таланты.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateQuests(LoadedData data, List<ValidationIssue> issues)
        {
            QuestRepository quests;
            ActorRepository actors;
            LocationRepository locations;
            ItemRepository items;
            try
            {
                quests = QuestRepository.FromLoadedData(data);
                actors = ActorRepository.FromLoadedData(data);
                locations = LocationRepository.FromLoadedData(data);
                items = ItemRepository.FromLoadedData(data);
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
                return;
            }

            foreach (var quest in quests.All)
            {
                if (quest.QuestKind != QuestKind.Repeatable)
                {
                    continue;
                }

                switch (quest.Type)
                {
                    case QuestType.ClearLocation:
                        ValidateClearQuest(quest, locations, issues);
                        break;
                    case QuestType.KillMutants:
                        ValidateKillQuest(quest, actors, issues);
                        break;
                    case QuestType.GatherArtifacts:
                        ValidateGatherQuest(quest, items, issues);
                        break;
                    case QuestType.FindItems:
                        ValidateFindQuest(quest, actors, items, issues);
                        break;
                }

                foreach (var reward in quest.Rewards.Items)
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

            ValidateQuestBoards(quests, locations, issues);
        }

        private static void ValidateQuestBoards(
            QuestRepository quests,
            LocationRepository locations,
            List<ValidationIssue> issues)
        {
            foreach (var location in locations.All)
            {
                var board = location.QuestBoard;
                if (board == null)
                {
                    continue;
                }

                foreach (var questId in board.QuestPool)
                {
                    if (!quests.TryGet(questId, out var template) ||
                        template.QuestKind != QuestKind.Repeatable)
                    {
                        issues.Add(new ValidationIssue(
                            location.Id,
                            CoreErrors.QuestBoardQuestMissing,
                            CoreText.Format(CoreErrors.QuestBoardQuestMissing, questId)));
                    }
                }

                var checkedDifficulties = new List<QuestDifficulty>();
                foreach (var slot in board.Slots)
                {
                    if (checkedDifficulties.Contains(slot.Difficulty))
                    {
                        continue;
                    }

                    checkedDifficulties.Add(slot.Difficulty);
                    var found = false;
                    foreach (var questId in board.QuestPool)
                    {
                        if (quests.TryGet(questId, out var template) &&
                            template.QuestKind == QuestKind.Repeatable &&
                            template.Difficulty == slot.Difficulty)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        issues.Add(new ValidationIssue(
                            location.Id,
                            CoreErrors.QuestBoardNoDifficulty,
                            CoreText.Format(
                                CoreErrors.QuestBoardNoDifficulty,
                                SchemaEnum.ToCode(slot.Difficulty))));
                    }
                }
            }
        }

        private static void ValidateTalents(LoadedData data, List<ValidationIssue> issues)
        {
            var records = data.All("talents");
            if (records.Count == 0)
            {
                issues.Add(new ValidationIssue("talents", CoreErrors.FileNotFound, CoreText.Get(CoreErrors.FileNotFound)));
                return;
            }

            if (records.Count > 1)
            {
                issues.Add(new ValidationIssue(
                    "talents",
                    CoreErrors.TalentTreeMultiple,
                    CoreText.Format(CoreErrors.TalentTreeMultiple, records.Count)));
            }

            Try(() => TalentTreeTemplate.FromJson(records[0], "talents"), issues);
        }
    }
}
