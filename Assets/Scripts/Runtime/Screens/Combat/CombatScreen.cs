#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Боевой экран, собранный в префабе по прототипу <c>бой.penpot</c>: поле с зонами
    /// ближняя/средняя/дальняя, фигуры бойцов, полосы здоровья с очками действия,
    /// оружие с патронами, форма действий (переход, стрельба, скиллы) с выпадашками,
    /// быстрые слоты, конец хода, побег, полоса опыта и журнал действий.
    /// Код заполняет префаб и связывает кнопки; разметки в коде нет.
    /// </summary>
    public sealed partial class CombatScreen : PrefabScreenBase<ICombatScreenHost>
    {
        [Tooltip("Фигура игрока на поле: тело, оружие, вспышка и трасса")]
        [SerializeField]
        private CombatFigureView? playerFigure = null;

        [Tooltip("Имя игрока")]
        [SerializeField]
        private Text? playerName = null;

        [Tooltip("Заполнение полосы здоровья игрока")]
        [SerializeField]
        private Image? playerHpFill = null;

        [Tooltip("Числа здоровья игрока: «100 / 200»")]
        [SerializeField]
        private Text? playerHp = null;

        [Tooltip("Ряд кругов очков действия игрока")]
        [SerializeField]
        private RectTransform? playerApRow = null;

        [Tooltip("Неактивный образец круга очков действия")]
        [SerializeField]
        private Image? playerApTemplate = null;

        [Tooltip("Строка травм игрока")]
        [SerializeField]
        private Text? playerInjuries = null;

        [Tooltip("Иконка активного оружия игрока")]
        [SerializeField]
        private Image? playerWeaponIcon = null;

        [Tooltip("Патроны в магазине игрока")]
        [SerializeField]
        private Text? playerAmmo = null;

        [Tooltip("Фигура противника на поле: тело, оружие, вспышка и трасса")]
        [SerializeField]
        private CombatFigureView? enemyFigure = null;

        [Tooltip("Имя противника")]
        [SerializeField]
        private Text? enemyName = null;

        [Tooltip("Заполнение полосы здоровья противника")]
        [SerializeField]
        private Image? enemyHpFill = null;

        [Tooltip("Числа здоровья противника")]
        [SerializeField]
        private Text? enemyHp = null;

        [Tooltip("Иконка оружия противника")]
        [SerializeField]
        private Image? enemyWeaponIcon = null;

        [Tooltip("Патроны противника")]
        [SerializeField]
        private Text? enemyAmmo = null;

        [Tooltip("Зона игрока «ближняя»")]
        [SerializeField]
        private Image? playerZoneNear = null;

        [Tooltip("Зона игрока «средняя»")]
        [SerializeField]
        private Image? playerZoneMid = null;

        [Tooltip("Зона игрока «дальняя»")]
        [SerializeField]
        private Image? playerZoneFar = null;

        [Tooltip("Зона противника «ближняя»")]
        [SerializeField]
        private Image? enemyZoneNear = null;

        [Tooltip("Зона противника «средняя»")]
        [SerializeField]
        private Image? enemyZoneMid = null;

        [Tooltip("Зона противника «дальняя»")]
        [SerializeField]
        private Image? enemyZoneFar = null;

        [Tooltip("Заполнение полосы опыта")]
        [SerializeField]
        private Image? xpFill = null;

        [Tooltip("Подпись полосы опыта: уровень и опыт")]
        [SerializeField]
        private Text? xpText = null;

        [Tooltip("Кнопка «Переход»: шаги по своей половине")]
        [SerializeField]
        private Button? move = null;

        [Tooltip("Кнопка «Атака»: дефолтный скилл оружия")]
        [SerializeField]
        private Button? attack = null;

        [Tooltip("Кнопка «Скиллы»: скиллы, перезарядка и смена оружия")]
        [SerializeField]
        private Button? skills = null;

        [Tooltip("Кнопка «Конец хода»")]
        [SerializeField]
        private Button? endTurn = null;

        [Tooltip("Кнопка «Выйти из боя»")]
        [SerializeField]
        private Button? flee = null;

        [Tooltip("Кнопка «Вернуться» после конца боя")]
        [SerializeField]
        private Button? back = null;

        [Tooltip("Кнопка журнала действий")]
        [SerializeField]
        private Button? journal = null;

        [Tooltip("Панель итога боя с кнопкой возврата")]
        [SerializeField]
        private GameObject? resultPanel = null;

        [Tooltip("Заголовок итога боя")]
        [SerializeField]
        private Text? resultTitle = null;

        [Tooltip("Выпадашка перехода")]
        [SerializeField]
        private GameObject? moveMenu = null;

        [Tooltip("Выпадашка скиллов")]
        [SerializeField]
        private GameObject? skillMenu = null;

        [Tooltip("Контейнер строк выпадашки скиллов")]
        [SerializeField]
        private Transform? skillEntries = null;

        [Tooltip("Неактивный образец строки выпадашки скиллов")]
        [SerializeField]
        private Button? skillEntryTemplate = null;

        [Tooltip("Прозрачная плашка, закрывающая выпадашку кликом мимо")]
        [SerializeField]
        private GameObject? menuCatcher = null;

        [Tooltip("Контейнер быстрых слотов")]
        [SerializeField]
        private Transform? quickSlots = null;

        [Tooltip("Неактивный образец быстрого слота")]
        [SerializeField]
        private Button? quickSlotTemplate = null;

        [Tooltip("Полупрозрачное окно журнала")]
        [SerializeField]
        private GameObject? journalPanel = null;

        [Tooltip("Текст журнала действий")]
        [SerializeField]
        private Text? journalText = null;

        [Tooltip("Прозрачная плашка: клик в любом месте закрывает журнал")]
        [SerializeField]
        private GameObject? journalCatcher = null;

        [Tooltip("Подсказка при наведении на действие")]
        [SerializeField]
        private GameObject? tooltip = null;

        [Tooltip("Текст подсказки")]
        [SerializeField]
        private Text? tooltipText = null;

        private readonly List<string> _lines = new List<string>();
        private readonly List<EnemyTurnAction> _lastEnemyActions = new List<EnemyTurnAction>();
        private readonly List<GameObject> _apCircles = new List<GameObject>();
        private readonly List<Button> _quickSlots = new List<Button>();
        private ConfirmDialog _confirm = null!;
        private bool _enemyTurnRunning;

        /// <summary>Строки журнала боя: по строке на каждое действие игрока и противника.</summary>
        public IReadOnlyList<string> LogLines => _lines;

        /// <summary>Действия последнего хода противника в порядке выполнения.</summary>
        public IReadOnlyList<EnemyTurnAction> LastEnemyActions => _lastEnemyActions;

        /// <summary>Фон боя — упрощённая боевая картинка текущей локации.</summary>
        protected override ScreenTheme Theme
        {
            get
            {
                var session = Host.Session;
                var background = CombatBackdrop.BackgroundFor(session?.Combat, session?.State.World);
                return new ScreenTheme(
                    CombatBackdrop.IdFor(background),
                    Color.white,
                    ScreenTheme.Default.TopColor,
                    ScreenTheme.Default.BottomColor);
            }
        }

        /// <summary>Готовит экран: диалог подтверждения побега и обработчики кнопок префаба.</summary>
        protected override void OnBound()
        {
            _confirm = Host.CreateConfirmDialog(Panel.transform);
            if (move != null)
            {
                move.gameObject.SetActive(false);
            }

            HideZonePlates();
            WireButton(skills, OpenSkillMenu);
            WireButton(attack, Attack);
            WireButton(endTurn, EndTurn);
            WireButton(flee, AskFlee);
            WireButton(back, Leave);
            WireButton(journal, OpenJournal);
            WireButton(menuCatcher == null ? null : menuCatcher.GetComponent<Button>(), CloseMenus);
            WireButton(journalCatcher == null ? null : journalCatcher.GetComponent<Button>(), CloseJournal);
            CloseMenus();
            CloseJournal();
            HideTooltip();
        }

        /// <summary>Показывает боевой экран: чистит журнал и перестраивает поля по состоянию боя.</summary>
        public void Show()
        {
            _lines.Clear();
            _lastEnemyActions.Clear();
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            Refresh();
        }

        /// <summary>Esc закрывает журнал, выпадашку и подтверждение побега; сам экран Esc не закрывает.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_confirm != null && _confirm.HandleEscape())
            {
                return true;
            }

            if (IsJournalOpen)
            {
                CloseJournal();
                return true;
            }

            if (IsMenuOpen)
            {
                CloseMenus();
                return true;
            }

            return false;
        }

        /// <summary>Убирает устаревшие плашки зон: позиций в бою больше нет.</summary>
        private void HideZonePlates()
        {
            HideZone(playerZoneNear);
            HideZone(playerZoneMid);
            HideZone(playerZoneFar);
            HideZone(enemyZoneNear);
            HideZone(enemyZoneMid);
            HideZone(enemyZoneFar);
        }

        /// <summary>Скрывает плашку зоны, сохраняя объект префаба.</summary>
        /// <param name="zone">Плашка зоны.</param>
        private static void HideZone(Image? zone)
        {
            if (zone != null)
            {
                zone.enabled = false;
            }
        }

        /// <summary>Игрок сейчас ходит и бой ещё идёт.</summary>
        /// <param name="combat">Состояние боя.</param>
        /// <returns><c>true</c>, если действия доступны игроку.</returns>
        private static bool IsPlayerTurn(CombatState? combat) =>
            combat != null && !combat.IsFinished && ReferenceEquals(combat.Current, combat.Player);

        /// <summary>Обновляет весь экран по текущему состоянию боя.</summary>
        private void Refresh()
        {
            var session = Host.Session;
            var combat = session?.Combat;
            RefreshCombatants(combat);
            RefreshFigures(combat);
            RefreshWeapons(combat);
            RefreshProgress(session, combat);
            RefreshQuickSlots(combat);
            RefreshSkills(combat);
            RefreshActions(combat);
            RefreshJournal();
            var finished = combat != null && combat.IsFinished;
            if (resultPanel != null)
            {
                resultPanel.SetActive(finished);
            }

            if (resultTitle != null && finished)
            {
                resultTitle.text = UiText.Get(
                    Host.Texts,
                    combat!.Player.IsDead ? UiTextKey.Combat_Result_Defeat : UiTextKey.Combat_Result_Victory);
            }
        }

        /// <summary>Включает и выключает кнопки формы действий по ходу игры.</summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        private void RefreshActions(CombatState? combat)
        {
            var playerTurn = IsPlayerTurn(combat);
            var weapon = PlayerWeapon(combat);
            SetEnabled(attack, playerTurn && weapon != null);
            SetEnabled(skills, playerTurn);
            SetEnabled(endTurn, playerTurn);
            SetEnabled(flee, playerTurn);
            SetEnabled(back, combat != null && combat.IsFinished);
            if (!playerTurn)
            {
                CloseMenus();
                HideTooltip();
            }
        }

        /// <summary>Ставит кнопке активность, не падая, если кнопки в префабе нет.</summary>
        /// <param name="button">Кнопка префаба.</param>
        /// <param name="enabled">Включить кнопку.</param>
        private static void SetEnabled(Button? button, bool enabled)
        {
            if (button != null)
            {
                button.interactable = enabled;
            }
        }

        /// <summary>Вешает действие на кнопку префаба, если она задана.</summary>
        /// <param name="button">Кнопка префаба.</param>
        /// <param name="action">Что выполнить по нажатию.</param>
        private static void WireButton(Button? button, Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        /// <summary>Оружие игрока в активном слоте; <c>null</c> — оружия нет.</summary>
        /// <param name="combat">Состояние боя.</param>
        /// <returns>Экземпляр оружия или <c>null</c>.</returns>
        private ItemInstance? PlayerWeapon(CombatState? combat) =>
            combat?.Player.Equipment?.Get(combat.PlayerWeaponSlot);

        /// <summary>Стрельба дефолтным скиллом оружия; результат уходит в журнал.</summary>
    }
}
