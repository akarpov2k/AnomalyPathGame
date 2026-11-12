#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Talents;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="FileFormatTests"/>: коллекции, наследование, null и массивы.</summary>
    public sealed partial class FileFormatTests
    {
        private void ExtraFormatTests()
        {
            Check("формат: экономика грузится как сущность economy", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("economy.json"),
                        ["economy.json"] = "{\"schemaVersion\":1,\"kind\":\"economy\",\"id\":\"economy\"," +
                                           "\"rounding\":5,\"minPrice\":10,\"repairCostFactor\":0.25}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("economy").Count, "economy");
                        Equal(5.0, data.OfKind("economy")["economy"].Get("rounding").Number, "rounding");
                        Equal(0.25, data.OfKind("economy")["economy"].Get("repairCostFactor").Number, "repairCostFactor");
                    });
            });

            Check("формат: квест грузится как сущность quests", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_x.json"),
                        ["quests/qst_x.json"] = "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\"," +
                                                "\"questKind\":\"repeatable\",\"questType\":\"killMutants\"," +
                                                "\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                                                "\"art\":\"quests/qst_x\"," +
                                                "\"failCondition\":\"time\",\"timeLimitHours\":24," +
                                                "\"target\":{\"count\":2,\"poolSize\":1,\"minTier\":1,\"maxTier\":1}," +
                                                "\"rewards\":{\"money\":100}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("quests").Count, "quests");
                        var template = QuestTemplate.FromJson(
                            data.OfKind("quests")["qst_x"],
                            "quests/qst_x.json");
                        Equal(QuestType.KillMutants, template.Type, "тип");
                        Equal(QuestFailCondition.Time, template.FailCondition, "условие провала");
                        Equal(24, template.TimeLimitHours, "срок");
                        Equal(2, template.KillMutants!.Count, "количество");
                        Equal(100, template.Rewards.Money, "награда");
                        Equal("quests/qst_x", template.Art, "картинка квеста");
                    });
            });

            Check("формат: у квеста без поля art картинки нет", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_y.json"),
                        ["quests/qst_y.json"] = "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_y\"," +
                                                "\"questKind\":\"repeatable\",\"questType\":\"killMutants\"," +
                                                "\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                                                "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":0}," +
                                                "\"rewards\":{\"money\":10}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var template = QuestTemplate.FromJson(
                            data.OfKind("quests")["qst_y"],
                            "quests/qst_y.json");
                        True(template.Art == null, "картинка не задана");
                    });
            });

            Check("формат: дерево талантов с вложенными эффектами", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("talents.json"),
                        ["talents.json"] = "{\"schemaVersion\":1,\"kind\":\"talents\",\"id\":\"talents\"," +
                                           "\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"nodes\":[" +
                                           "{\"id\":\"tal_a\",\"titleKey\":\"a\",\"descriptionKey\":\"b\"," +
                                           "\"icon\":\"icons/tal_a\",\"row\":0,\"column\":0,\"maxRank\":2," +
                                           "\"requiredPoints\":0," +
                                           "\"effects\":[{\"stat\":\"staminaRecovery\",\"perRank\":1}]}]}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("talents").Count, "talents");
                        var tree = TalentTreeTemplate.FromJson(data.OfKind("talents")["talents"], "talents");
                        Equal(1, tree.Nodes.Count, "талантов");
                        Equal(2, tree.Nodes[0].MaxRank, "рангов");
                        Equal("icons/tal_a", tree.Nodes[0].Icon, "картинка");
                        Equal(TalentStat.StaminaRecovery, tree.Nodes[0].Effects[0].Stat, "характеристика");
                        Equal(1.0, tree.Nodes[0].Effects[0].PerRank, "прибавка");
                    });
            });

            Check("формат: таблица лута и настройка полос", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("loot.json", "loot/loot_tbl.json"),
                        ["loot.json"] = "{\"schemaVersion\":1,\"kind\":\"loot\",\"id\":\"loot\"," +
                                        "\"tierBands\":[{\"offset\":-1,\"chance\":0.2}," +
                                        "{\"offset\":0,\"chance\":0.6},{\"offset\":1,\"chance\":0.2}]," +
                                        "\"stashes\":{\"baseChance\":0.05,\"chanceGrowth\":0.02," +
                                        "\"chanceCap\":0.5,\"jackpotBaseChance\":0.01," +
                                        "\"jackpotChanceGrowth\":0.01,\"jackpotChanceCap\":1.0," +
                                        "\"pools\":[{\"tier\":1,\"lootTableId\":\"loot_tbl\"}]," +
                                        "\"jackpotLootTableId\":\"loot_tbl\"}}",
                        ["loot/loot_tbl.json"] = "{\"schemaVersion\":1,\"kind\":\"lootTables\",\"id\":\"loot_tbl\"," +
                                                 "\"sharedTableIds\":[\"loot_common\"],\"entries\":[" +
                                                 "{\"itemId\":\"itm_a\",\"chance\":0.5,\"count\":[1,3]}," +
                                                 "{\"itemId\":\"itm_b\",\"always\":true,\"rarity\":\"legendary\"}]}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("lootTables").Count, "lootTables");
                        var table = LootTable.FromJson(data.OfKind("lootTables")["loot_tbl"], "loot");
                        Equal("loot_tbl", table.Id, "id таблицы");
                        Equal(1, table.SharedTableIds.Count, "общих таблиц");
                        Equal(2, table.Entries.Count, "записей");
                        Equal("itm_a", table.Entries[0].ItemId, "предмет");
                        Equal(3, table.Entries[0].CountMax, "максимум стопки");
                        True(table.Entries[1].Always, "always");
                        Equal(Rarity.Legendary, table.Entries[1].FixedRarity!.Value, "редкость записи");
                        var settings = LootSettings.FromJson(data.OfKind("loot")["loot"], "loot");
                        Equal(3, settings.Bands.Count, "полос");
                        True(settings.Stashes != null, "блок тайников");
                        Equal(1, settings.Stashes!.Pools.Count, "пулов тайников");
                        Equal(1, settings.Stashes.Pools[0].Tier, "тир пула");
                        Equal(0.07, System.Math.Round(settings.Stashes.Chance(1), 2), "шанс с ростом");
                    });
            });

            Check("формат: скилл — цена, точность, травмы и эффекты", () =>
            {
                var skill = AnomalyPath.Core.Combat.SkillTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(
                        "{\"schemaVersion\":1,\"kind\":\"skills\",\"id\":\"skill_x\"," +
                        "\"nameKey\":\"n\",\"descriptionKey\":\"d\",\"type\":\"attack\"," +
                        "\"apCost\":2,\"ammoCost\":3,\"accuracy\":0.9," +
                        "\"injuries\":[\"injury_head_concussion\"]," +
                        "\"effects\":[" +
                        "{\"type\":\"damage\",\"multiplier\":1.5,\"hits\":3,\"damageTypeId\":\"dmg_bullet\"}]}",
                        "test"),
                    "test");

                Equal(AnomalyPath.Core.Combat.SkillKind.Attack, skill.Kind, "тип");
                Equal(2, skill.ApCost, "цена ОД");
                Equal(3, skill.AmmoCost, "патроны");
                Equal(0.9, skill.Accuracy, "точность");
                Equal(1, skill.Injuries.Count, "травмы");
                Equal("injury_head_concussion", skill.Injuries[0], "id травмы");
                Equal(1.5, skill.Effects[0].Multiplier, "множитель");
                Equal(3, skill.Effects[0].Hits, "выстрелы");
                Equal("dmg_bullet", skill.Effects[0].DamageTypeId, "тип урона эффекта");
            });

            Check("формат: оружие — боевые поля без позиций", () =>
            {
                var item = AnomalyPath.Core.Templates.ItemTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(
                        "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"wpn_x\",\"nameKey\":\"n\"," +
                        "\"category\":\"weapon\",\"footprint\":[2,3],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"," +
                        "\"weapon\":{\"classId\":\"wclass_shotgun\",\"damageTypeId\":\"dmg_bullet\"," +
                        "\"damage\":1,\"accuracyCoefficient\":0.5," +
                        "\"magazine\":10,\"ammoCaliber\":\"5.45x39\",\"reloadTime\":2," +
                        "\"durabilityMax\":10,\"durabilityPerShot\":0.1," +
                        "\"skills\":[\"skill_shot\",\"skill_stock\"],\"defaultSkillId\":\"skill_shot\"}}",
                        "test"),
                    "test");

                Equal("wclass_shotgun", item.Weapon!.ClassId, "класс");
                Equal(0.5, item.Weapon.AccuracyCoefficient, "коэффициент точности");
                Equal(10, item.Weapon.Magazine, "магазин");
                Equal("skill_shot", item.Weapon.DefaultSkillId, "стандартный скилл");
                Equal(2, item.Weapon.Skills.Count, "скиллов");
                Equal("skill_stock", item.Weapon.Skills[1], "второй скилл");
            });

            Check("формат: скилл без эффектов и с неизвестным эффектом отклоняется", () =>
            {
                Throws<DataException>(
                    () => AnomalyPath.Core.Combat.SkillTemplate.FromJson(
                        AnomalyPath.Core.Json.JsonParser.Parse(
                            "{\"kind\":\"skills\",\"id\":\"skill_x\",\"nameKey\":\"n\"," +
                            "\"descriptionKey\":\"d\",\"type\":\"utility\",\"apCost\":1}",
                            "test"),
                        "test"),
                    "без эффектов");

                Throws<DataException>(
                    () => AnomalyPath.Core.Combat.SkillTemplate.FromJson(
                        AnomalyPath.Core.Json.JsonParser.Parse(
                            "{\"kind\":\"skills\",\"id\":\"skill_x\",\"nameKey\":\"n\"," +
                            "\"descriptionKey\":\"d\",\"type\":\"attack\",\"apCost\":1," +
                            "\"effects\":[{\"type\":\"move\",\"steps\":1}]}",
                            "test"),
                        "test"),
                    "неизвестный эффект");
            });

            Check("формат: повторная коллекция — ошибка", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/col_a.json"),
                        ["items/col_a.json"] = CollectionWithInclude("items/a.json", "items/col_a.json"),
                        ["items/a.json"] = Weapon("wpn_a")
                    },
                    root => Throws<DataException>(() => new DataLoader(new FileSystemDataSource(root)).Load(), "цикл"));
            });
        }
    }
}
