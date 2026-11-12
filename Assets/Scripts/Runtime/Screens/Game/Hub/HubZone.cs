#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Кликабельный объект хаба: в покое показывает обычную картинку предмета, при наведении
    /// мышью и выборе с клавиатуры подменяет её тем же предметом со свечением и просит
    /// подсказку показать своё название. Само действие вешает кнопка, на которой стоит компонент.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Tooltip("Код ключа названия объекта в каталоге текстов")]
        [SerializeField]
        private string nameKey = string.Empty;

        [Tooltip("Картинка объекта: в покое без свечения, при наведении — со свечением")]
        [SerializeField]
        private Image? art = null;

        [Tooltip("Спрайт объекта без свечения")]
        [SerializeField]
        private Sprite? normal = null;

        [Tooltip("Спрайт того же объекта со свечением")]
        [SerializeField]
        private Sprite? glow = null;

        [Tooltip("Подсказка экрана, которой объект сообщает своё название")]
        [SerializeField]
        private HubTooltip? tooltip = null;

        private string _nameText = string.Empty;

        /// <summary>Код ключа названия объекта.</summary>
        public string NameKey => nameKey;

        /// <summary>Подсвечен ли объект сейчас.</summary>
        public bool IsHighlighted { get; private set; }

        /// <summary>Задаёт код названия: у строки торговца это ключ имени актёра.</summary>
        /// <param name="key">Код ключа в каталоге текстов.</param>
        public void SetNameKey(string key) => nameKey = key ?? string.Empty;

        /// <summary>Задаёт готовый текст подсказки: например, действие значка вместе с ценой.</summary>
        /// <param name="text">Текст подсказки; пусто — подсказка снова берёт текст по ключу.</param>
        public void SetNameText(string text) => _nameText = text ?? string.Empty;

        /// <summary>Задаёт картинку объекта и пару спрайтов: обычный вид и вид со свечением.</summary>
        /// <param name="image">Картинка объекта, которой подменяется спрайт.</param>
        /// <param name="normalSprite">Спрайт без свечения.</param>
        /// <param name="glowSprite">Спрайт того же объекта со свечением.</param>
        public void Configure(Image image, Sprite? normalSprite, Sprite? glowSprite)
        {
            art = image;
            normal = normalSprite;
            glow = glowSprite;
            Apply();
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData) => SetHighlighted(true);

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData) => SetHighlighted(false);

        /// <inheritdoc />
        public void OnSelect(BaseEventData eventData) => SetHighlighted(true);

        /// <inheritdoc />
        public void OnDeselect(BaseEventData eventData) => SetHighlighted(false);

        private void OnDisable() => SetHighlighted(false);

        private void SetHighlighted(bool highlighted)
        {
            IsHighlighted = highlighted;
            Apply();

            if (tooltip == null)
            {
                return;
            }

            if (highlighted)
            {
                if (string.IsNullOrEmpty(_nameText))
                {
                    tooltip.Hover(nameKey);
                }
                else
                {
                    tooltip.HoverText(_nameText);
                }
            }
            else if (string.IsNullOrEmpty(_nameText))
            {
                tooltip.Leave(nameKey);
            }
            else
            {
                tooltip.Clear();
            }
        }

        private void Apply()
        {
            if (art == null)
            {
                return;
            }

            var sprite = IsHighlighted && glow != null ? glow : normal;
            if (sprite != null)
            {
                art.sprite = sprite;
            }
        }
    }
}
