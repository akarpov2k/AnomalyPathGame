#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: правила маршрута.</summary>
    public sealed partial class ValidationErrorTests
    {
        private void RouteErrorTests()
        {
            var cases = new (string Name, Dictionary<string, string> Files, string Path, string Fragment)[]
            {
                (
                    "ошибка: минимум веток на нечётном звене меньше одного",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("topology.json", "locations/hub.json", "locations/route.json"),
                        ["topology.json"] = TopologyJson().Replace("\"oddRange\":[1,2]", "\"oddRange\":[0,2]"),
                        ["locations/hub.json"] = Location("loc_hub", "hub", "hub", "[\"hub\"]"),
                        ["locations/route.json"] = Location("loc_route", "wild", "path", "[\"road\"]"),
                    },
                    "topology.json",
                    "Диапазон числа веток"),
                (
                    "ошибка: предел переходов с локации меньше двух",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("topology.json", "locations/hub.json", "locations/route.json"),
                        ["topology.json"] = TopologyJson().Replace("\"maxTransitionsPerNode\":3", "\"maxTransitionsPerNode\":1"),
                        ["locations/hub.json"] = Location("loc_hub", "hub", "hub", "[\"hub\"]"),
                        ["locations/route.json"] = Location("loc_route", "wild", "path", "[\"road\"]"),
                    },
                    "topology.json",
                    "Некорректный диапазон поля"),
                (
                    "ошибка: из ветки некуда выйти на путь",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog(
                            "topology.json",
                            "locations/hub.json",
                            "locations/route.json",
                            "locations/forest.json"),
                        ["topology.json"] = TopologyJson().Replace(
                            "{\"from\":\"path\",\"to\":\"path\",\"via\":[\"road\",\"path\"]}",
                            "{\"from\":\"path\",\"to\":\"path\",\"via\":[\"road\",\"path\"]}," +
                            "{\"from\":\"path\",\"to\":\"terrain\",\"via\":[\"path\"]}"),
                        ["locations/hub.json"] = Location("loc_hub", "hub", "hub", "[\"hub\"]"),
                        ["locations/route.json"] = Location("loc_route", "wild", "path", "[\"road\"]"),
                        ["locations/forest.json"] = Location("loc_forest", "wild", "terrain", "[\"outdoor\"]"),
                    },
                    "loc_forest",
                    "нельзя выйти на путь"),
                (
                    "ошибка: блок отдыха отсутствует",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("topology.json", "locations/hub.json", "locations/route.json"),
                        ["topology.json"] = TopologyJson().Replace(
                            "\"rest\":{\"hubStaminaPercent\":0.2,\"raidStaminaPercent\":0.1," +
                            "\"raidAttackChancePerHour\":0.1},",
                            string.Empty),
                        ["locations/hub.json"] = Location("loc_hub", "hub", "hub", "[\"hub\"]"),
                        ["locations/route.json"] = Location("loc_route", "wild", "path", "[\"road\"]"),
                    },
                    "topology.json",
                    "rest"),
                (
                    "ошибка: риск нападения за час вне диапазона",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("topology.json", "locations/hub.json", "locations/route.json"),
                        ["topology.json"] = TopologyJson().Replace(
                            "\"raidAttackChancePerHour\":0.1",
                            "\"raidAttackChancePerHour\":1.5"),
                        ["locations/hub.json"] = Location("loc_hub", "hub", "hub", "[\"hub\"]"),
                        ["locations/route.json"] = Location("loc_route", "wild", "path", "[\"road\"]"),
                    },
                    "topology.json",
                    "0..1"),
            };

            foreach (var testCase in cases)
            {
                Check(testCase.Name, () =>
                {
                    WithFixture(testCase.Files, root =>
                    {
                        var result = Preflight.Run(root);
                        True(
                            HasIssue(result.Report, testCase.Path, testCase.Fragment),
                            $"нет ошибки «{testCase.Fragment}» у {testCase.Path}");
                    });
                });
            }
        }

        private static string Location(string id, string locationKind, string subtype, string tags) =>
            "{\"kind\":\"locations\",\"id\":\"" + id + "\",\"titleKey\":\"n\",\"locationKind\":\"" + locationKind +
            "\",\"subtype\":\"" + subtype + "\",\"tags\":" + tags + ",\"tier\":0,\"backgrounds\":[\"bg\"]," +
            "\"objectCount\":[0,0],\"chainLength\":[1,1]}";
    }
}
