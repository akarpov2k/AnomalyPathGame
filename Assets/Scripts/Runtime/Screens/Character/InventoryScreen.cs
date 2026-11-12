#nullable enable
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Окно «инвентарь» по прототипу: слева персонаж (костюм в полный рост, слоты снаряжения,
    /// артефакты, панель защиты с кнопкой-глазом, полоса состояния, ник и уровень), справа инвентарь
    /// с сеткой, валютой и фильтрами внизу. Разметка, слоты, строки состояния и кнопки приходят
    /// из префаба; код заполняет динамику и обрабатывает перетаскивание и контекстное меню предмета.
    /// </summary>
    public sealed partial class InventoryScreen : PrefabScreenBase<IItemScreenHost>, ICharacterFrameHost
    {
        [Tooltip("Слоты снаряжения в порядке перечисления EquipmentSlot: основное оружие, второе, шлем, броня, рюкзак, пояс")]
        [SerializeField]
        private Image[] slotZones = null!;

        [Tooltip("Области предметов внутри слотов в том же порядке")]
        [SerializeField]
        private RectTransform[] slotBodies = null!;

        [Tooltip("Иконка надетого костюма в полный рост")]
        [SerializeField]
        private Image figure = null!;

        [Tooltip("Контейнер слотов артефактов")]
        [SerializeField]
        private RectTransform artifactRow = null!;

        [Tooltip("Неактивный образец слота артефакта")]
        [SerializeField]
        private Image artifactTemplate = null!;

        [Tooltip("Контейнер быстрых слотов в левой колонке персонажа")]
        [SerializeField]
        private RectTransform quickRow = null!;

        [Tooltip("Неактивный образец быстрого слота")]
        [SerializeField]
        private Image quickTemplate = null!;

        [Tooltip("Подписи значений состояния: здоровье, еда, вода, выносливость")]
        [SerializeField]
        private Text[] statValues = null!;

        [Tooltip("Контейнер иконок травм")]
        [SerializeField]
        private RectTransform injuries = null!;

        [Tooltip("Панель защиты: защита по типам урона")]
        [SerializeField]
        private GameObject statsPanel = null!;

        [Tooltip("Контейнер строк «Защита» на панели")]
        [SerializeField]
        private RectTransform statsDefenseRows = null!;

        [Tooltip("Неактивный образец строки защиты")]
        [SerializeField]
        private RectTransform statsRowTemplate = null!;

        [Tooltip("Кнопка-глаз: показать или скрыть панель защиты")]
        [SerializeField]
        private Button statsEye = null!;

        [Tooltip("Иконка кнопки-глаза")]
        [SerializeField]
        private Image statsEyeIcon = null!;

        [Tooltip("Ник персонажа")]
        [SerializeField]
        private Text nickname = null!;

        [Tooltip("Уровень персонажа")]
        [SerializeField]
        private Text level = null!;

        [Tooltip("Окно инвентаря: принимает брошенные мимо сетки предметы")]
        [SerializeField]
        private Image inventoryFrame = null!;

        [Tooltip("Область прокрутки сетки инвентаря внутри окна")]
        [SerializeField]
        private ScrollArea gridArea = null!;

        [Tooltip("Подпись валюты в плашке окна инвентаря")]
        [SerializeField]
        private Text money = null!;

        [Tooltip("Подпись веса инвентаря: текущий и предельный вес")]
        [SerializeField]
        private Text? weight = null;

        [Tooltip("Строка переполнения: предметы, которым не хватило места")]
        [SerializeField]
        private Text overflow = null!;

        [Tooltip("Кнопки фильтров: все, оружие, броня, расходники, прочее")]
        [SerializeField]
        private Button[] filterButtons = null!;

        [Tooltip("Крестик выхода в игровое поле")]
        [SerializeField]
        private Button close = null!;

        [Tooltip("Кнопка перехода к талантам")]
        [SerializeField]
        private Button talents = null!;

        [Tooltip("Строка сообщений о результате действия")]
        [SerializeField]
        private Text message = null!;

        [Tooltip("Подпись, которая летит за курсором при перетаскивании")]
        [SerializeField]
        private Text ghost = null!;

        [Tooltip("Подпись «Нет игры», если сессия ещё не загружена")]
        [SerializeField]
        private Text noGame = null!;

        private readonly ItemDragContext _drag = new ItemDragContext();
        private ItemContextMenu _menu = null!;
        private ItemTooltip _tooltip = null!;
        private CharacterActionsPresenter _presenter = null!;
        private CharacterInventoryFrame _inventory = null!;
        private CharacterStatsStrip _stats = null!;
        private CharacterStatsPanel _statsPanel = null!;

        /// <inheritdoc />
        protected override void OnBound()
        {
            _menu = new ItemContextMenu(Content, Host.Ui);
            _tooltip = new ItemTooltip(WidgetHost, Content);
            _presenter = new CharacterActionsPresenter(WidgetHost, SetMessage, Show);
            _inventory = new CharacterInventoryFrame(WidgetHost, this, _tooltip);
            _stats = new CharacterStatsStrip(WidgetHost, statValues, injuries);
            _statsPanel = new CharacterStatsPanel(WidgetHost, (RectTransform)statsPanel.transform, statsDefenseRows, statsRowTemplate);
            close.onClick.AddListener(BackToGame);
            talents.onClick.AddListener(Host.ShowTalents);
            statsEye.onClick.AddListener(ToggleStats);
            SetStatsVisible(statsPanel.activeSelf);
            BindFilters();
            BindDropZones();
        }

        /// <inheritdoc />
        public ItemDragContext Drag => _drag;

        /// <inheritdoc />
        public Text Ghost => ghost;

        /// <summary>Esc возвращает из инвентаря в игровое поле.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            BackToGame();
            return true;
        }

        /// <summary>Показывает окно и заполняет его текущим состоянием игрока.</summary>
        public void Show()
        {
            _menu.Close();
            _tooltip.Hide();
            _drag.End();
            ghost.gameObject.SetActive(false);
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();

            var player = Player;
            noGame.gameObject.SetActive(player == null);
            message.text = string.Empty;
            if (player == null)
            {
                FillEmpty();
                return;
            }

            RefreshFilters();
            FillHeader(player);
            FillSlots(player);
            FillArtifacts(player);
            FillQuickSlots(player);
            CharacterFigure.Apply(WidgetHost, figure, CharacterFigure.WornArmor(player), CharacterPalette.FigureArea);
            _stats.Build(player);
            _statsPanel.Build(player);
            FillGrid(player);
        }

        private PlayerState? Player => Host.Session?.State.Player;

        private void BackToGame() => Host.ShowGame(announce: false);

        /// <summary>Показывает или скрывает панель защиты по кнопке-глазу.</summary>
        private void ToggleStats() => SetStatsVisible(!statsPanel.activeSelf);

        /// <summary>Ставит панель защиты видимой или скрытой и приглушает иконку глаза.</summary>
        /// <param name="visible">Показывать ли панель.</param>
        private void SetStatsVisible(bool visible)
        {
            statsPanel.SetActive(visible);
            statsEyeIcon.color = visible ? Color.white : CharacterPalette.StatsEyeMuted;
            if (visible)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)statsPanel.transform);
            }
        }

        private void SetMessage(string text, bool ok)
        {
            message.text = text;
            message.color = ok ? UiFactory.ToastColor : CharacterPalette.Error;
        }
    }
}
