#nullable enable
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Сообщает экрану о наведении на элемент: экран показывает подсказку с ценой действия
    /// и тем, что оно даёт, и прячет её, когда курсор ушёл. Обработчики задаёт код экрана.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HoverInfo : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        /// <summary>Курсор наведён на элемент.</summary>
        public Action? Entered { get; set; }

        /// <summary>Курсор ушёл с элемента или элемент выключили.</summary>
        public Action? Left { get; set; }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData) => Entered?.Invoke();

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData) => Left?.Invoke();

        private void OnDisable() => Left?.Invoke();
    }
}
