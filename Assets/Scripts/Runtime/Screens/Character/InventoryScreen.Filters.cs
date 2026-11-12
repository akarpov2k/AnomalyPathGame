#nullable enable
using System;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>Часть класса <see cref="InventoryScreen"/>: фильтры внизу окна инвентаря.</summary>
    public sealed partial class InventoryScreen
    {
        private InventoryFilter _filter = InventoryFilter.All;

        private Func<ItemInstance, bool>? FilterPredicate
        {
            get
            {
                if (_filter == InventoryFilter.All)
                {
                    return null;
                }

                return item =>
                {
                    var template = CharacterWidgets.Template(WidgetHost, item);
                    return template != null && Matches(template.Category);
                };
            }
        }

        private void BindFilters()
        {
            var count = Math.Min(filterButtons.Length, Enum.GetValues(typeof(InventoryFilter)).Length);
            for (var index = 0; index < count; index++)
            {
                var filter = (InventoryFilter)index;
                filterButtons[index].onClick.AddListener(() => SelectFilter(filter));
            }

            RefreshFilters();
        }

        private void SelectFilter(InventoryFilter filter)
        {
            _filter = filter;
            Show();
        }

        /// <summary>Выбранный фильтр — единственная неактивная кнопка ряда.</summary>
        private void RefreshFilters()
        {
            var count = Math.Min(filterButtons.Length, Enum.GetValues(typeof(InventoryFilter)).Length);
            for (var index = 0; index < count; index++)
            {
                filterButtons[index].interactable = (InventoryFilter)index != _filter;
            }
        }

        private bool Matches(ItemCategory category)
        {
            switch (_filter)
            {
                case InventoryFilter.Weapons:
                    return category == ItemCategory.Weapon;
                case InventoryFilter.Armor:
                    return category == ItemCategory.Armor ||
                           category == ItemCategory.Helmet ||
                           category == ItemCategory.Backpack ||
                           category == ItemCategory.Belt;
                case InventoryFilter.Consumables:
                    return category == ItemCategory.Consumable;
                default:
                    return category == ItemCategory.Ammo ||
                           category == ItemCategory.Artifact ||
                           category == ItemCategory.Trophy ||
                           category == ItemCategory.Material ||
                           category == ItemCategory.Quest;
            }
        }
    }
}
