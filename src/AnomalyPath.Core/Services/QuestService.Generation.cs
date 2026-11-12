#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="QuestService"/>: генерация предложений на доске.</summary>
    public sealed partial class QuestService
    {
        private QuestOffer? GenerateOffer(
            GameSession session,
            QuestBoardTemplate board,
            string boardLocationId,
            int slotIndex,
            PlayerState player,
            double now)
        {
            var difficulty = board.Slots[slotIndex].Difficulty;
            var candidates = new List<QuestTemplate>();
            foreach (var questId in board.QuestPool)
            {
                if (!_quests.TryGet(questId, out var template))
                {
                    continue;
                }

                if (template.QuestKind != QuestKind.Repeatable ||
                    template.Difficulty != difficulty ||
                    !template.Requirements.IsMet(player) ||
                    IsActive(session, template.Id) ||
                    IsOnCooldown(session, template.Id, now) ||
                    IsOffered(session, template.Id))
                {
                    continue;
                }

                candidates.Add(template);
            }

            var serial = session.State.NextQuestSerial();
            var random = new SeededWorldRandom(StableHash.Combine(
                session.State.World.Map.Seed,
                boardLocationId + "#" + slotIndex + "#" + serial));
            while (candidates.Count > 0)
            {
                var index = random.NextInt(0, candidates.Count);
                var template = candidates[index];
                candidates.RemoveAt(index);
                var quest = new QuestState(
                    template.Id,
                    boardLocationId,
                    QuestStatus.Offered,
                    0,
                    0,
                    0,
                    requiredCount: RequiredCount(template),
                    slot: slotIndex);
                if (BuildOfferTarget(template, quest, random))
                {
                    session.State.AddQuest(quest);
                    return new QuestOffer(template, quest);
                }
            }

            return null;
        }

        private bool BuildOfferTarget(
            QuestTemplate template,
            QuestState quest,
            IWorldRandom random)
        {
            switch (template.Type)
            {
                case QuestType.ClearLocation:
                {
                    var target = template.ClearLocation;
                    if (target == null)
                    {
                        return false;
                    }

                    var chosen = SelectLocation(target, random);
                    if (chosen == null)
                    {
                        return false;
                    }

                    quest.LocationTemplateId = chosen.Id;
                    return true;
                }

                case QuestType.KillMutants:
                {
                    var target = template.KillMutants;
                    if (target == null)
                    {
                        return false;
                    }

                    var candidates = ActorCandidates(target.ActorPool, ActorRoles.Mutant, target.MinTier, target.MaxTier);
                    if (candidates.Count == 0)
                    {
                        return false;
                    }

                    var picked = PickDistinct(candidates, target.PoolSize, random);
                    foreach (var actorId in picked)
                    {
                        quest.AddActor(actorId);
                    }

                    return picked.Count > 0;
                }

                case QuestType.GatherArtifacts:
                {
                    var target = template.GatherArtifacts;
                    if (target == null)
                    {
                        return false;
                    }

                    var candidates = ItemCandidates(target.ItemPool, ItemCategory.Artifact, target.MinTier, target.MaxTier);
                    if (candidates.Count == 0)
                    {
                        return false;
                    }

                    var picked = PickDistinct(candidates, target.PoolSize, random);
                    foreach (var itemId in picked)
                    {
                        quest.AddItem(new QuestItemTarget(itemId, 1));
                    }

                    return picked.Count > 0;
                }

                case QuestType.FindItems:
                {
                    var target = template.FindItems;
                    if (target == null)
                    {
                        return false;
                    }

                    var itemCandidates = FindItemCandidates(target);
                    if (itemCandidates.Count == 0)
                    {
                        return false;
                    }

                    var pickedItems = PickDistinct(itemCandidates, target.ItemKinds, random);
                    foreach (var itemId in pickedItems)
                    {
                        quest.AddItem(new QuestItemTarget(itemId, target.ItemCount));
                    }

                    var npcCandidates = ActorCandidates(target.NpcPool, ActorRoles.Human, target.MinTier, target.MaxTier);
                    if (npcCandidates.Count == 0)
                    {
                        return false;
                    }

                    var pickedNpcs = PickDistinct(npcCandidates, target.PoolSize, random);
                    foreach (var actorId in pickedNpcs)
                    {
                        quest.AddActor(actorId);
                    }

                    return pickedItems.Count > 0 && pickedNpcs.Count > 0;
                }

                default:
                    return false;
            }
        }
    }
}
