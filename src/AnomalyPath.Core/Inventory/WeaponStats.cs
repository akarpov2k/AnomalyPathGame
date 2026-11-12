#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Статы оружия с поправкой на редкость: ступень экземпляра (выпавшая или шаблонная)
    /// даёт прибавку к урону и коэффициенту точности по данным <c>rarities[].weaponStatBonus</c>.
    /// Множитель равен <c>1 + weaponStatBonus</c>; у обычной ступени он 1, то есть без прибавки.
    /// </summary>
    public static class WeaponStats
    {
        /// <summary>Множитель урона и точности оружия по его редкости.</summary>
        /// <param name="instance">Экземпляр оружия; <c>null</c> — только шаблон.</param>
        /// <param name="template">Шаблон оружия; <c>null</c> или не оружие — множитель 1.</param>
        /// <param name="rarities">Редкости: поправки статов по ступеням.</param>
        /// <returns>Множитель не меньше 1; 1 — прибавки нет.</returns>
        /// <exception cref="ArgumentNullException">Репозиторий редкостей не задан.</exception>
        public static double Multiplier(ItemInstance? instance, ItemTemplate? template, IRarityRepository rarities)
        {
            if (rarities == null)
            {
                throw new ArgumentNullException(nameof(rarities));
            }

            if (template?.Weapon == null)
            {
                return 1;
            }

            var rarity = ItemRarity.Resolve(instance, template);
            if (rarity == null)
            {
                return 1;
            }

            return rarities.TryGet(RarityIds.Id(rarity.Value), out var rule)
                ? 1 + rule.WeaponStatBonus
                : 1;
        }
    }
}
