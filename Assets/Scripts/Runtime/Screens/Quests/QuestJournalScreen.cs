#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>
    /// Журнал заданий по прототипу: слева переключатели категорий и прокручиваемый список,
    /// справа описание выбранного задания с условиями и сроками, под ним кнопка действия
    /// («Сдать», когда цель выполнена, иначе «Отменить»). Разметка приходит из префаба.
    /// </summary>
    public sealed partial class QuestJournalScreen : PrefabScreenBase<IQuestJournalScreenHost>
    {
        [Tooltip("Заголовок журнала")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Кнопка категории «сюжетные»")]
        [SerializeField]
        private Button? storyTab = null;

        [Tooltip("Кнопка категории «побочные»")]
        [SerializeField]
        private Button? sideTab = null;

        [Tooltip("Кнопка категории «повторяемые»")]
        [SerializeField]
        private Button? repeatableTab = null;

        [Tooltip("Прокручиваемый список заданий категории")]
        [SerializeField]
        private ScrollArea? list = null;

        [Tooltip("Неактивный образец строки списка")]
        [SerializeField]
        private QuestJournalRow? rowTemplate = null;

        [Tooltip("Подпись пустой категории")]
        [SerializeField]
        private Text? empty = null;

        [Tooltip("Заголовок описания выбранного задания")]
        [SerializeField]
        private Text? detailsTitle = null;

        [Tooltip("Тип выбранного задания")]
        [SerializeField]
        private Text? detailsKind = null;

        [Tooltip("Описание выбранного задания")]
        [SerializeField]
        private Text? detailsDescription = null;

        [Tooltip("Прогресс выбранного задания")]
        [SerializeField]
        private Text? detailsProgress = null;

        [Tooltip("Условия выбранного задания: срок или провал")]
        [SerializeField]
        private Text? detailsConditions = null;

        [Tooltip("Заголовок блока условий")]
        [SerializeField]
        private Text? conditionsHeader = null;

        [Tooltip("Цели выбранного задания")]
        [SerializeField]
        private Text? detailsTargets = null;

        [Tooltip("Награда выбранного задания")]
        [SerializeField]
        private Text? detailsReward = null;

        [Tooltip("Подсказка панели описания")]
        [SerializeField]
        private Text? detailsHint = null;

        [Tooltip("Кнопка действия с выбранным заданием")]
        [SerializeField]
        private Button? action = null;

        [Tooltip("Подпись кнопки действия")]
        [SerializeField]
        private Text? actionLabel = null;

        [Tooltip("Всплывающее сообщение журнала")]
        [SerializeField]
        private Text? toast = null;

        [Tooltip("Кнопка закрытия журнала")]
        [SerializeField]
        private Button? close = null;

        private readonly List<JournalItem> _items = new List<JournalItem>();
        private QuestJournalCategory _category = QuestJournalCategory.Story;
        private string? _selectedId;

        /// <summary>Текущая категория журнала.</summary>
        public QuestJournalCategory Category => _category;

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (storyTab != null)
            {
                storyTab.onClick.AddListener(() => SelectCategory(QuestJournalCategory.Story));
            }

            if (sideTab != null)
            {
                sideTab.onClick.AddListener(() => SelectCategory(QuestJournalCategory.Side));
            }

            if (repeatableTab != null)
            {
                repeatableTab.onClick.AddListener(() => SelectCategory(QuestJournalCategory.Repeatable));
            }

            if (close != null)
            {
                close.onClick.AddListener(Back);
            }
        }

        /// <summary>Открывает журнал активных заданий.</summary>
        public void Show()
        {
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Rebuild();
        }

        /// <summary>Esc возвращает на игровой экран.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            Back();
            return true;
        }

        private void SelectCategory(QuestJournalCategory category)
        {
            if (_category == category)
            {
                return;
            }

            _category = category;
            _selectedId = null;
            Rebuild();
        }

        private void Rebuild()
        {
            if (title != null)
            {
                title.text = UiText.Get(Host.Texts, UiTextKey.Quest_Journal_Title);
            }

            ApplyTabs();
            BuildItems();
            FillList();
            FillDetails();
        }

        private void ApplyTabs()
        {
            ApplyTab(storyTab, _category == QuestJournalCategory.Story);
            ApplyTab(sideTab, _category == QuestJournalCategory.Side);
            ApplyTab(repeatableTab, _category == QuestJournalCategory.Repeatable);
        }

        private static void ApplyTab(Button? tab, bool active)
        {
            if (tab == null)
            {
                return;
            }

            var image = tab.GetComponent<Image>();
            if (image != null)
            {
                image.color = active ? QuestPalette.TabActive : QuestPalette.Tab;
            }
        }

        private void ShowToast(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? QuestPalette.Error : UiFactory.ToastColor;
        }

        private void Back() => Host.ShowGame(false);

        /// <summary>Одна запись журнала: сюжетная или повторяемая.</summary>
        private sealed class JournalItem
        {
            public JournalItem(
                string id,
                bool story,
                string title,
                string progress,
                string time,
                bool ready,
                QuestState? repeatable,
                StoryQuestState? storyState,
                QuestTemplate? repeatableTemplate,
                StoryQuestTemplate? storyTemplate)
            {
                Id = id;
                Story = story;
                Title = title;
                Progress = progress;
                Time = time;
                Ready = ready;
                Repeatable = repeatable;
                StoryState = storyState;
                RepeatableTemplate = repeatableTemplate;
                StoryTemplate = storyTemplate;
            }

            public string Id { get; }

            public bool Story { get; }

            public string Title { get; }

            public string Progress { get; }

            public string Time { get; }

            public bool Ready { get; }

            public QuestState? Repeatable { get; }

            public StoryQuestState? StoryState { get; }

            public QuestTemplate? RepeatableTemplate { get; }

            public StoryQuestTemplate? StoryTemplate { get; }
        }
    }
}
