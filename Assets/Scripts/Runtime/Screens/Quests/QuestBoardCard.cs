#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>
    /// Карточка задания на доске объявлений: картинка, сложность, название, описание и цель,
    /// таймер пустого слота и кнопки «Принять» и «Отказаться». Разметка приходит из префаба,
    /// код только заполняет поля и включает нужные части карточки.
    /// </summary>
    public sealed class QuestBoardCard : MonoBehaviour
    {
        [Tooltip("Фон карточки")]
        [SerializeField]
        private Image? background = null;

        [Tooltip("Картинка задания")]
        [SerializeField]
        private Image? art = null;

        [Tooltip("Сложность задания")]
        [SerializeField]
        private Text? difficulty = null;

        [Tooltip("Название задания")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Описание задания")]
        [SerializeField]
        private Text? description = null;

        [Tooltip("Цель задания")]
        [SerializeField]
        private Text? targets = null;

        [Tooltip("Таймер обновления пустого слота")]
        [SerializeField]
        private Text? timer = null;

        [Tooltip("Кнопка «Принять»")]
        [SerializeField]
        private Button? take = null;

        [Tooltip("Кнопка «Отказаться»")]
        [SerializeField]
        private Button? decline = null;

        /// <summary>Фон карточки.</summary>
        public Image? Background => background;

        /// <summary>Картинка задания.</summary>
        public Image? Art => art;

        /// <summary>Подпись сложности.</summary>
        public Text? Difficulty => difficulty;

        /// <summary>Название задания.</summary>
        public Text? Title => title;

        /// <summary>Описание задания.</summary>
        public Text? Description => description;

        /// <summary>Строка цели задания.</summary>
        public Text? Targets => targets;

        /// <summary>Строка таймера пустого слота.</summary>
        public Text? Timer => timer;

        /// <summary>Кнопка «Принять».</summary>
        public Button? Take => take;

        /// <summary>Кнопка «Отказаться».</summary>
        public Button? Decline => decline;

        /// <summary>Показывает слот с заданием: картинка, тексты и кнопки.</summary>
        /// <param name="background">Цвет фона карточки.</param>
        /// <param name="sprite">Картинка задания или <c>null</c>.</param>
        /// <param name="difficultyText">Подпись сложности.</param>
        /// <param name="titleText">Название.</param>
        /// <param name="descriptionText">Описание.</param>
        /// <param name="targetsText">Цель.</param>
        /// <param name="onTake">Что выполнить по кнопке «Принять».</param>
        /// <param name="onDecline">Что выполнить по кнопке «Отказаться».</param>
        public void ShowOffer(
            Color background,
            Sprite? sprite,
            string difficultyText,
            string titleText,
            string descriptionText,
            string targetsText,
            System.Action onTake,
            System.Action onDecline)
        {
            ApplyBackground(background);
            SetArt(sprite);
            if (art != null)
            {
                art.enabled = true;
            }
            SetText(difficulty, difficultyText);
            SetText(title, titleText);
            SetText(description, descriptionText);
            SetText(targets, targetsText);
            SetText(timer, string.Empty);
            Bind(take, onTake);
            Bind(decline, onDecline);
            SetActive(take, true);
            SetActive(decline, true);
        }

        /// <summary>Показывает пустой слот: карточка остаётся, вместо задания — таймер.</summary>
        /// <param name="background">Цвет фона карточки.</param>
        /// <param name="difficultyText">Подпись сложности слота.</param>
        /// <param name="timerText">Текст таймера обновления.</param>
        public void ShowTimer(Color background, string difficultyText, string timerText)
        {
            ApplyBackground(background);
            SetArt(null);
            if (art != null)
            {
                art.enabled = false;
            }
            SetText(difficulty, difficultyText);
            SetText(title, string.Empty);
            SetText(description, string.Empty);
            SetText(targets, string.Empty);
            SetText(timer, timerText);
            SetActive(take, false);
            SetActive(decline, false);
        }

        private void ApplyBackground(Color color)
        {
            if (background == null)
            {
                return;
            }

            // Текстурированный фон уже тёмный: цвет-заливка его бы погасил, поэтому карточка
            // с текстомурой остаётся белой, а заливка работает только у префаба без спрайта.
            background.color = background.sprite != null ? Color.white : color;
        }

        private void SetArt(Sprite? sprite)
        {
            if (art == null)
            {
                return;
            }

            art.sprite = sprite;
            art.color = sprite != null ? Color.white : QuestPalette.ArtPlaceholder;
            art.enabled = true;
        }

        private static void SetText(Text? label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        private static void Bind(Button? button, System.Action action)
        {
            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => action());
        }

        private static void SetActive(Button? button, bool active)
        {
            if (button != null)
            {
                button.gameObject.SetActive(active);
            }
        }
    }
}
