#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Validation;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: аномалии и артефакты.</summary>
    public sealed partial class ValidationErrorTests
    {
        private void AnomalyErrorTests()
        {
            const string artifact =
                "{\"kind\":\"items\",\"id\":\"art_a\",\"nameKey\":\"n\",\"icon\":\"i\"," +
                "\"category\":\"artifact\",\"artifactType\":\"fire\",\"footprint\":[1,1]," +
                "\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"modifiers\":[{\"stat\":\"healthMax\",\"value\":1}]}";
            const string damage =
                "{\"kind\":\"damageTypes\",\"id\":\"dmg_fire\",\"nameKey\":\"n\",\"damageKind\":\"anomaly\"}";
            const string anomaly =
                "{\"kind\":\"anomalies\",\"id\":\"anomaly_a\",\"artifactType\":\"fire\"," +
                "\"damageTypeId\":\"dmg_fire\",\"damage\":5,\"attempts\":1,\"artifactChance\":0.5," +
                "\"artifactTable\":\"loot_a\",\"tiers\":[1]}";

            var cases = new (string Name, Dictionary<string, string> Files, string Fragment)[]
            {
                (
                    "ошибка: сумма шансов редкости артефакта не равна 1",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("artifacts.json"),
                        ["artifacts.json"] =
                            "{\"kind\":\"artifactSettings\",\"id\":\"artifacts\",\"falloff\":0.5," +
                            "\"rarities\":[" +
                            "{\"rarity\":\"common\",\"chance\":0.5,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"rare\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"special\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"epic\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"legendary\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"unique\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}]}"
                    },
                    "Сумма шансов"),
                (
                    "ошибка: у артефакта верхний блок защиты",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/art_a.json"),
                        ["items/art_a.json"] =
                            "{\"kind\":\"items\",\"id\":\"art_a\",\"nameKey\":\"n\",\"icon\":\"i\"," +
                            "\"category\":\"artifact\",\"artifactType\":\"fire\",\"footprint\":[1,1]," +
                            "\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"protection\":{\"dmg_fire\":1}}"
                    },
                    "верхний блок protection"),
                (
                    "ошибка: запись спавна аномалии без anomalyId",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/a.json"),
                        ["spawns/a.json"] =
                            "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                            "\"objectKind\":\"anomaly\",\"weight\":1}"
                    },
                    "anomalyId"),
                (
                    "ошибка: аномалия записи спавна не найдена",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("spawns/a.json"),
                        ["spawns/a.json"] =
                            "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                            "\"objectKind\":\"anomaly\",\"anomalyId\":\"anomaly_missing\",\"weight\":1}"
                    },
                    "Аномалия не найдена"),
                (
                    "ошибка: в пуле аномалии лежит не артефакт",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog(
                            "items/art_a.json",
                            "items/plain.json",
                            "damage/d.json",
                            "loot/l.json",
                            "anomalies/a.json"),
                        ["items/art_a.json"] = artifact,
                        ["items/plain.json"] =
                            "{\"kind\":\"items\",\"id\":\"itm_plain\",\"nameKey\":\"n\",\"icon\":\"i\"," +
                            "\"category\":\"material\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1}",
                        ["damage/d.json"] = damage,
                        ["loot/l.json"] =
                            "{\"kind\":\"lootTables\",\"id\":\"loot_a\"," +
                            "\"entries\":[{\"itemId\":\"itm_plain\",\"always\":true}]}",
                        ["anomalies/a.json"] = anomaly
                    },
                    "не артефакт"),
                (
                    "ошибка: тип артефакта пула не совпадает с аномалией",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog(
                            "items/art_a.json",
                            "damage/d.json",
                            "loot/l.json",
                            "anomalies/a.json"),
                        ["items/art_a.json"] = artifact,
                        ["damage/d.json"] = damage,
                        ["loot/l.json"] =
                            "{\"kind\":\"lootTables\",\"id\":\"loot_a\"," +
                            "\"entries\":[{\"itemId\":\"art_a\",\"always\":true}]}",
                        ["anomalies/a.json"] =
                            "{\"kind\":\"anomalies\",\"id\":\"anomaly_a\",\"artifactType\":\"electric\"," +
                            "\"damageTypeId\":\"dmg_fire\",\"damage\":5,\"attempts\":1," +
                            "\"artifactChance\":0.5,\"artifactTable\":\"loot_a\",\"tiers\":[1]}"
                    },
                    "не совпадает по типу"),
                (
                    "ошибка: тир локации не входит в тиры аномалии",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog(
                            "items/art_a.json",
                            "damage/d.json",
                            "loot/l.json",
                            "anomalies/a.json",
                            "spawns/s.json",
                            "locations/loc.json"),
                        ["items/art_a.json"] = artifact,
                        ["damage/d.json"] = damage,
                        ["loot/l.json"] =
                            "{\"kind\":\"lootTables\",\"id\":\"loot_a\"," +
                            "\"entries\":[{\"itemId\":\"art_a\",\"always\":true}]}",
                        ["anomalies/a.json"] = anomaly,
                        ["spawns/s.json"] =
                            "{\"kind\":\"spawns\",\"id\":\"spawn_a\",\"titleKey\":\"n\"," +
                            "\"objectKind\":\"anomaly\",\"anomalyId\":\"anomaly_a\",\"weight\":1}",
                        ["locations/loc.json"] =
                            "{\"kind\":\"locations\",\"id\":\"loc_a\",\"titleKey\":\"n\"," +
                            "\"locationKind\":\"wild\",\"subtype\":\"terrain\"," +
                            "\"tags\":[\"outdoor\"],\"tier\":0,\"backgrounds\":[\"bg\"]," +
                            "\"objectCount\":[1,1],\"spawnPool\":[\"spawn_a\"]}"
                    },
                    "не может появиться")
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
