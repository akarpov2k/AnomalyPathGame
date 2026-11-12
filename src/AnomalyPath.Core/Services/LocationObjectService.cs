#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Спавн объектов локации: каждый объект её пула бросает свой шанс, число не-противников
    /// ограничено <c>objectCount</c>, противников — <c>enemyCount</c>. Если шансы не выпали,
    /// сервис добирает обещанное число, чтобы дорога не была пустой; лут рандомится здесь же.
    /// Seed — seed наполнения экземпляра локации и его id, поэтому один проход даёт тот же результат,
    /// а новый маршрут получает новые seed экземпляров и другие объекты.
    /// </summary>
    public sealed class LocationObjectService : ILocationObjectService
    {
        /// <summary>Верхний предел объектов на локации: девять слотов дороги похода.</summary>
        public const int MaxObjectsPerLocation = 9;

        private readonly ISpawnRepository _spawns;
        private readonly ILocationRepository _locations;
        private readonly ILootService _loot;
        private readonly IAnomalyRepository _anomalies;

        /// <summary>Создаёт сервис спавна объектов.</summary>
        /// <param name="spawns">Записи спавна.</param>
        /// <param name="locations">Шаблоны локаций: пул объектов, теги и тир.</param>
        /// <param name="loot">Бросок лута объекта при спавне.</param>
        /// <param name="anomalies">Аномалии: тиры локаций, где они могут появиться.</param>
        /// <exception cref="ArgumentNullException">Обязательная зависимость не задана.</exception>
        public LocationObjectService(
            ISpawnRepository spawns,
            ILocationRepository locations,
            ILootService loot,
            IAnomalyRepository anomalies)
        {
            _spawns = spawns ?? throw new ArgumentNullException(nameof(spawns));
            _locations = locations ?? throw new ArgumentNullException(nameof(locations));
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
        }

        /// <inheritdoc />
        public IReadOnlyList<LocationObject> Spawn(LocationInstance location)
        {
            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location.Spawned)
            {
                return location.Objects;
            }

            location.MarkSpawned();
            if (location.Subtype == LocationSubtype.Hub)
            {
                return location.Objects;
            }

            var template = _locations.Get(location.TemplateId);
            if (template.SpawnPool.Count == 0)
            {
                return location.Objects;
            }

            var spawnSeed = StableHash.Combine(location.Seed, "location_spawn:" + location.InstanceId);
            var random = new SeededWorldRandom(spawnSeed);
            var count = template.ObjectCount.Sample(random);
            if (count <= 0)
            {
                return location.Objects;
            }

            var eligible = new List<SpawnEntry>();
            foreach (var spawnId in template.SpawnPool)
            {
                if (!_spawns.TryGet(spawnId, out var entry) || entry == null)
                {
                    continue;
                }

                if (!entry.Matches(template.Tags, location.Tier, location.Subtype))
                {
                    continue;
                }

                if (entry.ObjectKind == LocationObjectKind.Anomaly)
                {
                    if (entry.AnomalyId == null ||
                        !_anomalies.TryGet(entry.AnomalyId, out var anomaly) ||
                        anomaly == null ||
                        !anomaly.AllowsTier(location.Tier))
                    {
                        continue;
                    }
                }

                if (entry.ObjectKind == LocationObjectKind.Enemy && location.EnemyGone)
                {
                    continue;
                }

                if (random.NextDouble() >= entry.SpawnChance)
                {
                    continue;
                }

                eligible.Add(entry);
            }

            var remaining = new List<SpawnEntry>(eligible);
            var enemyPicked = 0;
            var enemyLimit = Math.Max(0, template.EnemyCount);

            // На сцене два места под объекты, и тайник занимает одно из них.
            // Поэтому объектов пула (кроме противника, он стоит по центру) влезает
            // на один меньше, когда тайник уже найден.
            var nonEnemyLimit = Math.Max(
                0,
                template.ObjectCount.Max - (location.StashPresent ? 1 : 0));
            var nonEnemy = 0;
            for (var index = 0; index < count && remaining.Count > 0; index++)
            {
                var candidates = new List<SpawnEntry>(remaining.Count);
                foreach (var entry in remaining)
                {
                    if (enemyPicked >= enemyLimit && entry.ObjectKind == LocationObjectKind.Enemy)
                    {
                        continue;
                    }

                    if (nonEnemy >= nonEnemyLimit && entry.ObjectKind != LocationObjectKind.Enemy)
                    {
                        continue;
                    }

                    candidates.Add(entry);
                }

                if (candidates.Count == 0)
                {
                    break;
                }

                var picked = WeightedPick.ChooseIndex(
                    candidates.Count,
                    position => candidates[position].Weight,
                    random);
                var chosen = candidates[picked >= 0 ? picked : candidates.Count - 1];
                if (chosen.ObjectKind == LocationObjectKind.Enemy)
                {
                    enemyPicked++;
                }
                else
                {
                    remaining.Remove(chosen);
                    nonEnemy++;
                }

                var created = Create(chosen, spawnSeed, location);
                location.AddObject(created);
            }

            // Добираем обещанное число объектов и противников, даже если их шанс не выпал:
            // в походе пустая дорога выглядит как ошибка генерации.
            var nonEnemyNow = 0;
            foreach (var locationObject in location.Objects)
            {
                if (locationObject.Kind != LocationObjectKind.Enemy)
                {
                    nonEnemyNow++;
                }
            }

            var nonEnemyTarget = Math.Max(0, count - (location.StashPresent ? 1 : 0));
            foreach (var entry in eligible)
            {
                if (nonEnemyNow >= nonEnemyTarget)
                {
                    break;
                }

                if (entry.ObjectKind == LocationObjectKind.Enemy)
                {
                    continue;
                }

                location.AddObject(Create(entry, spawnSeed, location));
                nonEnemyNow++;
            }

            SpawnEntry? enemyCandidate = null;
            foreach (var entry in eligible)
            {
                if (entry.ObjectKind == LocationObjectKind.Enemy)
                {
                    enemyCandidate = entry;
                    break;
                }
            }

            while (enemyCandidate != null && enemyPicked < enemyLimit)
            {
                location.AddObject(Create(enemyCandidate, spawnSeed, location));
                enemyPicked++;
            }

            return location.Objects;
        }

        private LocationObject Create(SpawnEntry entry, int spawnSeed, LocationInstance location)
        {
            var created = entry.CreateObject();
            if (created is LootLocationObject lootObject)
            {
                // Одинаковые объекты на одной локации получают разные seed: в ключ входит
                // порядковый номер объекта, а не только id записи спавна.
                var lootSeed = StableHash.Combine(
                    spawnSeed,
                    "loot:" + entry.Id + ":" + location.Objects.Count);
                lootObject.SetLoot(_loot.RollTable(
                    lootObject.LootTableId,
                    location.Tier,
                    lootSeed,
                    ignoreTierBand: false));
            }

            return created;
        }
    }
}
