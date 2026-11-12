#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Character
{
    /// <summary>Часть класса <see cref="CharacterActions"/>: быстрые слоты.</summary>
    public sealed partial class CharacterActions
    {
        /// <inheritdoc />
        public CharacterActionResult PutQuick(
            GridContainer inventory,
            EquipmentState equipment,
            string instanceId,
            int index)
        {
            Require(inventory, equipment);
            if (index < 0 || index >= equipment.QuickSlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    CoreText.Format(CoreErrors.IndexOutOfRange, nameof(index)));
            }

            var fromGrid = FindInGrid(inventory, instanceId);
            if (fromGrid != null)
            {
                if (!_equipment.CanUseQuick(fromGrid))
                {
                    return CharacterActionResult.NotQuickItem;
                }

                var previous = equipment.GetQuick(index);
                if (previous != null && !FitsAfterRemoval(inventory, fromGrid, previous))
                {
                    return CharacterActionResult.NoRoom;
                }

                inventory.Remove(fromGrid);
                equipment.SetQuick(index, fromGrid);
                if (previous != null)
                {
                    PutBack(inventory, previous);
                }

                return CharacterActionResult.Ok;
            }

            var fromQuick = QuickIndex(equipment, instanceId);
            if (fromQuick < 0)
            {
                return CharacterActionResult.ItemMissing;
            }

            if (fromQuick == index)
            {
                return CharacterActionResult.Ok;
            }

            var swapped = equipment.GetQuick(index);
            equipment.SetQuick(index, equipment.GetQuick(fromQuick));
            equipment.SetQuick(fromQuick, swapped);
            return CharacterActionResult.Ok;
        }

        /// <inheritdoc />
        public CharacterActionResult TakeQuick(GridContainer inventory, EquipmentState equipment, int index)
        {
            Require(inventory, equipment);
            if (index < 0 || index >= equipment.QuickSlotCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(index),
                    CoreText.Format(CoreErrors.IndexOutOfRange, nameof(index)));
            }

            var item = equipment.GetQuick(index);
            if (item == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            if (!_inventory.CanFit(inventory, new List<ItemInstance> { item }))
            {
                return CharacterActionResult.NoRoom;
            }

            equipment.SetQuick(index, null);
            PutBack(inventory, item);
            return CharacterActionResult.Ok;
        }

        private static int QuickIndex(EquipmentState equipment, string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return -1;
            }

            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                var item = equipment.GetQuick(index);
                if (item != null && string.Equals(item.InstanceId, instanceId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>Проверяет, помещается ли возвращаемый предмет, если убрать из сетки перемещаемый.</summary>
        private bool FitsAfterRemoval(GridContainer inventory, ItemInstance moving, ItemInstance returned)
        {
            var probe = inventory.Clone();
            var clone = probe.Find(moving.InstanceId);
            if (clone != null)
            {
                probe.Remove(clone);
            }

            return _inventory.CanFit(probe, new List<ItemInstance> { returned });
        }

        /// <summary>Возвращает предмет в инвентарь с сохранением прочности, магазина и варианта.</summary>
        private void PutBack(GridContainer inventory, ItemInstance item) =>
            _inventory.TryAdd(
                inventory,
                item.ItemId,
                item.Stack,
                item.Durability,
                item.VariantKey,
                item.Magazine,
                out _,
                item.AmmoItemId);
    }
}
