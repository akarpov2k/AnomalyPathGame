#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="FileFormatTests"/>: новые виды данных и вложенные блоки.</summary>
    public sealed partial class FileFormatTests
    {
        private void AnomalyFormatTests()
        {
            Check("формат: вид anomalies читается каталогом", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("anomalies/a.json"),
                        ["anomalies/a.json"] =
                            "{\"kind\":\"anomalies\",\"id\":\"anomaly_a\",\"artifactType\":\"fire\"," +
                            "\"damageTypeId\":\"dmg_fire\",\"damage\":5,\"attempts\":2," +
                            "\"artifactChance\":0.5,\"artifactTable\":\"loot_a\",\"tiers\":[1,2]}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        True(data.TryGet("anomalies", "anomaly_a", out var entity), "аномалия");
                        Equal(2, entity.Get("tiers").Items.Count, "тиры");
                        Equal(5.0, entity.Get("damage").Number, "урон");
                    });
            });

            Check("формат: настройка артефактов читается каталогом", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("artifacts.json"),
                        ["artifacts.json"] =
                            "{\"kind\":\"artifactSettings\",\"id\":\"artifacts\",\"falloff\":0.5," +
                            "\"rarities\":[" +
                            "{\"rarity\":\"common\",\"chance\":1,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"rare\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"special\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"epic\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"legendary\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}," +
                            "{\"rarity\":\"unique\",\"chance\":0,\"positiveFactor\":1,\"negativeFactor\":1}]}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var records = data.All("artifactSettings");
                        Equal(1, records.Count, "запись");
                        Equal(6, records[0].Get("rarities").Items.Count, "ступени");
                    });
            });

            Check("формат: модификаторы артефакта — вложенный массив объектов", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/art_a.json"),
                        ["items/art_a.json"] =
                            "{\"kind\":\"items\",\"id\":\"art_a\",\"nameKey\":\"n\",\"icon\":\"i\"," +
                            "\"category\":\"artifact\",\"artifactType\":\"fire\",\"footprint\":[1,1]," +
                            "\"maxStack\":1,\"weight\":0.1,\"tier\":1," +
                            "\"modifiers\":[{\"stat\":\"healthMax\",\"value\":4}," +
                            "{\"stat\":\"protection\",\"damageTypeId\":\"dmg_fire\",\"value\":2}]}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        var modifiers = data.OfKind("items")["art_a"].Get("modifiers");
                        Equal(2, modifiers.Items.Count, "модификаторы");
                        Equal("protection", modifiers.Items[1].Get("stat").Text, "характеристика");
                    });
            });
        }
    }
}
