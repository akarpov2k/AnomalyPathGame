#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="CombatService"/>: применение медицины в бою.</summary>
    public sealed partial class CombatService
    {
        /// <inheritdoc />
        public bool CanUseMedicine(CombatState state, ItemInstance item)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (state.IsFinished || !ReferenceEquals(state.Current, state.Player))
            {
                return false;
            }

            var block = _items.Get(item.ItemId).Consumable;
            if (block == null || !block.UsableInCombat || ChargesOf(item, block) <= 0)
            {
                return false;
            }

            if (state.Player.Ap < block.UseTime + PlayerApPenalty(state, state.Player))
            {
                return false;
            }

            // В бою медицина применяется только из слота быстрого доступа.
            return QuickSlotIndex(state.Player.Equipment, item) >= 0;
        }

        /// <inheritdoc />
        public ConsumableUseResult TryUseMedicine(CombatState state, ItemInstance item)
        {
            if (!CanUseMedicine(state, item))
            {
                return ConsumableUseResult.Failed;
            }

            var block = _items.Get(item.ItemId).Consumable!;
            var player = state.Player;
            var healthRestored = HealthToRestore(player, block.TotalHeal());
            var curableInjuries = CountCurableInjuries(player, block);
            var apCost = block.UseTime + PlayerApPenalty(state, player);
            var apAfterCost = player.Ap - apCost;
            var apRoom = Math.Max(0, player.MaxAp - apAfterCost);
            var apRestored = Math.Min(block.TotalApRestore(), apRoom);
            var hasModifiers = HasModifiers(block);
            if (healthRestored == 0 && curableInjuries == 0 && apRestored == 0 && !hasModifiers)
            {
                // Лечить и восстанавливать нечего: попытка не состоялась, ОД и предмет целы.
                return ConsumableUseResult.Failed;
            }

            player.Ap = apAfterCost;
            if (healthRestored > 0)
            {
                player.Health += healthRestored;
            }

            player.Ap += apRestored;
            var healed = new List<string>();
            foreach (var injuryId in block.HealsInjuries)
            {
                if (player.RemoveStatus(injuryId))
                {
                    healed.Add(injuryId);
                }
            }

            ApplyModifiers(player, block, item);
            var charges = ChargesOf(item, block);
            var consumed = SpendCharge(player, item, charges);
            return new ConsumableUseResult(
                true,
                healthRestored,
                apRestored,
                0,
                0,
                healed,
                consumed ? 0 : charges - 1,
                consumed);
        }

        /// <inheritdoc />
        public ConsumableUseResult TryEnemyUseMedkit(CombatState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var enemy = state.Enemy;
            if (state.IsFinished || !ReferenceEquals(state.Current, enemy) || state.EnemyMedkitCharges <= 0)
            {
                return ConsumableUseResult.Failed;
            }

            var medkitItemId = enemy.Template.Combat?.MedkitItemId;
            if (medkitItemId == null)
            {
                return ConsumableUseResult.Failed;
            }

            var block = _items.Get(medkitItemId).Consumable;
            if (block == null || !block.UsableInCombat || enemy.Ap < block.UseTime)
            {
                return ConsumableUseResult.Failed;
            }

            var healthRestored = HealthToRestore(enemy, block.TotalHeal());
            if (healthRestored == 0)
            {
                return ConsumableUseResult.Failed;
            }

            enemy.Ap -= block.UseTime;
            enemy.Health += healthRestored;
            state.EnemyMedkitCharges--;
            var left = state.EnemyMedkitCharges;
            return new ConsumableUseResult(
                true,
                healthRestored,
                0,
                0,
                0,
                Array.Empty<string>(),
                left,
                left == 0);
        }

        /// <summary>Считает, сколько здоровья реально добавится: не больше максимума и не меньше нуля.</summary>
        private static int HealthToRestore(Combatant combatant, int amount)
        {
            var room = combatant.MaxHealth - combatant.Health;
            if (amount <= 0 || room <= 0)
            {
                return 0;
            }

            return (int)Math.Min(amount, Math.Floor(room));
        }

        /// <summary>Считает, сколько перечисленных травм у бойца действительно есть.</summary>
        private static int CountCurableInjuries(Combatant combatant, ConsumableBlock block)
        {
            var count = 0;
            foreach (var injuryId in block.HealsInjuries)
            {
                foreach (var status in combatant.Statuses)
                {
                    if (status.IsInjury && string.Equals(status.Id, injuryId, StringComparison.Ordinal))
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private static bool HasModifiers(ConsumableBlock block)
        {
            foreach (var effect in block.Effects)
            {
                if (effect.Type == ConsumableEffectType.Modifier)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Накладывает временные модификаторы медицины: один статус на предмет, до конца боя.</summary>
        private static void ApplyModifiers(Combatant combatant, ConsumableBlock block, ItemInstance item)
        {
            double accuracy = 0;
            var actionAp = 0;
            Dictionary<string, int>? protection = null;
            var any = false;
            foreach (var effect in block.Effects)
            {
                if (effect.Type != ConsumableEffectType.Modifier || !effect.Stat.HasValue)
                {
                    continue;
                }

                any = true;
                var stat = effect.Stat.Value;
                var whole = ConsumableStatCodec.IsInteger(stat)
                    ? (int)Math.Round(effect.Value, MidpointRounding.AwayFromZero)
                    : 0;
                switch (stat)
                {
                    case ConsumableStat.Accuracy:
                        accuracy += effect.Value;
                        break;
                    case ConsumableStat.Protection:
                        protection ??= new Dictionary<string, int>(StringComparer.Ordinal);
                        protection.TryGetValue(effect.DamageTypeId!, out var current);
                        protection[effect.DamageTypeId!] = current + whole;
                        break;
                    case ConsumableStat.ActionApCost:
                        actionAp += whole;
                        break;
                }
            }

            if (!any)
            {
                return;
            }

            combatant.ApplyStatus(new CombatStatus(
                item.ItemId,
                new CombatModifiers(accuracy, actionAp, protection)));
        }

        /// <summary>Индекс слота быстрого доступа, где лежит предмет; −1, если его там нет.</summary>
        private static int QuickSlotIndex(EquipmentState? equipment, ItemInstance item)
        {
            if (equipment == null)
            {
                return -1;
            }

            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                if (string.Equals(equipment.GetQuick(index)?.InstanceId, item.InstanceId, StringComparison.Ordinal))
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>Списывает применение; последнее убирает предмет из быстрого слота.</summary>
        private static bool SpendCharge(Combatant combatant, ItemInstance item, int charges)
        {
            var left = charges - 1;
            if (left > 0)
            {
                item.Charges = left;
                return false;
            }

            item.Charges = 0;
            var equipment = combatant.Equipment;
            var index = QuickSlotIndex(equipment, item);
            if (equipment != null && index >= 0)
            {
                equipment.SetQuick(index, null);
            }

            return true;
        }

        private static int ChargesOf(ItemInstance item, ConsumableBlock block) =>
            item.Charges ?? block.Charges;
    }
}
