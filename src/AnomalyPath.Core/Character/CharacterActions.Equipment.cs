#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Character
{
    /// <summary>Часть класса <see cref="CharacterActions"/>: снаряжение.</summary>
    public sealed partial class CharacterActions
    {
        /// <inheritdoc />
        public CharacterActionResult Equip(GridContainer inventory, EquipmentState equipment, string instanceId, EquipmentSlot slot)
        {
            Require(inventory, equipment);
            var item = FindInGrid(inventory, instanceId);
            if (item == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            if (!_equipment.CanEquip(item, slot))
            {
                return CharacterActionResult.WrongSlot;
            }

            var capacityAfter = CapacityAfterEquip(equipment, slot, item);
            var previous = equipment.Get(slot);
            if (previous != null)
            {
                var probe = inventory.Clone();
                probe.SetWeightLimit(capacityAfter);
                var clone = probe.Find(instanceId);
                if (clone != null)
                {
                    probe.Remove(clone);
                }

                if (!_inventory.CanFit(probe, new List<ItemInstance> { previous }))
                {
                    return CharacterActionResult.NoRoom;
                }
            }

            inventory.Remove(item);
            equipment.Set(slot, item);
            inventory.SetWeightLimit(capacityAfter);
            if (previous != null)
            {
                _inventory.TryAdd(
                    inventory,
                    previous.ItemId,
                    previous.Stack,
                    previous.Durability,
                    previous.VariantKey,
                    previous.Magazine,
                    out _,
                    previous.AmmoItemId);
            }

            return CharacterActionResult.Ok;
        }

        /// <inheritdoc />
        public CharacterActionResult Unequip(GridContainer inventory, EquipmentState equipment, EquipmentSlot slot)
        {
            Require(inventory, equipment);
            var item = equipment.Get(slot);
            if (item == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            var moving = new List<ItemInstance> { item };
            var freed = ArtifactsFreedByRemoving(equipment, slot);
            foreach (var index in freed)
            {
                var artifact = equipment.Artifacts[index];
                if (artifact != null)
                {
                    moving.Add(artifact);
                }
            }

            var capacityAfter = CapacityAfterUnequip(equipment, slot);
            var probe = inventory.Clone();
            // Снятие рюкзака снижает лимит, поэтому вещи вправе оказаться в перегрузе;
            // проверяется только место в клетках.
            probe.SetWeightLimit(slot == EquipmentSlot.Backpack ? double.PositiveInfinity : capacityAfter);
            foreach (var entry in moving)
            {
                _inventory.TryAdd(
                    probe,
                    entry.ItemId,
                    entry.Stack,
                    entry.Durability,
                    entry.VariantKey,
                    entry.Magazine,
                    out var overflow,
                    entry.AmmoItemId);
                if (overflow.Count > 0)
                {
                    return CharacterActionResult.NoRoom;
                }
            }

            equipment.Set(slot, null);
            foreach (var index in freed)
            {
                equipment.SetArtifact(index, null);
            }

            inventory.SetWeightLimit(slot == EquipmentSlot.Backpack ? double.PositiveInfinity : capacityAfter);
            foreach (var entry in moving)
            {
                _inventory.TryAdd(
                    inventory,
                    entry.ItemId,
                    entry.Stack,
                    entry.Durability,
                    entry.VariantKey,
                    entry.Magazine,
                    out _,
                    entry.AmmoItemId);
            }

            inventory.SetWeightLimit(capacityAfter);

            return CharacterActionResult.Ok;
        }

        private double CapacityAfterEquip(EquipmentState equipment, EquipmentSlot slot, ItemInstance item)
        {
            var current = equipment.Get(slot);
            var currentExtra = current == null ? 0 : _items.Get(current.ItemId).ExtraWeight;
            var nextExtra = _items.Get(item.ItemId).ExtraWeight;
            return _equipment.CarryCapacity(equipment, _baseCarryWeight) - currentExtra + nextExtra;
        }

        private double CapacityAfterUnequip(EquipmentState equipment, EquipmentSlot slot)
        {
            var current = equipment.Get(slot);
            var currentExtra = current == null ? 0 : _items.Get(current.ItemId).ExtraWeight;
            return _equipment.CarryCapacity(equipment, _baseCarryWeight) - currentExtra;
        }

        /// <inheritdoc />
        public CharacterActionResult EquipArtifact(GridContainer inventory, EquipmentState equipment, string instanceId)
        {
            Require(inventory, equipment);
            var item = FindInGrid(inventory, instanceId);
            if (item == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            var template = _items.Get(item.ItemId);
            if (string.IsNullOrWhiteSpace(template.ArtifactType))
            {
                return CharacterActionResult.NotArtifact;
            }

            if (!_equipment.TryEquipArtifact(equipment, item, out _))
            {
                return CharacterActionResult.NoFreeArtifactSlot;
            }

            inventory.Remove(item);
            return CharacterActionResult.Ok;
        }

        /// <inheritdoc />
        public CharacterActionResult UnequipArtifact(GridContainer inventory, EquipmentState equipment, int index)
        {
            Require(inventory, equipment);
            if (index < 0 || index >= equipment.Artifacts.Count)
            {
                return CharacterActionResult.ItemMissing;
            }

            var artifact = equipment.Artifacts[index];
            if (artifact == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            if (!_inventory.CanFit(inventory, new List<ItemInstance> { artifact }))
            {
                return CharacterActionResult.NoRoom;
            }

            equipment.SetArtifact(index, null);
            _inventory.TryAdd(
                inventory,
                artifact.ItemId,
                artifact.Stack,
                artifact.Durability,
                artifact.VariantKey,
                artifact.Magazine,
                out _,
                artifact.AmmoItemId);
            return CharacterActionResult.Ok;
        }
    }
}
