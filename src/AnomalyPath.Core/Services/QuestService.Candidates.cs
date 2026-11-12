#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="QuestService"/>: кандидаты цели.</summary>
    public sealed partial class QuestService
    {
        private LocationTemplate? SelectLocation(ClearLocationTarget target, IWorldRandom random)
        {
            var candidates = new List<LocationTemplate>();
            foreach (var template in _locations.All)
            {
                if (template.LocationKind != LocationKind.Wild ||
                    template.Subtype == LocationSubtype.Path ||
                    template.Subtype == LocationSubtype.Hub ||
                    template.Unique)
                {
                    continue;
                }

                if (template.Tier < target.MinTier || template.Tier > target.MaxTier)
                {
                    continue;
                }

                if (target.LocationPool.Count > 0 && !Contains(target.LocationPool, template.Id))
                {
                    continue;
                }

                if (template.SpawnChance <= 0)
                {
                    continue;
                }

                candidates.Add(template);
            }

            if (candidates.Count == 0)
            {
                return null;
            }

            var index = WeightedPick.ChooseIndex(
                candidates.Count,
                position => candidates[position].SpawnChance,
                random);
            return index >= 0 ? candidates[index] : candidates[random.NextInt(0, candidates.Count)];
        }

        private List<string> ActorCandidates(
            IReadOnlyList<string> pool,
            string role,
            int minTier,
            int maxTier)
        {
            var result = new List<string>();
            if (pool.Count > 0)
            {
                foreach (var actorId in pool)
                {
                    if (!_actors.Contains(actorId))
                    {
                        continue;
                    }

                    var actor = _actors.Get(actorId);
                    if (actor.Type != ActorType.Enemy || !actor.HasRole(role) || actor.Combat == null)
                    {
                        continue;
                    }

                    if (actor.Combat.Tier < minTier || actor.Combat.Tier > maxTier)
                    {
                        continue;
                    }

                    if (!Contains(result, actorId))
                    {
                        result.Add(actorId);
                    }
                }

                return result;
            }

            foreach (var actor in _actors.All)
            {
                if (actor.Type != ActorType.Enemy || !actor.HasRole(role) || actor.Combat == null)
                {
                    continue;
                }

                if (actor.Combat.Tier < minTier || actor.Combat.Tier > maxTier)
                {
                    continue;
                }

                result.Add(actor.Id);
            }

            return result;
        }

        private List<string> ItemCandidates(
            IReadOnlyList<string> pool,
            ItemCategory category,
            int minTier,
            int maxTier)
        {
            var result = new List<string>();
            if (pool.Count > 0)
            {
                foreach (var itemId in pool)
                {
                    if (!_items.TryGet(itemId, out var template) || template.Category != category)
                    {
                        continue;
                    }

                    if (template.Tier < minTier || template.Tier > maxTier)
                    {
                        continue;
                    }

                    if (!Contains(result, itemId))
                    {
                        result.Add(itemId);
                    }
                }

                return result;
            }

            foreach (var item in _items.All)
            {
                if (item.Category != category || item.Tier < minTier || item.Tier > maxTier)
                {
                    continue;
                }

                result.Add(item.Id);
            }

            return result;
        }

        private List<string> FindItemCandidates(FindItemsTarget target)
        {
            var result = new List<string>();
            if (target.ItemPool.Count > 0)
            {
                foreach (var itemId in target.ItemPool)
                {
                    if (!_items.TryGet(itemId, out var template))
                    {
                        continue;
                    }

                    if (!ContainsCategory(target.Categories, template.Category))
                    {
                        continue;
                    }

                    if (template.Tier < target.MinTier || template.Tier > target.MaxTier)
                    {
                        continue;
                    }

                    if (!Contains(result, itemId))
                    {
                        result.Add(itemId);
                    }
                }

                return result;
            }

            foreach (var item in _items.All)
            {
                if (!ContainsCategory(target.Categories, item.Category) ||
                    item.Tier < target.MinTier || item.Tier > target.MaxTier)
                {
                    continue;
                }

                result.Add(item.Id);
            }

            return result;
        }
    }
}
