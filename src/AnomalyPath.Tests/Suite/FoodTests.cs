#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Еда и вода: небоевые расходники восполняют потребности, их данные, источники
    /// (торговцы, лут, противники), разбор эффектов и применение вне боя.
    /// </summary>
    public sealed class FoodTests : DataTestBase
    {
        private static readonly string[] FoodIds =
        {
            "itm_food_stew",
            "itm_water_bottle",
            "itm_food_soup",
            "itm_food_irp"
        };

        /// <summary>Создаёт категорию тестов еды и воды.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public FoodTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var items = services.Resolve<IItemRepository>();
            var consumables = services.Resolve<IConsumableService>();

            DataTests(items, consumables);
            SourceTests(items, services);
            UseTests(consumables, items);
            StackTests(services, items, consumables);
            ParserTests();
            ValidationTests();
        }

        private void DataTests(IItemRepository items, IConsumableService consumables)
        {
            Check("еда: четыре расходника еды и воды загружены", () =>
            {
                var count = 0;
                foreach (var item in items.All)
                {
                    if (item.Consumable != null &&
                        (item.Consumable.TotalFoodRestore() > 0 || item.Consumable.TotalWaterRestore() > 0))
                    {
                        count++;
                    }
                }

                Equal(4, count, "расходников еды и воды");
            });

            Check("еда: тушёнка восстанавливает 60 еды", () =>
            {
                var item = items.Get("itm_food_stew");
                var block = item.Consumable!;
                Equal(ItemCategory.Consumable, item.Category, "категория");
                False(block.UsableInCombat, "не боевая");
                Equal(0, item.EquipSlots.Count, "в быстрый слот не кладётся");
                Equal(1, block.Charges, "применений");
                Equal(60, block.TotalFoodRestore(), "еда");
                Equal(0, block.TotalWaterRestore(), "вода");
                Equal(0, block.TotalHeal(), "здоровье");
                True(item.Price != null && item.Price.Buy > 0 && item.Price.Sell > 0, "цена покупки и продажи");
            });

            Check("еда: бутылка воды восстанавливает 40 воды", () =>
            {
                var item = items.Get("itm_water_bottle");
                var block = item.Consumable!;
                False(block.UsableInCombat, "не боевая");
                Equal(0, item.EquipSlots.Count, "в быстрый слот не кладётся");
                Equal(0, block.TotalFoodRestore(), "еда");
                Equal(40, block.TotalWaterRestore(), "вода");
                True(item.Price != null && item.Price.Buy > 0, "цена покупки");
            });

            Check("еда: суп восстанавливает 30 еды и 30 воды", () =>
            {
                var item = items.Get("itm_food_soup");
                var block = item.Consumable!;
                False(block.UsableInCombat, "не боевая");
                Equal(30, block.TotalFoodRestore(), "еда");
                Equal(30, block.TotalWaterRestore(), "вода");
                Equal(0, block.TotalHeal(), "здоровье");
            });

            Check("еда: ИРП восстанавливает 100 еды", () =>
            {
                var item = items.Get("itm_food_irp");
                var block = item.Consumable!;
                False(block.UsableInCombat, "не боевая");
                Equal(100, block.TotalFoodRestore(), "еда");
                Equal(0, block.TotalWaterRestore(), "вода");
            });

            Check("еда: небоевые расходники не в быстром слоте и вне боя доступны", () =>
            {
                foreach (var itemId in FoodIds)
                {
                    var item = new ItemInstance("inst_" + itemId, itemId, 1);
                    True(consumables.CanUse(item, false), $"{itemId}: вне боя");
                    False(consumables.CanUse(item, true), $"{itemId}: в бою");
                }
            });

            Check("еда: тушёнка, вода и ИРП стакаются до 10", () =>
            {
                foreach (var itemId in new[] { "itm_food_stew", "itm_water_bottle", "itm_food_irp" })
                {
                    Equal(10, items.Get(itemId).MaxStack, $"{itemId}: предел стопки");
                }

                Equal(1, items.Get("itm_food_soup").MaxStack, "суп пока не стакается");
            });
        }

        private void SourceTests(IItemRepository items, ServiceContainer services)
        {
            var actors = services.Resolve<IActorRepository>();
            var loot = services.Resolve<ILootRepository>();

            Check("еда: продаётся у фельдшера и контрабандиста", () =>
            {
                foreach (var traderId in new[] { "trd_hub_medic", "trd_hub_smuggler" })
                {
                    var assortment = actors.Get(traderId).Trader!.Assortment;
                    foreach (var itemId in FoodIds)
                    {
                        True(HasOffer(assortment, itemId), $"{traderId}: нет {itemId}");
                        True(items.TryGet(itemId, out _), $"{traderId}: предмет {itemId} не загружен");
                    }
                }
            });

            Check("еда: лежит в пуле лута людей и выпадает с противников", () =>
            {
                var table = loot.Get("loot_human");
                foreach (var itemId in FoodIds)
                {
                    True(HasLootEntry(table, itemId), $"loot_human: нет {itemId}");
                }

                Equal("loot_human", actors.Get("enemy_bandit").LootTableId, "таблица лута бандита");
            });

            Check("еда: лежит в пуле низкого тайника", () =>
            {
                var table = loot.Get("loot_stash_0");
                True(HasLootEntry(table, "itm_food_stew"), "тайник: тушёнка");
                True(HasLootEntry(table, "itm_water_bottle"), "тайник: вода");
            });
        }

        private void UseTests(IConsumableService consumables, IItemRepository items)
        {
            Check("еда: тушёнка восполняет еду, не трогает воду и здоровье", () =>
            {
                var player = NewPlayer(50, 20, 100);
                var stew = AddToInventory(player, items, "itm_food_stew");

                var result = consumables.UseOutOfCombat(player, stew, 100);

                True(result.Succeeded, "применение");
                Equal(60, result.FoodRestored, "еда");
                Equal(0, result.WaterRestored, "вода");
                Equal(0, result.HealthRestored, "здоровье");
                Equal(80, player.Needs.Hunger, "сытость");
                Equal(100, player.Needs.Thirst, "питьё");
                Equal(50, player.Health, "здоровье не изменилось");
                True(result.ItemConsumed, "израсходован");
                True(player.Inventory.Find(stew.InstanceId) == null, "убран из сетки");
            });

            Check("еда: вода не поднимает питьё выше предела", () =>
            {
                var player = NewPlayer(100, 100, 80);
                var water = AddToInventory(player, items, "itm_water_bottle");

                var result = consumables.UseOutOfCombat(player, water, 100);

                True(result.Succeeded, "применение");
                Equal(0, result.FoodRestored, "еда");
                Equal(20, result.WaterRestored, "добавлено воды");
                Equal(100, player.Needs.Thirst, "предел");
                True(result.ItemConsumed, "израсходован");
            });

            Check("еда: суп восполняет обе шкалы", () =>
            {
                var player = NewPlayer(100, 50, 50);
                var soup = AddToInventory(player, items, "itm_food_soup");

                var result = consumables.UseOutOfCombat(player, soup, 100);

                True(result.Succeeded, "применение");
                Equal(30, result.FoodRestored, "еда");
                Equal(30, result.WaterRestored, "вода");
                Equal(80, player.Needs.Hunger, "сытость");
                Equal(80, player.Needs.Thirst, "питьё");
                True(result.ItemConsumed, "израсходован");
            });

            Check("еда: ИРП поднимает еду с нуля до предела", () =>
            {
                var player = NewPlayer(100, 0, 100);
                var irp = AddToInventory(player, items, "itm_food_irp");

                var result = consumables.UseOutOfCombat(player, irp, 100);

                True(result.Succeeded, "применение");
                Equal(100, result.FoodRestored, "еда");
                Equal(100, player.Needs.Hunger, "предел");
                True(result.ItemConsumed, "израсходован");
            });

            Check("еда: если обе шкалы полны — предмет не тратится", () =>
            {
                var player = NewPlayer(100, 100, 100);
                var soup = AddToInventory(player, items, "itm_food_soup");

                var result = consumables.UseOutOfCombat(player, soup, 100);

                False(result.Succeeded, "отказ");
                Equal(1, soup.Charges ?? -1, "заряды целы");
                True(player.Inventory.Find(soup.InstanceId) != null, "предмет на месте");
            });

            Check("еда: полная шкала не мешает второй", () =>
            {
                var player = NewPlayer(100, 100, 50);
                var soup = AddToInventory(player, items, "itm_food_soup");

                var result = consumables.UseOutOfCombat(player, soup, 100);

                True(result.Succeeded, "применение");
                Equal(0, result.FoodRestored, "еда не добавлена");
                Equal(30, result.WaterRestored, "вода");
                Equal(100, player.Needs.Hunger, "сытость на пределе");
                Equal(80, player.Needs.Thirst, "питьё");
                True(result.ItemConsumed, "израсходован");
            });

            Check("еда: без зарядов не применяется", () =>
            {
                var player = NewPlayer(100, 50, 50);
                var item = new ItemInstance("empty_food", "itm_food_stew", 1, charges: 0);

                False(consumables.CanUse(item, false), "нельзя");
                False(consumables.UseOutOfCombat(player, item, 100).Succeeded, "отказ");
                Equal(50, player.Needs.Hunger, "сытость не изменилась");
            });

            Check("еда: оружие не является расходником", () =>
            {
                var weapon = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 30);
                False(consumables.CanUse(weapon, false), "оружие не расходник");
            });
        }

        private void StackTests(ServiceContainer services, IItemRepository items, IConsumableService consumables)
        {
            Check("еда: одинаковые тушёнки сливаются в одну стопку", () =>
            {
                var inventory = services.Resolve<IInventoryService>();
                var player = NewPlayer(100, 0, 100);
                True(inventory.TryAdd(player.Inventory, "itm_food_stew", 5, out _), "добавление");
                inventory.TryAdd(player.Inventory, "itm_food_stew", 5, out _);

                var stacks = 0;
                foreach (var item in player.Inventory.Items)
                {
                    if (item.ItemId == "itm_food_stew")
                    {
                        stacks++;
                    }
                }

                Equal(1, stacks, "одна стопка");
                Equal(10, inventory.Count(player.Inventory, "itm_food_stew"), "количество");
            });

            Check("еда: применение из стопки уменьшает её на одну и оставляет остаток", () =>
            {
                var player = NewPlayer(100, 0, 100);
                var template = items.Get("itm_food_stew");
                var stew = new ItemInstance("inst_stew_stack", "itm_food_stew", 3, charges: template.Consumable!.Charges);
                player.Inventory.Place(stew, template, 0, 0, rotated: false);

                var result = consumables.UseOutOfCombat(player, stew, 100);

                True(result.Succeeded, "применение");
                Equal(60, result.FoodRestored, "еда");
                Equal(2, stew.Stack, "в стопке осталось две");
                False(result.ItemConsumed, "стопка не израсходована целиком");
                Equal(1, result.ChargesLeft, "у следующей штуки полные заряды");
                True(player.Inventory.Find(stew.InstanceId) != null, "стопка на месте");
            });

            Check("еда: последняя штука стопки убирается из инвентаря", () =>
            {
                var player = NewPlayer(100, 0, 100);
                var template = items.Get("itm_food_stew");
                var stew = new ItemInstance("inst_stew_last", "itm_food_stew", 2, charges: template.Consumable!.Charges);
                player.Inventory.Place(stew, template, 0, 0, rotated: false);

                True(consumables.UseOutOfCombat(player, stew, 100).Succeeded, "первое применение");
                Equal(1, stew.Stack, "осталась одна штука");
                var last = consumables.UseOutOfCombat(player, stew, 100);
                True(last.Succeeded, "второе применение");
                True(last.ItemConsumed, "последняя штука израсходована");
                True(player.Inventory.Find(stew.InstanceId) == null, "предмета в сетке нет");
            });
        }

        private void ParserTests()
        {
            Check("разбор: эффекты еды и воды читаются", () =>
            {
                var item = Parse(ConsumableJson(
                    "{\"useTime\":1,\"effects\":[" +
                    "{\"type\":\"restoreFood\",\"amount\":60}," +
                    "{\"type\":\"restoreWater\",\"amount\":40}]}"));

                Equal(60, item.Consumable!.TotalFoodRestore(), "еда");
                Equal(40, item.Consumable.TotalWaterRestore(), "вода");
            });

            Check("разбор: количество еды и воды больше нуля", () =>
            {
                Throws<DataException>(
                    () => Parse(ConsumableJson("{\"useTime\":1,\"effects\":[{\"type\":\"restoreFood\",\"amount\":0}]}")),
                    "еда 0");
                Throws<DataException>(
                    () => Parse(ConsumableJson("{\"useTime\":1,\"effects\":[{\"type\":\"restoreWater\",\"amount\":-1}]}")),
                    "вода −1");
            });

            Check("разбор: кодек типов эффектов еды и воды", () =>
            {
                Equal("restoreFood", ConsumableEffectCodec.ToCode(ConsumableEffectType.RestoreFood), "еда");
                Equal("restoreWater", ConsumableEffectCodec.ToCode(ConsumableEffectType.RestoreWater), "вода");
                True(ConsumableEffectCodec.TryParse("restoreFood", out var food), "разбор еды");
                Equal(ConsumableEffectType.RestoreFood, food, "еда");
                True(ConsumableEffectCodec.TryParse("restoreWater", out var water), "разбор воды");
                Equal(ConsumableEffectType.RestoreWater, water, "вода");
            });
        }

        private void ValidationTests()
        {
            Check("ошибка данных: у еды нет ни одного эффекта", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/food.json"),
                        ["items/food.json"] =
                            "{\"kind\":\"items\",\"id\":\"itm_food_empty\",\"nameKey\":\"n\"," +
                            "\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1," +
                            "\"icon\":\"i\",\"equipSlots\":[],\"consumable\":{\"useTime\":1,\"usableInCombat\":false}}"
                    },
                    root =>
                    {
                        var report = Preflight.Validate(root);
                        True(HasAnyError(report, "нет ни эффектов"), "нет ошибки эффектов: " + Describe(report));
                    });
            });
        }

        private static bool HasOffer(IReadOnlyList<TraderOfferTemplate> assortment, string itemId)
        {
            foreach (var offer in assortment)
            {
                if (string.Equals(offer.ItemId, itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasLootEntry(LootTable table, string itemId)
        {
            foreach (var entry in table.Entries)
            {
                if (string.Equals(entry.ItemId, itemId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static PlayerState NewPlayer(int health, int hunger, int thirst) =>
            new PlayerState(
                "player",
                1,
                0,
                health,
                8,
                new GridContainer("player", 8, 6),
                new Equipment("player", 4),
                Array.Empty<string>(),
                new NeedsState(hunger, thirst, 100));

        private static ItemTemplate Parse(string json) =>
            ItemTemplate.FromJson(JsonParser.Parse(json, "items"), "items");

        private static string ConsumableJson(string consumable) =>
            "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"itm_test\"," +
            "\"nameKey\":\"n\",\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1," +
            "\"tier\":1,\"icon\":\"i\",\"equipSlots\":[],\"consumable\":" + consumable + "}";
    }
}
