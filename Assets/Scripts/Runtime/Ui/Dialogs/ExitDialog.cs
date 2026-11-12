#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Dialogs
{
    /// <summary>Модальный диалог выхода поверх текущего экрана; разметка приходит из префаба.</summary>
    public sealed class ExitDialog : MonoBehaviour
    {
        [Tooltip("Кнопка затемнения: клик закрывает диалог")]
        [SerializeField]
        private Button? backdropButton = null;

        [Tooltip("Кнопка «В главное меню»")]
        [SerializeField]
        private Button? toMenuButton = null;

        [Tooltip("Кнопка «Выйти на рабочий стол»")]
        [SerializeField]
        private Button? toDesktopButton = null;

        /// <summary>Панель диалога.</summary>
        public GameObject Panel => gameObject;

        /// <summary>Связывает префаб с хостом: подписи и действия кнопок.</summary>
        /// <param name="host">Хост интерфейса: сервисы и переходы.</param>
        public void Bind(IUiHost host)
        {
            foreach (var label in GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(host.Texts);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(host.HideExitDialog);
            }

            if (toMenuButton != null)
            {
                toMenuButton.onClick.AddListener(() =>
                {
                    host.SaveExit();
                    host.HideExitDialog();
                    host.ShowMainMenu();
                });
            }

            if (toDesktopButton != null)
            {
                toDesktopButton.onClick.AddListener(() =>
                {
                    host.SaveExit();
                    host.HideExitDialog();
                    host.Quit();
                });
            }

            gameObject.SetActive(false);
        }

        /// <summary>Показывает диалог поверх остальных панелей.</summary>
        public void Show()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Скрывает диалог.</summary>
        public void Hide() => gameObject.SetActive(false);

        /// <summary>Esc закрывает диалог, как клик по фону.</summary>
        /// <returns><c>true</c>, если диалог был открыт и закрыт.</returns>
        public bool HandleEscape()
        {
            if (!gameObject.activeSelf)
            {
                return false;
            }

            Hide();
            return true;
        }
    }
}
