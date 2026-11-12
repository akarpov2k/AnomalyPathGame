#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Core.Quests
{
    /// <summary>
    /// Предметы награды квеста, подготовленные к выдаче как лут: в инвентарь они не кладутся,
    /// их забирает экран наград, а незабранное пропадает при закрытии.
    /// </summary>
    public static class QuestRewardLoot
    {
        /// <summary>Создаёт экземпляры предметов награды, не меняя инвентарь игрока.</summary>
        /// <param name="idPrefix">Префикс id экземпляров: вместе с индексом даёт уникальный ключ.</param>
        /// <param name="rewards">Награда квеста.</param>
        /// <param name="items">Шаблоны предметов: прочность новой вещи.</param>
        /// <returns>Предметы награды в порядке данных.</returns>
        /// <exception cref="ArgumentNullException">Награда или предметы не заданы.</exception>
        public static IReadOnlyList<ItemInstance> Build(
            string idPrefix,
            QuestRewardTemplate rewards,
            IItemRepository items)
        {
            if (rewards == null)
            {
                throw new ArgumentNullException(nameof(rewards));
            }

            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var loot = new List<ItemInstance>(rewards.Items.Count);
            for (var index = 0; index < rewards.Items.Count; index++)
            {
                var reward = rewards.Items[index];
                int? durability = null;
                if (items.TryGet(reward.ItemId, out var template))
                {
                    durability = ItemInstances.NewDurability(template);
                }

                loot.Add(new ItemInstance(
                    idPrefix + "#" + index,
                    reward.ItemId,
                    reward.Count,
                    durability,
                    durability));
            }

            return loot;
        }
    }
}
