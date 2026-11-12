#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Подсвечивает кнопку при наведении: обычный кадр с пустым индикатором и кадр с зелёным
    /// индикатором, когда кнопка выбрана мышью или с клавиатуры. Меняется только спрайт, поэтому
    /// затемнение нажатия и отключения остаётся у <see cref="Selectable"/>.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class ButtonArt : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Tooltip("Изображение кнопки; пусто — берётся с этого объекта")]
        [SerializeField]
        private Image? target = null;

        [Tooltip("Кадр, пока курсор не наведён и кнопка не выбрана")]
        [SerializeField]
        private Sprite? normal = null;

        [Tooltip("Кадр с зелёным индикатором для наведения и выбора")]
        [SerializeField]
        private Sprite? highlighted = null;

        private Button? _button;
        private bool _hovered;
        private bool _selected;

        /// <summary>Задаёт изображение и кадры состояний; вызывается сборщиком префаба и фабрикой интерфейса.</summary>
        /// <param name="image">Изображение кнопки.</param>
        /// <param name="normalSprite">Кадр без подсветки.</param>
        /// <param name="highlightedSprite">Кадр с зелёным индикатором.</param>
        public void Configure(Image image, Sprite? normalSprite, Sprite? highlightedSprite)
        {
            target = image;
            normal = normalSprite;
            highlighted = highlightedSprite;
            Apply();
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Apply();
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            Apply();
        }

        /// <inheritdoc />
        public void OnSelect(BaseEventData eventData)
        {
            _selected = true;
            Apply();
        }

        /// <inheritdoc />
        public void OnDeselect(BaseEventData eventData)
        {
            _selected = false;
            Apply();
        }

        private void OnDisable()
        {
            _hovered = false;
            _selected = false;
            if (target != null && normal != null)
            {
                target.sprite = normal;
            }
        }

        private void Apply()
        {
            if (target == null)
            {
                target = GetComponent<Image>();
            }

            if (target == null || normal == null)
            {
                return;
            }

            _button ??= GetComponent<Button>();
            var active = _hovered || _selected;
            if (_button != null && !_button.IsInteractable())
            {
                active = false;
            }

            target.sprite = active && highlighted != null ? highlighted : normal;
        }
    }
}
