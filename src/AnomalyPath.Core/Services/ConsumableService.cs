#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Расходники вне боя: лечение здоровья и травм, восполнение еды и воды по данным блока
    /// <c>consumable</c>. В бою те же эффекты применяет боевой сервис, потому что там они стоят ОД.
    /// </summary>
    public sealed class ConsumableService : IConsumableService
    {
        private readonly IItemRepository _items;

        /// <summary>Создаёт сервис расходников.</summary>
        /// <param name="items">Репозиторий предметов.</param>
        /// <exception cref="ArgumentNullException">Репозиторий не передан.</exception>
        public ConsumableService(IItemRepository items)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
        }

        /// <inheritdoc />
        public bool CanUse(ItemInstance item, bool inCombat)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var block = _items.Get(item.ItemId).Consumable;
            if (block == null || ChargesOf(item, block) <= 0)
            {
                return false;
            }

            return !inCombat || block.UsableInCombat;
        }

        /// <inheritdoc />
        public ConsumableUseResult UseOutOfCombat(PlayerState player, ItemInstance item, int maxHealth)
        {
            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var block = _items.Get(item.ItemId).Consumable;
            if (block == null)
            {
                return ConsumableUseResult.Failed;
            }

            var charges = ChargesOf(item, block);
            if (charges <= 0)
            {
                return ConsumableUseResult.Failed;
            }

            var healthLimit = maxHealth < 0 ? 0 : maxHealth;
            var healthRestored = RestoreAmount(player.Health, healthLimit, block.TotalHeal());
            var foodRestored = RestoreAmount(player.Needs.Hunger, player.Needs.FoodMax, block.TotalFoodRestore());
            var waterRestored = RestoreAmount(player.Needs.Thirst, player.Needs.WaterMax, block.TotalWaterRestore());
            var healed = new List<string>();
            foreach (var injuryId in block.HealsInjuries)
            {
                if (player.RemoveInjury(injuryId))
                {
                    healed.Add(injuryId);
                }
            }

            if (healthRestored == 0 && foodRestored == 0 && waterRestored == 0 && healed.Count == 0)
            {
                // Восполнять нечего: предмет не тратится.
                return ConsumableUseResult.Failed;
            }

            if (healthRestored > 0)
            {
                player.SetHealth(player.Health + healthRestored);
            }

            if (foodRestored > 0)
            {
                player.Needs.SetHunger(player.Needs.Hunger + foodRestored);
            }

            if (waterRestored > 0)
            {
                player.Needs.SetThirst(player.Needs.Thirst + waterRestored);
            }

            var remaining = SpendCharge(player, item, charges, block.Charges);
            return new ConsumableUseResult(
                true,
                healthRestored,
                0,
                foodRestored,
                waterRestored,
                healed,
                remaining,
                remaining == 0);
        }

        /// <summary>
        /// Считает, сколько реально добавится: не больше предела, не меньше нуля
        /// и ничего при полной шкале.
        /// </summary>
        private static int RestoreAmount(int current, int limit, int amount)
        {
            if (amount <= 0 || current >= limit)
            {
                return 0;
            }

            var target = Math.Min(limit, current + amount);
            return Math.Max(0, target - current);
        }

        /// <summary>
        /// Списывает применение: частичный расход оставляет заряды, последнее применение стопки
        /// уменьшает её на единицу и возвращает полные заряды следующей вещи, а последняя вещь
        /// убирается из инвентаря или слота.
        /// </summary>
        /// <param name="player">Состояние игрока.</param>
        /// <param name="item">Экземпляр, который применяют.</param>
        /// <param name="charges">Сколько применений у него осталось.</param>
        /// <param name="fullCharges">Сколько применений у новой вещи из данных.</param>
        /// <returns>Сколько применений осталось; 0 — предмет израсходован целиком.</returns>
        private static int SpendCharge(PlayerState player, ItemInstance item, int charges, int fullCharges)
        {
            var left = charges - 1;
            if (left > 0)
            {
                item.Charges = left;
                return left;
            }

            if (item.Stack > 1)
            {
                item.Stack -= 1;
                item.Charges = fullCharges;
                return fullCharges;
            }

            item.Charges = 0;
            RemoveItem(player, item);
            return 0;
        }

        private static void RemoveItem(PlayerState player, ItemInstance item)
        {
            var inGrid = player.Inventory.Find(item.InstanceId);
            if (inGrid != null)
            {
                player.Inventory.Remove(inGrid);
                return;
            }

            var equipment = player.Equipment;
            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                if (string.Equals(equipment.GetQuick(index)?.InstanceId, item.InstanceId, StringComparison.Ordinal))
                {
                    equipment.SetQuick(index, null);
                    return;
                }
            }

            foreach (var pair in equipment.Slots)
            {
                if (string.Equals(pair.Value.InstanceId, item.InstanceId, StringComparison.Ordinal))
                {
                    equipment.Set(pair.Key, null);
                    return;
                }
            }
        }

        private static int ChargesOf(ItemInstance item, ConsumableBlock block) =>
            item.Charges ?? block.Charges;
    }
}
