#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Механика инвентаря: стаки по ключу <c>(itemId, durability, variantKey)</c>,
    /// автоподбор места с поворотом и переполнение, когда места нет.
    /// </summary>
    public sealed class InventoryService : IInventoryService
    {
        private readonly IItemRepository _items;
        private readonly IInstanceIdGenerator _ids;
        private readonly double _overloadKg;

        /// <summary>Создаёт сервис поверх репозитория предметов.</summary>
        /// <param name="items">Репозиторий шаблонов.</param>
        /// <param name="ids">Генератор id экземпляров.</param>
        /// <param name="overloadKg">Разрешённый перегруз сверх лимита веса, килограммы.</param>
        public InventoryService(IItemRepository items, IInstanceIdGenerator ids, double overloadKg = 0)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _ids = ids ?? throw new ArgumentNullException(nameof(ids));
            _overloadKg = overloadKg < 0 || double.IsNaN(overloadKg) || double.IsInfinity(overloadKg)
                ? 0
                : overloadKg;
        }

        /// <inheritdoc />
        public bool TryAdd(GridContainer container, string itemId, int count, out IReadOnlyList<ItemInstance> overflow) =>
            TryAdd(container, itemId, count, null, null, null, out overflow);

        /// <inheritdoc />
        public bool TryAdd(
            GridContainer container,
            string itemId,
            int count,
            int? durability,
            string? variantKey,
            out IReadOnlyList<ItemInstance> overflow) =>
            TryAdd(container, itemId, count, durability, variantKey, null, out overflow);

        /// <inheritdoc />
        public bool TryAdd(
            GridContainer container,
            string itemId,
            int count,
            int? durability,
            string? variantKey,
            int? magazine,
            out IReadOnlyList<ItemInstance> overflow,
            string? ammoItemId = null)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(count),
                    CoreText.Format(CoreErrors.MinimumOne, nameof(count)));
            }

            var template = _items.Get(itemId);
            var stackSize = Math.Max(1, template.MaxStack);

            var durabilityValue = durability;
            var durabilityMax = template.Weapon?.DurabilityMax;
            if (template.Weapon != null && !durabilityValue.HasValue)
            {
                durabilityValue = durabilityMax;
            }

            var magazineValue = magazine;
            if (template.Weapon != null && !magazineValue.HasValue)
            {
                // Запас торговца и лут создаются разряженными; стартовый набор заряжает оружие сам.
                magazineValue = 0;
            }

            // Новый расходник приходит с полным числом применений; остаток живёт в экземпляре.
            var chargesValue = template.Consumable?.Charges;

            // Жёсткий лимит веса плюс разрешённый перегруз: если стопка целиком не влезает,
            // не добавляем ничего. Частичное добавление запрещено, иначе подбор одного и того же
            // лута стал бы неоднозначным.
            var weightLimit = container.WeightLimit;
            if (!double.IsPositiveInfinity(weightLimit) &&
                TotalWeight(container) + (template.Weight * count) > weightLimit + _overloadKg + 1e-9)
            {
                var rejected = new ItemInstance(
                    _ids.Next(),
                    itemId,
                    count,
                    durabilityValue,
                    durabilityMax,
                    variantKey,
                    magazineValue,
                    chargesValue,
                    ammoItemId);
                overflow = new[] { rejected };
                return false;
            }

            var remaining = count;
            StackIntoExisting(container, itemId, durabilityValue, variantKey, chargesValue, stackSize, ref remaining);

            var overflowItems = new List<ItemInstance>();
            while (remaining > 0)
            {
                var take = Math.Min(stackSize, remaining);
                remaining -= take;

                var instance = new ItemInstance(
                    _ids.Next(),
                    itemId,
                    take,
                    durabilityValue,
                    durabilityMax,
                    variantKey,
                    magazineValue,
                    chargesValue,
                    ammoItemId);
                if (!TryPlaceFirst(container, instance, template))
                {
                    container.AddOverflow(instance);
                    overflowItems.Add(instance);
                }
            }

            overflow = overflowItems;
            return true;
        }

        /// <inheritdoc />
        public double TotalWeight(GridContainer container)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var total = 0d;
            foreach (var item in container.Items)
            {
                if (_items.TryGet(item.ItemId, out var template))
                {
                    total += template.Weight * item.Stack;
                }
            }

            return total;
        }

        /// <inheritdoc />
        public bool CanFit(GridContainer container, System.Collections.Generic.IReadOnlyList<ItemInstance> items)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var probe = container.Clone();
            foreach (var item in items)
            {
                var accepted = TryAdd(probe, item.ItemId, item.Stack, item.Durability, item.VariantKey, out var overflow);
                if (!accepted || overflow.Count > 0)
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc />
        public bool Remove(GridContainer container, string instanceId)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var instance = container.Find(instanceId);
            return instance != null && container.Remove(instance);
        }

        /// <inheritdoc />
        public bool TryMove(GridContainer container, string instanceId, int x, int y, bool rotated)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var instance = container.Find(instanceId);
            if (instance == null)
            {
                return false;
            }

            var template = _items.Get(instance.ItemId);
            if (!container.CanPlace(template, x, y, rotated, instance.InstanceId))
            {
                return false;
            }

            container.Place(instance, template, x, y, rotated);
            return true;
        }

        /// <inheritdoc />
        public int TakeAmmo(GridContainer container, string caliber, int count) =>
            TakeAmmo(container, caliber, count, null, out _);

        /// <inheritdoc />
        public int TakeAmmo(
            GridContainer container,
            string caliber,
            int count,
            string? ammoItemId,
            out string? takenItemId)
        {
            takenItemId = null;
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (count <= 0)
            {
                return 0;
            }

            var candidates = new List<ItemInstance>();
            foreach (var item in container.Items)
            {
                if (item.Overflow || item.Stack <= 0)
                {
                    continue;
                }

                var template = _items.Get(item.ItemId);
                if (template.Category != ItemCategory.Ammo || !string.Equals(template.Caliber, caliber, StringComparison.Ordinal))
                {
                    continue;
                }

                if (ammoItemId != null && !string.Equals(item.ItemId, ammoItemId, StringComparison.Ordinal))
                {
                    continue;
                }

                candidates.Add(item);
            }

            candidates.Sort(AmmoOrder);
            if (candidates.Count == 0)
            {
                return 0;
            }

            // Весь заряд магазина одного вида: берём вид самой маленькой стопки.
            var targetId = candidates[0].ItemId;
            candidates.RemoveAll(item => !string.Equals(item.ItemId, targetId, StringComparison.Ordinal));
            var taken = 0;
            foreach (var item in candidates)
            {
                if (taken >= count)
                {
                    break;
                }

                var take = Math.Min(item.Stack, count - taken);
                item.Stack -= take;
                taken += take;
                if (item.Stack <= 0)
                {
                    Remove(container, item.InstanceId);
                }
            }

            takenItemId = taken > 0 ? targetId : null;
            return taken;
        }

        /// <summary>Порядок стопок: сначала маленькие, при равенстве — по id экземпляра.</summary>
        private static int AmmoOrder(ItemInstance left, ItemInstance right)
        {
            var byStack = left.Stack.CompareTo(right.Stack);
            return byStack != 0
                ? byStack
                : string.CompareOrdinal(left.InstanceId, right.InstanceId);
        }

        /// <inheritdoc />
        public int TakeAmmoFrom(GridContainer container, string instanceId, int count)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            if (count <= 0)
            {
                return 0;
            }

            var item = container.Find(instanceId);
            if (item == null || item.Overflow || item.Stack <= 0)
            {
                return 0;
            }

            var template = _items.Get(item.ItemId);
            if (template.Category != ItemCategory.Ammo)
            {
                return 0;
            }

            var take = Math.Min(item.Stack, count);
            item.Stack -= take;
            if (item.Stack <= 0)
            {
                Remove(container, item.InstanceId);
            }

            return take;
        }

        /// <inheritdoc />
        public int Count(GridContainer container, string itemId)
        {
            if (container == null)
            {
                throw new ArgumentNullException(nameof(container));
            }

            var total = 0;
            foreach (var item in container.Items)
            {
                if (item.ItemId == itemId)
                {
                    total += item.Stack;
                }
            }

            return total;
        }

        private static void StackIntoExisting(
            GridContainer container,
            string itemId,
            int? durability,
            string? variantKey,
            int? charges,
            int stackSize,
            ref int remaining)
        {
            foreach (var item in container.Items)
            {
                if (remaining == 0)
                {
                    return;
                }

                if (item.ItemId != itemId ||
                    item.Durability != durability ||
                    item.VariantKey != variantKey ||
                    item.Charges != charges ||
                    item.Stack >= stackSize)
                {
                    continue;
                }

                var space = stackSize - item.Stack;
                var take = Math.Min(space, remaining);
                item.Stack += take;
                remaining -= take;
            }
        }

        /// <summary>
        /// Ищет первое свободное место, добавляя ряды сетке, пока это разрешено.
        /// Сетка растёт только в пределах <see cref="GridContainer.MaxRows"/>.
        /// </summary>
        private static bool TryPlaceFirst(GridContainer container, ItemInstance instance, ItemTemplate template)
        {
            while (true)
            {
                if (TryPlaceInCurrentRows(container, instance, template))
                {
                    return true;
                }

                if (container.Rows >= container.MaxRows)
                {
                    return false;
                }

                container.Resize(container.Rows + 1);
            }
        }

        /// <summary>Ищет свободное место в текущей высоте сетки, без добавления рядов.</summary>
        private static bool TryPlaceInCurrentRows(GridContainer container, ItemInstance instance, ItemTemplate template)
        {
            for (var y = 0; y < container.Rows; y++)
            {
                for (var x = 0; x < container.Width; x++)
                {
                    if (container.CanPlace(template, x, y, rotated: false, exceptInstanceId: instance.InstanceId))
                    {
                        container.Place(instance, template, x, y, rotated: false);
                        return true;
                    }

                    if (template.Rotatable &&
                        container.CanPlace(template, x, y, rotated: true, exceptInstanceId: instance.InstanceId))
                    {
                        container.Place(instance, template, x, y, rotated: true);
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
