#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Редкость выпадающего лута: код варианта <c>rar:</c>, чтение редкости экземпляра,
    /// бросок ступени для оружия, брони и шлема, независимость от редкости шаблона.
    /// </summary>
    public sealed class LootRarityTests : DataTestBase
    {
        /// <summary>Создаёт категорию проверок редкости лута.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public LootRarityTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            CodecTests();
            ResolverTests();

            var services = BuildServices();
            var items = services.Resolve<IItemRepository>();
            var artifacts = services.Resolve<IArtifactService>();
            var source = services.Resolve<ILootRepository>();
            var rarities = services.Resolve<IRarityRepository>();

            RollTests(items, artifacts, source, rarities);
            RealLootTests(items, services.Resolve<ILootService>());
            TemplateIndependenceTests(items, artifacts, source, rarities);
            CategoryTests(items, artifacts, source, rarities);
            ArtifactLootTests(items, artifacts, source, rarities);
            FixedRarityTests(items, artifacts, source, rarities);
            JackpotDataTests(items, services.Resolve<ILootService>());
            ZeroWeightsTests(items, artifacts, source);
        }

        private void CodecTests()
        {
            Check("редкость лута: код варианта rar: пишется и читается", () =>
            {
                Equal("rar:epic", RarityVariantCodec.Format(Rarity.Epic), "код");
                True(RarityVariantCodec.TryParse("rar:legendary", out var rarity), "разбор");
                Equal(Rarity.Legendary, rarity, "ступень");
            });

            Check("редкость лута: чужой и битый код не читается", () =>
            {
                False(RarityVariantCodec.TryParse(null, out _), "null");
                False(RarityVariantCodec.TryParse(string.Empty, out _), "пустой");
                False(RarityVariantCodec.TryParse("art:epic", out _), "артефактный код");
                False(RarityVariantCodec.TryParse("epic", out _), "без префикса");
                False(RarityVariantCodec.TryParse("rar:mythic", out _), "неизвестная ступень");
            });
        }

        private void ResolverTests()
        {
            Check("редкость лута: код варианта сильнее шаблона", () =>
            {
                var item = new ItemInstance("i1", "wpn_akm", 1, variantKey: "rar:common");
                var template = Template(
                    "{\"kind\":\"items\",\"id\":\"wpn_akm\",\"nameKey\":\"n\",\"category\":\"trophy\"," +
                    "\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":3,\"rarity\":\"epic\",\"icon\":\"i\"}");
                Equal(Rarity.Common, ItemRarity.Resolve(item, template)!.Value, "выпавшая ступень");
            });

            Check("редкость лута: артефакт берёт ступень из своего кода", () =>
            {
                var item = new ItemInstance("i2", "art_fire_ember", 1, variantKey: "art:legendary:1");
                Equal(Rarity.Legendary, ItemRarity.Resolve(item, null)!.Value, "ступень артефакта");
            });

            Check("редкость лута: без варианта — ступень шаблона", () =>
            {
                var template = Template(
                    "{\"kind\":\"items\",\"id\":\"art_x\",\"nameKey\":\"n\",\"category\":\"artifact\"," +
                    "\"artifactType\":\"fire\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1," +
                    "\"rarity\":\"rare\",\"icon\":\"i\"}");
                var item = new ItemInstance("i3", "art_x", 1);
                Equal(Rarity.Rare, ItemRarity.Resolve(item, template)!.Value, "ступень шаблона");
            });

            Check("редкость лута: нет ни кода, ни шаблона — редкости нет", () =>
            {
                True(ItemRarity.Resolve(new ItemInstance("i4", "unknown", 1), null) == null, "null");
                True(ItemRarity.Resolve(null, null) == null, "нет экземпляра");
            });
        }

        private void RollTests(
            IItemRepository items,
            IArtifactService artifacts,
            ILootRepository source,
            IRarityRepository rarities)
        {
            var service = Probe(source, items, artifacts, rarities, Always("loot_probe", "wpn_ak74"));

            Check("редкость лута: оружие из пула получает брошенную ступень", () =>
            {
                var item = service.RollTable("loot_probe", 1, 7, true)[0];
                True(RarityVariantCodec.TryParse(item.VariantKey, out var rarity), "код редкости");
                Equal(rarity, ItemRarity.Resolve(item, items.Get(item.ItemId))!.Value, "редкость экземпляра");
            });

            Check("редкость лута: один seed даёт одну ступень", () =>
            {
                for (var seed = 1; seed <= 50; seed++)
                {
                    var first = service.RollTable("loot_probe", 1, seed, true)[0].VariantKey;
                    var second = service.RollTable("loot_probe", 1, seed, true)[0].VariantKey;
                    Equal(first, second, "seed " + seed);
                }
            });

            Check("редкость лута: выпадают все шесть ступеней по весам данных", () =>
            {
                var counts = new Dictionary<Rarity, int>();
                for (var seed = 1; seed <= 600; seed++)
                {
                    var item = service.RollTable("loot_probe", 1, seed, true)[0];
                    True(RarityVariantCodec.TryParse(item.VariantKey, out var rarity), "код");
                    counts[rarity] = counts.TryGetValue(rarity, out var seen) ? seen + 1 : 1;
                }

                Equal(6, counts.Count, "ступени");
                True(counts[Rarity.Common] > counts[Rarity.Rare], "обычная чаще редкой");
                True(counts[Rarity.Rare] > counts[Rarity.Special], "редкая чаще особой");
                True(counts[Rarity.Special] > counts[Rarity.Epic], "особая чаще эпической");
                True(counts[Rarity.Epic] > counts[Rarity.Legendary], "эпическая чаще легендарной");
                True(counts[Rarity.Legendary] > counts[Rarity.Unique], "легендарная чаще уникальной");
            });
        }

        private void RealLootTests(IItemRepository items, ILootService loot)
        {
            Check("редкость лута: из пула врага выпадает оружие со ступенью", () =>
            {
                ItemInstance? weapon = null;
                for (var seed = 1; seed <= 200 && weapon == null; seed++)
                {
                    foreach (var item in loot.Roll(
                                 new LootContext("enemy_merc", Rarity.Rare, "loot_human", 2, seed)))
                    {
                        if (items.Get(item.ItemId).Category != ItemCategory.Weapon)
                        {
                            continue;
                        }

                        weapon = item;
                        break;
                    }
                }

                True(weapon != null, "оружие из loot_human не выпало");
                True(RarityVariantCodec.TryParse(weapon!.VariantKey, out var rarity), "у оружия код редкости");
                Equal(rarity, ItemRarity.Resolve(weapon, items.Get(weapon.ItemId))!.Value, "ступень читается");
            });

            Check("редкость лута: ступень оружия не обязана совпадать с шаблонной", () =>
            {
                var differs = false;
                for (var seed = 1; seed <= 300 && !differs; seed++)
                {
                    foreach (var item in loot.Roll(
                                 new LootContext("enemy_merc", Rarity.Rare, "loot_human", 2, seed)))
                    {
                        var template = items.Get(item.ItemId);
                        if (template.Category != ItemCategory.Weapon)
                        {
                            continue;
                        }

                        if (RarityVariantCodec.TryParse(item.VariantKey, out var rolled) && rolled != template.Rarity)
                        {
                            differs = true;
                            break;
                        }
                    }
                }

                True(differs, "ступень отличается от шаблонной");
            });

            Check("редкость лута: из пула тайника выпадает оружие со ступенью", () =>
            {
                ItemInstance? weapon = null;
                for (var seed = 1; seed <= 300 && weapon == null; seed++)
                {
                    foreach (var item in loot.RollTable("loot_stash_1", 1, seed, ignoreTierBand: true))
                    {
                        if (items.Get(item.ItemId).Category != ItemCategory.Weapon)
                        {
                            continue;
                        }

                        weapon = item;
                        break;
                    }
                }

                True(weapon != null, "оружие из тайника не выпало");
                True(RarityVariantCodec.TryParse(weapon!.VariantKey, out _), "код редкости");
            });
        }

        private void TemplateIndependenceTests(
            IItemRepository items,
            IArtifactService artifacts,
            ILootRepository source,
            IRarityRepository rarities)
        {
            Check("редкость лута: сильный шаблон может выпасть обычным", () =>
            {
                var service = Probe(source, items, artifacts, rarities, Always("loot_probe", "wpn_akm"));
                var common = 0;
                for (var seed = 1; seed <= 200; seed++)
                {
                    if (RarityVariantCodec.TryParse(service.RollTable("loot_probe", 1, seed, true)[0].VariantKey, out var rarity) &&
                        rarity == Rarity.Common)
                    {
                        common++;
                    }
                }

                True(common > 0, "обычных АКМ: " + common);
            });

            Check("редкость лута: простой шаблон может выпасть выше эпической", () =>
            {
                var service = Probe(source, items, artifacts, rarities, Always("loot_probe", "wpn_pm"));
                var above = 0;
                for (var seed = 1; seed <= 200; seed++)
                {
                    if (RarityVariantCodec.TryParse(service.RollTable("loot_probe", 1, seed, true)[0].VariantKey, out var rarity) &&
                        (int)rarity >= (int)Rarity.Epic)
                    {
                        above++;
                    }
                }

                True(above > 0, "эпических и выше ПМ: " + above);
            });
        }

        private void CategoryTests(
            IItemRepository items,
            IArtifactService artifacts,
            ILootRepository source,
            IRarityRepository rarities)
        {
            Check("редкость лута: броня и шлем получают ступень", () =>
            {
                var service = Probe(
                    source,
                    items,
                    artifacts,
                    rarities,
                    Always("loot_armor", "itm_armor_jacket"),
                    Always("loot_helmet", "itm_helmet_light"));
                True(RarityVariantCodec.TryParse(service.RollTable("loot_armor", 1, 3, true)[0].VariantKey, out _), "броня");
                True(RarityVariantCodec.TryParse(service.RollTable("loot_helmet", 1, 3, true)[0].VariantKey, out _), "шлем");
            });

            Check("редкость лута: патроны редкости не получают", () =>
            {
                var service = Probe(source, items, artifacts, rarities, Always("loot_ammo", "ammo_9x18"));
                var item = service.RollTable("loot_ammo", 1, 3, true)[0];
                True(item.VariantKey == null, "варианта нет");
                True(ItemRarity.Resolve(item, items.Get(item.ItemId)) == null, "редкости нет");
            });
        }

        private void ArtifactLootTests(
            IItemRepository items,
            IArtifactService artifacts,
            ILootRepository source,
            IRarityRepository rarities)
        {
            Check("редкость лута: артефакт сохраняет свой код art:, а не rar:", () =>
            {
                var service = Probe(source, items, artifacts, rarities, Always("loot_art", "art_fire_ember"));
                var item = service.RollTable("loot_art", 1, 11, true)[0];
                True(item.VariantKey != null && item.VariantKey!.StartsWith("art:", StringComparison.Ordinal), "код артефакта");
                False(RarityVariantCodec.TryParse(item.VariantKey, out _), "не код редкости лута");
                True(ItemRarity.Resolve(item, items.Get(item.ItemId)) != null, "ступень читается");
            });
        }

        private void FixedRarityTests(
            IItemRepository items,
            IArtifactService artifacts,
            ILootRepository source,
            IRarityRepository rarities)
        {
            Check("редкость лута: заданная редкость записи заменяет бросок", () =>
            {
                var service = Probe(
                    source,
                    items,
                    artifacts,
                    rarities,
                    Fixed("loot_fixed", "wpn_akm", Rarity.Legendary));
                var item = service.RollTable("loot_fixed", 1, 7, true)[0];
                True(RarityVariantCodec.TryParse(item.VariantKey, out var rarity), "код редкости");
                Equal(Rarity.Legendary, rarity, "ступень");
            });

            Check("редкость лута: заданная редкость не зависит от seed", () =>
            {
                var service = Probe(
                    source,
                    items,
                    artifacts,
                    rarities,
                    Fixed("loot_fixed", "wpn_akm", Rarity.Epic));
                for (var seed = 1; seed <= 10; seed++)
                {
                    var item = service.RollTable("loot_fixed", 1, seed, true)[0];
                    True(RarityVariantCodec.TryParse(item.VariantKey, out var rarity), "код");
                    Equal(Rarity.Epic, rarity, "seed " + seed);
                }
            });
        }

        private void JackpotDataTests(IItemRepository items, ILootService loot)
        {
            Check("редкость лута: джекпот даёт гарантированные легендарные вещи", () =>
            {
                var looted = loot.RollTable("loot_jackpot", 1, 12345, true);
                foreach (var itemId in new[] { "wpn_akm", "itm_armor_exoskeleton", "itm_helmet_light" })
                {
                    ItemInstance? found = null;
                    foreach (var item in looted)
                    {
                        if (item.ItemId == itemId)
                        {
                            found = item;
                            break;
                        }
                    }

                    True(found != null, "в джекпоте нет " + itemId);
                    Equal(
                        Rarity.Legendary,
                        ItemRarity.Resolve(found, items.Get(itemId))!.Value,
                        "редкость " + itemId);
                }
            });
        }

        private void ZeroWeightsTests(
            IItemRepository items,
            IArtifactService artifacts,
            ILootRepository source)
        {
            Check("редкость лута: нулевые веса — редкость не выдаётся", () =>
            {
                var zero = new RarityRepository(new[]
                {
                    new RarityTemplate("rar_common", Rarity.Common, "n", 0, 0),
                    new RarityTemplate("rar_rare", Rarity.Rare, "n", 1, 0),
                    new RarityTemplate("rar_special", Rarity.Special, "n", 2, 0),
                    new RarityTemplate("rar_epic", Rarity.Epic, "n", 3, 0),
                    new RarityTemplate("rar_legendary", Rarity.Legendary, "n", 4, 0),
                    new RarityTemplate("rar_unique", Rarity.Unique, "n", 5, 0),
                });
                var service = Probe(source, items, artifacts, zero, Always("loot_probe", "wpn_ak74"));
                var item = service.RollTable("loot_probe", 1, 5, true)[0];
                True(item.VariantKey == null, "варианта нет");
            });
        }

        private static LootService Probe(
            ILootRepository source,
            IItemRepository items,
            IArtifactService artifacts,
            IRarityRepository rarities,
            params LootTable[] tables) =>
            new LootService(
                new LootRepository(tables, source.Settings),
                items,
                new SequentialInstanceIdGenerator(),
                artifacts,
                rarities);

        private static LootTable Always(string id, string itemId) =>
            new LootTable(id, Array.Empty<string>(), new[] { new LootEntry(itemId, 0, true, 1, 1, true) });

        private static LootTable Fixed(string id, string itemId, Rarity rarity) =>
            new LootTable(
                id,
                Array.Empty<string>(),
                new[] { new LootEntry(itemId, 0, true, 1, 1, true, rarity) });
    }
}
