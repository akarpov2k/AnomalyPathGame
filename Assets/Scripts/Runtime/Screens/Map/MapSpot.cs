#nullable enable
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Map
{
    /// <summary>
    /// Кликабельная вырезка на карте мира: подсвечивается при наведении, выборе с клавиатуры
    /// и пока её хаб выбран. Подписи у вырезки нет — объект виден по картинке, а подсветка
    /// показывает, что на него можно нажать. Действие вешает кнопка, на которой стоит компонент.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class MapSpot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [Tooltip("Подсветка вырезки, включается при наведении и у выбранного хаба")]
        [SerializeField]
        private Image? highlight = null;

        /// <summary>Курсор или клавиатурный выбор на вырезке.</summary>
        public bool IsPointed { get; private set; }

        /// <summary>Хаб этой вырезки выбран: на карте открыт выбор способа перехода.</summary>
        public bool IsSelected { get; private set; }

        /// <summary>Показывает вырезку подсвеченной, пока её хаб выбран.</summary>
        /// <param name="selected">Хаб выбран.</param>
        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            Apply();
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData)
        {
            IsPointed = true;
            Apply();
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData)
        {
            IsPointed = false;
            Apply();
        }

        /// <inheritdoc />
        public void OnSelect(BaseEventData eventData)
        {
            IsPointed = true;
            Apply();
        }

        /// <inheritdoc />
        public void OnDeselect(BaseEventData eventData)
        {
            IsPointed = false;
            Apply();
        }

        private void OnDisable()
        {
            IsPointed = false;
            IsSelected = false;
            Apply();
        }

        private void Apply()
        {
            if (highlight != null)
            {
                highlight.gameObject.SetActive(IsPointed || IsSelected);
            }
        }
    }
}
