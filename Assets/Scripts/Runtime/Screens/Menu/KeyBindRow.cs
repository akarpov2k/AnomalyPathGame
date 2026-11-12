#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Строка экрана настройки клавиш: название действия, текущая клавиша и отметка
    /// несохранённого изменения. Разметка — объекты префаба, код заполняет значения.
    /// </summary>
    public sealed class KeyBindRow : MonoBehaviour
    {
        [Tooltip("Название действия")]
        [SerializeField]
        private Text actionLabel = null!;

        [Tooltip("Кнопка с текущей клавишей")]
        [SerializeField]
        private Button keyButton = null!;

        [Tooltip("Подпись текущей клавиши")]
        [SerializeField]
        private Text keyLabel = null!;

        [Tooltip("Отметка несохранённого изменения")]
        [SerializeField]
        private GameObject star = null!;

        /// <summary>Действие строки.</summary>
        public HotkeyAction Action { get; private set; }

        /// <summary>Кнопка назначения клавиши.</summary>
        public Button KeyButton => keyButton;

        /// <summary>Заполняет строку действием, клавишей и отметкой изменения.</summary>
        /// <param name="texts">Каталог строк.</param>
        /// <param name="action">Действие.</param>
        /// <param name="key">Текущая клавиша рабочей копии.</param>
        /// <param name="changed">Привязка отличается от сохранённой.</param>
        /// <param name="capturing">Строка ждёт нажатия клавиши.</param>
        public void Bind(IGameTexts texts, HotkeyAction action, Key key, bool changed, bool capturing = false)
        {
            Action = action;
            actionLabel.text = UiText.Get(texts, HotkeyText.ActionKey(action));
            keyLabel.text = capturing
                ? UiText.Get(texts, UiTextKey.Keys_Press)
                : HotkeyText.KeyLabel(key, texts);
            star.SetActive(changed);
        }
    }
}
