#nullable enable
using System;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>
    /// Доска объявлений хаба по прототипу: окно с сеткой карточек, в карточке картинка задания,
    /// сложность, описание и цель, внизу «Принять» и «Отказаться». Взятый или отклонённый слот
    /// остаётся пустой карточкой с таймером обновления. Разметка приходит из префаба.
    /// </summary>
    public sealed class QuestBoardScreen : PrefabScreenBase<IQuestScreenHost>
    {
        [Tooltip("Заголовок окна: название хаба")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Подпись пустой доски")]
        [SerializeField]
        private Text? empty = null;

        [Tooltip("Сетка карточек с прокруткой")]
        [SerializeField]
        private ScrollArea? slots = null;

        [Tooltip("Неактивный образец карточки задания")]
        [SerializeField]
        private QuestBoardCard? cardTemplate = null;

        [Tooltip("Всплывающее сообщение окна")]
        [SerializeField]
        private Text? toast = null;

        [Tooltip("Кнопка закрытия окна")]
        [SerializeField]
        private Button? close = null;

        private string? _boardLocationId;

        /// <summary>Кнопка закрытия окна.</summary>
        public Button? Close => close;

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (close != null)
            {
                close.onClick.AddListener(Back);
            }
        }

        /// <summary>Открывает доску объявлений хаба.</summary>
        /// <param name="boardLocationId">Экземпляр хаба, в котором стоит доска.</param>
        public void Show(string boardLocationId)
        {
            _boardLocationId = boardLocationId;
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        /// <summary>Esc возвращает на игровой экран.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            Back();
            return true;
        }

        private void Back() => Host.ShowGame(false);

        private void Refresh()
        {
            ClearCards();
            var session = Host.Session;
            if (empty != null)
            {
                empty.gameObject.SetActive(false);
            }

            if (title != null)
            {
                title.text = string.Empty;
            }

            if (session == null || _boardLocationId == null)
            {
                ShowEmpty();
                return;
            }

            if (!session.State.World.Map.TryGetLocation(_boardLocationId, out var location) || location == null)
            {
                ShowEmpty();
                return;
            }

            if (title != null)
            {
                title.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Quest_Board_Title,
                    LocationText.Title(WidgetHost, location));
            }

            var board = Host.Quests.GetBoard(session, _boardLocationId);
            if (board.Count == 0)
            {
                ShowEmpty();
                return;
            }

            var now = session.State.World.Clock.AbsoluteHours;
            for (var index = 0; index < board.Count; index++)
            {
                AddCard(session, board[index], now);
            }
        }

        private void ShowEmpty()
        {
            if (empty != null)
            {
                empty.gameObject.SetActive(true);
                empty.text = UiText.Get(Host.Texts, UiTextKey.Quest_Empty_Offers);
            }
        }

        private void AddCard(GameSession session, QuestBoardSlot slot, double now)
        {
            if (slots == null || cardTemplate == null)
            {
                return;
            }

            var card = Instantiate(cardTemplate, slots.Content);
            card.gameObject.name = "Board_" + slot.Index;
            card.gameObject.SetActive(true);
            var difficulty = UiText.Format(
                Host.Texts,
                UiTextKey.Quest_Difficulty,
                QuestText.Difficulty(WidgetHost, slot.Difficulty));

            if (slot.Offer == null)
            {
                var timer = slot.IsLocked(now)
                    ? UiText.Format(Host.Texts, UiTextKey.Quest_Refresh_In, Math.Ceiling(slot.HoursLeft(now)))
                    : UiText.Get(Host.Texts, UiTextKey.Quest_Empty_Offers);
                card.ShowTimer(QuestPalette.Row, difficulty, timer);
                return;
            }

            var template = slot.Offer.Template;
            var state = slot.Offer.State;
            var progress = new QuestProgress(template.Type, 0, state.RequiredCount, false);
            card.ShowOffer(
                QuestPalette.Row,
                QuestArt.Resolve(WidgetHost, template),
                difficulty,
                Host.Texts.Get(template.TitleKey),
                Host.Texts.Get(template.DescriptionKey),
                QuestText.TargetsLine(WidgetHost, state, template, progress),
                () => Accept(slot.Index),
                () => Decline(slot.Index));
        }

        private void ClearCards()
        {
            if (slots == null || cardTemplate == null)
            {
                return;
            }

            foreach (Transform child in slots.Content)
            {
                if (child.gameObject == cardTemplate.gameObject)
                {
                    continue;
                }

                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }

        private void Accept(int slot)
        {
            var session = Host.Session;
            if (session == null || _boardLocationId == null)
            {
                return;
            }

            var result = Host.Quests.Accept(session, _boardLocationId, slot);
            if (result.Succeeded && result.QuestId != null &&
                Host.Quests.TryGetTemplate(result.QuestId, out var template))
            {
                ShowToast(UiText.Format(
                    Host.Texts,
                    UiTextKey.Quest_Accepted,
                    Host.Texts.Get(template.TitleKey)));
                Refresh();
                return;
            }

            ShowToast(UiText.Get(Host.Texts, QuestText.ResultKey(result.Status)), error: true);
        }

        private void Decline(int slot)
        {
            var session = Host.Session;
            if (session == null || _boardLocationId == null)
            {
                return;
            }

            var result = Host.Quests.Decline(session, _boardLocationId, slot);
            if (result.Succeeded)
            {
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Quest_Offer_Declined));
                Refresh();
                return;
            }

            ShowToast(UiText.Get(Host.Texts, QuestText.ResultKey(result.Status)), error: true);
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
    }
}
