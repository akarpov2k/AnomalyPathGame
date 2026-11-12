#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="LootTests"/>: помощники — каталог, таблицы, настройки и JSON.</summary>
    public sealed partial class LootTests
    {
        private int CountDrops(LootService service, Rarity rarity, int attempts)
        {
            var drops = 0;
            for (var seed = 1; seed <= attempts; seed++)
            {
                drops += service.Roll(Context("loot_x", rarity, 1, seed)).Count;
            }

            return drops;
        }

        private string Describe(IReadOnlyList<ItemInstance> loot)
        {
            var parts = new List<string>();
            foreach (var item in loot)
            {
                parts.Add(item.ItemId + ":" + item.Stack);
            }

            return string.Join(",", parts);
        }

        private LootService Service(params LootTable[] tables) =>
            Service(tables, Catalog(("itm_a", 1), ("itm_b", 1), ("itm_c", 1)));

        private LootService Service(LootTable table, ItemRepository items) => Service(new[] { table }, items);

        private LootService Service(LootTable[] tables, ItemRepository items) =>
            new LootService(
                new LootRepository(tables, Bands()),
                items,
                new SequentialInstanceIdGenerator(),
                TestArtifacts.Service(items),
                LootRarities());

        private ItemRepository Catalog(params (string Id, int Tier)[] items)
        {
            var templates = new List<ItemTemplate>(items.Length);
            foreach (var item in items)
            {
                templates.Add(Template(ItemJson(item.Id, item.Tier)));
            }

            return new ItemRepository(templates);
        }

        private static LootSettings Bands() =>
            new LootSettings(new[]
            {
                new TierBand(-1, 0.2),
                new TierBand(0, 0.6),
                new TierBand(1, 0.2)
            });

        private static LootContext Context(string tableId, Rarity rarity, int tier, int seed) =>
            new LootContext("enemy_x", rarity, tableId, tier, seed);

        private static LootEntry Entry(
            string itemId,
            double chance,
            bool always = false,
            int min = 1,
            int max = 1,
            bool ignoreTierBand = false) =>
            new LootEntry(itemId, chance, always, min, max, ignoreTierBand);

        private static LootTable Table(string id, IReadOnlyList<string> shared, params LootEntry[] entries) =>
            new LootTable(id, shared, entries);

        private static void Near(double expected, double actual, string what)
        {
            if (Math.Abs(expected - actual) > 1e-9)
            {
                throw new InvalidOperationException(what + ": ожидалось " + expected + ", получено " + actual);
            }
        }

        private static string ItemJson(string id, int tier, string? rarity = null)
        {
            var rarityPart = rarity == null ? string.Empty : "\"rarity\":\"" + rarity + "\",";
            return "{\"kind\":\"items\",\"id\":\"" + id + "\",\"nameKey\":\"n\",\"category\":\"trophy\"," +
                   "\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":" + tier + "," + rarityPart + "\"icon\":\"i\"}";
        }

        private static string EnemyJson(string? rarity, string? lootTable)
        {
            var rarityPart = rarity == null ? string.Empty : "\"rarity\":\"" + rarity + "\",";
            var lootPart = lootTable == null ? string.Empty : "\"lootTable\":\"" + lootTable + "\",";
            return "{\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                   rarityPart + lootPart +
                   "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                   "\"attack\":{\"weaponId\":\"wpn_x\"}}}";
        }

        private static string SettingsJson(string id, double a = 0.2, double b = 0.6, double c = 0.2) =>
            "{\"kind\":\"loot\",\"id\":\"" + id + "\",\"tierBands\":[" +
            "{\"offset\":-1,\"chance\":" + a.ToString(CultureInfo.InvariantCulture) + "}," +
            "{\"offset\":0,\"chance\":" + b.ToString(CultureInfo.InvariantCulture) + "}," +
            "{\"offset\":1,\"chance\":" + c.ToString(CultureInfo.InvariantCulture) + "}]}";
    }
}
