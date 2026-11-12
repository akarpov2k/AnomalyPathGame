#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Наведение и выбор карточки торговца: сообщает наружу позицию, у которой показать
    /// имя торговца, и скрывает подсказку, когда курсор ушёл или карточка потеряла фокус.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TraderCardHover : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler,
        ISelectHandler,
        IDeselectHandler
    {
        private Action<Vector2, Camera?>? _show;
        private Action<Vector2, Camera?>? _move;
        private Action? _hide;
        private bool _shown;

        /// <summary>Подключает показ, перемещение и скрытие подсказки с именем торговца.</summary>
        /// <param name="show">Показать подсказку в точке экрана.</param>
        /// <param name="move">Переместить уже показанную подсказку.</param>
        /// <param name="hide">Скрыть подсказку.</param>
        public void Configure(
            Action<Vector2, Camera?> show,
            Action<Vector2, Camera?> move,
            Action hide)
        {
            _show = show;
            _move = move;
            _hide = hide;
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData) =>
            ShowAt(eventData.position, eventData.enterEventCamera);

        /// <inheritdoc />
        public void OnPointerMove(PointerEventData eventData)
        {
            if (_shown)
            {
                _move?.Invoke(eventData.position, eventData.enterEventCamera);
            }
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData) => Hide();

        /// <summary>Выбор карточки с клавиатуры показывает имя над её центром.</summary>
        /// <param name="eventData">Данные события выбора.</param>
        public void OnSelect(BaseEventData eventData)
        {
            var rect = (RectTransform)transform;
            var screen = RectTransformUtility.WorldToScreenPoint(null, rect.position);
            ShowAt(screen, null);
        }

        /// <inheritdoc />
        public void OnDeselect(BaseEventData eventData) => Hide();

        private void OnDisable() => Hide();

        private void ShowAt(Vector2 position, Camera? camera)
        {
            if (_show == null)
            {
                return;
            }

            _shown = true;
            _show(position, camera);
        }

        private void Hide()
        {
            _shown = false;
            _hide?.Invoke();
        }
    }
}
