#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Tests
{
    /// <summary>Аномалии: данные, попытки, урон вне боя, поиск артефакта и смерть.</summary>
    public sealed class AnomalyTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов аномалий.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public AnomalyTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var anomalies = services.Resolve<IAnomalyRepository>();
            var spawns = services.Resolve<ISpawnRepository>();
            var locations = services.Resolve<ILocationRepository>();
            var damage = services.Resolve<IPlayerDamageService>();
            var loot = services.Resolve<ILootService>();

            Check("аномалии: в данных есть аномалии с непустыми тирами", () =>
            {
                True(anomalies.All.Count > 0, "список аномалий");
                foreach (var anomaly in anomalies.All)
                {
                    True(anomaly.Tiers.Count > 0, "тиры " + anomaly.Id);
                    True(anomaly.Attempts >= 1, "попытки " + anomaly.Id);
                }
            });

            Check("аномалии: запись спавна ссылается на аномалию и подходит по тиру", () =>
            {
                True(spawns.TryGet("spawn_anomaly_glow", out var entry), "запись спавна");
                Equal(LocationObjectKind.Anomaly, entry!.ObjectKind, "вид");
                True(entry.AnomalyId != null, "ссылка на аномалию");

                foreach (var location in locations.All)
                {
                    foreach (var spawnId in location.SpawnPool)
                    {
                        if (!spawns.TryGet(spawnId, out var poolEntry) || poolEntry == null ||
                            poolEntry.ObjectKind != LocationObjectKind.Anomaly)
                        {
                            continue;
                        }

                        True(
                            anomalies.Get(poolEntry.AnomalyId!).AllowsTier(location.Tier),
                            "тир " + location.Id + " → " + poolEntry.AnomalyId);
                    }
                }
            });

            Check("аномалии: разбор записи проверяет тиры и шанс", () =>
            {
                Throws<DataException>(() => Parse(
                    "{\"id\":\"a\",\"artifactType\":\"fire\",\"damageTypeId\":\"dmg_fire\"," +
                    "\"attempts\":1,\"artifactChance\":0.5,\"artifactTable\":\"t\",\"tiers\":[]}"), "пустые тиры");
                Throws<DataException>(() => Parse(
                    "{\"id\":\"a\",\"artifactType\":\"fire\",\"damageTypeId\":\"dmg_fire\"," +
                    "\"attempts\":1,\"artifactChance\":1.5,\"artifactTable\":\"t\",\"tiers\":[1]}"), "шанс");
                Throws<DataException>(() => Parse(
                    "{\"id\":\"a\",\"artifactType\":\"fire\",\"damageTypeId\":\"dmg_fire\"," +
                    "\"attempts\":0,\"artifactChance\":0.5,\"artifactTable\":\"t\",\"tiers\":[1]}"), "попытки");
            });

            Check("аномалии: попытки объекта расходуются и артефакт забирается", () =>
            {
                var anomaly = new AnomalyLocationObject("spawn", "title", "anomaly_glow");
                True(anomaly.CanSearch(2), "первая попытка");
                anomaly.RegisterAttempt();
                True(anomaly.CanSearch(2), "вторая попытка");
                anomaly.RegisterAttempt();
                False(anomaly.CanSearch(2), "попытки кончились");
                Equal(0, anomaly.RemainingAttempts(2), "остаток");

                var artifact = new ItemInstance("art_1", "art_fire_ember", 1);
                anomaly.AddLoot(new[] { artifact });
                True(anomaly.HasLoot, "лут есть");
                True(anomaly.Take("art_1"), "забран");
                False(anomaly.HasLoot, "лут пуст");
            });

            Check("аномалии: попытки и найденное переживают сейв", () =>
            {
                var saved = new LocationObjectSaveData("spawn", false, null, 2);
                var restored = LocationObjectSaveData.FromJson(saved.ToJson(), "object");
                Equal(2, restored.AttemptsUsed, "попытки");
            });

            Check("аномалии: объект переживает снимок мира", () =>
            {
                var session = NewSession(services);
                var location = session.State.World.CurrentLocation;
                location.MarkSpawned();
                var anomaly = new AnomalyLocationObject(
                    "spawn_anomaly_glow",
                    "spawn.spawn_anomaly_glow.title",
                    "anomaly_glow");
                anomaly.RegisterAttempt();
                anomaly.AddLoot(new[]
                {
                    new ItemInstance("art_x", "art_fire_ember", 1, variantKey: "art:legendary:0")
                });
                location.AddObject(anomaly);

                var snapshot = WorldSaveMapper.ToSaveData(session.State);
                var world = WorldSaveMapper.LoadWorld(
                    snapshot,
                    session.State.World.Map.Seed,
                    "save",
                    services.Resolve<ISpawnRepository>());
                var restoredLocation = world.Map.GetLocation(location.InstanceId);
                var restored = FindAnomaly(restoredLocation);

                True(restored != null, "аномалия восстановлена");
                Equal(1, restored!.AttemptsUsed, "попытки");
                Equal(1, restored.Loot.Count, "артефакт");
                Equal("art:legendary:0", restored.Loot[0].VariantKey, "вариант");
            });

            Check("урон вне боя: защита снижает фиксированный урон", () =>
            {
                var session = NewSession(services);
                var player = session.State.Player!;
                var protection = damage.Protection(session.State, "dmg_fire");
                var before = player.Health;

                var result = damage.Apply(session.State, 10, "dmg_fire");

                Equal(Math.Max(0, 10 - protection), result.Damage, "прошедший урон");
                Equal(before - result.Damage, player.Health, "остаток здоровья");
                False(result.Died, "смерти нет");
            });

            Check("урон вне боя: артефакт повышает защиту и максимум здоровья", () =>
            {
                var session = NewSession(services);
                var player = session.State.Player!;
                var baseHealth = damage.MaxHealth(session.State);
                var baseFire = damage.Protection(session.State, "dmg_fire");
                player.Equipment.SetArtifact(0, new ItemInstance("art_1", "art_fire_ember", 1));

                Equal(baseHealth + 4, damage.MaxHealth(session.State), "здоровье с артефактом");
                Equal(baseFire + 3, damage.Protection(session.State, "dmg_fire"), "защита с артефактом");
            });

            Check("урон вне боя: смерть теряет инвентарь и возвращает в хаб", () =>
            {
                var session = NewSession(services);
                var player = session.State.Player!;
                player.SetHealth(5);

                var result = damage.Apply(session.State, 1000, "dmg_fire");

                True(result.Died, "смерть");
                True(result.Death != null, "последствия смерти");
                True(result.HealthAfter >= 1, "здоровье после респавна");
                Equal(session.State.World.LastHubId, session.State.World.CurrentLocationId, "хаб");
                Equal(0, player.Inventory.Items.Count, "инвентарь потерян");
            });

            Check("поиск: успех даёт артефакт с вариантом редкости", () =>
            {
                var session = NewSession(services);
                var anomaly = AddAnomaly(session, "spawn_test", "anomaly_test");
                var template = new AnomalyTemplate(
                    "anomaly_test",
                    "fire",
                    "dmg_fire",
                    5,
                    1,
                    1,
                    "loot_artifact_fire",
                    new[] { 1 });
                var rules = services.Resolve<IGameRulesRepository>();
                var service = new AnomalyService(new FakeAnomalies(template), loot, damage, rules);
                var expected = Math.Max(0, 5 - damage.Protection(session.State, "dmg_fire"));
                var radar = service.OpenRadar(session, anomaly);
                True(radar != null, "радар открыт");
                Equal(1, anomaly.ArtifactCells.Count, "артефакт разложен");

                var result = service.SearchCell(session, anomaly, anomaly.ArtifactCells[0]);

                Equal(AnomalySearchStatus.Success, result.Status, "итог");
                Equal(expected, result.Damage!.Damage, "урон с защитой");
                True(result.Found, "артефакт найден");
                Equal(1, anomaly.Loot.Count, "артефакт лежит в аномалии");
                True(result.Artifacts[0].VariantKey != null, "редкость брошена");
                Equal(0, result.AttemptsLeft, "попытки кончились");
                Equal(AnomalySearchStatus.NoAttempts, service.SearchCell(session, anomaly, 0).Status, "повтор");
            });

            Check("поиск: смерть от аномалии не отдаёт артефакт", () =>
            {
                var session = NewSession(services);
                var anomaly = AddAnomaly(session, "spawn_test", "anomaly_deadly");
                var template = new AnomalyTemplate(
                    "anomaly_deadly",
                    "fire",
                    "dmg_fire",
                    1000,
                    1,
                    1,
                    "loot_artifact_fire",
                    new[] { 1 });
                var rules = services.Resolve<IGameRulesRepository>();
                var service = new AnomalyService(new FakeAnomalies(template), loot, damage, rules);
                session.State.Player!.SetHealth(5);
                service.OpenRadar(session, anomaly);

                var result = service.SearchCell(session, anomaly, anomaly.ArtifactCells[0]);

                True(result.Died, "смерть");
                False(result.Found, "артефакта нет");
                Equal(0, anomaly.Loot.Count, "лута нет");
            });
        }

        private static AnomalyLocationObject AddAnomaly(GameSession session, string spawnId, string anomalyId)
        {
            var anomaly = new AnomalyLocationObject(spawnId, "spawn.spawn_anomaly_glow.title", anomalyId);
            session.State.World.CurrentLocation.AddObject(anomaly);
            return anomaly;
        }

        private static AnomalyLocationObject? FindAnomaly(AnomalyPath.Core.World.LocationInstance location)
        {
            foreach (var item in location.Objects)
            {
                if (item is AnomalyLocationObject anomaly)
                {
                    return anomaly;
                }
            }

            return null;
        }

        private static GameSession NewSession(ServiceContainer services) =>
            new GameSession(services.Resolve<INewGameService>().Create(
                1,
                "loadout_rifleman",
                new NewGameSettings(1, 1)));

        private static AnomalyTemplate Parse(string json) =>
            AnomalyTemplate.FromJson(JsonParser.Parse(json, "anomaly"), "anomaly");

        private sealed class FakeAnomalies : IAnomalyRepository
        {
            private readonly AnomalyTemplate _template;

            internal FakeAnomalies(AnomalyTemplate template) => _template = template;

            public IReadOnlyList<AnomalyTemplate> All => new[] { _template };

            public bool TryGet(string id, out AnomalyTemplate? template)
            {
                var found = string.Equals(id, _template.Id, StringComparison.Ordinal);
                template = found ? _template : null;
                return found;
            }

            public AnomalyTemplate Get(string id) =>
                string.Equals(id, _template.Id, StringComparison.Ordinal)
                    ? _template
                    : throw new DataException(id, "error.anomaly_not_found", id);
        }
    }
}
