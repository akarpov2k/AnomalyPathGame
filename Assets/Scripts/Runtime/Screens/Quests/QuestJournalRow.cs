#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>
    /// Строка списка журнала заданий: название, прогресс и условие провала. Нажатие на строку
    /// выбирает задание и заполняет панель описания справа; выбранная строка подсвечена.
    /// </summary>
    public sealed class QuestJournalRow : MonoBehaviour
    {
        [Tooltip("Фон строки")]
        [SerializeField]
        private Image? background = null;

        [Tooltip("Название задания")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Строка прогресса")]
        [SerializeField]
        private Text? progress = null;

        [Tooltip("Строка условия провала")]
        [SerializeField]
        private Text? time = null;

        [Tooltip("Кнопка выбора задания")]
        [SerializeField]
        private Button? select = null;

        /// <summary>Идентификатор задания строки.</summary>
        public string QuestId { get; private set; } = string.Empty;

        /// <summary>Кнопка выбора задания.</summary>
        public Button? Select => select;

        /// <summary>Заполняет строку и подписывает выбор задания.</summary>
        /// <param name="questId">Идентификатор задания.</param>
        /// <param name="titleText">Название.</param>
        /// <param name="progressText">Строка прогресса.</param>
        /// <param name="timeText">Строка условия провала; пусто — строки нет.</param>
        /// <param name="ready">Готово ли задание к сдаче.</param>
        /// <param name="selected">Выбрана ли строка.</param>
        /// <param name="onSelect">Что выполнить при выборе строки.</param>
        public void Bind(
            string questId,
            string titleText,
            string progressText,
            string timeText,
            bool ready,
            bool selected,
            System.Action onSelect)
        {
            QuestId = questId;
            if (title != null)
            {
                title.text = titleText;
            }

            if (progress != null)
            {
                progress.text = progressText;
                progress.color = ready ? UiFactory.ToastColor : UiFactory.TextColor;
            }

            if (time != null)
            {
                time.text = timeText;
                time.gameObject.SetActive(timeText.Length > 0);
            }

            if (background != null)
            {
                background.color = selected
                    ? QuestPalette.SelectedRow
                    : ready ? QuestPalette.ReadyRow : QuestPalette.Row;
            }

            if (select != null)
            {
                select.onClick.RemoveAllListeners();
                select.onClick.AddListener(() => onSelect());
            }
        }
    }
}
