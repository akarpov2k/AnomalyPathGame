#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Локации квестовой цепочки на карте: создание с принудительными объектами, проверка
    /// и удаление. Цепочку не соединяют с маршрутом здесь — подключение делает генератор,
    /// когда квест активен.
    /// </summary>
    public static class QuestChainLocations
    {
        /// <summary>
        /// Создаёт отсутствующие локации цепочки. Принудительные объекты ставятся из записей
        /// спавна с флагом <c>unique</c>, вид объекта обязан совпадать с ожидаемым.
        /// </summary>
        /// <param name="map">Карта мира.</param>
        /// <param name="chain">Шаблон цепочки.</param>
        /// <param name="locations">Шаблоны локаций.</param>
        /// <param name="spawns">Записи спавна; <c>null</c> — объекты не ставятся.</param>
        /// <param name="seed">Seed мира для детерминированного выбора фона.</param>
        /// <returns><c>true</c>, если все локации цепочки есть на карте.</returns>
        public static bool Ensure(
            WorldMap map,
            QuestChainTemplate chain,
            ILocationRepository locations,
            ISpawnRepository? spawns,
            int seed)
        {
            var ok = true;
            for (var index = 0; index < chain.Locations.Count; index++)
            {
                var step = chain.Locations[index];
                var instanceId = QuestChainInstanceIds.For(chain.Id, index);
                if (map.TryGetLocation(instanceId, out _))
                {
                    continue;
                }

                if (!locations.Contains(step.LocationId))
                {
                    ok = false;
                    continue;
                }

                var template = locations.Get(step.LocationId);
                var location = new LocationInstance(
                    instanceId,
                    template.Id,
                    StableHash.Combine(seed, chain.Id + ":" + index),
                    template.Backgrounds.Count > 0 ? template.Backgrounds[0] : string.Empty,
                    template.Subtype,
                    template.Tier,
                    template.Danger);
                AddForcedObject(step.EnemySpawnId, LocationObjectKind.Enemy, location, spawns);
                AddForcedObject(step.NpcSpawnId, LocationObjectKind.Npc, location, spawns);
                map.AddLocation(location);
            }

            return ok && HasAll(map, chain);
        }

        /// <summary>Проверяет, что все локации цепочки есть на карте.</summary>
        /// <param name="map">Карта мира.</param>
        /// <param name="chain">Шаблон цепочки.</param>
        /// <returns><c>true</c>, если все локации цепочки есть на карте.</returns>
        public static bool HasAll(WorldMap map, QuestChainTemplate chain)
        {
            for (var index = 0; index < chain.Locations.Count; index++)
            {
                if (!map.TryGetLocation(QuestChainInstanceIds.For(chain.Id, index), out _))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Удаляет локации цепочки с карты вместе с их рёбрами.</summary>
        /// <param name="map">Карта мира.</param>
        /// <param name="chain">Шаблон цепочки.</param>
        /// <returns>Сколько локаций удалено.</returns>
        public static int Remove(WorldMap map, QuestChainTemplate chain)
        {
            var removed = 0;
            var ids = new List<string>();
            for (var index = 0; index < chain.Locations.Count; index++)
            {
                ids.Add(QuestChainInstanceIds.For(chain.Id, index));
            }

            foreach (var id in ids)
            {
                if (!map.ContainsLocation(id))
                {
                    continue;
                }

                map.RemoveLocation(id);
                removed++;
            }

            return removed;
        }

        private static void AddForcedObject(
            string? spawnId,
            LocationObjectKind expected,
            LocationInstance location,
            ISpawnRepository? spawns)
        {
            if (string.IsNullOrWhiteSpace(spawnId) ||
                spawns == null ||
                !spawns.TryGet(spawnId, out var entry) ||
                entry == null ||
                entry.ObjectKind != expected)
            {
                return;
            }

            location.AddObject(entry.CreateObject());
        }
    }
}
