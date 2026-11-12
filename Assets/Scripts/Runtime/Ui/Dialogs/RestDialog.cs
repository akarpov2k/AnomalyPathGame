#nullable enable
using System;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Dialogs
{
    /// <summary>
    /// Модальный диалог отдыха: ползунок часов от 0 до предела полного восстановления,
    /// выносливость до и после, риск нападения вне хаба. Кнопка «Отдыхать» запускает отдых,
    /// «Отмена», Esc и клик по фону закрывают диалог без отдыха. Разметка и ползунок приходят
    /// из префаба, код заполняет числа и подписывает нажатия.
    /// </summary>
    public sealed class RestDialog : MonoBehaviour
    {
        [Tooltip("Строка выносливости до отдыха")]
        [SerializeField]
        private Text? staminaText = null;

        [Tooltip("Ползунок часов отдыха")]
        [SerializeField]
        private Slider? hoursSlider = null;

        [Tooltip("Строка выбранного числа часов")]
        [SerializeField]
        private Text? hoursText = null;

        [Tooltip("Строка восстановления")]
        [SerializeField]
        private Text? recoveryText = null;

        [Tooltip("Строка риска нападения")]
        [SerializeField]
        private Text? riskText = null;

        [Tooltip("Кнопка «Отдыхать»")]
        [SerializeField]
        private Button? confirmButton = null;

        [Tooltip("Кнопка «Отмена»")]
        [SerializeField]
        private Button? cancelButton = null;

        [Tooltip("Кнопка затемнения: клик закрывает диалог")]
        [SerializeField]
        private Button? backdropButton = null;

        private IUiHost _host = null!;
        private RestQuote? _quote;
        private Action<int>? _onConfirm;

        /// <summary>Панель диалога.</summary>
        public GameObject Panel => gameObject;

        /// <summary><c>true</c>, если диалог открыт.</summary>
        public bool IsOpen => gameObject.activeSelf;

        /// <summary>Связывает префаб с хостом: подписи из каталога и нажатия кнопок.</summary>
        /// <param name="host">Хост интерфейса.</param>
        public void Bind(IUiHost host)
        {
            _host = host;
            foreach (var label in GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(host.Texts);
            }

            if (hoursSlider != null)
            {
                hoursSlider.wholeNumbers = true;
                hoursSlider.onValueChanged.AddListener(OnHoursChanged);
            }

            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(Hide);
            }

            if (backdropButton != null)
            {
                backdropButton.onClick.AddListener(Hide);
            }

            Hide();
        }

        /// <summary>Открывает диалог с ползунком на пределе полного восстановления.</summary>
        /// <param name="quote">Предложение отдыха по текущему состоянию.</param>
        /// <param name="onConfirm">Что выполнить с выбранным числом часов.</param>
        /// <exception cref="ArgumentNullException">Предложение или действие не заданы.</exception>
        public void Show(RestQuote quote, Action<int> onConfirm)
        {
            if (quote == null)
            {
                throw new ArgumentNullException(nameof(quote));
            }

            _onConfirm = onConfirm ?? throw new ArgumentNullException(nameof(onConfirm));
            _quote = quote;
            if (staminaText != null)
            {
                staminaText.text = UiText.Format(
                    _host.Texts,
                    UiTextKey.Rest_Stamina,
                    quote.Stamina,
                    quote.StaminaMax);
            }

            if (hoursSlider != null)
            {
                hoursSlider.minValue = 0f;
                hoursSlider.maxValue = quote.MaxHours;
                hoursSlider.value = quote.MaxHours;
            }

            UpdateTexts(quote.MaxHours);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        /// <summary>Esc закрывает диалог без отдыха.</summary>
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

        private void OnHoursChanged(float value) => UpdateTexts(Mathf.RoundToInt(value));

        private void UpdateTexts(int hours)
        {
            var quote = _quote;
            if (quote == null)
            {
                return;
            }

            var restored = Math.Min(quote.Missing, hours * quote.StaminaPerHour);
            if (hoursText != null)
            {
                hoursText.text = UiText.Format(_host.Texts, UiTextKey.Rest_Hours, hours);
            }

            if (recoveryText != null)
            {
                recoveryText.text = UiText.Format(_host.Texts, UiTextKey.Rest_Recovery, restored);
            }

            if (riskText != null)
            {
                riskText.text = quote.LocationIsHub
                    ? UiText.Get(_host.Texts, UiTextKey.Rest_Safe)
                    : UiText.Format(
                        _host.Texts,
                        UiTextKey.Rest_Risk,
                        (int)Math.Round(quote.AttackChance(hours) * 100));
            }

            if (confirmButton != null)
            {
                confirmButton.interactable = hours > 0;
            }
        }

        private void Confirm()
        {
            var hours = hoursSlider != null ? Mathf.RoundToInt(hoursSlider.value) : 0;
            if (hours <= 0)
            {
                return;
            }

            var action = _onConfirm;
            Hide();
            action?.Invoke(hours);
        }

        private void Hide()
        {
            _quote = null;
            _onConfirm = null;
            gameObject.SetActive(false);
        }
    }
}
