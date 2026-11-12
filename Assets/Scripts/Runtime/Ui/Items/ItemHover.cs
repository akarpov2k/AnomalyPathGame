#nullable enable
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>
    /// Наведение на предмет: показывает подсказку с описанием и характеристиками и скрывает её,
    /// когда курсор ушёл, началось перетаскивание или предмет выключили. Наведение на товар
    /// торговца работает по шаблону: экземпляра у товара ещё нет.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ItemHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler
    {
        private ItemInstance? _item;
        private ItemTemplate? _template;
        private int _stack = 1;
        private ItemTooltip? _tooltip;

        /// <summary>Связывает плашку предмета с подсказкой.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="tooltip">Подсказка экрана.</param>
        public void Configure(ItemInstance item, ItemTooltip tooltip)
        {
            _item = item;
            _template = null;
            _stack = item.Stack;
            _tooltip = tooltip;
        }

        /// <summary>Связывает плашку товара торговца с подсказкой по данным шаблона.</summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="stack">Количество в стопке или остаток товара у торговца.</param>
        /// <param name="tooltip">Подсказка экрана.</param>
        public void Configure(ItemTemplate template, int stack, ItemTooltip tooltip)
        {
            _item = null;
            _template = template;
            _stack = stack;
            _tooltip = tooltip;
        }

        /// <inheritdoc />
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (eventData == null || _tooltip == null)
            {
                return;
            }

            if (_item != null)
            {
                _tooltip.Show(_item, eventData.position, eventData.pressEventCamera);
                return;
            }

            if (_template != null)
            {
                _tooltip.Show(_template, _stack, eventData.position, eventData.pressEventCamera);
            }
        }

        /// <inheritdoc />
        public void OnPointerExit(PointerEventData eventData) => _tooltip?.Hide();

        /// <inheritdoc />
        public void OnBeginDrag(PointerEventData eventData) => _tooltip?.Hide();

        private void OnDisable() => _tooltip?.Hide();
    }
}
