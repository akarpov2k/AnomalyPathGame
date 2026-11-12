#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Полноэкранная заглушка для локации, контент которой ещё не готов: заголовок, текст
    /// «Дальнейший контент будет позже» и кнопка закрытия. Тексты стоят в префабе ключами
    /// каталога, код только подписывает кнопку. Закрытие и Esc возвращают в игровой экран
    /// первого хаба; переход при этом не выполняется.
    /// </summary>
    public sealed class DevelopmentStubScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Кнопка закрытия: возврат в игру")]
        [SerializeField]
        private Button? close = null;

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (close != null)
            {
                close.onClick.AddListener(BackToHub);
            }
        }

        /// <summary>Показывает заглушку поверх текущего экрана.</summary>
        public void Show()
        {
            ApplyTheme();
            Host.Router.Show(Panel);
        }

        /// <summary>Esc возвращает в игру.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            BackToHub();
            return true;
        }

        private void BackToHub() => Host.ShowGame(announce: false);
    }
}
