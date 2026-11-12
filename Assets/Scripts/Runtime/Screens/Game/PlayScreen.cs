#nullable enable
using System;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Основной игровой экран из префаба: полоса неба, дорога локации, объекты сцены
    /// (не больше двух) и противник по центру. Клик по объекту сразу выполняет его
    /// действие; панели действий нет. Внизу — полоса опыта, меню, отдых, возврат в хаб
    /// и инвентарь. Префаб хранит вёрстку, код заполняет динамику.
    /// </summary>
    public sealed partial class PlayScreen : PrefabScreenBase<IPlayScreenHost>
    {
        /// <summary>Старые PlayMode-тесты проверяют обычный экран; в игре всегда <c>false</c>.</summary>
        public static bool LegacyModeForTests { get; set; }

        [Tooltip("Сцена локации: дорога, небо и переходы")]
        [SerializeField]
        private PlayScene? scene = null;

        [Tooltip("Корень часов")]
        [SerializeField]
        private GameObject? clockRoot = null;

        [Tooltip("Левое место объекта сцены")]
        [SerializeField]
        private GameObject? objectLeftRoot = null;

        [Tooltip("Правое место объекта сцены")]
        [SerializeField]
        private GameObject? objectRightRoot = null;

        [Tooltip("Место противника по центру")]
        [SerializeField]
        private GameObject? enemyRoot = null;

        [Tooltip("Шестерёнка: меню игры")]
        [SerializeField]
        private Button? gear = null;

        [Tooltip("Полумесяц: отдых")]
        [SerializeField]
        private Button? restButton = null;

        [Tooltip("Рюкзак: инвентарь")]
        [SerializeField]
        private Button? backpack = null;

        [Tooltip("Кнопка «В хаб»: видна только на пути")]
        [SerializeField]
        private Button? returnButton = null;

        [Tooltip("Плашка полосы опыта")]
        [SerializeField]
        private Image? bar = null;

        [Tooltip("Заполнение полосы опыта")]
        [SerializeField]
        private Image? fill = null;

        [Tooltip("Подсказка уровня и опыта")]
        [SerializeField]
        private Text? tooltip = null;

        [Tooltip("Всплывающее сообщение")]
        [SerializeField]
        private Text? toastText = null;

        [Tooltip("Движущийся задник режима похода; пусто — дорога локации")]
        [SerializeField]
        private Sprite? walkBackdrop = null;

        [Tooltip("Слой режима похода из префаба WalkLane")]
        [SerializeField]
        private WalkLaneView? walkLane = null;

        [Tooltip("Дорога режима похода; пусто — старая линия земли")]
        [SerializeField]
        private Sprite? walkRoad = null;

        [Tooltip("Включить режим похода (для пилота; F8 переключает на ходу)")]
        [SerializeField]
        private bool walkMode = true;

        [Tooltip("Скорость ходьбы режима похода, единиц канваса в секунду")]
        [SerializeField]
        private float walkSpeed = 420f;

        [Tooltip("Длина дороги режима похода")]
        [SerializeField]
        private float walkLaneLength = 7200f;

        [Tooltip("Где стоит персонаж по X в режиме похода")]
        [SerializeField]
        private float walkPlayerX = 420f;

        [Tooltip("Линия земли режима похода")]
        [SerializeField]
        private float walkGroundY = 180f;

        [Tooltip("Радиус взаимодействия режима похода")]
        [SerializeField]
        private float walkInteractRange = 160f;

        [Tooltip("Первый объект на дороге режима похода")]
        [SerializeField]
        private float walkObjectStartX = 2200f;

        [Tooltip("Расстояние между объектами режима похода")]
        [SerializeField]
        private float walkObjectGap = 500f;

        private IPlayScreenHost _host = null!;
        private PlayClock _clock = null!;
        private PlayScene _scene = null!;
        private PlayObject _objectLeft = null!;
        private PlayObject _objectRight = null!;
        private PlayObject _enemy = null!;
        private Text _toast = null!;
        private Image _xpBar = null!;
        private Image _xpFill = null!;
        private Text _xpTooltip = null!;
        private Button _rest = null!;
        private Button _returnToHub = null!;
        private LocationObject? _leftObject;
        private LocationObject? _rightObject;

        /// <inheritdoc />
        protected override void OnBound()
        {
            _host = Host;
            _scene = scene ?? throw Missing("PlayScene");
            _scene.SetSprites(Host.Sprites);
            _clock = new PlayClock(Require(clockRoot, "Clock"), Host.Texts);
            _objectLeft = new PlayObject(Require(objectLeftRoot, "ObjectLeft"), "ObjectLeft");
            _objectRight = new PlayObject(Require(objectRightRoot, "ObjectRight"), "ObjectRight");
            _enemy = new PlayObject(Require(enemyRoot, "Enemy"), "Enemy");

            _toast = toastText ?? throw Missing("Toast");
            _xpBar = bar ?? throw Missing("PlayerXpBar");
            _xpFill = fill ?? throw Missing("PlayerXpFill");
            _xpTooltip = tooltip ?? throw Missing("XpTooltip");
            _rest = restButton ?? throw Missing("Rest");
            _returnToHub = returnButton ?? throw Missing("ReturnToHub");

            Wire(gear, Host.ShowGameMenu);
            Wire(backpack, Host.ShowInventory);
            Wire(restButton, OpenRest);
            Wire(returnButton, ReturnToHub);
            AttachXpTooltip();
        }

        private static GameObject Require(GameObject? part, string name) =>
            part != null ? part : throw Missing(name);

        private static InvalidOperationException Missing(string part) =>
            new InvalidOperationException(CoreText.Format(CoreErrors.PrefabPartMissing, part));

        private static void Wire(Button? button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private void AttachXpTooltip()
        {
            var trigger = _xpBar.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = _xpBar.gameObject.AddComponent<EventTrigger>();
            }

            AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => ShowXpTooltip(true));
            AddPointerEvent(trigger, EventTriggerType.PointerExit, () => ShowXpTooltip(false));
        }

        private static void AddPointerEvent(EventTrigger trigger, EventTriggerType type, Action action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        /// <summary>Фон экрана — картинка текущей локации во всю яркость.</summary>
        protected override ScreenTheme Theme
        {
            get
            {
                var world = Host.Session?.State.World;
                return world == null
                    ? ScreenTheme.Default
                    : ScreenTheme.LocationFull(world.CurrentLocation.Background);
            }
        }

        /// <summary>Показывает экран и перестраивает его по текущему состоянию.</summary>
        public void Show()
        {
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        /// <summary>Esc открывает меню игры.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_host.Session == null)
            {
                return false;
            }

            _host.ShowGameMenu();
            return true;
        }

        private void Refresh()
        {
            var session = _host.Session;
            if (session == null)
            {
                _clock.Hide();
                _scene.SetVisible(false);
                HideFigures();
                HideXp();
                SetEnabled(_rest, false);
                _returnToHub.gameObject.SetActive(false);
                ShowToast(UiText.Get(_host.Texts, UiTextKey.Common_No_Game), error: true);
                return;
            }

            var world = session.State.World;
            var location = world.CurrentLocation;
            var template = _host.Locations.Get(location.TemplateId);

            _clock.Show(world.Clock);
            _scene.SetVisible(true);
            _scene.SetRoad(template.RoadArt ?? PlaySceneArt.DefaultRoad);
            _scene.SetSky(PlaySceneArt.Sky(world.Clock.Phase));
            _host.Sessions.RollLocationStash(session);
            HideFigures();

            if (session.InCombat)
            {
                HideWalk();
                _scene.HideTransitions();
                SetEnabled(_rest, false);
                _returnToHub.gameObject.SetActive(false);
                RefreshXp(session.State.Player);
                return;
            }

            if (!LegacyModeForTests && walkMode && (_host.Rules.Rules?.Walk.Enabled ?? false))
            {
                _scene.HideTransitions();
                BuildWalk(session, location, template);
                RefreshXp(session.State.Player);
                var walkRest = _host.Sessions.GetRestQuote(session);
                SetEnabled(_rest, walkRest.Available);
                _returnToHub.gameObject.SetActive(false);
                return;
            }

            HideWalk();
            _scene.SetTransitions(
                BuildTransitions(_host.Sessions.AvailableEdges(session), world, out var forwardIndex),
                forwardIndex);
            BuildFigures(session, location, template);
            RefreshXp(session.State.Player);

            var rest = _host.Sessions.GetRestQuote(session);
            SetEnabled(_rest, rest.Available);
            var canReturn = location.Subtype != LocationSubtype.Hub;
            _returnToHub.gameObject.SetActive(canReturn);
            SetEnabled(_returnToHub, true);
        }

        /// <summary>
        /// Раскладывает объекты локации по двум местам сцены: слева — первый объект пула,
        /// справа — второй объект, а если есть тайник, он занимает правое место. Действующие
        /// объекты идут первыми, раньше показанные остаются на своих местах, пустые и забранные
        /// показываются неактивными и занимают место, только если оно свободно.
        /// </summary>
        /// <param name="session">Текущая сессия.</param>
        /// <param name="location">Текущая локация.</param>
        private void RefreshXp(AnomalyPath.Core.Player.PlayerState? player)
        {
            if (player == null)
            {
                HideXp();
                return;
            }

            _xpBar.gameObject.SetActive(true);
            var level = Math.Max(1, player.Level);
            var required = Math.Max(1, _host.Progression.RequiredXp(level));
            var levelFloor = _host.Progression.TotalXpToLevel(level);
            var into = Math.Min(required, Math.Max(0, player.Xp - levelFloor));
            var width = _xpBar.rectTransform.rect.width * into / required;
            _xpFill.rectTransform.sizeDelta = new Vector2(width, 0f);
            _xpFill.gameObject.SetActive(width > 0f);
            _xpTooltip.text =
                UiText.Format(_host.Texts, UiTextKey.Game_Level, level) +
                "\n" +
                UiText.Format(_host.Texts, UiTextKey.Game_Xp_Line, into, required);
            _xpTooltip.gameObject.SetActive(false);
        }

        private void HideXp()
        {
            _xpBar.gameObject.SetActive(false);
            _xpFill.gameObject.SetActive(false);
            _xpTooltip.gameObject.SetActive(false);
        }

        private static void SetEnabled(Button? button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        /// <summary>Показывает текст подсказки полосы опыта: уровень и опыт текущего уровня.</summary>
        /// <param name="visible">Показывать ли подсказку.</param>
        private void ShowXpTooltip(bool visible)
        {
            _xpTooltip.gameObject.SetActive(visible && _xpBar.gameObject.activeSelf);
        }
    }
}
