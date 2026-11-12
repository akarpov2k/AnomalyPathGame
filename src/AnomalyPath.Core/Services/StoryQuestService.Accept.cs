#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="StoryQuestService"/>: взятие сюжетного квеста у актёра.</summary>
    public sealed partial class StoryQuestService
    {
        /// <inheritdoc />
        public QuestResult Accept(GameSession session, string questId, string? variantId = null)
        {
            var player = RequirePlayer(session);
            if (player == null)
            {
                return QuestResult.Error(QuestResultStatus.Invalid, questId);
            }

            if (!_quests.TryGet(questId, out var template))
            {
                return QuestResult.Error(QuestResultStatus.QuestNotFound, questId);
            }

            if (player.HasCompletedQuest(questId) ||
                (player.TryGetStoryQuest(questId, out var existing) &&
                 existing != null && existing.Status == QuestStatus.Completed))
            {
                return QuestResult.Error(QuestResultStatus.AlreadyCompleted, questId);
            }

            if (player.TryGetStoryQuest(questId, out _))
            {
                return QuestResult.Error(QuestResultStatus.AlreadyActive, questId);
            }

            if (IsLineBusy(player, template.StoryLine))
            {
                return QuestResult.Error(QuestResultStatus.LineBusy, questId);
            }

            if (!template.Requirements.IsMet(player))
            {
                return QuestResult.Error(QuestResultStatus.RequirementsNotMet, questId);
            }

            var code = string.IsNullOrWhiteSpace(variantId)
                ? StoryQuestTemplate.StandardVariantId
                : variantId!;
            if (!template.TryGetVariant(code, out var variant))
            {
                return QuestResult.Error(QuestResultStatus.VariantNotFound, questId);
            }

            var progress = new List<QuestObjectiveProgress>();
            foreach (var objective in variant.Objectives)
            {
                progress.Add(new QuestObjectiveProgress(objective.Type));
            }

            var now = session!.State.World.Clock.AbsoluteHours;
            player.AddStoryQuest(new StoryQuestState(
                template.Id,
                template.StoryLine,
                variant.Id,
                template.ProgressScope,
                progress,
                QuestStatus.Active,
                now));
            foreach (var objective in variant.Objectives)
            {
                if (objective.Type == QuestObjectiveType.ClearChain &&
                    !string.IsNullOrWhiteSpace(objective.ChainId))
                {
                    _world?.EnsureChain(session!, objective.ChainId!);
                }

                if (objective.Type == QuestObjectiveType.ReachLocation &&
                    !string.IsNullOrWhiteSpace(objective.LocationId))
                {
                    _world?.EnsureLocation(session!, objective.LocationId!);
                }
            }

            return QuestResult.Ok(questId);
        }
    }
}
