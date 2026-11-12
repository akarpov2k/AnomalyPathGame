#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: квестовые цепочки локаций.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateChains(LoadedData data, List<ValidationIssue> issues)
        {
            QuestChainRepository chains;
            LocationRepository locations;
            SpawnRepository spawns;
            StoryQuestRepository quests;
            ItemRepository items;
            try
            {
                chains = QuestChainRepository.FromLoadedData(data);
                locations = LocationRepository.FromLoadedData(data);
                spawns = SpawnRepository.FromLoadedData(data);
                quests = StoryQuestRepository.FromLoadedData(data);
                items = ItemRepository.FromLoadedData(data);
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
                return;
            }

            foreach (var chain in chains.All)
            {
                foreach (var step in chain.Locations)
                {
                    if (!locations.Contains(step.LocationId))
                    {
                        AddChainIssue(issues, chain, CoreErrors.ChainLocationMissing, step.LocationId);
                    }

                    ValidateChainSpawn(issues, chain, spawns, step.EnemySpawnId, LocationObjectKind.Enemy);
                    ValidateChainSpawn(issues, chain, spawns, step.NpcSpawnId, LocationObjectKind.Npc);
                }

                if (chain.CompletionItemId != null && !items.TryGet(chain.CompletionItemId, out _))
                {
                    AddChainIssue(issues, chain, CoreErrors.QuestRewardItemMissing, chain.CompletionItemId);
                }
            }

            foreach (var quest in quests.All)
            {
                foreach (var objective in quest.Objectives)
                {
                    if (objective.Type != QuestObjectiveType.ClearChain || objective.ChainId == null)
                    {
                        continue;
                    }

                    if (!chains.TryGet(objective.ChainId, out var chain))
                    {
                        AddChainIssue(issues, chain!, CoreErrors.ChainNotFound, objective.ChainId);
                        continue;
                    }

                    if (chain.Locations.Count != objective.Count)
                    {
                        AddChainIssue(issues, chain, CoreErrors.ChainQuestCount, objective.ChainId);
                    }
                }
            }
        }

        private static void ValidateChainSpawn(
            List<ValidationIssue> issues,
            QuestChainTemplate chain,
            SpawnRepository spawns,
            string? spawnId,
            LocationObjectKind expected)
        {
            if (spawnId == null)
            {
                return;
            }

            if (!spawns.TryGet(spawnId, out var entry))
            {
                AddChainIssue(issues, chain, CoreErrors.ChainSpawnMissing, spawnId);
                return;
            }

            if (entry == null || entry.ObjectKind != expected)
            {
                AddChainIssue(issues, chain, CoreErrors.ChainSpawnKind, spawnId);
            }
        }

        private static void AddChainIssue(
            List<ValidationIssue> issues,
            QuestChainTemplate? chain,
            string code,
            params object[] args)
        {
            issues.Add(new ValidationIssue(
                chain?.Id ?? "chains",
                code,
                CoreText.Format(code, args)));
        }
    }
}
