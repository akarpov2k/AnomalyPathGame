#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>Часть класса <see cref="QuestJournalScreen"/>: действия с выбранным заданием.</summary>
    public sealed partial class QuestJournalScreen
    {
        private void UpdateAction(JournalItem item)
        {
            if (action == null)
            {
                return;
            }

            if (item.Story)
            {
                action.gameObject.SetActive(false);
                return;
            }

            action.gameObject.SetActive(true);
            action.onClick.RemoveAllListeners();
            var quest = item.Repeatable!;
            if (item.Ready)
            {
                if (actionLabel != null)
                {
                    actionLabel.text = UiText.Get(Host.Texts, UiTextKey.Quest_Turn_In);
                }

                action.interactable = OnQuestBoard(quest.BoardLocationId);
                action.onClick.AddListener(() => TurnIn(quest.QuestId));
                return;
            }

            if (actionLabel != null)
            {
                actionLabel.text = UiText.Get(Host.Texts, UiTextKey.Quest_Cancel);
            }

            action.interactable = true;
            action.onClick.AddListener(() => CancelQuest(quest.QuestId));
        }

        private bool OnQuestBoard(string boardLocationId)
        {
            var session = Host.Session;
            return session != null && string.Equals(
                boardLocationId,
                session.State.World.CurrentLocation.InstanceId,
                StringComparison.Ordinal);
        }

        private void TurnIn(string questId)
        {
            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var boardLocationId = session.State.World.CurrentLocation.InstanceId;
            var result = Host.Quests.TurnIn(session, boardLocationId, questId);
            if (result.Succeeded)
            {
                if (result.Loot.Count > 0 || result.Money > 0)
                {
                    Host.ShowRewards(result.Loot, result.Money, RewardSource.Quest, () => Host.ShowQuests(null));
                    return;
                }

                var name = Host.Quests.TryGetTemplate(questId, out var template)
                    ? Host.Texts.Get(template.TitleKey)
                    : questId;
                ShowToast(UiText.Format(Host.Texts, UiTextKey.Quest_Completed, name, result.Money));
                Rebuild();
                return;
            }

            ShowToast(UiText.Get(Host.Texts, QuestText.ResultKey(result.Status)), error: true);
        }

        private void CancelQuest(string questId)
        {
            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var result = Host.Quests.CancelQuest(session, questId);
            if (result.Succeeded)
            {
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Quest_Cancelled));
                _selectedId = null;
                Rebuild();
                return;
            }

            ShowToast(UiText.Get(Host.Texts, QuestText.ResultKey(result.Status)), error: true);
        }
    }
}
