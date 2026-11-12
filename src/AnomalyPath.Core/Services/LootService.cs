#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Бросок лута: каждая запись пула проверяется отдельно — полоса тира, затем
    /// шанс с множителем редкости противника. Запись <c>always</c> падает без бросков.
    /// Оружие, броня и шлем получают независимо брошенную ступень редкости; артефакт —
    /// свою ступень из пула артефактов.
    /// </summary>
    public sealed class LootService : ILootService
    {
        private readonly ILootRepository _loot;
        private readonly IItemRepository _items;
        private readonly IInstanceIdGenerator _ids;
        private readonly IArtifactService _artifacts;
        private readonly IRarityRepository _rarities;
        private readonly int _minItemTier;
        private readonly int _maxItemTier;

        /// <summary>Создаёт сервис лута.</summary>
        /// <param name="loot">Таблицы лута и полосы тира.</param>
        /// <param name="items">Каталог предметов: тир и существование id.</param>
        /// <param name="ids">Генератор id экземпляров.</param>
        /// <param name="artifacts">Артефакты: бросок редкости найденной копии.</param>
        /// <param name="rarities">Редкости: веса выпадения ступени у найденного предмета.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public LootService(
            ILootRepository loot,
            IItemRepository items,
            IInstanceIdGenerator ids,
            IArtifactService artifacts,
            IRarityRepository rarities)
        {
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _ids = ids ?? throw new ArgumentNullException(nameof(ids));
            _artifacts = artifacts ?? throw new ArgumentNullException(nameof(artifacts));
            _rarities = rarities ?? throw new ArgumentNullException(nameof(rarities));

            var min = int.MaxValue;
            var max = int.MinValue;
            foreach (var item in items.All)
            {
                if (item.Tier < min)
                {
                    min = item.Tier;
                }

                if (item.Tier > max)
                {
                    max = item.Tier;
                }
            }

            _minItemTier = min == int.MaxValue ? 0 : min;
            _maxItemTier = max == int.MinValue ? -1 : max;
        }

        /// <inheritdoc />
        public IReadOnlyList<ItemInstance> Roll(LootContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (string.IsNullOrWhiteSpace(context.LootTableId))
            {
                return Array.Empty<ItemInstance>();
            }

            return RollTableCore(
                context.LootTableId!,
                context.LocationTier,
                context.Seed,
                ignoreTierBand: false,
                (int)context.EnemyRarity);
        }

        /// <inheritdoc />
        public IReadOnlyList<ItemInstance> RollTable(
            string lootTableId,
            int locationTier,
            int seed,
            bool ignoreTierBand)
        {
            if (string.IsNullOrWhiteSpace(lootTableId))
            {
                return Array.Empty<ItemInstance>();
            }

            return RollTableCore(lootTableId, locationTier, seed, ignoreTierBand, multiplier: 1);
        }

        private IReadOnlyList<ItemInstance> RollTableCore(
            string lootTableId,
            int locationTier,
            int seed,
            bool ignoreTierBand,
            int multiplier)
        {
            if (!_loot.TryGet(lootTableId, out var table))
            {
                return Array.Empty<ItemInstance>();
            }

            var entries = new List<LootEntry>();
            Collect(table, entries, new HashSet<string>(StringComparer.Ordinal));
            if (entries.Count == 0)
            {
                return Array.Empty<ItemInstance>();
            }

            var random = new SeededWorldRandom(seed);
            var result = new List<ItemInstance>();

            for (var index = 0; index < entries.Count; index++)
            {
                var entry = entries[index];
                if (!_items.TryGet(entry.ItemId, out var template))
                {
                    continue;
                }

                if (!entry.Always)
                {
                    if (!ignoreTierBand && !entry.IgnoreTierBand)
                    {
                        var band = _loot.Settings.ChanceForTier(
                            template.Tier,
                            locationTier,
                            _minItemTier,
                            _maxItemTier);
                        if (random.NextDouble() >= band)
                        {
                            continue;
                        }
                    }

                    var chance = entry.Chance * multiplier;
                    if (chance < 1 && random.NextDouble() >= chance)
                    {
                        continue;
                    }
                }

                var count = entry.CountMax > entry.CountMin
                    ? random.NextInt(entry.CountMin, entry.CountMax + 1)
                    : entry.CountMin;
                var instanceId = _ids.Next();
                var variantKey = _artifacts.RollVariantKey(
                    entry.ItemId,
                    StableHash.Combine(seed, "artifact:" + instanceId));
                if (variantKey == null && RollsRarity(template.Category))
                {
                    var rolled = entry.FixedRarity ??
                                 RollRarity(StableHash.Combine(
                                     seed,
                                     "rarity:" + index.ToString(CultureInfo.InvariantCulture)));
                    if (rolled.HasValue)
                    {
                        variantKey = RarityVariantCodec.Format(rolled.Value);
                    }
                }

                result.Add(new ItemInstance(instanceId, entry.ItemId, count, variantKey: variantKey));
            }

            return result;
        }

        /// <summary><c>true</c> — категория получает брошенную редкость экземпляра.</summary>
        /// <param name="category">Категория шаблона.</param>
        /// <returns>Оружие, броня и шлем.</returns>
        private static bool RollsRarity(ItemCategory category) =>
            category == ItemCategory.Weapon || category == ItemCategory.Armor || category == ItemCategory.Helmet;

        /// <summary>
        /// Бросает ступень редкости по весам данных: сумма весов нормирует бросок,
        /// ступень с нулевым весом не выпадает. Все веса нулевые — редкость не выдаётся.
        /// </summary>
        /// <param name="seed">Seed броска: один seed даёт одну ступень.</param>
        /// <returns>Выпавшая ступень или <c>null</c>, если веса не заданы.</returns>
        private Rarity? RollRarity(int seed)
        {
            var total = 0.0;
            foreach (var rarity in _rarities.All)
            {
                if (rarity.DropWeight > 0)
                {
                    total += rarity.DropWeight;
                }
            }

            if (total <= 0)
            {
                return null;
            }

            var roll = new SeededWorldRandom(seed).NextDouble() * total;
            Rarity? last = null;
            foreach (var rarity in _rarities.All)
            {
                if (rarity.DropWeight <= 0)
                {
                    continue;
                }

                last = rarity.Rarity;
                if (roll < rarity.DropWeight)
                {
                    return rarity.Rarity;
                }

                roll -= rarity.DropWeight;
            }

            return last;
        }

        private void Collect(LootTable table, List<LootEntry> entries, HashSet<string> visited)
        {
            if (!visited.Add(table.Id))
            {
                return;
            }

            foreach (var sharedId in table.SharedTableIds)
            {
                if (_loot.TryGet(sharedId, out var shared))
                {
                    Collect(shared, entries, visited);
                }
            }

            entries.AddRange(table.Entries);
        }
    }
}
