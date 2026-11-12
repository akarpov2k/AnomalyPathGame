#nullable enable
using AnomalyPath.Core.Talents;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Talents
{
    /// <summary>
    /// Карточка таланта из префаба: картинка, счётчик ранга и наведение. Код только
    /// перекрашивает карточку по состоянию и подписывает счётчик; разметка — в префабе.
    /// </summary>
    public sealed class TalentNodeView : MonoBehaviour
    {
        [Tooltip("Фон карточки")]
        [SerializeField]
        private Image? background = null;

        [Tooltip("Картинка таланта")]
        [SerializeField]
        private Image? icon = null;

        [Tooltip("Счётчик вложенных очков в правом нижнем углу")]
        [SerializeField]
        private Text? counter = null;

        [Tooltip("Кнопка карточки")]
        [SerializeField]
        private Button? button = null;

        [Tooltip("Наведение на карточку с задержкой")]
        [SerializeField]
        private TalentHover? hover = null;

        /// <summary>Кнопка карточки: нажатие вкладывает очко.</summary>
        public Button? Button => button;

        /// <summary>Наведение карточки для подсказки.</summary>
        public TalentHover? Hover => hover;

        /// <summary>Перекрашивает карточку и ставит счётчик по текущему рангу.</summary>
        /// <param name="node">Талант из данных.</param>
        /// <param name="rank">Вложено очков с учётом несохранённых.</param>
        /// <param name="unlocked">Открыт ли талант по вложенным очкам.</param>
        /// <param name="counterText">Готовая подпись счётчика, например <c>2/5</c>.</param>
        /// <param name="catalog">Каталог спрайтов; <c>null</c> оставляет цветную заглушку.</param>
        public void Show(
            TalentNodeTemplate node,
            int rank,
            bool unlocked,
            string counterText,
            ISpriteCatalog? catalog)
        {
            var maxed = rank >= node.MaxRank;
            if (background != null)
            {
                background.color = maxed
                    ? TalentPalette.MaxNode
                    : unlocked ? TalentPalette.Node : TalentPalette.LockedNode;
            }

            if (icon != null)
            {
                Sprite? sprite = null;
                if (!string.IsNullOrWhiteSpace(node.Icon) && catalog != null &&
                    catalog.TryGet(node.Icon, out var found) && found != null)
                {
                    sprite = found;
                }

                icon.sprite = sprite;
                icon.color = sprite != null
                    ? maxed
                        ? TalentPalette.MaxIcon
                        : unlocked ? TalentPalette.NodeIcon : TalentPalette.LockedIcon
                    : TalentPalette.MissingIcon;
            }

            if (counter != null)
            {
                counter.text = counterText;
                counter.color = maxed
                    ? TalentPalette.MaxIcon
                    : unlocked ? UiFactory.TextColor : UiFactory.MutedTextColor;
            }
        }
    }
}
