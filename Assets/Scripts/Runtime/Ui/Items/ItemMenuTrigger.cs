#nullable enable
using System;
using AnomalyPath.Core.Ui;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>
    /// Открывает контекстное меню предмета: правый клик на компьютере и удержание на телефоне.
    /// Двойной клик левой кнопкой отдаётся отдельному обработчику.
    /// </summary>
    public sealed class ItemMenuTrigger : MonoBehaviour,
        IPointerClickHandler,
        IPointerDownHandler,
        IPointerUpHandler,
        IBeginDragHandler
    {
        private readonly LongPressGesture _longPress = new LongPressGesture(UiInputMetrics.LongPressSeconds);
        private Action<Vector2> _onRightClick = null!;
        private Action<Vector2>? _onDoubleClick;
        private Vector2 _pressedAt;

        /// <summary>Настраивает обработчики кликов.</summary>
        /// <param name="onRightClick">Куда передать позицию курсора по правому клику или удержанию.</param>
        /// <param name="onDoubleClick">Двойной клик левой кнопкой; <c>null</c> — не обрабатывается.</param>
        public void Configure(Action<Vector2> onRightClick, Action<Vector2>? onDoubleClick = null)
        {
            _onRightClick = onRightClick;
            _onDoubleClick = onDoubleClick;
        }

        /// <inheritdoc />
        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || _longPress.Fired)
            {
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                _onRightClick?.Invoke(eventData.position);
                return;
            }

            if (eventData.button == PointerEventData.InputButton.Left &&
                eventData.clickCount >= 2 &&
                !eventData.dragging)
            {
                _onDoubleClick?.Invoke(eventData.position);
            }
        }

        /// <inheritdoc />
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pressedAt = eventData.position;
            _longPress.Begin();
        }

        /// <inheritdoc />
        public void OnPointerUp(PointerEventData eventData) => _longPress.Cancel();

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData) => _longPress.Cancel();

        private void Update()
        {
            if (_longPress.Advance(Time.unscaledDeltaTime))
            {
                _onRightClick?.Invoke(_pressedAt);
            }
        }

        private void OnDisable() => _longPress.Cancel();
    }
}
