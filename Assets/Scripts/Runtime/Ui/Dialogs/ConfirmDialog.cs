#nullable enable
using System;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Dialogs
{
    /// <summary>
    /// Модальное подтверждение действия: затемнение, вопрос и две кнопки — зелёная кнопка
    /// с подписью действия («Удалить» по умолчанию) и «Отмена». Esc и клик по фону
    /// закрывают без действия. Разметка и кнопки приходят из префаба, код подписывает нажатия.
    /// </summary>
    public sealed class ConfirmDialog : MonoBehaviour
    {
        [Tooltip("Заголовок диалога")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Текст вопроса")]
        [SerializeField]
        private Text? message = null;

        [Tooltip("Подпись зелёной кнопки")]
        [SerializeField]
        private Text? confirmLabel = null;

        [Tooltip("Зелёная кнопка подтверждения")]
        [SerializeField]
        private Button? confirmButton = null;

        [Tooltip("Кнопка «Отмена»")]
        [SerializeField]
        private Button? cancelButton = null;

        [Tooltip("Кнопка затемнения: клик закрывает без действия")]
        [SerializeField]
        private Button? backdropButton = null;

        [Tooltip("Плашка окна: не пропускает клики к экрану под диалогом")]
        [SerializeField]
        private Button? boxBlocker = null;

        private string _defaultConfirmLabel = string.Empty;
        private Action? _action;

        /// <summary>Панель диалога.</summary>
        public GameObject Panel => gameObject;

        /// <summary><c>true</c>, если диалог открыт.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Связывает префаб с хостом: подписи из каталога и нажатия кнопок.</summary>
        /// <param name="host">Хост интерфейса.</param>
        public void Bind(IUiHost host)
        {
            ApplyLabels(host);
            _defaultConfirmLabel = UiText.Get(host.Texts, UiTextKey.Common_Delete);
            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(Cancel);
            }

            if (boxBlocker != null)
            {
                boxBlocker.onClick.AddListener(() => { });
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Cancel);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(Confirm);
            }

            gameObject.SetActive(false);
        }

        /// <summary>Открывает диалог с вопросом; зелёная кнопка подписана «Удалить».</summary>
        /// <param name="title">Заголовок.</param>
        /// <param name="message">Текст вопроса.</param>
        /// <param name="onConfirm">Действие после подтверждения.</param>
        public void Show(string title, string message, Action onConfirm) =>
            Show(title, message, _defaultConfirmLabel, onConfirm);

        /// <summary>Открывает диалог с вопросом и своей подписью зелёной кнопки.</summary>
        /// <param name="title">Заголовок.</param>
        /// <param name="message">Текст вопроса.</param>
        /// <param name="confirmLabel">Подпись зелёной кнопки действия.</param>
        /// <param name="onConfirm">Действие после подтверждения.</param>
        public void Show(string title, string message, string confirmLabel, Action onConfirm)
        {
            if (this.title != null)
            {
                this.title.text = title;
            }

            if (this.message != null)
            {
                this.message.text = message;
            }

            if (this.confirmLabel != null)
            {
                this.confirmLabel.text = confirmLabel;
            }

            if (confirmButton != null)
            {
                confirmButton.gameObject.name = confirmLabel;
            }

            _action = onConfirm;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Esc закрывает подтверждение без действия.</summary>
        /// <returns><c>true</c>, если диалог был открыт и закрыт.</returns>
        public bool HandleEscape()
        {
            if (!gameObject.activeSelf)
            {
                return false;
            }

            Cancel();
            return true;
        }

        private void ApplyLabels(IUiHost host)
        {
            foreach (var label in GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(host.Texts);
            }
        }

        private void Confirm()
        {
            var action = _action;
            Hide();
            action?.Invoke();
        }

        private void Cancel() => Hide();

        private void Hide()
        {
            _action = null;
            gameObject.SetActive(false);
        }
    }
}
