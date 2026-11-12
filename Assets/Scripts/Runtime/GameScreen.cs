#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Localization;
using AnomalyPath.Runtime.Audio;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Screens.Dialogs;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Screens.Map;
using AnomalyPath.Runtime.Screens.Minigames;
using AnomalyPath.Runtime.Screens.Quests;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Screens.Talents;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace AnomalyPath.Runtime
{
    /// <summary>
    /// Точка входа интерфейса: создаёт канвас и экраны, переключает их и связывает с сервисами.
    /// Сами панели живут в отдельных классах <c>Screens</c>; здесь только навигация.
    /// </summary>
    public sealed partial class GameScreen : MonoBehaviour, IUiHost,
        IItemScreenHost, IGameMenuScreenHost, ICombatScreenHost, IHubScreenHost, IPlayScreenHost,
        IMapScreenHost, IQuestScreenHost, IQuestJournalScreenHost, ITalentScreenHost, ITradeScreenHost
    {
        [Tooltip("Каталог спрайтов: иконки предметов и фоны локаций. Пусто — цветные заглушки")]
        [SerializeField]
        private SpriteCatalog? spriteCatalog = null;

        [Tooltip("Каталог звуков: эмбиенты локаций и музыка. Пусто — игра без звука")]
        [SerializeField]
        private AudioCatalog? audioCatalog = null;

        [Tooltip("Слой звука на сцене: источники эмбиента и музыки. Пусто — создаётся при старте")]
        [SerializeField]
        private AudioDirector? audioDirector = null;

        [Tooltip("Префаб экрана новой игры")]
        [SerializeField]
        private GameObject? newGameScreenPrefab = null;

        [Tooltip("Префаб стартового меню")]
        [SerializeField]
        private GameObject? mainMenuScreenPrefab = null;

        [Tooltip("Префаб экрана загрузки")]
        [SerializeField]
        private GameObject? loadScreenPrefab = null;

        [Tooltip("Префаб экрана сохранения")]
        [SerializeField]
        private GameObject? saveScreenPrefab = null;

        [Tooltip("Префаб экрана настроек")]
        [SerializeField]
        private GameObject? settingsScreenPrefab = null;

        [Tooltip("Префаб экрана настройки клавиш")]
        [SerializeField]
        private GameObject? keyBindingsScreenPrefab = null;

        [Tooltip("Префаб экрана авторов: заголовок игры, идущие титры и кнопка выхода в меню")]
        [SerializeField]
        private GameObject? authorsScreenPrefab = null;

        [Tooltip("Префаб окна «инвентарь»: персонаж и сетка инвентаря")]
        [SerializeField]
        private GameObject? inventoryScreenPrefab = null;

        [Tooltip("Префаб большого хаба: карта, дверь, доска и нижние значки")]
        [SerializeField]
        private GameObject? bigHubScreenPrefab = null;

        [Tooltip("Префаб комнаты торговца (малого хаба)")]
        [SerializeField]
        private GameObject? hubScreenPrefab = null;

        [Tooltip("Префаб экрана карты мира: картинка зоны с кликабельными хабами")]
        [SerializeField]
        private GameObject? mapScreenPrefab = null;

        [Tooltip("Префаб экрана-заглушки «Игра в стадии разработки»")]
        [SerializeField]
        private GameObject? developmentStubScreenPrefab = null;

        [Tooltip("Префаб меню игры: сводка состояния и кнопки продолжения, сохранения, загрузки и выхода")]
        [SerializeField]
        private GameObject? gameMenuScreenPrefab = null;

        [Tooltip("Префаб доски объявлений хаба")]
        [SerializeField]
        private GameObject? questBoardScreenPrefab = null;

        [Tooltip("Префаб журнала заданий")]
        [SerializeField]
        private GameObject? questJournalScreenPrefab = null;

        [Tooltip("Префаб экрана талантов: дерево, уровень, очки и кнопки")]
        [SerializeField]
        private GameObject? talentScreenPrefab = null;

        [Tooltip("Префаб окна торговли: товары торговца слева, инвентарь игрока справа")]
        [SerializeField]
        private GameObject? tradeScreenPrefab = null;

        [Tooltip("Префаб экрана наград: добыча слева, инвентарь игрока справа, «Взять всё» и крестик")]
        [SerializeField]
        private GameObject? rewardScreenPrefab = null;

        [Tooltip("Префаб основного игрового экрана: дорога, небо, объекты, противник и нижний ряд")]
        [SerializeField]
        private GameObject? playScreenPrefab = null;

        [Tooltip("Префаб боевого экрана: поле с зонами, полосы здоровья и ОД, форма действий, слоты и журнал")]
        [SerializeField]
        private GameObject? combatScreenPrefab = null;

        [Tooltip("Префаб окна взлома тайника: код, попытки и история")]
        [SerializeField]
        private GameObject? lockMinigameScreenPrefab = null;

        [Tooltip("Префаб окна радара поиска артефакта: поле, сканы и обыск клетки")]
        [SerializeField]
        private GameObject? radarMinigameScreenPrefab = null;

        [Tooltip("Префаб модального подтверждения")]
        [SerializeField]
        private GameObject? confirmDialogPrefab = null;

        [Tooltip("Префаб модального диалога выхода")]
        [SerializeField]
        private GameObject? exitDialogPrefab = null;

        [Tooltip("Префаб модального диалога отдыха")]
        [SerializeField]
        private GameObject? restDialogPrefab = null;

        [Tooltip("Префаб окна диалога: меню разделов и окно разговора")]
        [SerializeField]
        private GameObject? dialogOverlayPrefab = null;

        private GameBootstrap _bootstrap = null!;
        private readonly List<IScreen> _screens = new List<IScreen>();
        private MainMenuScreen _mainMenu = null!;
        private NewGameScreen _newGame = null!;
        private LoadGameScreen _load = null!;
        private SaveGameScreen _saveGame = null!;
        private SettingsScreen _settingsScreen = null!;
        private KeyBindingsScreen _keyBindings = null!;
        private AuthorsScreen _authors = null!;
        private PlayScreen _play = null!;
        private BigHubScreen _bigHub = null!;
        private HubScreen _hub = null!;
        private GameMenuScreen _gameMenu = null!;
        private MapScreen _map = null!;
        private DevelopmentStubScreen? _developmentStub = null;
        private InventoryScreen _inventoryScreen = null!;
        private NpcDialogOverlay _npcDialog = null!;
        private QuestBoardScreen _questBoard = null!;
        private QuestJournalScreen _questJournal = null!;
        private TalentScreen _talents = null!;
        private TradeScreen _trade = null!;
        private RewardScreen _rewards = null!;
        private CombatScreen _combat = null!;
        private LockMinigameScreen _lock = null!;
        private RadarMinigameScreen _radar = null!;
        private ExitDialog _exitDialog = null!;
        private RestDialog _restDialog = null!;
        private IInventoryService _inventory = null!;
        private AudioDirector _audio = null!;

        /// <summary>Модальный диалог отдыха: общий для экрана локации и экрана хаба.</summary>
        public bool HandleEscape()
        {
            if (_restDialog != null && _restDialog.HandleEscape())
            {
                return true;
            }

            if (_exitDialog != null && _exitDialog.HandleEscape())
            {
                return true;
            }

            if (_npcDialog != null && _npcDialog.HandleEscape())
            {
                return true;
            }

            var current = Router.Current;
            if (current == null)
            {
                return false;
            }

            foreach (var screen in _screens)
            {
                if (screen.Panel == current)
                {
                    return screen.HandleEscape();
                }
            }

            return false;
        }

        private void Update()
        {
            if (TextInputField.EscapeConsumedFrame == Time.frameCount)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                HandleEscape();
            }

            UpdateHotkeys();
        }

        private static void EnsureCamera()
        {
            if (UnityEngine.Object.FindAnyObjectByType<Camera>() != null)
            {
                return;
            }

            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.04f, 0.05f, 0.06f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.cullingMask = 0;
            camera.depth = -1f;
            camera.useOcclusionCulling = false;
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            var module = go.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static Transform BuildCanvas()
        {
            var go = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var portrait = LayoutOrientation.IsPortrait;
            scaler.referenceResolution = portrait
                ? new Vector2(LayoutOrientation.PortraitWidth, 1920f)
                : new Vector2(LayoutOrientation.LandscapeWidth, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            var image = background.GetComponent<Image>();
            image.color = Color.black;
            image.raycastTarget = false;
            Stretch((RectTransform)background.transform);

            var safe = new GameObject("SafeArea", typeof(RectTransform), typeof(SafeAreaRoot));
            safe.transform.SetParent(go.transform, false);
            Stretch((RectTransform)safe.transform);
            return safe.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private T CreateScreen<T>(GameObject? prefab, Transform canvas, string panelName)
            where T : MonoBehaviour, IScreenPanel
        {
            if (prefab == null)
            {
                throw new InvalidOperationException(CoreText.Format(CoreErrors.ScreenPrefabMissing, typeof(T).Name));
            }

            var instance = Instantiate(prefab, canvas);
            instance.name = panelName;
            var screen = instance.GetComponent<T>();
            if (screen == null)
            {
                throw new InvalidOperationException(CoreText.Format(CoreErrors.ScreenPrefabMissing, prefab.name));
            }

            screen.BindHost(this, this);
            return screen;
        }

        private void BuildDataError(Transform canvas) =>
            BuildError(canvas, _bootstrap.Preflight?.Report.Summary ?? CoreText.Get(CoreErrors.DataErrorMissing));

        private void BuildError(Transform canvas, string summary)
        {
            var panel = Ui.CreatePanel("DataError", canvas);
            var column = Ui.CreateColumn(panel.transform, 900);
            Ui.CreateTitle(column, CoreText.Get(CoreErrors.DataErrorTitle));
            var text = Ui.CreateText(column, summary, 20, TextAnchor.UpperLeft);
            text.GetComponent<LayoutElement>().preferredHeight = 400;
            Ui.CreateButton(column, CoreText.Get(CoreErrors.DataErrorQuit), Quit);
            panel.SetActive(true);
        }
    }
}
