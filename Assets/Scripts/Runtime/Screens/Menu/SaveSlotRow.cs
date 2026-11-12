#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Одна строка списка сохранений: картинка, название, прогресс и кнопки действия.
    /// Вся разметка строки приходит из префаба; код только подписывает тексты и кнопки.
    /// </summary>
    public sealed class SaveSlotRow : MonoBehaviour
    {
        [Tooltip("Название сохранения")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Уровень персонажа и деньги")]
        [SerializeField]
        private Text? details = null;

        [Tooltip("День мира и часы")]
        [SerializeField]
        private Text? day = null;

        [Tooltip("Пояснение для пустой или повреждённой ячейки")]
        [SerializeField]
        private Text? note = null;

        [Tooltip("Картинка сохранения")]
        [SerializeField]
        private Image? thumbnail = null;

        [Tooltip("Кнопка загрузки строки")]
        [SerializeField]
        private Button? loadButton = null;

        [Tooltip("Кнопка сохранения строки")]
        [SerializeField]
        private Button? saveButton = null;

        [Tooltip("Кнопка удаления строки")]
        [SerializeField]
        private Button? deleteButton = null;

        [Tooltip("Поле названия внутри карточки нового сохранения")]
        [SerializeField]
        private TextInputField? nameField = null;

        /// <summary>Имя слота, к которому привязана строка.</summary>
        public string Slot { get; private set; } = string.Empty;

        /// <summary>Картинка строки.</summary>
        public Image? Thumbnail => thumbnail;

        /// <summary>Кнопка загрузки; <c>null</c> — у строки нет такой кнопки.</summary>
        public Button? Load => loadButton;

        /// <summary>Кнопка сохранения; <c>null</c> — у строки нет такой кнопки.</summary>
        public Button? Save => saveButton;

        /// <summary>Кнопка удаления; <c>null</c> — у строки нет такой кнопки.</summary>
        public Button? Delete => deleteButton;

        /// <summary>Поле названия; <c>null</c> — у строки нет поля.</summary>
        public TextInputField? NameField => nameField;

        /// <summary>Подписывает строку данными слота.</summary>
        /// <param name="texts">Каталог текстов игры.</param>
        /// <param name="slot">Имя слота.</param>
        /// <param name="number">Номер ячейки или 0 для слота с особым именем.</param>
        /// <param name="info">Метаданные слота; <c>null</c> — свободная ячейка.</param>
        /// <param name="isNew">Строка создания нового сохранения.</param>
        /// <param name="newName">Имя по умолчанию для свободной ячейки: подставляется сразу в поле.</param>
        public void Bind(
            IGameTexts texts,
            string slot,
            int number,
            SaveSlotInfo? info,
            bool isNew,
            string newName = "")
        {
            Slot = slot;
            var readable = info != null && info.IsReadable;
            var fallback = number > 0 ? UiText.Format(texts, UiTextKey.SaveSlot_Cell, number) : slot;
            if (title != null)
            {
                title.gameObject.SetActive(!isNew);
                title.text = readable
                    ? string.IsNullOrWhiteSpace(info!.Title) ? fallback : info.Title
                    : fallback;
            }

            if (details != null)
            {
                details.text = readable
                    ? UiText.Format(texts, UiTextKey.Common_Level_Cap, info!.Level) + " · " +
                      SaveSlotText.Money(texts, info.Money)
                    : string.Empty;
            }

            if (day != null)
            {
                day.text = readable
                    ? UiText.Format(
                        texts,
                        UiTextKey.Common_Day_Hours_Tile,
                        info!.Day,
                        info.Hours.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture))
                    : string.Empty;
            }

            if (note != null)
            {
                note.text = readable || isNew
                    ? string.Empty
                    : info == null
                        ? UiText.Get(texts, UiTextKey.SaveSlot_Empty)
                        : UiText.Get(texts, UiTextKey.SaveSlot_Corrupted);
            }

            if (nameField != null)
            {
                nameField.gameObject.SetActive(isNew);
                if (isNew)
                {
                    nameField.SetText(newName);
                }
            }
        }
    }
}
