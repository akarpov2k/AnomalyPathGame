#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Character
{
    /// <summary>Часть класса <see cref="CharacterActions"/>: патроны.</summary>
    public sealed partial class CharacterActions
    {
        /// <inheritdoc />
        public CharacterActionResult Unload(GridContainer inventory, EquipmentState equipment, string instanceId, out int rounds)
        {
            Require(inventory, equipment);
            rounds = 0;
            var item = FindAnywhere(inventory, equipment, instanceId);
            if (item == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            var template = _items.Get(item.ItemId);
            if (template.Weapon == null)
            {
                return CharacterActionResult.NotAWeapon;
            }

            var inMagazine = item.MagazineRounds;
            if (inMagazine <= 0)
            {
                return CharacterActionResult.NothingToUnload;
            }

            var ammoId = item.AmmoItemId ?? FindAmmoId(template.Weapon.AmmoCaliber);
            if (ammoId == null)
            {
                return CharacterActionResult.AmmoTemplateMissing;
            }

            var probe = new ItemInstance("probe", ammoId, inMagazine);
            if (!_inventory.CanFit(inventory, new List<ItemInstance> { probe }))
            {
                return CharacterActionResult.NoRoom;
            }

            _inventory.TryAdd(inventory, ammoId, inMagazine, out _);
            item.Magazine = 0;
            item.AmmoItemId = null;
            rounds = inMagazine;
            return CharacterActionResult.Ok;
        }

        /// <inheritdoc />
        public CharacterActionResult Load(GridContainer inventory, EquipmentState equipment, string instanceId, out int rounds)
        {
            Require(inventory, equipment);
            rounds = 0;
            var item = FindAnywhere(inventory, equipment, instanceId);
            if (item == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            var template = _items.Get(item.ItemId);
            if (template.Weapon == null)
            {
                return CharacterActionResult.NotAWeapon;
            }

            var need = template.Weapon.Magazine - item.MagazineRounds;
            if (need <= 0)
            {
                return CharacterActionResult.MagazineFull;
            }

            var wanted = item.MagazineRounds > 0 ? item.AmmoItemId : null;
            var taken = _inventory.TakeAmmo(inventory, template.Weapon.AmmoCaliber, need, wanted, out var ammoId);
            if (taken <= 0)
            {
                return CharacterActionResult.NoAmmo;
            }

            item.Magazine = item.MagazineRounds + taken;
            if (item.AmmoItemId == null)
            {
                item.AmmoItemId = ammoId;
            }

            rounds = taken;
            return CharacterActionResult.Ok;
        }

        /// <inheritdoc />
        public CharacterActionResult LoadFrom(
            GridContainer inventory,
            EquipmentState equipment,
            string weaponInstanceId,
            string ammoInstanceId,
            out int rounds)
        {
            Require(inventory, equipment);
            rounds = 0;
            var weapon = FindAnywhere(inventory, equipment, weaponInstanceId);
            if (weapon == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            var weaponTemplate = _items.Get(weapon.ItemId);
            if (weaponTemplate.Weapon == null)
            {
                return CharacterActionResult.NotAWeapon;
            }

            var ammo = FindInGrid(inventory, ammoInstanceId);
            if (ammo == null)
            {
                return CharacterActionResult.ItemMissing;
            }

            var ammoTemplate = _items.Get(ammo.ItemId);
            if (ammoTemplate.Category != ItemCategory.Ammo)
            {
                return CharacterActionResult.NotAmmo;
            }

            if (!string.Equals(ammoTemplate.Caliber, weaponTemplate.Weapon.AmmoCaliber, StringComparison.Ordinal))
            {
                return CharacterActionResult.AmmoCaliberMismatch;
            }

            if (weapon.MagazineRounds > 0 && weapon.AmmoItemId != null &&
                !string.Equals(weapon.AmmoItemId, ammo.ItemId, StringComparison.Ordinal))
            {
                return CharacterActionResult.AmmoTypeMismatch;
            }

            var need = weaponTemplate.Weapon.Magazine - weapon.MagazineRounds;
            if (need <= 0)
            {
                return CharacterActionResult.MagazineFull;
            }

            var taken = _inventory.TakeAmmoFrom(inventory, ammoInstanceId, need);
            if (taken <= 0)
            {
                return CharacterActionResult.NoAmmo;
            }

            weapon.Magazine = weapon.MagazineRounds + taken;
            if (weapon.AmmoItemId == null)
            {
                weapon.AmmoItemId = ammo.ItemId;
            }

            rounds = taken;
            return CharacterActionResult.Ok;
        }
    }
}
