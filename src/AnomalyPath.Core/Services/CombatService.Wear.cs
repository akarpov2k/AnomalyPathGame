#nullable enable
using System;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="CombatService"/>: износ и защита.</summary>
    public sealed partial class CombatService
    {
        private int ProtectionOf(Combatant defender, string damageType)
        {
            var own = defender.OwnProtection(damageType) + defender.Modifiers.ProtectionOf(damageType);
            if (defender.Equipment == null)
            {
                return own;
            }

            return own + _equipment.GetProtection(defender.Equipment, damageType);
        }

        private int RestoreAp(Combatant combatant) => _settings.ApRestore switch
        {
            ApRestoreKind.None => combatant.Ap,
            ApRestoreKind.Partial => Math.Min(
                combatant.MaxAp,
                combatant.Ap + _settings.ApPerTurn),
            _ => combatant.MaxAp
        };

        private void ApplyWear(ItemInstance? weapon, Combatant defender, int damage)
        {
            if (weapon != null)
            {
                ApplyWeaponWear(weapon);
            }

            ApplyArmorWear(defender, damage);
        }

        private void ApplyWeaponWear(ItemInstance weapon)
        {
            if (!weapon.Durability.HasValue || _items.Get(weapon.ItemId).Weapon is not { } weaponBlock)
            {
                return;
            }

            var wear = weaponBlock.DurabilityPerShot > 0
                ? Math.Max(1, (int)Math.Ceiling(weaponBlock.DurabilityPerShot))
                : 0;
            if (wear > 0)
            {
                weapon.Durability = Math.Max(0, weapon.Durability.Value - wear);
            }
        }

        private void ApplyArmorWear(Combatant defender, int damage)
        {
            if (damage <= 0 || defender.Equipment == null)
            {
                return;
            }

            var armor = defender.Equipment.Get(EquipmentSlot.Armor);
            if (armor != null)
            {
                _wear.ApplyWear(armor, damage);
            }

            var helmet = defender.Equipment.Get(EquipmentSlot.Helmet);
            if (helmet != null)
            {
                _wear.ApplyWear(helmet, damage);
            }
        }
    }
}
