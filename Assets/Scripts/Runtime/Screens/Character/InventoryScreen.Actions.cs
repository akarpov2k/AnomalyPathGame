#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>Часть класса <see cref="InventoryScreen"/>: меню предмета и перетаскивание.</summary>
    public sealed partial class InventoryScreen
    {
        /// <inheritdoc />
        public bool CanEquip(ItemInstance item)
        {
            var player = Player;
            if (player == null)
            {
                return false;
            }

            var template = CharacterWidgets.Template(WidgetHost, item);
            if (template == null)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(template.ArtifactType))
            {
                return true;
            }

            return Host.Actions.DefaultSlot(player.Equipment, item) != null;
        }

        /// <inheritdoc />
        public void OpenItemMenu(Vector2 position, ItemInstance item, EquipmentSlot? slot, int artifactIndex)
        {
            _tooltip.Hide();
            var entries = new List<KeyValuePair<string, Action>>();
            var template = CharacterWidgets.Template(WidgetHost, item);

            if (template?.Consumable != null && Host.Consumables.CanUse(item, inCombat: false))
            {
                var useId = item.InstanceId;
                entries.Add(new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Use),
                    () => _presenter.Use(useId)));
            }

            if (slot.HasValue)
            {
                var equipped = slot.Value;
                entries.Add(new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Unequip),
                    () => _presenter.Unequip(equipped)));
            }
            else if (artifactIndex >= 0)
            {
                var index = artifactIndex;
                entries.Add(new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Unequip),
                    () => _presenter.UnequipArtifact(index)));
            }
            else if (CanEquip(item))
            {
                var equipId = item.InstanceId;
                entries.Add(new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Equip),
                    () => _presenter.EquipAuto(equipId)));
            }

            if (template?.Weapon != null)
            {
                var loadId = item.InstanceId;
                entries.Add(new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Load),
                    () => _presenter.Load(loadId)));
                entries.Add(new KeyValuePair<string, Action>(
                    UiText.Get(Host.Texts, UiTextKey.Character_Menu_Unload),
                    () => _presenter.Unload(loadId)));
            }

            entries.Add(new KeyValuePair<string, Action>(
                UiText.Get(Host.Texts, UiTextKey.Character_Menu_Discard),
                () => _presenter.Discard(item.InstanceId)));

            _menu.Open(position, entries);
        }

        /// <inheritdoc />
        public void UseItem(string instanceId) => _presenter.Use(instanceId);

        /// <inheritdoc />
        public void DropOnSlot(string instanceId, EquipmentSlot slot)
        {
            if (_drag.Origin != DragOrigin.Inventory || string.IsNullOrEmpty(instanceId))
            {
                return;
            }

            var player = Player;
            var dragged = player?.Inventory.Find(instanceId);
            var equipped = player?.Equipment.Get(slot);
            if (dragged != null && equipped != null && IsAmmo(dragged) && IsWeapon(equipped))
            {
                _presenter.LoadFrom(equipped.InstanceId, dragged.InstanceId);
                return;
            }

            _presenter.Equip(instanceId, slot);
        }

        /// <inheritdoc />
        public void DropOnArtifact(string instanceId)
        {
            if (_drag.Origin == DragOrigin.Inventory)
            {
                _presenter.EquipArtifact(instanceId);
            }
        }

        /// <inheritdoc />
        public void DropOnQuick(string instanceId, int index)
        {
            if (string.IsNullOrEmpty(instanceId))
            {
                return;
            }

            if (_drag.Origin == DragOrigin.Inventory || _drag.Origin == DragOrigin.QuickSlot)
            {
                _presenter.PutQuick(instanceId, index);
            }
        }

        /// <inheritdoc />
        public void DropOnInventory(string instanceId, int x, int y, bool rotated)
        {
            var player = Player;
            if (player == null)
            {
                return;
            }

            if (_drag.Origin == DragOrigin.Inventory)
            {
                if (x < 0 || y < 0)
                {
                    return;
                }

                var dragged = player.Inventory.Find(instanceId);
                var target = player.Inventory.ItemAt(x, y);
                if (dragged != null && target != null &&
                    target.InstanceId != dragged.InstanceId &&
                    IsAmmo(dragged) &&
                    IsWeapon(target))
                {
                    _presenter.LoadFrom(target.InstanceId, dragged.InstanceId);
                    return;
                }

                _presenter.Move(instanceId, x, y, rotated);
                return;
            }

            if (_drag.Origin == DragOrigin.Equipment)
            {
                foreach (var pair in player.Equipment.Slots)
                {
                    if (pair.Value.InstanceId == instanceId)
                    {
                        _presenter.Unequip(pair.Key);
                        return;
                    }
                }

                return;
            }

            if (_drag.Origin == DragOrigin.QuickSlot)
            {
                var equipment = player.Equipment;
                for (var index = 0; index < equipment.QuickSlotCount; index++)
                {
                    var quick = equipment.GetQuick(index);
                    if (quick != null && quick.InstanceId == instanceId)
                    {
                        _presenter.TakeQuick(index);
                        return;
                    }
                }

                return;
            }

            for (var index = 0; index < player.Equipment.Artifacts.Count; index++)
            {
                var artifact = player.Equipment.Artifacts[index];
                if (artifact != null && artifact.InstanceId == instanceId)
                {
                    _presenter.UnequipArtifact(index);
                    return;
                }
            }
        }

        private void BindDropZones()
        {
            var frameZone = inventoryFrame.GetComponent<ItemDropZone>();
            if (frameZone == null)
            {
                frameZone = inventoryFrame.gameObject.AddComponent<ItemDropZone>();
            }

            frameZone.Configure(_drag, id => DropOnInventory(id, -1, -1, false));

            var limit = Math.Min(slotZones.Length, slotBodies.Length);
            for (var index = 0; index < limit; index++)
            {
                var zone = slotZones[index].GetComponent<ItemDropZone>();
                if (zone == null)
                {
                    zone = slotZones[index].gameObject.AddComponent<ItemDropZone>();
                }

                var slot = (EquipmentSlot)index;
                zone.Configure(_drag, id => DropOnSlot(id, slot));
            }
        }

        private bool IsAmmo(ItemInstance item) =>
            CharacterWidgets.Template(WidgetHost, item)?.Category == ItemCategory.Ammo;

        private bool IsWeapon(ItemInstance item) =>
            CharacterWidgets.Template(WidgetHost, item)?.Weapon != null;
    }
}
