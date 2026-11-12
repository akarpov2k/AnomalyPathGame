#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="StoryQuestService"/>: прогресс целей по событиям мира —
    /// убийство, зачистка, прибытие, тайник, осмотр и разговор.
    /// </summary>
    public sealed partial class StoryQuestService
    {
        /// <inheritdoc />
        public void NotifyActorKilled(GameSession session, string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return;
            }

            // Каждое убийство — отдельное событие: два одинаковых противника
            // засчитываются двумя, поэтому дедупликации по актёру здесь нет.
            ForEachObjective(
                session,
                QuestObjectiveType.KillActors,
                objective => objective.ActorPool.Count == 0 || Contains(objective.ActorPool, actorId),
                (state, index, objective) => state.ObjectiveAt(index).TryAdvance(null, objective.Count));
        }

        /// <inheritdoc />
        public void NotifyLocationCleared(
            GameSession session,
            string instanceId,
            string templateId,
            LocationSubtype subtype)
        {
            if (string.IsNullOrWhiteSpace(instanceId) ||
                subtype == LocationSubtype.Hub ||
                subtype == LocationSubtype.Path)
            {
                return;
            }

            session?.State.Raid.MarkCleared(instanceId);
            ForEachActiveQuest(session, (state, objectives) =>
            {
                for (var index = 0; index < objectives.Count; index++)
                {
                    var objective = objectives[index];
                    if (objective.Type == QuestObjectiveType.ClearLocations &&
                        !ContainsSubtype(objective.ExcludeSubtypes, subtype))
                    {
                        state.ObjectiveAt(index).TryAdvance(instanceId, objective.Count);
                        continue;
                    }

                    if (objective.Type == QuestObjectiveType.ClearChain &&
                        objective.ChainId != null &&
                        _chains != null &&
                        _chains.TryGet(objective.ChainId, out var chain) &&
                        chain.FindStep(templateId) != null)
                    {
                        var progress = state.ObjectiveAt(index);
                        var advanced = progress.TryAdvance(instanceId, objective.Count);
                        if (advanced &&
                            progress.IsDone(objective.Count) &&
                            chain.CompletionItemId != null &&
                            session?.State.Player != null)
                        {
                            GrantCompletionItem(session.State.Player, chain.CompletionItemId);
                        }
                    }
                }
            });
        }

        /// <inheritdoc />
        public void NotifyLocationReached(GameSession session, string instanceId, string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return;
            }

            ForEachObjective(
                session,
                QuestObjectiveType.ReachLocation,
                objective => string.Equals(objective.LocationId, templateId, StringComparison.Ordinal),
                (state, index, objective) => state.ObjectiveAt(index).TryAdvance(instanceId, objective.Count));
        }

        /// <inheritdoc />
        public void NotifyStashOpened(GameSession session, string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return;
            }

            ForEachObjective(
                session,
                QuestObjectiveType.OpenStashes,
                _ => true,
                (state, index, objective) => state.ObjectiveAt(index).TryAdvance(instanceId, objective.Count));
        }

        /// <inheritdoc />
        public void NotifyObjectInspected(
            GameSession session,
            string instanceId,
            LocationObjectKind kind,
            string objectKey)
        {
            if (string.IsNullOrWhiteSpace(instanceId) || string.IsNullOrWhiteSpace(objectKey))
            {
                return;
            }

            // Осмотр считается по конкретному объекту на конкретной локации:
            // два одинаковых ящика в разных местах — два осмотра.
            ForEachObjective(
                session,
                QuestObjectiveType.InspectObjects,
                objective => objective.ObjectKind == kind,
                (state, index, objective) =>
                    state.ObjectiveAt(index).TryAdvance(instanceId + ":" + objectKey, objective.Count));
        }

        /// <inheritdoc />
        public void NotifyTalkedToActor(GameSession session, string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId))
            {
                return;
            }

            ForEachObjective(
                session,
                QuestObjectiveType.TalkToActor,
                objective => string.Equals(objective.ActorId, actorId, StringComparison.Ordinal),
                (state, index, objective) => state.ObjectiveAt(index).TryAdvance(actorId, objective.Count));
        }

        private static bool Contains(IReadOnlyList<string> values, string value)
        {
            foreach (var item in values)
            {
                if (string.Equals(item, value, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsSubtype(
            IReadOnlyList<LocationSubtype> values,
            LocationSubtype value)
        {
            foreach (var item in values)
            {
                if (item == value)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
