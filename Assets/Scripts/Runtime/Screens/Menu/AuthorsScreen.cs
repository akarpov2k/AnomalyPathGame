#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Экран авторов: фон, заголовок игры сверху по центру, большое окно с идущими титрами
    /// по центру и кнопка выхода в стартовое меню снизу. Разметка и подписи живут в префабе,
    /// код только запускает титры и связывает кнопку с хостом.
    /// </summary>
    public sealed class AuthorsScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Идущие титры: текст едет снизу вверх внутри окна")]
        [SerializeField]
        private CreditsScroller? credits = null;

        [Tooltip("Кнопка выхода в стартовое меню")]
        [SerializeField]
        private Button? back = null;

        /// <summary>Открывает экран авторов и начинает титры с начала.</summary>
        public void Show()
        {
            ApplyTheme();
            Host.Router.Show(Panel);
            Canvas.ForceUpdateCanvases();
            credits?.Restart();
        }

        /// <summary>Esc работает как кнопка выхода: возвращает в стартовое меню.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            Leave();
            return true;
        }

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (back != null)
            {
                back.onClick.AddListener(Leave);
            }
        }

        private void Leave() => Host.ShowMainMenu();
    }
}
