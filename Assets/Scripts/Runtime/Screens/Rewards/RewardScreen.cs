#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Rewards
{
    /// <summary>
    /// Окно наград по прототипу `награды`: слева трофеи противника, содержимое тайника,
    /// лут объекта/аномалии или предметы награды за задание, справа инвентарь игрока
    /// (только просмотр); под левой колонкой «Взять всё», крестик спрашивает подтверждение,
    /// если лут остался. Разметка приходит из префаба, код заполняет сетки и переносит предметы.
    /// </summary>
    public sealed class RewardScreen : PrefabScreenBase<IItemScreenHost>
    {
        [Tooltip("Подпись левой колонки: «Трофеи», «Тайник» или «Награда»")]
        [SerializeField]
        private Text? sourceLabel = null;

        [Tooltip("Строка денег награды; скрыта, когда награда без денег")]
        [SerializeField]
        private Text? moneyLabel = null;

        [Tooltip("Деньги игрока на плашке окна инвентаря")]
        [SerializeField]
        private Text? playerMoney = null;

        [Tooltip("Вес инвентаря игрока: по нему видно, почему предмет не помещается")]
        [SerializeField]
        private Text? playerWeight = null;

        [Tooltip("Сетка награды или добычи (слева)")]
        [SerializeField]
        private ScrollArea? rewardGrid = null;

        [Tooltip("Сетка инвентаря игрока (справа)")]
        [SerializeField]
        private ScrollArea? playerGrid = null;

        [Tooltip("Кнопка «Взять всё»")]
        [SerializeField]
        private Button? takeAll = null;

        [Tooltip("Крестик закрытия окна")]
        [SerializeField]
        private Button? close = null;

        [Tooltip("Строка сообщений о переполнении инвентаря")]
        [SerializeField]
        private Text? toast = null;

        private List<ItemInstance> _loot = new List<ItemInstance>();
        private PlayerState? _player;
        private RewardSource _source = RewardSource.Victory;
        private bool _ignoreWeight;
        private int _money;
        private Action? _onClosed;
        private Action<ItemInstance>? _onTaken;
        private ConfirmDialog? _confirm;
        private ItemContextMenu _menu = null!;
        private ItemTooltip _tooltip = null!;
        private CharacterActionsPresenter _presenter = null!;

        /// <summary>Сколько предметов ещё лежит в левой колонке.</summary>
        public int Remaining => _loot.Count;

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (close != null)
            {
                close.onClick.AddListener(AttemptClose);
            }

            if (takeAll != null)
            {
                takeAll.onClick.AddListener(TakeAll);
            }

            _confirm = Host.CreateConfirmDialog(Panel.transform);
            _menu = new ItemContextMenu(Content, Host.Ui);
            _tooltip = new ItemTooltip(WidgetHost, Content);
            _presenter = new CharacterActionsPresenter(WidgetHost, (text, ok) => SetToast(text, !ok), Rebuild);
        }

        /// <summary>Открывает окно наград и заполняет обе колонки.</summary>
        /// <param name="loot">Предметы награды, добычи или тайника; ещё не в инвентаре.</param>
        /// <param name="money">Деньги награды, уже зачисленные игроку; 0 — строку скрыть.</param>
        /// <param name="source">Откуда пришла награда: подпись левой колонки.</param>
        /// <param name="onClosed">Что выполнить после закрытия; <c>null</c> — вернуть игровой экран.</param>
        /// <param name="onTaken">Что выполнить с каждым забранным предметом; <c>null</c> — ничего.</param>
        public void Show(
            IReadOnlyList<ItemInstance> loot,
            int money,
            RewardSource source,
            Action? onClosed = null,
            Action<ItemInstance>? onTaken = null,
            bool ignoreWeight = false)
        {
            _loot = loot == null ? new List<ItemInstance>() : new List<ItemInstance>(loot);
            _money = Mathf.Max(0, money);
            _source = source;
            _ignoreWeight = ignoreWeight;
            _onClosed = onClosed;
            _onTaken = onTaken;
            _player = Host.Session?.State.Player;
            SetToast(string.Empty);
            _menu.Close();
            _tooltip.Hide();
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Rebuild();
        }

        /// <summary>Esc закрывает окно; с оставшимся лутом сначала спрашивает подтверждение.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_menu != null && _menu.IsOpen)
            {
                _menu.Close();
                return true;
            }

            if (_confirm != null && _confirm.HandleEscape())
            {
                return true;
            }

            AttemptClose();
            return true;
        }

        /// <summary>Забирает всё, что помещается: не влезшее остаётся в колонке.</summary>
        public void TakeAll()
        {
            if (_player == null)
            {
                return;
            }

            var remaining = new List<ItemInstance>(_loot.Count);
            var overflowed = false;
            ItemInstance? refused = null;
            foreach (var item in _loot)
            {
                if (!Give(item, out var overflow))
                {
                    refused ??= item;
                    remaining.Add(item);
                    continue;
                }

                overflowed |= overflow;
            }

            _loot = remaining;
            Rebuild();
            if (overflowed)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Loot_Full), true);
            }
            else if (refused != null)
            {
                ShowCannotTake(refused);
            }

            if (_loot.Count == 0)
            {
                Close();
            }
        }

        private void TakeItem(string instanceId)
        {
            var item = Find(instanceId);
            if (item == null)
            {
                return;
            }

            if (!Give(item, out var overflow))
            {
                ShowCannotTake(item);
                return;
            }

            _loot.Remove(item);
            Rebuild();
            if (overflow)
            {
                SetToast(UiText.Get(Host.Texts, UiTextKey.Loot_Full), true);
            }

            if (_loot.Count == 0)
            {
                Close();
            }
        }

        private bool Give(ItemInstance item, out bool overflow)
        {
            overflow = false;
            if (_player == null)
            {
                return false;
            }

            // Джекпот забирается целиком даже при перегрузе: лимит веса отключается
            // только на время добавления, клетки по-прежнему проверяются.
            var limit = _player.Inventory.WeightLimit;
            if (_ignoreWeight)
            {
                _player.Inventory.SetWeightLimit(double.PositiveInfinity);
            }

            var accepted = false;
            IReadOnlyList<ItemInstance> unwanted = Array.Empty<ItemInstance>();
            try
            {
                accepted = Host.Inventory.TryAdd(
                    _player.Inventory,
                    item.ItemId,
                    item.Stack,
                    item.Durability,
                    item.VariantKey,
                    out unwanted);
            }
            finally
            {
                if (_ignoreWeight)
                {
                    _player.Inventory.SetWeightLimit(limit);
                }
            }

            if (!accepted)
            {
                return false;
            }

            overflow = unwanted.Count > 0;
            _onTaken?.Invoke(item);
            return true;
        }

        /// <summary>
        /// Объясняет, почему предмет не взят: если упирается в вес — показывает вес инвентаря
        /// и предел, иначе сообщает, что в сетке нет места. Без этого отказ выглядел как
        /// «кнопка не работает».
        /// </summary>
        /// <param name="item">Предмет, который не поместился.</param>
        private void ShowCannotTake(ItemInstance item)
        {
            if (_player == null)
            {
                return;
            }

            var limit = _player.Inventory.WeightLimit;
            var total = Host.Inventory.TotalWeight(_player.Inventory);
            var weight = WeightOf(item);
            if (total + weight > limit + 1e-9)
            {
                SetToast(UiText.Format(Host.Texts, UiTextKey.Loot_Weight_Full, total, limit), true);
                return;
            }

            SetToast(UiText.Get(Host.Texts, UiTextKey.Loot_No_Room), true);
        }

        private double WeightOf(ItemInstance item)
        {
            if (!Host.Items.TryGet(item.ItemId, out var template) || template == null)
            {
                return 0;
            }

            return template.Weight * Math.Max(1, item.Stack);
        }

        private ItemInstance? Find(string instanceId)
        {
            foreach (var item in _loot)
            {
                if (string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private void AttemptClose()
        {
            if (_loot.Count == 0 || _confirm == null)
            {
                Close();
                return;
            }

            _confirm.Show(
                UiText.Get(Host.Texts, UiTextKey.Common_Confirm_Title),
                UiText.Get(Host.Texts, UiTextKey.Rewards_Lost),
                UiText.Get(Host.Texts, UiTextKey.Loot_Close),
                Close);
        }

        private void Close()
        {
            var action = _onClosed;
            _onClosed = null;
            _onTaken = null;
            _loot.Clear();
            _menu.Close();
            _tooltip.Hide();
            if (action != null)
            {
                action();
                return;
            }

            Host.ShowGame(false);
        }

        private void Rebuild()
        {
            if (sourceLabel != null)
            {
                sourceLabel.text = UiText.Get(Host.Texts, SourceKey());
            }

            if (moneyLabel != null)
            {
                moneyLabel.gameObject.SetActive(_money > 0);
                moneyLabel.text = _money > 0
                    ? UiText.Format(Host.Texts, UiTextKey.Rewards_Money, _money)
                    : string.Empty;
            }

            if (playerMoney != null)
            {
                playerMoney.text = UiText.Format(Host.Texts, UiTextKey.Common_Money, _player?.Money ?? 0);
            }

            if (playerWeight != null)
            {
                var player = _player;
                playerWeight.gameObject.SetActive(player != null);
                if (player != null)
                {
                    var total = Host.Inventory.TotalWeight(player.Inventory);
                    var limit = player.Inventory.WeightLimit;
                    playerWeight.text = UiText.Format(Host.Texts, UiTextKey.Character_Weight, total, limit);
                    playerWeight.color = total > limit + 1e-9 ? RewardPalette.Error : UiFactory.MutedTextColor;
                }
            }

            if (rewardGrid == null || playerGrid == null)
            {
                return;
            }

            // В окне лута сетка показывается, а не раскладывается в реальный инвентарь:
            // 4 крупные колонки читаются лучше 8 мелких, предметы упаковываются подряд.
            var columns = 4;
            var rewardHeight = TradeGridBuilder.Build(
                Host.Ui,
                rewardGrid.Content,
                rewardGrid.ViewportWidth,
                rewardGrid.ViewportHeight,
                BuildRewardItems(),
                columns,
                1,
                null,
                TakeItem,
                SpriteOf,
                "RewardItem",
                pack: true,
                tooltip: _tooltip);
            rewardGrid.SetContentHeight(rewardHeight);

            var playerHeight = TradeGridBuilder.Build(
                Host.Ui,
                playerGrid.Content,
                playerGrid.ViewportWidth,
                playerGrid.ViewportHeight,
                BuildPlayerItems(),
                columns,
                _player?.Inventory.Rows ?? 1,
                null,
                null,
                SpriteOf,
                "RewardPlayerItem",
                pack: true,
                tooltip: _tooltip,
                onMenu: OpenItemMenu);
            playerGrid.SetContentHeight(playerHeight);

            if (takeAll != null)
            {
                takeAll.interactable = _loot.Count > 0;
            }
        }

        private List<TradeGridItem> BuildRewardItems()
        {
            var items = new List<TradeGridItem>(_loot.Count);
            foreach (var item in _loot)
            {
                var template = ItemPresentation.Template(WidgetHost, item);
                items.Add(new TradeGridItem(
                    item.InstanceId,
                    item.ItemId,
                    template?.FootprintWidth ?? 1,
                    template?.FootprintHeight ?? 1,
                    ItemPresentation.Describe(WidgetHost, item),
                    template?.Icon,
                    item.Stack,
                    rarity: ItemRarity.Resolve(item, template),
                    instance: item,
                    template: template));
            }

            return items;
        }

        private List<TradeGridItem> BuildPlayerItems()
        {
            var items = new List<TradeGridItem>();
            if (_player == null)
            {
                return items;
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
                items.Add(new TradeGridItem(
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

            return items;
        }

        /// <summary>
        /// Открывает меню предмета инвентаря игрока по правому клику. Пункт «Выбросить» нужен,
        /// чтобы освободить место под лут, не закрывая окно наград.
        /// </summary>
        /// <param name="instanceId">Id экземпляра в инвентаре игрока.</param>
        /// <param name="position">Позиция курсора в координатах экрана.</param>
        private void OpenItemMenu(string instanceId, Vector2 position)
        {
            _tooltip.Hide();
            var entries = new List<KeyValuePair<string, Action>>
            {
                new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Discard),
                    () => _presenter.Discard(instanceId))
            };
            _menu.Open(position, entries);
        }

        private Sprite? SpriteOf(string id) => Host.Sprites.TryGet(id, out var sprite) ? sprite : null;

        private UiTextKey SourceKey() => _source switch
        {
            RewardSource.Victory => UiTextKey.Rewards_Source_Victory,
            RewardSource.Stash => UiTextKey.Rewards_Source_Stash,
            RewardSource.Loot => UiTextKey.Rewards_Source_Loot,
            _ => UiTextKey.Rewards_Source_Quest,
        };

        private void SetToast(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? RewardPalette.Error : UiFactory.ToastColor;
        }
    }
}
