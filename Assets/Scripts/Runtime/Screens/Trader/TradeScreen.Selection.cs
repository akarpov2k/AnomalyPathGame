#nullable enable
using System.Globalization;
using AnomalyPath.Core.Trade;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Часть класса <see cref="TradeScreen"/>: выбор предмета, количество, подписи середины окна
    /// и сама сделка «Купить» или «Продать».
    /// </summary>
    public sealed partial class TradeScreen
    {
        private void SelectTraderItem(string key)
        {
            var offer = FindOffer(key);
            if (offer == null)
            {
                return;
            }

            _selectedKey = key;
            _mode = TradeMode.Buy;
            _count = 1;
            _max = Mathf.Max(1, offer.Stock);
            ShowMessage(string.Empty);
            Refresh();
        }

        private void SelectPlayerItem(string key)
        {
            var item = _player?.Inventory.Find(key);
            if (item == null)
            {
                return;
            }

            _selectedKey = key;
            _mode = TradeMode.Sell;
            _count = item.Stack;
            _max = item.Stack;
            ShowMessage(string.Empty);
            Refresh();
        }

        private void ValidateSelection()
        {
            if (_mode == TradeMode.Buy)
            {
                var offer = FindOffer(_selectedKey);
                if (offer == null)
                {
                    ClearSelection();
                    return;
                }

                _max = Mathf.Max(1, offer.Stock);
                _count = Mathf.Clamp(_count, 1, _max);
                return;
            }

            if (_mode == TradeMode.Sell && _selectedKey != null)
            {
                var item = _player?.Inventory.Find(_selectedKey);
                if (item == null)
                {
                    ClearSelection();
                    return;
                }

                _max = Mathf.Max(1, item.Stack);
                _count = Mathf.Clamp(_count, 1, _max);
                return;
            }

            ClearSelection();
        }

        private void ClearSelection()
        {
            _selectedKey = null;
            _mode = TradeMode.None;
            _count = 1;
            _max = 1;
        }

        private void UpdateMiddle()
        {
            var texts = Host.Texts;
            if (_mode == TradeMode.None || _selectedKey == null)
            {
                ShowNoSelection();
                return;
            }

            if (_mode == TradeMode.Buy)
            {
                ShowBuy(FindOffer(_selectedKey));
                return;
            }

            ShowSell(_selectedKey);
        }

        private void ShowNoSelection()
        {
            var texts = Host.Texts;
            if (selectedIcon != null)
            {
                selectedIcon.enabled = false;
                selectedIcon.sprite = null;
            }

            if (selectedName != null)
            {
                selectedName.text = UiText.Get(texts, UiTextKey.Trade_Select);
            }

            if (selectedPrice != null)
            {
                selectedPrice.text = string.Empty;
            }

            if (quantity != null)
            {
                quantity.text = UiText.Get(texts, UiTextKey.Common_Dash);
            }

            SetStepButtons(false, false, false);
            if (action != null)
            {
                action.interactable = false;
            }

            if (actionLabel != null)
            {
                actionLabel.text = UiText.Get(texts, UiTextKey.Trade_Select);
            }
        }

        private void ShowBuy(TraderOffer? offer)
        {
            if (offer == null)
            {
                ClearSelection();
                ShowNoSelection();
                return;
            }

            var texts = Host.Texts;
            var template = Host.Items.TryGet(offer.ItemId, out var found) ? found : null;
            ShowIcon(template?.Icon);
            if (selectedName != null)
            {
                selectedName.text = template != null ? texts.Get(template.NameKey) : offer.ItemId;
            }

            if (selectedPrice != null)
            {
                selectedPrice.text = UiText.Format(texts, UiTextKey.Trade_Unit_Price, offer.UnitPrice);
            }

            if (quantity != null)
            {
                quantity.text = _count.ToString(CultureInfo.InvariantCulture);
            }

            if (action != null)
            {
                action.interactable = true;
            }

            if (actionLabel != null)
            {
                actionLabel.text = UiText.Format(
                    texts,
                    UiTextKey.Trade_Buy_Total,
                    (long)offer.UnitPrice * _count);
            }

            SetStepButtons(_count > 1, _count < _max, _count < _max);
        }

        private void ShowSell(string instanceId)
        {
            var item = _player?.Inventory.Find(instanceId);
            if (item == null)
            {
                ClearSelection();
                ShowNoSelection();
                return;
            }

            var texts = Host.Texts;
            var template = ItemPresentation.Template(WidgetHost, item);
            var offer = FindSellOffer(instanceId);
            ShowIcon(template?.Icon);
            if (selectedName != null)
            {
                selectedName.text = template != null ? texts.Get(template.NameKey) : item.ItemId;
            }

            if (selectedPrice != null)
            {
                selectedPrice.text = offer != null
                    ? UiText.Format(texts, UiTextKey.Trade_Unit_Price, offer.UnitPrice)
                    : UiText.Get(texts, UiTextKey.Trader_Result_Not_Sellable);
            }

            if (quantity != null)
            {
                quantity.text = _count.ToString(CultureInfo.InvariantCulture);
            }

            if (action != null)
            {
                action.interactable = offer != null;
            }

            if (actionLabel != null)
            {
                actionLabel.text = offer != null
                    ? UiText.Format(texts, UiTextKey.Trade_Sell_Total, (long)offer.UnitPrice * _count)
                    : UiText.Get(texts, UiTextKey.Trader_Result_Not_Sellable);
            }

            SetStepButtons(_count > 1, _count < _max, _count < _max);
        }

        private void ShowIcon(string? iconId)
        {
            if (selectedIcon == null)
            {
                return;
            }

            var sprite = iconId != null ? SpriteOf(iconId) : null;
            selectedIcon.sprite = sprite;
            selectedIcon.enabled = sprite != null;
        }

        private void SetStepButtons(bool canDecrease, bool canIncrease, bool canMax)
        {
            if (minus != null)
            {
                minus.interactable = canDecrease;
            }

            if (plus != null)
            {
                plus.interactable = canIncrease;
            }

            if (max != null)
            {
                max.interactable = canMax;
            }
        }

        private void Step(int delta)
        {
            if (_mode == TradeMode.None)
            {
                return;
            }

            _count = Mathf.Clamp(_count + delta, 1, Mathf.Max(1, _max));
            ShowMessage(string.Empty);
            UpdateMiddle();
        }

        private void SelectMax()
        {
            if (_mode == TradeMode.None)
            {
                return;
            }

            _count = Mathf.Max(1, _max);
            ShowMessage(string.Empty);
            UpdateMiddle();
        }

        private void Perform()
        {
            var session = Host.Session;
            if (session == null || _player == null || _selectedKey == null || _mode == TradeMode.None)
            {
                return;
            }

            var key = _selectedKey;
            if (_mode == TradeMode.Buy)
            {
                var bought = Host.Trade.Buy(session, _actorId, key, _count);
                if (!bought.Succeeded)
                {
                    ShowMessage(UiText.Get(Host.Texts, KeyFor(bought.Status)), true);
                    return;
                }

                var name = Host.Items.TryGet(bought.ItemId, out var template)
                    ? Host.Texts.Get(template.NameKey)
                    : bought.ItemId;
                ShowMessage(UiText.Format(Host.Texts, UiTextKey.Trader_Bought, name, bought.Count, bought.Price));
                Refresh();
                return;
            }

            var sold = Host.Trade.Sell(session, _actorId, key, _count);
            if (!sold.Succeeded)
            {
                ShowMessage(UiText.Get(Host.Texts, KeyFor(sold.Status)), true);
                return;
            }

            var soldName = Host.Items.TryGet(sold.ItemId, out var soldTemplate)
                ? Host.Texts.Get(soldTemplate.NameKey)
                : sold.ItemId;
            ShowMessage(UiText.Format(
                Host.Texts,
                UiTextKey.Trader_Sold,
                soldName + " ×" + sold.Count.ToString(CultureInfo.InvariantCulture),
                sold.Price));
            Refresh();
        }

        private TraderOffer? FindOffer(string? itemId)
        {
            if (string.IsNullOrEmpty(itemId))
            {
                return null;
            }

            foreach (var offer in _offers)
            {
                if (string.Equals(offer.ItemId, itemId, System.StringComparison.Ordinal))
                {
                    return offer;
                }
            }

            return null;
        }

        private TraderSellOffer? FindSellOffer(string? instanceId)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return null;
            }

            foreach (var offer in _sellOffers)
            {
                if (string.Equals(offer.InstanceId, instanceId, System.StringComparison.Ordinal))
                {
                    return offer;
                }
            }

            return null;
        }

        private static UiTextKey KeyFor(TradeStatus status) => status switch
        {
            TradeStatus.NotEnoughMoney => UiTextKey.Trader_Result_Not_Enough_Money,
            TradeStatus.NoStock => UiTextKey.Trader_Result_No_Stock,
            TradeStatus.NoRoom => UiTextKey.Trader_Result_No_Room,
            TradeStatus.OfferNotFound => UiTextKey.Trader_Result_Offer_Missing,
            TradeStatus.ItemNotFound => UiTextKey.Trader_Result_Item_Missing,
            TradeStatus.ItemNotSellable => UiTextKey.Trader_Result_Not_Sellable,
            TradeStatus.InvalidCount => UiTextKey.Trader_Result_Invalid_Count,
            TradeStatus.TraderNotFound => UiTextKey.Trader_Result_Unavailable,
            TradeStatus.NotTrader => UiTextKey.Trader_Result_Unavailable,
            TradeStatus.Unavailable => UiTextKey.Trader_Result_Unavailable,
            TradeStatus.WrongLocation => UiTextKey.Trader_Result_Unavailable,
            _ => UiTextKey.Trader_Result_Failed
        };
    }
}
