#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Большой хаб — локация со всеми действиями по прототипу: на стене карта мира, дверь
    /// (выход на маршрут) и доска объявлений, внизу значки меню, торговцев, отдыха и инвентаря.
    /// Панели действий нет: всё открывается предметами и значками.
    /// </summary>
    public sealed class BigHubScreen : PrefabScreenBase<IHubScreenHost>
    {
        [Tooltip("Картинка интерьера хаба")]
        [SerializeField]
        private ScreenBackdrop? background = null;

        [Tooltip("Стрелка часов")]
        [SerializeField]
        private RectTransform? clockHand = null;

        [Tooltip("Подпись дня и времени")]
        [SerializeField]
        private Text? clockTime = null;

        [Tooltip("Карта мира на стене")]
        [SerializeField]
        private Button? map = null;

        [Tooltip("Дверь: выход из хаба на маршрут")]
        [SerializeField]
        private Button? door = null;

        [Tooltip("Доска объявлений")]
        [SerializeField]
        private Button? board = null;

        [Tooltip("Шестерёнка: меню игры")]
        [SerializeField]
        private Button? gear = null;

        [Tooltip("Мешочек: торговцы хаба")]
        [SerializeField]
        private Button? traders = null;

        [Tooltip("Полумесяц: отдых")]
        [SerializeField]
        private Button? rest = null;

        [Tooltip("Рюкзак: инвентарь")]
        [SerializeField]
        private Button? inventory = null;

        [Tooltip("Фигура NPC хаба: квестовый персонаж, с которым можно поговорить")]
        [SerializeField]
        private GameObject? npcFigure = null;

        [Tooltip("Полоса опыта внизу экрана")]
        [SerializeField]
        private Image? xpBar = null;

        [Tooltip("Заполнение полосы опыта")]
        [SerializeField]
        private Image? xpFill = null;

        [Tooltip("Подсказка с названием объекта")]
        [SerializeField]
        private HubTooltip? tooltip = null;

        [Tooltip("Всплывающее сообщение экрана")]
        [SerializeField]
        private Text? toast = null;

        private TraderSwitchMenu? _menu;
        private PlayObject? _npc;
        private string? _boardId;

        /// <summary>Поля вокруг вписанной сцены чёрные, а не зелёные.</summary>
        protected override ScreenTheme Theme => ScreenTheme.Void;

        /// <summary>Открыта ли выпадашка торговцев.</summary>
        public bool TradersShown => _menu != null && _menu.IsShown;

        /// <summary>Подсказка экрана.</summary>
        public HubTooltip? Tooltip => tooltip;

        /// <summary>Показывает большой хаб и перестраивает его по текущему миру.</summary>
        public void Show()
        {
            ApplyTheme();
            ApplyBackground();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        /// <summary>Показывает сообщение на полосе хаба.</summary>
        /// <param name="text">Текст.</param>
        /// <param name="error">Показать как ошибку.</param>
        public void ShowToast(string text, bool error = false)
        {
            if (toast == null)
            {
                return;
            }

            toast.text = text;
            toast.color = error ? PlayPalette.Error : UiFactory.ToastColor;
        }

        /// <summary>Esc закрывает выпадашку торговцев, иначе открывает меню игры.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_menu != null && _menu.IsShown)
            {
                _menu.Hide();
                return true;
            }

            if (Host.Session == null)
            {
                return false;
            }

            Host.ShowGameMenu();
            return true;
        }

        /// <inheritdoc />
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

            _menu = new TraderSwitchMenu(WidgetHost, Content, OpenTraderRoom);
            if (npcFigure != null)
            {
                _npc = new PlayObject(npcFigure, "HubNpc");
            }

            Wire(map, OpenMap);
            Wire(door, LeaveHub);
            Wire(board, OpenBoard);
            Wire(gear, OpenMenu);
            Wire(traders, ToggleTraders);
            Wire(rest, OpenRest);
            Wire(inventory, OpenInventory);
        }

        private void OpenMap() => Host.ShowMap();

        private void OpenMenu() => Host.ShowGameMenu();

        private void OpenInventory() => Host.ShowInventory();

        private void OpenBoard()
        {
            if (_boardId != null)
            {
                Host.ShowQuests(_boardId);
            }
        }

        private void LeaveHub() => GameFlow.LeaveHub(WidgetHost, () => Host.ShowGame(false), FlowToast);

        private void OpenRest() => GameFlow.TryOpenRest(WidgetHost, Host.RestDialog, ConfirmRest, FlowToast);

        private void ConfirmRest(int hours) => GameFlow.Rest(WidgetHost, hours, Show, FlowToast);

        /// <summary>Сообщение действия: в хабе — своя полоса, после выхода — полоса нового экрана.</summary>
        private void FlowToast(string text, bool error)
        {
            if (Panel.activeInHierarchy)
            {
                ShowToast(text, error);
                return;
            }

            Host.Toast(text);
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

            var available = AvailableTraders();
            if (available.Count == 0)
            {
                return;
            }

            _menu.Show(available, string.Empty);
        }

        private void OpenTraderRoom(string actorId)
        {
            _menu?.Hide();
            Host.ShowTraderRoom(actorId);
        }

        private IReadOnlyList<string> AvailableTraders()
        {
            var result = new List<string>();
            var session = Host.Session;
            if (session == null)
            {
                return result;
            }

            var location = Host.Locations.Get(session.State.World.CurrentLocation.TemplateId);
            foreach (var traderId in location.Traders)
            {
                if (Host.Trade.IsAvailable(session, traderId))
                {
                    result.Add(traderId);
                }
            }

            return result;
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
                SetEnabled(map, false);
                SetEnabled(door, false);
                SetEnabled(board, false);
                SetEnabled(gear, false);
                SetEnabled(traders, false);
                SetEnabled(rest, false);
                SetEnabled(inventory, false);
                _boardId = null;
                _menu?.Hide();
                _npc?.Hide();
                RefreshXp(null);
                ShowToast(UiText.Get(Host.Texts, UiTextKey.Common_No_Game), true);
                return;
            }

            var world = session.State.World;
            var location = world.CurrentLocation;
            var template = Host.Locations.Get(location.TemplateId);

            RefreshClock(world.Clock);
            RefreshXp(session.State.Player);
            _boardId = location.InstanceId;
            SetEnabled(map, true);
            SetEnabled(door, !session.InCombat);
            SetEnabled(board, template.QuestBoard != null);
            SetEnabled(gear, true);
            SetEnabled(traders, AvailableTraders().Count > 0);
            SetEnabled(rest, Host.Sessions.GetRestQuote(session).Available);
            SetEnabled(inventory, true);
            _menu?.Hide();
            tooltip?.Clear();
            RefreshNpc(session);
        }

        /// <summary>
        /// Показывает на большом хабе квестового NPC: у хаба пул объектов — только уникальные
        /// NPC, которых добавляет квест (например, Гвоздь в Депо). Клик открывает диалог,
        /// как у NPC на локации.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        private void RefreshNpc(GameSession session)
        {
            if (_npc == null)
            {
                return;
            }

            foreach (var item in session.State.World.CurrentLocation.Objects)
            {
                if (item is not NpcLocationObject npc ||
                    npc.Looted ||
                    !Host.Actors.Contains(npc.ActorId))
                {
                    continue;
                }

                var actor = Host.Actors.Get(npc.ActorId);
                var dialogId = actor.DialogId;
                if (dialogId == null || !Host.Dialogs.CanStart(session, dialogId))
                {
                    continue;
                }

                var actorId = npc.ActorId;
                var name = Host.Texts.Get(actor.NameKey);
                _npc.Show(
                    name,
                    UiText.Format(Host.Texts, UiTextKey.Game_Talk_Action, name),
                    () => Host.ShowDialog(actorId, dialogId),
                    true,
                    "Object_" + npc.SpawnId,
                    Icon(actor.Portrait));
                return;
            }

            _npc.Hide();
        }

        /// <summary>Текстура из каталога спрайтов по id; <c>null</c>, если id пуст или картинки нет.</summary>
        /// <param name="id">Id картинки в каталоге.</param>
        /// <returns>Спрайт или <c>null</c>.</returns>
        private Sprite? Icon(string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            return Host.Sprites.TryGet(id!, out var sprite) ? sprite : null;
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

        private void RefreshXp(AnomalyPath.Core.Player.PlayerState? player)
        {
            if (xpBar == null || xpFill == null)
            {
                return;
            }

            xpBar.gameObject.SetActive(player != null);
            if (player == null)
            {
                xpFill.gameObject.SetActive(false);
                return;
            }

            var level = Math.Max(1, player.Level);
            var required = Math.Max(1, Host.Progression.RequiredXp(level));
            var levelFloor = Host.Progression.TotalXpToLevel(level);
            var into = Math.Min(required, Math.Max(0, player.Xp - levelFloor));
            var width = xpBar.rectTransform.rect.width * into / required;
            xpFill.rectTransform.sizeDelta = new Vector2(width, 0f);
            xpFill.gameObject.SetActive(width > 0f);
        }

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
