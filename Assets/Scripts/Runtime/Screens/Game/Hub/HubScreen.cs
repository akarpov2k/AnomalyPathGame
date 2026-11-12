#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Trade;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Экран хаба: интерьер во весь экран, по центру выбранный торговец за передним
    /// столом-стойкой, слева дверь (обычная и со свечением) и часы, внизу — настройки
    /// слева и инвентарь справа. При входе поверх комнаты открывается диалог торговца:
    /// «Поговорить», «Поторгуем?» и «Прощай»; клик по фигуре открывает его снова.
    /// </summary>
    public sealed partial class HubScreen : PrefabScreenBase<IHubScreenHost>
    {
        [Tooltip("Картинка интерьера: тема задника берётся из фона текущей локации")]
        [SerializeField]
        private ScreenBackdrop? background = null;

        [Tooltip("Стрелка часов, направленная вверх в исходном положении")]
        [SerializeField]
        private RectTransform? clockHand = null;

        [Tooltip("Подпись времени и дня рядом с часами")]
        [SerializeField]
        private Text? clockTime = null;

        [Tooltip("Шестерёнка: меню игры")]
        [SerializeField]
        private Button? gear = null;

        [Tooltip("Дверь слева от торговца: выход из хаба")]
        [SerializeField]
        private Button? door = null;

        [Tooltip("Рюкзак: окно инвентаря")]
        [SerializeField]
        private Button? inventory = null;

        [Tooltip("Мешочек: выпадашка выбора торговца")]
        [SerializeField]
        private Button? traders = null;

        [Tooltip("Значок ремонта; виден только у торговца с ремонтом")]
        [SerializeField]
        private Button? repair = null;

        [Tooltip("Картинка выбранного торговца в полный рост за стойкой")]
        [SerializeField]
        private Image? merchant = null;

        [Tooltip("Кликабельная зона торговца; меню откроется позже")]
        [SerializeField]
        private Button? merchantButton = null;

        [Tooltip("Зона торговца: подсказка с его именем")]
        [SerializeField]
        private HubZone? merchantZone = null;

        [Tooltip("Подсказка с названием объекта под курсором")]
        [SerializeField]
        private HubTooltip? tooltip = null;

        [Tooltip("Всплывающее сообщение экрана")]
        [SerializeField]
        private Text? toast = null;

        private string? _traderId;
        private string? _selectedTraderId;
        private TraderSwitchMenu? _menu;
        private TraderDialogOverlay? _dialog;

        /// <summary>Id торговца, который сейчас стоит за стойкой, или <c>null</c>.</summary>
        public string? TraderId => _traderId;

        /// <summary>Открыта ли выпадашка выбора торговца.</summary>
        public bool TradersShown => _menu != null && _menu.IsShown;

        /// <summary>Диалог торговца поверх комнаты; <c>null</c> до привязки.</summary>
        public TraderDialogOverlay? Dialog => _dialog;

        /// <summary>Тёмный фон за полями сцены: полосы по краям не должны быть зелёными.</summary>
        protected override ScreenTheme Theme => ScreenTheme.Void;

        /// <summary>Подсказка экрана с названием объекта под курсором.</summary>
        public HubTooltip? Tooltip => tooltip;

        /// <summary>Подпись времени рядом с часами.</summary>
        public string TimeText => clockTime != null ? clockTime.text : string.Empty;

        /// <summary>Связывает кнопки префаба с действиями и готовит значки.</summary>
        protected override void OnBound()
        {
            if (tooltip != null)
            {
                tooltip.Initialize(Host.Texts);
            }

            if (background != null)
            {
                background.Initialize(WidgetHost);
            }

            Wire(gear, OpenMenu);
            Wire(door, LeaveHub);
            Wire(inventory, OpenInventory);
            Wire(merchantButton, OpenMerchant);
            Wire(traders, ToggleTraders);
            Wire(repair, Repair);
            _menu = new TraderSwitchMenu(WidgetHost, Content, SelectTrader);
            _menu.AnchorLeft(TraderMetrics.BottomMargin);
            _dialog = new TraderDialogOverlay(WidgetHost, Content);
            door?.GetComponent<HubZone>()?.SetNameKey(UiText.Code(UiTextKey.Hub_Back));
        }

        /// <summary>Показывает комнату торговца и перестраивает её по текущему состоянию мира.</summary>
        /// <param name="actorId">Кого поставить за стойку; <c>null</c> — первого доступного торговца.</param>
        public void Show(string? actorId = null)
        {
            _selectedTraderId = actorId;
            ApplyTheme();
            ApplyBackground();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
            _dialog?.ShowMenu(_traderId);
        }

        /// <summary>Показывает всплывающее сообщение экрана.</summary>
        /// <param name="text">Текст сообщения.</param>
        /// <param name="error">Показать сообщение как ошибку.</param>
        public void ShowToast(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? PlayPalette.Error : UiFactory.ToastColor;
        }

        /// <summary>Esc закрывает выпадашку, затем разговор, затем возвращает из комнаты в большой хаб.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_menu != null && _menu.IsShown)
            {
                _menu.Hide();
                return true;
            }

            if (_dialog != null && _dialog.Back())
            {
                return true;
            }

            if (Host.Session == null)
            {
                return false;
            }

            _dialog?.Hide();
            Host.ShowGame(false);
            return true;
        }

        private void ApplyBackground()
        {
            if (background == null)
            {
                return;
            }

            var location = Host.Session?.State.World.CurrentLocation;
            background.Apply(ScreenTheme.LocationFull(location?.Background));
        }

        private void Refresh()
        {
            var session = Host.Session;
            if (session == null)
            {
                RefreshClock(null);
                SetEnabled(gear, false);
                SetEnabled(door, false);
                SetEnabled(inventory, false);
                SetEnabled(traders, false);
                if (repair != null)
                {
                    repair.gameObject.SetActive(false);
                }

                _menu?.Hide();
                _dialog?.Hide();
                HideMerchant();
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            var world = session.State.World;
            var location = world.CurrentLocation;
            var template = Host.Locations.Get(location.TemplateId);

            RefreshClock(world.Clock);
            SetEnabled(gear, true);
            SetEnabled(door, !session.InCombat);
            SetEnabled(inventory, true);
            SetEnabled(traders, AvailableTraders(session, template).Count > 0);
            ShowMerchant(session, template);
            ShowRepair(session);
            _menu?.Hide();

            if (tooltip != null)
            {
                tooltip.Clear();
            }
        }

        private void RefreshClock(WorldClock? clock)
        {
            if (clockHand == null || clockTime == null)
            {
                return;
            }

            if (clock == null)
            {
                clockHand.localEulerAngles = Vector3.zero;
                clockTime.text = string.Empty;
                return;
            }

            clockHand.localEulerAngles = new Vector3(0f, 0f, PlayClock.Angle(clock));
            var hour = Mathf.FloorToInt((float)clock.Hours);
            var minute = Mathf.FloorToInt((float)((clock.Hours - hour) * 60d));
            clockTime.text = UiText.Format(Host.Texts, UiTextKey.Game_Clock, clock.Day, hour, minute);
        }

        private void ShowMerchant(GameSession session, LocationTemplate template)
        {
            _traderId = FirstTrader(session, template);
            if (_traderId == null)
            {
                HideMerchant();
                return;
            }

            var actor = Host.Actors.Get(_traderId);
            if (merchant != null)
            {
                merchant.sprite = MerchantSprite(actor);
                merchant.gameObject.SetActive(true);
            }

            SetEnabled(merchantButton, true);
            merchantZone?.SetNameKey(actor.NameKey);
        }

        private void HideMerchant()
        {
            _traderId = null;
            if (merchant != null)
            {
                merchant.gameObject.SetActive(false);
            }

            SetEnabled(merchantButton, false);
            merchantZone?.SetNameKey(string.Empty);
        }

        private void ToggleTraders()
        {
            if (_menu == null)
            {
                return;
            }

            if (_menu.IsShown)
            {
                _menu.Hide();
                return;
            }

            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var location = Host.Locations.Get(session.State.World.CurrentLocation.TemplateId);
            var available = AvailableTraders(session, location);
            if (available.Count == 0)
            {
                return;
            }

            _menu.Show(available, _traderId ?? string.Empty);
        }

        private void SelectTrader(string actorId) => Show(actorId);

        private IReadOnlyList<string> AvailableTraders(GameSession session, LocationTemplate template)
        {
            var result = new List<string>();
            foreach (var traderId in template.Traders)
            {
                if (Host.Trade.IsAvailable(session, traderId))
                {
                    result.Add(traderId);
                }
            }

            return result;
        }

        private void ShowRepair(GameSession session)
        {
            if (repair == null)
            {
                return;
            }

            var quote = _traderId == null ? null : Host.Repair.GetQuote(session, _traderId);
            repair.gameObject.SetActive(quote != null && quote.Available);
            if (quote == null || !quote.Available)
            {
                return;
            }

            // Кнопка остаётся нажимаемой и без работы: иначе клик молчал, и было непонятно,
            // что ремонт вообще доступен. Отказ придёт сообщением «Нечего ремонтировать».
            repair.interactable = true;
            var zone = repair.GetComponent<HubZone>();
            if (zone != null)
            {
                zone.SetNameText(quote.HasWork
                    ? UiText.Format(Host.Texts, UiTextKey.Trader_Repair_Cost, quote.Cost)
                    : UiText.Get(Host.Texts, UiTextKey.Trader_Repair));
            }
        }

        private void Repair()
        {
            var session = Host.Session;
            if (session == null || _traderId == null)
            {
                return;
            }

            var result = Host.Repair.Repair(session, _traderId);
            if (result.Succeeded)
            {
                ShowToast(UiText.Format(
                    Host.Texts, UiTextKey.Trader_Repair_Done, result.RepairedCount, result.Cost));
                Refresh();
                return;
            }

            ShowToast(UiText.Get(Host.Texts, KeyForRepair(result.Status)), true);
        }

        private static UiTextKey KeyForRepair(RepairStatus status) => status switch
        {
            RepairStatus.NothingToRepair => UiTextKey.Trader_Result_Nothing_To_Repair,
            RepairStatus.NotEnoughMoney => UiTextKey.Trader_Result_Not_Enough_Money,
            RepairStatus.NotMechanic => UiTextKey.Trader_Result_Unavailable,
            RepairStatus.TraderNotFound => UiTextKey.Trader_Result_Unavailable,
            RepairStatus.Unavailable => UiTextKey.Trader_Result_Unavailable,
            _ => UiTextKey.Trader_Result_Failed
        };

        private static void Wire(Button? button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private static void SetEnabled(Button? button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }
    }
}
