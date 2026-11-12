#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Validation;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: объекты локаций.</summary>
    public sealed partial class ValidationErrorTests
    {
        private void SpawnErrorTests()
        {
            var cases = new (string Name, Dictionary<string, string> Files, string Fragment)[]
            {
                (
                    "ошибка: таблица лута объекта не найдена",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/a.json"),
                        ["spawns/a.json"] = "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                                            "\"objectKind\":\"loot\",\"weight\":1,\"lootTable\":\"loot_missing\"}"
                    },
                    "Таблица лута не найдена"),
                (
                    "ошибка: актёр объекта не найден",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/a.json"),
                        ["spawns/a.json"] = "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                                            "\"objectKind\":\"npc\",\"weight\":1,\"actorId\":\"npc_missing\"}"
                    },
                    "Актёр не найден"),
                (
                    "ошибка: повтор объекта в пуле локации",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_x\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"wild\",\"subtype\":\"building\"," +
                                               "\"tags\":[\"outdoor\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[1,2],\"spawnPool\":[\"spawn_a\",\"spawn_a\"]}"
                    },
                    "повтор")
                ,
                (
                    "ошибка: объект пула не подходит локации",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/a.json", "locations/a.json"),
                        ["spawns/a.json"] = "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                                            "\"objectKind\":\"loot\",\"weight\":1,\"requiresTags\":[\"road\"]," +
                                            "\"lootTable\":\"loot_missing\"}",
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_x\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"wild\",\"subtype\":\"building\"," +
                                               "\"tags\":[\"outdoor\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[1,1],\"spawnPool\":[\"spawn_a\"]}"
                    },
                    "не подходит")
                ,
                (
                    "ошибка: тайник не входит в пул объектов",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/a.json", "locations/a.json"),
                        ["spawns/a.json"] = "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                                            "\"objectKind\":\"stash\",\"weight\":1}",
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_x\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"wild\",\"subtype\":\"building\"," +
                                               "\"tags\":[\"outdoor\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[1,1],\"spawnPool\":[\"spawn_a\"]}"
                    },
                    "отдельный бросок")
                ,
                (
                    "ошибка: у хаба objectCount не ноль",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_hub\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                               "\"tags\":[\"hub\",\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[1,1],\"spawnPool\":[]}"
                    },
                    "objectCount")
                ,
                (
                    "ошибка: предел объектов больше девяти слотов",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_x\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"wild\",\"subtype\":\"building\"," +
                                               "\"tags\":[\"outdoor\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[1,10],\"spawnPool\":[]}"
                    },
                    "девяти")
                ,
                (
                    "ошибка: у хаба нет пула объектов",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_hub\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                               "\"tags\":[\"hub\",\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[0,0],\"spawnPool\":[\"spawn_a\"]}"
                    },
                    "хаба")
                ,
                (
                    "ошибка: вид экрана хаба у локации, которая не является хабом",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_x\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"wild\",\"subtype\":\"building\"," +
                                               "\"tags\":[\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[0,0],\"hubView\":\"house\"}"
                    },
                    "не является хабом")
                ,
                (
                    "ошибка: точка карты мира у локации, которая не является хабом",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_x\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"wild\",\"subtype\":\"building\"," +
                                               "\"tags\":[\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[0,0]," +
                                               "\"worldMap\":{\"left\":0.1,\"bottom\":0.1,\"right\":0.2,\"top\":0.2}}"
                    },
                    "не является хабом")
                ,
                (
                    "ошибка: граница точки карты мира вне диапазона",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_hub\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                               "\"tags\":[\"hub\",\"outdoor\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[0,0]," +
                                               "\"worldMap\":{\"left\":0.1,\"bottom\":0.1,\"right\":1.4,\"top\":0.2}}"
                    },
                    "вне диапазона")
                ,
                (
                    "ошибка: границы точки карты мира в неверном порядке",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("locations/a.json"),
                        ["locations/a.json"] = "{\"kind\":\"locations\",\"id\":\"loc_hub\",\"titleKey\":\"n\"," +
                                               "\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                               "\"tags\":[\"hub\",\"outdoor\"],\"tier\":1,\"backgrounds\":[\"bg\"]," +
                                               "\"objectCount\":[0,0]," +
                                               "\"worldMap\":{\"left\":0.4,\"bottom\":0.1,\"right\":0.2,\"top\":0.2}}"
                    },
                    "неверном порядке")
            };

            foreach (var testCase in cases)
            {
                Check(testCase.Name, () =>
                {
                    WithFixture(testCase.Files, root =>
                    {
                        var error = Throws<ValidationException>(() => Bootstrap.Build(root), testCase.Name);
                        True(
                            HasFragment(error.Report, testCase.Fragment),
                            $"нет ошибки: {testCase.Fragment}; {Describe(error.Report)}");
                    });
                });
            }
        }
    }
}
