#nullable enable
using System;
using AnomalyPath.Core.Quests;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>Часть класса <see cref="QuestJournalScreen"/>: категории и список заданий.</summary>
    public sealed partial class QuestJournalScreen
    {
        private void BuildItems()
        {
            _items.Clear();
            var session = Host.Session;
            if (session == null)
            {
                _selectedId = null;
                return;
            }

            if (_category == QuestJournalCategory.Repeatable)
            {
                BuildRepeatable(session);
            }
            else
            {
                BuildStory(session);
            }

            if (_selectedId == null || FindItem(_selectedId) == null)
            {
                _selectedId = _items.Count > 0 ? _items[0].Id : null;
            }
        }

        private void BuildRepeatable(AnomalyPath.Core.Session.GameSession session)
        {
            foreach (var quest in Host.Quests.GetActive(session))
            {
                if (!Host.Quests.TryGetTemplate(quest.QuestId, out var template) ||
                    template.QuestKind != QuestKind.Repeatable)
                {
                    continue;
                }

                var progress = Host.Quests.GetProgress(session, quest);
                _items.Add(new JournalItem(
                    quest.QuestId,
                    story: false,
                    Host.Texts.Get(template.TitleKey),
                    QuestText.ProgressLine(WidgetHost, template, progress),
                    QuestText.FailLine(WidgetHost, session, quest, template),
                    progress.Ready,
                    quest,
                    null,
                    template,
                    null));
            }
        }

        private void BuildStory(AnomalyPath.Core.Session.GameSession session)
        {
            var mainLine = Host.Rules.Require().MainStoryLine;
            var wantMain = _category == QuestJournalCategory.Story;
            foreach (var quest in Host.StoryQuests.GetActive(session))
            {
                if (!Host.StoryQuests.TryGetTemplate(quest.QuestId, out var template))
                {
                    continue;
                }

                var isMain = string.Equals(template.StoryLine, mainLine, StringComparison.Ordinal);
                if (isMain != wantMain)
                {
                    continue;
                }

                var progressText = QuestText.StoryProgressLine(WidgetHost, quest, template, out var ready);
                _items.Add(new JournalItem(
                    quest.QuestId,
                    story: true,
                    Host.Texts.Get(template.TitleKey),
                    progressText,
                    string.Empty,
                    ready,
                    null,
                    quest,
                    null,
                    template));
            }
        }

        private JournalItem? FindItem(string id)
        {
            foreach (var item in _items)
            {
                if (string.Equals(item.Id, id, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private void FillList()
        {
            if (empty != null)
            {
                empty.gameObject.SetActive(_items.Count == 0);
                empty.text = UiText.Get(Host.Texts, UiTextKey.Quest_Empty_Category);
                empty.transform.SetAsLastSibling();
            }

            if (list != null)
            {
                ClearRows();
            }

            if (list == null || rowTemplate == null)
            {
                return;
            }

            foreach (var item in _items)
            {
                var row = Instantiate(rowTemplate, list.Content);
                row.gameObject.name = "QuestRow_" + item.Id;
                row.gameObject.SetActive(true);
                row.Bind(
                    item.Id,
                    item.Title,
                    item.Progress,
                    item.Time,
                    item.Ready,
                    string.Equals(item.Id, _selectedId, StringComparison.Ordinal),
                    () => SelectQuest(item.Id));
            }
        }

        private void ClearRows()
        {
            if (list == null || rowTemplate == null)
            {
                return;
            }

            foreach (Transform child in list.Content)
            {
                if (child.gameObject == rowTemplate.gameObject)
                {
                    continue;
                }

                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void SelectQuest(string id)
        {
            _selectedId = id;
            FillList();
            FillDetails();
        }
    }
}
