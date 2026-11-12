#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Trade;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Окно торговли по прототипу: слева товары торговца, справа инвентарь игрока,
    /// между ними — выбранный предмет, количество и кнопка «Купить» или «Продать» с итоговой ценой.
    /// Деньги стоят на плашке окна инвентаря, как на экране персонажа; разметка приходит из префаба,
    /// код заполняет сетки и обрабатывает выбор предмета, количество и сделку.
    /// </summary>
    public sealed partial class TradeScreen : PrefabScreenBase<ITradeScreenHost>
    {
        [Tooltip("Заголовок окна: имя торговца")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Крестик закрытия окна")]
        [SerializeField]
        private Button? close = null;

        [Tooltip("Сетка товаров торговца (слева)")]
        [SerializeField]
        private ScrollArea? traderGrid = null;

        [Tooltip("Сетка инвентаря игрока (справа)")]
        [SerializeField]
        private ScrollArea? playerGrid = null;

        [Tooltip("Подпись денег на плашке окна инвентаря")]
        [SerializeField]
        private Text? money = null;

        [Tooltip("Иконка выбранного предмета")]
        [SerializeField]
        private Image? selectedIcon = null;

        [Tooltip("Название выбранного предмета")]
        [SerializeField]
        private Text? selectedName = null;

        [Tooltip("Цена одной штуки")]
        [SerializeField]
        private Text? selectedPrice = null;

        [Tooltip("Значение количества")]
        [SerializeField]
        private Text? quantity = null;

        [Tooltip("Кнопка уменьшения количества")]
        [SerializeField]
        private Button? minus = null;

        [Tooltip("Кнопка увеличения количества")]
        [SerializeField]
        private Button? plus = null;

        [Tooltip("Кнопка «Макс»: выбрать весь остаток или всю стопку")]
        [SerializeField]
        private Button? max = null;

        [Tooltip("Кнопка сделки: «Купить» или «Продать» с итоговой ценой")]
        [SerializeField]
        private Button? action = null;

        [Tooltip("Подпись кнопки сделки")]
        [SerializeField]
        private Text? actionLabel = null;

        [Tooltip("Строка сообщений о результате")]
        [SerializeField]
        private Text? toast = null;

        private readonly List<TradeGridItem> _traderItems = new List<TradeGridItem>();
        private readonly List<TradeGridItem> _playerItems = new List<TradeGridItem>();
        private ItemTooltip _tooltip = null!;
        private IReadOnlyList<TraderOffer> _offers = new List<TraderOffer>();
        private IReadOnlyList<TraderSellOffer> _sellOffers = new List<TraderSellOffer>();
        private PlayerState? _player;
        private string _actorId = string.Empty;
        private string? _selectedKey;
        private TradeMode _mode;
        private int _count = 1;
        private int _max = 1;
        private bool _returnToDialog;

        /// <summary>Id торговца, чьё окно открыто.</summary>
        public string ActorId => _actorId;

        /// <summary>Открывает окно торговли и строит сетки по текущему состоянию игрока.</summary>
        /// <param name="actorId">Идентификатор торговца.</param>
        /// <param name="returnToDialog">Вернуть в меню диалога торговца после закрытия окна.</param>
        public void Show(string actorId, bool returnToDialog = false)
        {
            _actorId = actorId;
            _returnToDialog = returnToDialog;
            _selectedKey = null;
            _mode = TradeMode.None;
            _count = 1;
            _tooltip.Hide();
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        /// <summary>Esc и крестик закрывают окно и возвращают на игровой экран.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            Close();
            return true;
        }

        /// <inheritdoc />
        protected override void OnBound()
        {
            _tooltip = new ItemTooltip(WidgetHost, Content);
            if (close != null)
            {
                close.onClick.AddListener(Close);
            }

            if (minus != null)
            {
                minus.onClick.AddListener(() => Step(-1));
            }

            if (plus != null)
            {
                plus.onClick.AddListener(() => Step(1));
            }

            if (max != null)
            {
                max.onClick.AddListener(SelectMax);
            }

            if (action != null)
            {
                action.onClick.AddListener(Perform);
            }
        }

        private void Close()
        {
            _tooltip.Hide();
            if (_returnToDialog)
            {
                Host.ShowTraderRoom(_actorId);
                return;
            }

            Host.ShowGame(false);
        }

        private void Refresh()
        {
            var session = Host.Session;
            var texts = Host.Texts;
            var player = session?.State.Player;
            _player = player;
            _offers = new List<TraderOffer>();
            _sellOffers = new List<TraderSellOffer>();

            if (session == null || player == null)
            {
                _selectedKey = null;
                _mode = TradeMode.None;
                _count = 1;
                _max = 1;
                if (title != null)
                {
                    title.text = UiText.Get(texts, UiTextKey.Common_No_Game);
                }

                if (money != null)
                {
                    money.text = string.Empty;
                }

                RebuildGrids(0);
                ShowMessage(UiText.Get(texts, UiTextKey.Common_No_Game), true);
                UpdateMiddle();
                return;
            }

            var actor = Host.Actors.Contains(_actorId) ? Host.Actors.Get(_actorId) : null;
            var available = actor != null && Host.Trade.IsAvailable(session, _actorId);
            if (title != null)
            {
                title.text = available && actor != null
                    ? texts.Get(actor.NameKey)
                    : UiText.Get(texts, UiTextKey.Trader_Unavailable);
            }

            if (money != null)
            {
                money.text = UiText.Format(texts, UiTextKey.Common_Money, player.Money);
            }

            if (!available)
            {
                _selectedKey = null;
                _mode = TradeMode.None;
                _count = 1;
                _max = 1;
                RebuildGrids(player.Inventory.Width);
                ShowMessage(UiText.Get(texts, UiTextKey.Trader_Unavailable), true);
                UpdateMiddle();
                return;
            }

            _offers = Host.Trade.GetOffers(session, _actorId);
            _sellOffers = Host.Trade.GetSellOffers(session, _actorId);
            _player = player;
            ValidateSelection();
            RebuildGrids(player.Inventory.Width);
            UpdateMiddle();
        }

        private void RebuildGrids(int columns)
        {
            if (traderGrid == null || playerGrid == null)
            {
                return;
            }

            var safeColumns = Mathf.Max(6, columns);
            BuildTraderItems();
            BuildPlayerItems();
            var traderHeight = TradeGridBuilder.Build(
                Host.Ui,
                traderGrid.Content,
                traderGrid.ViewportWidth,
                traderGrid.ViewportHeight,
                _traderItems,
                safeColumns,
                1,
                _mode == TradeMode.Buy ? _selectedKey : null,
                SelectTraderItem,
                SpriteOf,
                "TradeItem",
                pack: true,
                tooltip: _tooltip);
            traderGrid.SetContentHeight(traderHeight);

            var playerHeight = TradeGridBuilder.Build(
                Host.Ui,
                playerGrid.Content,
                playerGrid.ViewportWidth,
                playerGrid.ViewportHeight,
                _playerItems,
                safeColumns,
                _player?.Inventory.Rows ?? 1,
                _mode == TradeMode.Sell ? _selectedKey : null,
                SelectPlayerItem,
                SpriteOf,
                "PlayerItem",
                pack: false,
                tooltip: _tooltip);
            playerGrid.SetContentHeight(playerHeight);
        }

        private void BuildTraderItems()
        {
            _traderItems.Clear();
            foreach (var offer in _offers)
            {
                var template = Host.Items.TryGet(offer.ItemId, out var found) ? found : null;
                var name = template != null ? Host.Texts.Get(template.NameKey) : offer.ItemId;
                _traderItems.Add(new TradeGridItem(
                    offer.ItemId,
                    offer.ItemId,
                    template?.FootprintWidth ?? 1,
                    template?.FootprintHeight ?? 1,
                    name,
                    template?.Icon,
                    offer.Stock,
                    rarity: template?.Rarity,
                    template: template));
            }
        }

        private void BuildPlayerItems()
        {
            _playerItems.Clear();
            if (_player == null)
            {
                return;
            }

            foreach (var item in _player.Inventory.Items)
            {
                if (item.Overflow)
                {
                    continue;
                }

                var template = ItemPresentation.Template(WidgetHost, item);
                var width = item.PlacedWidth > 0 ? item.PlacedWidth : template?.FootprintWidth ?? 1;
                var height = item.PlacedHeight > 0 ? item.PlacedHeight : template?.FootprintHeight ?? 1;
                _playerItems.Add(new TradeGridItem(
                    item.InstanceId,
                    item.ItemId,
                    width,
                    height,
                    ItemPresentation.Describe(WidgetHost, item),
                    template?.Icon,
                    item.Stack,
                    item.X,
                    item.Y,
                    ItemRarity.Resolve(item, template),
                    instance: item,
                    template: template));
            }
        }

        private Sprite? SpriteOf(string id) => Host.Sprites.TryGet(id, out var sprite) ? sprite : null;

        private void ShowMessage(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? TradePalette.Error : UiFactory.ToastColor;
        }

        /// <summary>Режим выбранного предмета: ничего, покупка или продажа.</summary>
        private enum TradeMode
        {
            /// <summary>Предмет ещё не выбран.</summary>
            None,

            /// <summary>Выбран товар торговца — кнопка «Купить».</summary>
            Buy,

            /// <summary>Выбран предмет игрока — кнопка «Продать».</summary>
            Sell
        }
    }
}
