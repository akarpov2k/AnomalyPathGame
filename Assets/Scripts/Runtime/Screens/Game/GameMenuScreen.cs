#nullable enable
using System;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Меню игры: продолжить, сохранить, загрузить и выйти. Открывается шестерёнкой и Esc,
    /// панель, задник, сводка состояния и кнопки приходят из префаба.
    /// Во время боя пункт сохранения неактивен: сохраняться в бою нельзя.
    /// </summary>
    public sealed class GameMenuScreen : PrefabScreenBase<IGameMenuScreenHost>
    {
        [Tooltip("Строка состояния: локация, выносливость, день и время, здоровье")]
        [SerializeField]
        private Text status = null!;

        [Tooltip("Строка счётчика смертей за прохождение")]
        [SerializeField]
        private Text deaths = null!;

        [Tooltip("Кнопка «Продолжить»")]
        [SerializeField]
        private Button continueButton = null!;

        [Tooltip("Кнопка «Сохранить игру»")]
        [SerializeField]
        private Button saveButton = null!;

        [Tooltip("Кнопка «Загрузить игру»")]
        [SerializeField]
        private Button loadButton = null!;

        [Tooltip("Кнопка «Выйти»")]
        [SerializeField]
        private Button exitButton = null!;

        /// <inheritdoc />
        protected override void OnBound()
        {
            continueButton.onClick.AddListener(() => Host.ShowGame(announce: false));
            saveButton.onClick.AddListener(Host.ShowSave);
            loadButton.onClick.AddListener(() => Host.ShowLoad(fromGame: true));
            exitButton.onClick.AddListener(Host.ShowExitDialog);
        }

        /// <summary>Фон меню — текущая локация, если игра уже началась.</summary>
        protected override ScreenTheme Theme
        {
            get
            {
                var world = Host.Session?.State.World;
                return world == null ? ScreenTheme.Default : ScreenTheme.Location(world.CurrentLocation.Background);
            }
        }

        /// <summary>Показывает меню и обновляет состояние.</summary>
        public void Show()
        {
            Refresh();
            Host.Router.Show(Panel);
        }

        /// <summary>Esc возвращает в игру; без активной игры — в стартовое меню.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (Host.Session == null)
            {
                Host.ShowMainMenu();
            }
            else
            {
                Host.ShowGame(announce: false);
            }

            return true;
        }

        /// <summary>Обновляет сводку состояния.</summary>
        public void Refresh()
        {
            ApplyTheme();
            var host = Host;
            var session = host.Session;
            if (session == null)
            {
                status.text = UiText.Get(host.Texts, UiTextKey.Common_No_Game);
                deaths.text = string.Empty;
                saveButton.interactable = false;
                return;
            }

            saveButton.interactable = !session.InCombat;

            var world = session.State.World;
            var location = world.CurrentLocation;
            var player = session.State.Player;
            var health = player == null
                ? UiText.Get(host.Texts, UiTextKey.Common_Dash)
                : player.Health.ToString();
            var staminaMax = world.StaminaMax;
            if (player != null)
            {
                staminaMax += Math.Max(0, host.Talents.GetModifiers(player).StaminaMax);
                staminaMax += Math.Max(0, host.Artifacts.GetEquippedModifiers(player.Equipment).Stamina);
            }

            status.text = UiText.Format(
                host.Texts,
                UiTextKey.Game_Status,
                LocationText.Title(WidgetHost, location),
                world.Stamina,
                staminaMax,
                world.Clock.Day,
                PlayClock.Time(world.Clock),
                health);

            var deathCount = player == null
                ? UiText.Get(host.Texts, UiTextKey.Common_Dash)
                : player.Deaths.ToString();
            deaths.text = UiText.Format(host.Texts, UiTextKey.GameMenu_Deaths, deathCount);
        }
    }
}
