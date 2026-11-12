#nullable enable
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>Часть класса <see cref="QuestJournalScreen"/>: описание выбранного задания.</summary>
    public sealed partial class QuestJournalScreen
    {
        private void FillDetails()
        {
            var item = _selectedId != null ? FindItem(_selectedId) : null;
            if (item == null)
            {
                SetText(detailsTitle, string.Empty);
                SetText(detailsKind, string.Empty);
                SetText(detailsDescription, string.Empty);
                SetText(detailsProgress, string.Empty);
                SetText(detailsConditions, string.Empty);
                SetText(detailsTargets, string.Empty);
                SetText(detailsReward, string.Empty);
                SetText(detailsHint, UiText.Get(Host.Texts, UiTextKey.Quest_Details_Empty));
                ShowConditions(false);
                if (action != null)
                {
                    action.gameObject.SetActive(false);
                }

                return;
            }

            if (item.Story)
            {
                FillStory(item);
            }
            else
            {
                FillRepeatable(item);
            }

            UpdateAction(item);
        }

        private void FillRepeatable(JournalItem item)
        {
            var session = Host.Session;
            var template = item.RepeatableTemplate!;
            var quest = item.Repeatable!;
            if (session == null)
            {
                return;
            }

            var progress = Host.Quests.GetProgress(session, quest);
            SetText(detailsTitle, Host.Texts.Get(template.TitleKey));
            ShowConditions(true);
            SetText(
                detailsKind,
                QuestText.TypeName(WidgetHost, template.Type) + " · " +
                QuestText.Difficulty(WidgetHost, template.Difficulty));
            SetText(detailsDescription, Host.Texts.Get(template.DescriptionKey));
            SetText(detailsProgress, QuestText.ProgressLine(WidgetHost, template, progress));
            SetText(detailsConditions, QuestText.FailLine(WidgetHost, session, quest, template));
            SetText(detailsTargets, QuestText.TargetsLine(WidgetHost, quest, template, progress));
            SetText(detailsReward, QuestText.RewardLine(WidgetHost, template.Rewards));
            SetText(detailsHint, string.Empty);
        }

        private void FillStory(JournalItem item)
        {
            var template = item.StoryTemplate!;
            var quest = item.StoryState!;
            var rewards = template.Rewards;
            if (template.TryGetVariant(quest.VariantId, out var variant))
            {
                rewards = variant.Rewards;
            }

            SetText(detailsTitle, Host.Texts.Get(template.TitleKey));
            SetText(detailsKind, UiText.Get(Host.Texts, UiTextKey.Quest_Category_Story));
            ShowConditions(false);
            SetText(detailsDescription, Host.Texts.Get(template.DescriptionKey));
            SetText(detailsProgress, QuestText.StoryProgressLine(WidgetHost, quest, template, out _));
            SetText(detailsConditions, string.Empty);
            SetText(detailsTargets, string.Empty);
            SetText(detailsReward, QuestText.RewardLine(WidgetHost, rewards));
            SetText(detailsHint, UiText.Get(Host.Texts, UiTextKey.Quest_Story_Turn_In));
        }

        private void ShowConditions(bool shown)
        {
            if (conditionsHeader != null)
            {
                conditionsHeader.gameObject.SetActive(shown);
            }

            if (!shown)
            {
                SetText(detailsConditions, string.Empty);
            }
        }

        private static void SetText(UnityEngine.UI.Text? label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }
    }
}
