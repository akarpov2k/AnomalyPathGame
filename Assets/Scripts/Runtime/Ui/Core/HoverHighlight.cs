#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Подсветка кликабельного объекта на игровой сцене: при наведении мышью и при выборе
    /// с клавиатуры включается мягкое свечение позади объекта, картинка становится ярче,
    /// а сам объект немного увеличивается. Без подсветки объект возвращается к обычному виду.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverHighlight :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Graphic? _art;
        private GameObject? _glow;
        private Color _normalColor;
        private Color _highlightColor;
        private float _highlightScale = 1f;

        /// <summary>Включена ли сейчас подсветка.</summary>
        public bool IsHighlighted { get; private set; }

        /// <summary>Настраивает подсветку.</summary>
        /// <param name="art">Картинка объекта, которой меняется яркость.</param>
        /// <param name="glow">Объект свечения позади картинки; <c>null</c> — только осветление и масштаб.</param>
        /// <param name="normalColor">Цвет картинки без подсветки.</param>
        /// <param name="highlightColor">Цвет картинки при подсветке.</param>
        /// <param name="highlightScale">Множитель размера при подсветке.</param>
        /// <exception cref="ArgumentNullException">Картинка не задана.</exception>
        public void Configure(
            Graphic art,
            GameObject? glow,
            Color normalColor,
            Color highlightColor,
            float highlightScale)
        {
            _art = art != null ? art : throw new ArgumentNullException(nameof(art));
            _glow = glow;
            _normalColor = normalColor;
            _highlightColor = highlightColor;
            _highlightScale = highlightScale;
            SetHighlighted(false);
        }

        /// <summary>Меняет обычный цвет картинки: у указателя он темнее, чем у вырезки.</summary>
        /// <param name="color">Цвет картинки без подсветки.</param>
        public void SetNormalColor(Color color)
        {
            _normalColor = color;
            if (!IsHighlighted && _art != null)
            {
                _art.color = color;
            }
        }

        /// <summary>Включает или выключает подсветку.</summary>
        /// <param name="highlighted">Показать объект подсвеченным.</param>
        public void SetHighlighted(bool highlighted)
        {
            if (_art == null)
            {
                return;
            }

            IsHighlighted = highlighted;
            _art.color = highlighted ? _highlightColor : _normalColor;
            if (_glow != null)
            {
                _glow.SetActive(highlighted);
            }

            transform.localScale = highlighted ? Vector3.one * _highlightScale : Vector3.one;
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
    }
}
