#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Стартовое меню по прототипу: фон, колонка кнопок, картинка и версия игры.
    /// Разметка и кнопки живут в префабе; код только подписывает действия и версию.
    /// </summary>
    public sealed class MainMenuScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Кнопка «Новая игра»")]
        [SerializeField]
        private Button newGame = null!;

        [Tooltip("Кнопка «Загрузить игру»")]
        [SerializeField]
        private Button load = null!;

        [Tooltip("Кнопка «Настройки»: открывает экран настроек")]
        [SerializeField]
        private Button settings = null!;

        [Tooltip("Кнопка «Авторы»")]
        [SerializeField]
        private Button authors = null!;

        [Tooltip("Кнопка «Выход»")]
        [SerializeField]
        private Button quit = null!;

        [Tooltip("Подпись версии игры в правом нижнем углу")]
        [SerializeField]
        private Text? version = null;

        /// <inheritdoc />
        protected override void OnBound()
        {
            newGame.onClick.AddListener(Host.ShowNewGame);
            load.onClick.AddListener(() => Host.ShowLoad(fromGame: false));
            settings.onClick.AddListener(Host.ShowSettings);
            authors.onClick.AddListener(Host.ShowAuthors);
            quit.onClick.AddListener(Host.Quit);
            if (version != null)
            {
                // Номер версии — из сборки, подпись «Alpha Demo» — из каталога строк.
                version.text = UiText.Format(Host.Texts, UiTextKey.MainMenu_Version, Application.version);
            }
        }
    }
}
