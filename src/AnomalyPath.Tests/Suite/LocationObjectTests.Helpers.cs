#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="LocationObjectTests"/>: помощники — сцена, шаблоны и записи спавна.</summary>
    public sealed partial class LocationObjectTests
    {
        private static string Ids(IReadOnlyList<LocationObject> objects)
        {
            var ids = new List<string>();
            foreach (var locationObject in objects)
            {
                ids.Add(locationObject.SpawnId);
            }

            ids.Sort(StringComparer.Ordinal);
            return string.Join(",", ids);
        }

        private static (LocationObjectService Service, LocationInstance Location) Scenario(
            string instanceId,
            string templateId,
            string subtype,
            string tags,
            string spawnPool,
            string objectCount,
            params SpawnEntry[] entries) =>
            Scenario(instanceId, templateId, subtype, tags, spawnPool, objectCount, new FakeLoot(), entries);

        private static (LocationObjectService Service, LocationInstance Location) Scenario(
            string instanceId,
            string templateId,
            string subtype,
            string tags,
            string spawnPool,
            string objectCount,
            FakeLoot loot,
            params SpawnEntry[] entries)
        {
            var template = Template(templateId, subtype, tags, spawnPool, objectCount);
            var service = new LocationObjectService(
                new SpawnRepository(entries),
                new LocationRepository(new[] { template }),
                loot,
                TestArtifacts.NoAnomalies());
            var location = new LocationInstance(
                instanceId,
                template.Id,
                42,
                "bg",
                Subtype(subtype),
                1,
                0.2);
            return (service, location);
        }

        private static LocationSubtype Subtype(string code)
        {
            switch (code)
            {
                case "hub":
                    return LocationSubtype.Hub;
                case "building":
                    return LocationSubtype.Building;
                default:
                    return LocationSubtype.Path;
            }
        }

        private static string RoadTags() => "[\"road\",\"outdoor\"]";

        private static string BuildingTags() => "[\"indoor\",\"house\"]";

        private static LocationTemplate Template(
            string id,
            string subtype,
            string tags,
            string spawnPool,
            string objectCount) =>
            LocationTemplate.FromJson(
                JsonParser.Parse(
                    "{\"kind\":\"locations\",\"id\":\"" + id + "\",\"titleKey\":\"n\"," +
                    "\"locationKind\":\"" + (subtype == "hub" ? "hub" : "wild") + "\",\"subtype\":\"" + subtype + "\"," +
                    "\"tags\":" + tags + ",\"tier\":1,\"backgrounds\":[\"bg\"]," +
                    "\"objectCount\":" + objectCount + ",\"spawnPool\":" + spawnPool + "," +
                    "\"chainLength\":[1,1]}",
                    "test"),
                "test");

        private static SpawnEntry Entry(string id, string tags, double chance)
        {
            var parts = tags.Split(',');
            var requires = new List<string>();
            foreach (var part in parts)
            {
                requires.Add("\"" + part + "\"");
            }

            return Parse(
                "{\"kind\":\"spawns\",\"id\":\"" + id + "\",\"titleKey\":\"spawn.title\"," +
                "\"objectKind\":\"loot\",\"requiresTags\":[" + string.Join(",", requires) + "]," +
                "\"excludesTags\":[\"hub\"],\"weight\":1,\"spawnChance\":" +
                chance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "\"minTier\":0,\"maxTier\":3,\"lootTable\":\"loot_human\"}");
        }

        private static SpawnEntry Enemy(string id, string actorId, double chance) =>
            Parse(
                "{\"kind\":\"spawns\",\"id\":\"" + id + "\",\"titleKey\":\"spawn.title\"," +
                "\"objectKind\":\"enemy\",\"requiresTags\":[\"road\",\"outdoor\"]," +
                "\"excludesTags\":[\"hub\"],\"weight\":1,\"spawnChance\":" +
                chance.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," +
                "\"minTier\":0,\"maxTier\":3,\"actorId\":\"" + actorId + "\"}");

        private static SpawnEntry Parse(string json) =>
            SpawnEntry.FromJson(JsonParser.Parse(json, "test"), "test");

        private sealed class FakeLoot : ILootService
        {
            public IReadOnlyList<ItemInstance> Items { get; set; } = Array.Empty<ItemInstance>();

            public int RollCalls { get; private set; }

            public IReadOnlyList<ItemInstance> Roll(LootContext context)
            {
                RollCalls++;
                return Items;
            }

            public IReadOnlyList<ItemInstance> RollTable(
                string lootTableId,
                int locationTier,
                int seed,
                bool ignoreTierBand)
            {
                RollCalls++;
                return Items;
            }
        }
    }
}
