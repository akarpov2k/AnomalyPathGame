#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Выпадашка выбора торговца: заголовок и карточки с портретами торговцев текущего хаба.
    /// Портрет показывается без рамки и подписи, а имя торговца всплывает подсказкой при наведении.
    /// Нажатие на уже открытого торговца ничего не делает, на другого — сообщает его id наружу.
    /// </summary>
    public sealed class TraderSwitchMenu
    {
        private const string RootName = "TraderMenu";

        private readonly IUiHost _host;
        private readonly Action<string> _choose;
        private readonly Image _background;
        private readonly Text _header;
        private readonly RectTransform _cards;
        private readonly TraderTooltip _tooltip;
        private readonly List<GameObject> _built = new List<GameObject>();
        private string _selectedId = string.Empty;

        /// <summary>Создаёт выпадашку скрытой.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="parent">Родитель: контент экрана торговца.</param>
        /// <param name="choose">Что выполнить с id выбранного торговца.</param>
        public TraderSwitchMenu(IUiHost host, Transform parent, Action<string> choose)
        {
            _host = host;
            _choose = choose;

            _background = host.Ui.CreateImage(RootName, parent, TraderPalette.Menu);
            _background.raycastTarget = true;
            var rect = _background.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, TraderMetrics.MenuBottom);
            rect.sizeDelta = new Vector2(TraderMetrics.CardWidth, TraderMetrics.MenuHeight);

            _header = host.Ui.CreateText(
                rect,
                UiText.Get(host.Texts, UiTextKey.Trader_Traders),
                TraderMetrics.MenuHeaderSize,
                TextAnchor.MiddleCenter);
            _header.gameObject.name = "Header";
            _header.raycastTarget = false;
            var headerRect = _header.rectTransform;
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(
                TraderMetrics.MenuPadding,
                -(TraderMetrics.MenuPadding + TraderMetrics.MenuHeaderHeight));
            headerRect.offsetMax = new Vector2(-TraderMetrics.MenuPadding, -TraderMetrics.MenuPadding);

            _cards = new GameObject("Cards", typeof(RectTransform)).GetComponent<RectTransform>();
            _cards.SetParent(rect, false);
            _cards.anchorMin = Vector2.zero;
            _cards.anchorMax = Vector2.one;
            _cards.offsetMin = Vector2.zero;
            _cards.offsetMax = Vector2.zero;

            _tooltip = new TraderTooltip(host, parent);
            _background.gameObject.SetActive(false);
        }

        /// <summary>Корень выпадашки.</summary>
        public RectTransform Root => _background.rectTransform;

        /// <summary>Открыта ли выпадашка.</summary>
        public bool IsShown => _background.gameObject.activeSelf;

        /// <summary>Подсказка с именем торговца под курсором.</summary>
        public TraderTooltip Tooltip => _tooltip;

        /// <summary>
        /// Ставит выпадашку к левому краю: в комнате торговца она должна открываться
        /// слева от окна диалога, а не по центру.
        /// </summary>
        /// <param name="left">Отступ от левого края в единицах канваса.</param>
        public void AnchorLeft(float left)
        {
            var rect = Root;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(left, TraderMetrics.MenuBottom);
        }

        /// <summary>Показывает карточки торговцев; открытый сейчас торговец запоминается для нажатия.</summary>
        /// <param name="traderIds">Торговцы текущего хаба.</param>
        /// <param name="selectedId">Id торговца, чьё окно открыто.</param>
        public void Show(IReadOnlyList<string> traderIds, string selectedId)
        {
            _selectedId = selectedId;
            ClearCards();
            _tooltip.Hide();
            var count = traderIds.Count;
            var width = (TraderMetrics.MenuPadding * 2f) +
                        (count * TraderMetrics.CardWidth) +
                        (Mathf.Max(0, count - 1) * TraderMetrics.CardGap);
            Root.sizeDelta = new Vector2(width, TraderMetrics.MenuHeight);
            for (var index = 0; index < count; index++)
            {
                BuildCard(traderIds[index], index);
            }

            Root.SetAsLastSibling();
            _background.gameObject.SetActive(true);
        }

        /// <summary>Скрывает выпадашку; карточки остаются до следующего показа.</summary>
        public void Hide()
        {
            _tooltip.Hide();
            _background.gameObject.SetActive(false);
        }

        private void BuildCard(string traderId, int index)
        {
            var go = new GameObject(
                "TraderCard_" + traderId,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(TraderCardHover));
            go.transform.SetParent(_cards, false);
            var rect = (RectTransform)go.transform;
            UiWidgets.Place(
                rect,
                TraderMetrics.MenuPadding + (index * (TraderMetrics.CardWidth + TraderMetrics.CardGap)),
                TraderMetrics.MenuPadding + TraderMetrics.MenuHeaderHeight,
                TraderMetrics.CardWidth,
                TraderMetrics.CardHeight);

            var image = go.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;
            var id = traderId;
            button.onClick.AddListener(() => Choose(id));

            var actor = _host.Actors.Contains(id) ? _host.Actors.Get(id) : null;
            var portrait = _host.Ui.CreateImage("Portrait", rect, Color.white);
            portrait.sprite = PortraitOf(actor);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            UiWidgets.Place(
                portrait.rectTransform,
                0f,
                0f,
                TraderMetrics.CardWidth,
                TraderMetrics.CardHeight);

            var cardName = actor == null ? string.Empty : _host.Texts.Get(actor.NameKey);
            var hover = go.GetComponent<TraderCardHover>();
            hover.Configure(
                (position, camera) => _tooltip.Show(cardName, position, camera),
                (position, camera) => _tooltip.Move(position, camera),
                _tooltip.Hide);

            _built.Add(go);
        }

        private Sprite? PortraitOf(ActorTemplate? actor)
        {
            if (actor == null)
            {
                return null;
            }

            if (actor.Portrait != null)
            {
                if (_host.Sprites.TryGet(actor.Portrait + "_thumb", out var thumb) && thumb != null)
                {
                    return thumb;
                }

                if (_host.Sprites.TryGet(actor.Portrait, out var sprite) && sprite != null)
                {
                    return sprite;
                }
            }

            return null;
        }

        /// <summary>Нажатие на открытого торговца ничего не делает.</summary>
        private void Choose(string traderId)
        {
            if (string.Equals(traderId, _selectedId, StringComparison.Ordinal))
            {
                return;
            }

            _choose(traderId);
        }

        private void ClearCards()
        {
            foreach (var card in _built)
            {
                card.SetActive(false);
                UnityEngine.Object.Destroy(card);
            }

            _built.Clear();
        }
    }
}
