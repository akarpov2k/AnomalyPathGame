#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.Stage;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    public sealed partial class FileFormatTests : TestBase
    {



        public override void Run()
        {
            RarityFormatTests();
            AnomalyFormatTests();

            Check("формат: сущность прямо из каталога", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_x.json"),
                        ["items/wpn_x.json"] = Weapon("wpn_x")
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("items").Count, "items");
                    });
            });

            Check("формат: коллекция с вложенной коллекцией", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/col_a.json"),
                        ["items/col_a.json"] = CollectionWithInclude("items/a.json", "items/col_b.json"),
                        ["items/col_b.json"] = Collection("items/b.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["items/b.json"] = Weapon("wpn_b")
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(2, data.All("items").Count, "items");
                    });
            });

            Check("формат: абстрактный шаблон не регистрируется, но наследуется", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_x.json"),
                        ["_base/base.json"] = Abstract("{\"sellable\":true}"),
                        ["items/wpn_x.json"] = Weapon("wpn_x", extends: "_base/base.json")
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("items").Count, "items");
                        True(data.OfKind("items")["wpn_x"].Get("sellable").Bool, "sellable из базы");
                        False(data.TryGet("items", "base", out _), "абстрактный файл не должен регистрироваться");
                    });
            });

            Check("формат: признак unlosable наследуется и перекрывается", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_a.json", "items/wpn_b.json"),
                        ["_base/base.json"] = Abstract("{\"unlosable\":true}"),
                        ["items/wpn_a.json"] = Weapon("wpn_a", extends: "_base/base.json"),
                        ["items/wpn_b.json"] = Weapon("wpn_b", extends: "_base/base.json", extra: ",\"unlosable\":false")
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        True(data.OfKind("items")["wpn_a"].Get("unlosable").Bool, "унаследован");
                        False(data.OfKind("items")["wpn_b"].Get("unlosable").Bool, "перекрыт");
                    });
            });

            Check("формат: null удаляет унаследованное поле", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_x.json"),
                        ["_base/base.json"] = Abstract("{\"sellable\":true}"),
                        ["items/wpn_x.json"] = Weapon("wpn_x", extends: "_base/base.json", extra: ",\"sellable\":null")
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        False(data.OfKind("items")["wpn_x"].TryGet("sellable", out _), "sellable должен исчезнуть");
                    });
            });

            Check("формат: массивы заменяются, а не сливаются", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_x.json"),
                        ["_base/base.json"] = Abstract("{\"tags\":[\"a\",\"b\"]}"),
                        ["items/wpn_x.json"] = Weapon("wpn_x", extends: "_base/base.json", extra: ",\"tags\":[\"c\"]")
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var tags = data.OfKind("items")["wpn_x"].Get("tags");
                        Equal(1, tags.Items.Count, "tags");
                        Equal("c", tags.Items[0].Text, "tag");
                    });
            });

            Check("формат: вложенный блок consumable сливается с базой", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/itm_med.json"),
                        ["_base/med.json"] =
                            "{\"schemaVersion\":1,\"kind\":\"items\",\"abstract\":true," +
                            "\"consumable\":{\"useTime\":1,\"effects\":[{\"type\":\"heal\",\"amount\":5}]}}",
                        ["items/itm_med.json"] =
                            "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"itm_med\",\"nameKey\":\"n\"," +
                            "\"extends\":\"_base/med.json\"," +
                            "\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"," +
                            "\"consumable\":{\"charges\":2,\"usableInCombat\":true}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var block = data.OfKind("items")["itm_med"].Get("consumable");
                        Equal(2.0, block.Get("charges").Number, "заряды из предмета");
                        Equal(1.0, block.Get("useTime").Number, "цена из базы");
                        Equal(1, block.Get("effects").Items.Count, "эффект из базы");
                    });
            });

            Check("формат: эффекты еды и воды в consumable", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/food.json"),
                        ["items/food.json"] =
                            "{\"kind\":\"items\",\"id\":\"itm_food\",\"nameKey\":\"n\"," +
                            "\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1," +
                            "\"icon\":\"i\",\"equipSlots\":[],\"consumable\":{\"useTime\":1,\"usableInCombat\":false," +
                            "\"effects\":[{\"type\":\"restoreFood\",\"amount\":60}," +
                            "{\"type\":\"restoreWater\",\"amount\":40}]}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var item = ItemTemplate.FromJson(data.OfKind("items")["itm_food"], "items/food.json");
                        Equal(60, item.Consumable!.TotalFoodRestore(), "еда");
                        Equal(40, item.Consumable.TotalWaterRestore(), "вода");
                    });
            });

            Check("формат: тип урона грузится как сущность damageTypes", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("damage/dmg_x.json"),
                        ["damage/dmg_x.json"] = "{ \"kind\":\"damageTypes\",\"id\":\"dmg_x\",\"nameKey\":\"n\",\"damageKind\":\"anomaly\",\"stackable\":true }"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("damageTypes").Count, "damageTypes");
                        Equal("anomaly", data.OfKind("damageTypes")["dmg_x"].Get("damageKind").Text, "вид");
                    });
            });

            Check("формат: формула грузится как сущность formulas", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("formulas/f.json"),
                        ["formulas/f.json"] = "{ \"kind\":\"formulas\",\"id\":\"f\",\"value\":7,\"variables\":[\"x\"],\"result\":\"int\" }"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("formulas").Count, "formulas");
                    });
            });

            Check("формат: локация грузится как сущность locations", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/loc_x.json"),
                        ["locations/loc_x.json"] = "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_x\"," +
                                                   "\"titleKey\":\"t\",\"locationKind\":\"wild\",\"subtype\":\"path\"," +
                                                   "\"tags\":[\"road\",\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                                   "\"objectCount\":[1,2],\"danger\":0.2}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("locations").Count, "locations");
                        Equal("path", data.OfKind("locations")["loc_x"].Get("subtype").Text, "подтип");
                    });
            });

            Check("формат: запись спавна несёт иконку объекта", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/spawn_x.json"),
                        ["spawns/spawn_x.json"] = "{\"schemaVersion\":1,\"kind\":\"spawns\",\"id\":\"spawn_x\"," +
                                                  "\"titleKey\":\"spawn.spawn_x.title\",\"icon\":\"icons/obj_x\"," +
                                                  "\"objectKind\":\"loot\",\"weight\":1,\"lootTable\":\"loot_human\"}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var entry = SpawnEntry.FromJson(data.OfKind("spawns")["spawn_x"], "spawns/spawn_x.json");
                        Equal("icons/obj_x", entry.Icon, "иконка объекта");
                    });
            });

            Check("формат: запись спавна без иконки остаётся без картинки", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/spawn_y.json"),
                        ["spawns/spawn_y.json"] = "{\"schemaVersion\":1,\"kind\":\"spawns\",\"id\":\"spawn_y\"," +
                                                  "\"titleKey\":\"spawn.spawn_y.title\",\"objectKind\":\"stash\"," +
                                                  "\"weight\":1}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var entry = SpawnEntry.FromJson(data.OfKind("spawns")["spawn_y"], "spawns/spawn_y.json");
                        Equal(null, entry.Icon, "иконки нет");
                    });
            });

            Check("формат: запись спавна несёт высоту фигуры", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/spawn_h.json"),
                        ["spawns/spawn_h.json"] = "{\"schemaVersion\":1,\"kind\":\"spawns\",\"id\":\"spawn_h\"," +
                                                  "\"titleKey\":\"spawn.spawn_h.title\",\"objectKind\":\"loot\"," +
                                                  "\"weight\":1,\"lootTable\":\"loot_human\",\"height\":0.6}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var entry = SpawnEntry.FromJson(data.OfKind("spawns")["spawn_h"], "spawns/spawn_h.json");
                        Equal(0.6, entry.Height ?? 0, "высота фигуры");
                    });
            });

            Check("формат: локация несёт сцену с точками земли", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/loc_stage.json"),
                        ["locations/loc_stage.json"] = "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_stage\"," +
                                                       "\"titleKey\":\"t\",\"locationKind\":\"wild\",\"subtype\":\"path\"," +
                                                       "\"tags\":[\"road\",\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                                       "\"objectCount\":[1,2],\"stage\":{\"tint\":\"#e0d6c0\",\"brightness\":0.95," +
                                                       "\"anchors\":[{\"slot\":\"left\",\"x\":0.22,\"y\":0.14,\"ppm\":160}," +
                                                       "{\"slot\":\"center\",\"x\":0.5,\"y\":0.2,\"ppm\":150}]}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var template = LocationTemplate.FromJson(
                            data.OfKind("locations")["loc_stage"],
                            "locations/loc_stage.json");
                        True(template.Stage != null, "сцена разобрана");
                        Equal(2, template.Stage!.Anchors.Count, "точек земли");
                        Equal(StageSlot.Center, template.Stage.Anchors[1].Slot, "второй слот");
                        Equal("#e0d6c0", template.Stage.Tint, "тон сцены");
                    });
            });

            Check("формат: локация закрывается заглушкой после квеста", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/loc_stub.json"),
                        ["locations/loc_stub.json"] = "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_stub\"," +
                                                      "\"titleKey\":\"t\",\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                                      "\"tags\":[\"hub\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                                      "\"objectCount\":[0,0],\"stubAfterQuest\":\"qst_story_05_angar\"}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var template = LocationTemplate.FromJson(
                            data.OfKind("locations")["loc_stub"],
                            "locations/loc_stub.json");
                        Equal("qst_story_05_angar", template.StubAfterQuest, "квест-заглушка разобран");
                        var without = LocationTemplate.FromJson(
                            AnomalyPath.Core.Json.JsonParser.Parse(
                                "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_ready\"," +
                                "\"titleKey\":\"t\",\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                "\"tags\":[\"hub\"],\"tier\":1,\"backgrounds\":[\"bg\"],\"objectCount\":[0,0]}",
                                "locations/loc_ready.json"),
                            "locations/loc_ready.json");
                        True(without.StubAfterQuest == null, "без поля локация доступна всегда");
                    });
            });

            Check("формат: топология грузится как сущность topology", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("topology.json"),
                        ["topology.json"] = "{\"schemaVersion\":1,\"kind\":\"topology\",\"id\":\"topology\"," +
                                            "\"route\":{\"transitions\":[{\"from\":\"hub\",\"to\":\"path\",\"via\":[\"road\"]}]," +
                                            "\"lookaheadEdges\":2,\"maxTransitionsPerNode\":3,\"hubReturn\":{\"stamina\":2,\"hours\":0.5}," +
                                            "\"branches\":{\"oddRange\":[1,2],\"evenGrowth\":1,\"maxChains\":2}}," +
                                            "\"costs\":{\"road\":{\"stamina\":2,\"hours\":1}}," +
                                            "\"initialStamina\":10," +
                                            "\"dayStartHour\":6,\"nightStartHour\":22}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("topology").Count, "topology");
                        Equal(2, data.OfKind("topology")["topology"].Get("route").Get("branches").Get("oddRange").Items[1].Number, "oddRange");
                        Equal(3, data.OfKind("topology")["topology"].Get("route").Get("maxTransitionsPerNode").Number, "maxTransitionsPerNode");
                    });
            });

            Check("формат: набор грузится как сущность loadouts", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("loadouts/loadout_x.json"),
                        ["loadouts/loadout_x.json"] = "{\"schemaVersion\":1,\"kind\":\"loadouts\"," +
                                                       "\"id\":\"loadout_x\",\"nameKey\":\"n\"," +
                                                       "\"items\":[{\"itemId\":\"wpn_pm\",\"count\":1}]," +
                                                       "\"equipment\":{\"main\":\"wpn_pm\"}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("loadouts").Count, "loadouts");
                        Equal("loadouts", data.OfKind("loadouts")["loadout_x"].Get("kind").Text, "kind");
                    });
            });

            Check("формат: правила грузятся как сущность rules", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rules.json"),
                        ["rules.json"] = "{\"schemaVersion\":1,\"kind\":\"rules\",\"id\":\"rules\"," +
                                         "\"insuranceChance\":0.15," +
                                         "\"insuranceStep\":0.05,\"baseRows\":4,\"tierCount\":4}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("rules").Count, "rules");
                        Equal(0.05, data.OfKind("rules")["rules"].Get("insuranceStep").Number, "insuranceStep");
                    });
            });

            Check("формат: торговец грузится как сущность actors", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/trd_x.json"),
                        ["actors/trd_x.json"] = "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"trd_x\"," +
                                                "\"type\":\"trader\",\"nameKey\":\"n\"," +
                                                "\"portrait\":\"portraits/trd_x\",\"portraitMode\":\"waist\"," +
                                                "\"roles\":[\"trader\"],\"trader\":{\"restockHours\":12," +
                                                "\"playerBuyModifier\":1.1,\"playerSellModifier\":0.5," +
                                                "\"assortment\":[{\"itemId\":\"wpn_x\",\"stock\":2," +
                                                "\"requirements\":{\"quests\":[\"quest_a\"]}}]}}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("actors").Count, "actors");
                        Equal("trader", data.OfKind("actors")["trd_x"].Get("type").Text, "тип");
                    });
            });

            Check("формат: hubPortrait торговца читается", () =>
            {
                const string json = "{\"kind\":\"actors\",\"id\":\"trd_x\",\"type\":\"trader\",\"nameKey\":\"n\"," +
                                    "\"portrait\":\"portraits/trd_x\",\"portraitMode\":\"waist\"," +
                                    "\"hubPortrait\":\"portraits/trd_x_full\",\"roles\":[\"trader\"]," +
                                    "\"trader\":{\"restockHours\":12,\"playerBuyModifier\":1.1," +
                                    "\"playerSellModifier\":0.5,\"assortment\":[{\"itemId\":\"wpn_x\",\"stock\":2}]}}";
                var actor = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(json, "test"), "test");
                Equal("portraits/trd_x_full", actor.HubPortrait, "hubPortrait");

                const string without = "{\"kind\":\"actors\",\"id\":\"trd_y\",\"type\":\"trader\",\"nameKey\":\"n\"," +
                                       "\"roles\":[\"trader\"],\"trader\":{\"restockHours\":12," +
                                       "\"playerBuyModifier\":1.1,\"playerSellModifier\":0.5," +
                                       "\"assortment\":[{\"itemId\":\"wpn_x\",\"stock\":1}]}}";
                var fallback = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(without, "test"), "test");
                True(fallback.HubPortrait == null, "hubPortrait необязателен");
            });

            Check("формат: costume актёра читается", () =>
            {
                const string json = "{\"kind\":\"actors\",\"id\":\"enemy_c\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                    "\"costume\":\"costumes/enemy_c\"," +
                                    "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":10," +
                                    "\"accuracy\":0.7,\"attack\":{\"weaponId\":\"wpn_akm\"}}}";
                var actor = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(json, "test"), "test");
                Equal("costumes/enemy_c", actor.Costume, "costume");

                const string without = "{\"kind\":\"actors\",\"id\":\"enemy_d\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                       "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":10," +
                                       "\"accuracy\":0.7,\"attack\":{\"weaponId\":\"wpn_akm\"}}}";
                var fallback = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(without, "test"), "test");
                True(fallback.Costume == null, "costume необязателен");
            });

            Check("формат: атака врага — оружие и естественное оружие", () =>
            {
                var armed = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(
                        "{\"kind\":\"actors\",\"id\":\"enemy_a\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                        "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":10," +
                        "\"accuracy\":0.7,\"attack\":{\"weaponId\":\"wpn_akm\"}}}",
                        "test"),
                    "test");
                Equal("wpn_akm", armed.Combat!.Attack!.WeaponId, "оружие");
                False(armed.Combat.Attack.IsNatural, "атака оружием");

                var natural = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(
                        "{\"kind\":\"actors\",\"id\":\"enemy_b\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                        "\"roles\":[\"mutant\"]," +
                        "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":10," +
                        "\"accuracy\":0.7," +
                        "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                        "\"attack\":{\"damage\":8,\"damageTypeId\":\"dmg_rupture\"}}}",
                        "test"),
                    "test");
                True(natural.Combat!.Attack!.IsNatural, "естественная атака");
                Equal(8.0, natural.Combat.Attack.Damage, "урон");
                Equal("dmg_rupture", natural.Combat.Attack.DamageTypeId, "тип урона");
                Equal("skill_bite", natural.Combat.DefaultSkillId, "дефолтный скилл");
                Equal("skill_bite", natural.Combat.Skills[0], "скилл в списке");
            });

            Check("формат: пул оружия атаки читается", () =>
            {
                var armed = AnomalyPath.Core.Combat.ActorTemplate.FromJson(
                    AnomalyPath.Core.Json.JsonParser.Parse(
                        "{\"kind\":\"actors\",\"id\":\"enemy_a\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                        "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":10," +
                        "\"accuracy\":0.7,\"attack\":{\"weaponIds\":[\"wpn_pm\",\"wpn_obrez\"]}}}",
                        "test"),
                    "test");
                Equal(2, armed.Combat!.Attack!.WeaponIds.Count, "два оружия");
                Equal("wpn_pm", armed.Combat.Attack.WeaponIds[0], "первое оружие");
                Equal("wpn_obrez", armed.Combat.Attack.WeaponIds[1], "второе оружие");
                True(armed.Combat.Attack.WeaponId == null, "у пула нет одного weaponId");
                False(armed.Combat.Attack.IsNatural, "атака оружием");
            });

            Check("формат: локация с привязкой торговцев", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/loc_hub.json"),
                        ["locations/loc_hub.json"] = "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_hub\"," +
                                                     "\"titleKey\":\"t\",\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                                     "\"tags\":[\"hub\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                                     "\"objectCount\":[0,0]," +
                                                     "\"traders\":[\"trd_x\",\"trd_y\"]}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(2, data.OfKind("locations")["loc_hub"].Get("traders").Items.Count, "traders");
                    });
            });
            ExtraFormatTests();
        }
    }
}
