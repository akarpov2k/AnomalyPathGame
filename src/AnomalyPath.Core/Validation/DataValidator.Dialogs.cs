#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: диалоги.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateDialogs(LoadedData data, List<ValidationIssue> issues)
        {
            DialogRepository dialogs;
            StoryQuestRepository quests;
            QuestRepository repeatable;
            ActorRepository actors;
            ItemRepository items;
            try
            {
                dialogs = DialogRepository.FromLoadedData(data);
                quests = StoryQuestRepository.FromLoadedData(data);
                repeatable = QuestRepository.FromLoadedData(data);
                actors = ActorRepository.FromLoadedData(data);
                items = ItemRepository.FromLoadedData(data);
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
                return;
            }

            foreach (var dialog in dialogs.All)
            {
                ValidateDialogNodes(dialog, quests, repeatable, actors, items, issues);
            }

            ValidateActorDialogs(actors, dialogs, issues);
        }

        private static void ValidateDialogNodes(
            DialogTemplate dialog,
            StoryQuestRepository quests,
            QuestRepository repeatable,
            ActorRepository actors,
            ItemRepository items,
            List<ValidationIssue> issues)
        {
            if (!dialog.TryGetNode(dialog.StartNodeId, out _))
            {
                AddDialogIssue(issues, dialog, CoreErrors.DialogNodeMissing, dialog.StartNodeId);
            }

            var reachable = ReachableNodes(dialog);
            foreach (var node in dialog.Nodes)
            {
                if (node.SpeakerActorId != null && !actors.Contains(node.SpeakerActorId))
                {
                    AddDialogIssue(issues, dialog, CoreErrors.DialogSpeakerMissing, node.SpeakerActorId);
                }

                ValidateDialogRequirements(dialog, node.Requirements, quests, repeatable, items, issues);
                foreach (var option in node.Options)
                {
                    ValidateDialogRequirements(dialog, option.Requirements, quests, repeatable, items, issues);
                    if (option.NextNodeId != null && !dialog.TryGetNode(option.NextNodeId, out _))
                    {
                        AddDialogIssue(
                            issues,
                            dialog,
                            CoreErrors.DialogOptionNextMissing,
                            option.NextNodeId);
                    }

                    foreach (var effect in option.Effects)
                    {
                        ValidateDialogEffect(dialog, effect, quests, items, issues);
                    }
                }

                if (!reachable.Contains(node.Id))
                {
                    AddDialogIssue(issues, dialog, CoreErrors.DialogNodeUnreachable, node.Id);
                }
            }
        }

        private static HashSet<string> ReachableNodes(DialogTemplate dialog)
        {
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var pending = new Queue<string>();
            pending.Enqueue(dialog.StartNodeId);
            reachable.Add(dialog.StartNodeId);
            while (pending.Count > 0)
            {
                var nodeId = pending.Dequeue();
                if (!dialog.TryGetNode(nodeId, out var node) || node == null)
                {
                    continue;
                }

                foreach (var option in node.Options)
                {
                    if (option.NextNodeId == null || !reachable.Add(option.NextNodeId))
                    {
                        continue;
                    }

                    pending.Enqueue(option.NextNodeId);
                }
            }

            return reachable;
        }

        private static void ValidateDialogRequirements(
            DialogTemplate dialog,
            RequirementsTemplate requirements,
            StoryQuestRepository quests,
            QuestRepository repeatable,
            ItemRepository items,
            List<ValidationIssue> issues)
        {
            foreach (var condition in requirements.QuestStates)
            {
                if (!quests.Contains(condition.QuestId))
                {
                    AddDialogIssue(
                        issues,
                        dialog,
                        CoreErrors.DialogRequirementState,
                        condition.QuestId);
                }
            }

            foreach (var variant in requirements.QuestVariants)
            {
                if (!quests.TryGet(variant.QuestId, out var template) ||
                    !template.TryGetVariant(variant.VariantId, out _))
                {
                    AddDialogIssue(
                        issues,
                        dialog,
                        CoreErrors.DialogRequirementVariant,
                        variant.QuestId + ":" + variant.VariantId);
                }
            }

            foreach (var questId in requirements.Quests)
            {
                if (!quests.Contains(questId) && !repeatable.Contains(questId))
                {
                    AddDialogIssue(issues, dialog, CoreErrors.QuestNotFound, questId);
                }
            }

            foreach (var item in requirements.Items)
            {
                if (!items.TryGet(item.ItemId, out _))
                {
                    AddDialogIssue(issues, dialog, CoreErrors.QuestRewardItemMissing, item.ItemId);
                }
            }
        }

        private static void ValidateDialogEffect(
            DialogTemplate dialog,
            DialogEffect effect,
            StoryQuestRepository quests,
            ItemRepository items,
            List<ValidationIssue> issues)
        {
            switch (effect.Type)
            {
                case DialogEffectType.Quest:
                    if (effect.QuestId == null || !quests.TryGet(effect.QuestId, out var template))
                    {
                        AddDialogIssue(
                            issues,
                            dialog,
                            CoreErrors.DialogEffectQuest,
                            effect.QuestId ?? string.Empty);
                        return;
                    }

                    if (effect.VariantId != null &&
                        !template.TryGetVariant(effect.VariantId, out _))
                    {
                        AddDialogIssue(
                            issues,
                            dialog,
                            CoreErrors.DialogRequirementVariant,
                            effect.QuestId + ":" + effect.VariantId);
                    }

                    break;

                case DialogEffectType.Item:
                    if (effect.ItemId == null || !items.TryGet(effect.ItemId, out _))
                    {
                        AddDialogIssue(
                            issues,
                            dialog,
                            CoreErrors.DialogEffectItem,
                            effect.ItemId ?? string.Empty);
                    }

                    break;

                case DialogEffectType.Unlock:
                    if (string.IsNullOrWhiteSpace(effect.UnlockId))
                    {
                        AddDialogIssue(issues, dialog, CoreErrors.DialogEffectUnlock);
                    }

                    break;
            }
        }

        private static void ValidateActorDialogs(
            ActorRepository actors,
            DialogRepository dialogs,
            List<ValidationIssue> issues)
        {
            foreach (var actor in actors.All)
            {
                if (actor.DialogId != null && !dialogs.Contains(actor.DialogId))
                {
                    issues.Add(new ValidationIssue(
                        actor.Id,
                        CoreErrors.DialogNotFound,
                        CoreText.Format(CoreErrors.DialogNotFound, actor.DialogId)));
                }
            }
        }

        private static void AddDialogIssue(
            List<ValidationIssue> issues,
            DialogTemplate dialog,
            string code,
            params object[] args)
        {
            issues.Add(new ValidationIssue(dialog.Id, code, CoreText.Format(code, args)));
        }
    }
}
