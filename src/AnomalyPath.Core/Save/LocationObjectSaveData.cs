#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Сохранённый объект локации: запись спавна, состояние и оставшийся лут.</summary>
    public sealed class LocationObjectSaveData
    {
        /// <summary>Создаёт запись объекта локации.</summary>
        /// <param name="spawnId">Id записи спавна.</param>
        /// <param name="looted">Забран ли объект (или побеждён противник).</param>
        /// <param name="items">Оставшийся в объекте лут.</param>
        /// <param name="attemptsUsed">Сколько попыток потратила аномалия; у остальных видов 0.</param>
        /// <param name="radarPlaced">Раскладка радара уже выполнялась.</param>
        /// <param name="artifactCells">Клетки с ненайденными артефактами.</param>
        /// <param name="artifactAttempts">Номера бросков, параллельно клеткам.</param>
        /// <param name="scannedCells">Отсканированные клетки.</param>
        /// <param name="searchedCells">Обысканные клетки.</param>
        public LocationObjectSaveData(
            string spawnId,
            bool looted,
            IReadOnlyList<ItemSaveData>? items = null,
            int attemptsUsed = 0,
            bool radarPlaced = false,
            IReadOnlyList<int>? artifactCells = null,
            IReadOnlyList<int>? artifactAttempts = null,
            IReadOnlyList<int>? scannedCells = null,
            IReadOnlyList<int>? searchedCells = null)
        {
            SpawnId = spawnId;
            Looted = looted;
            Items = items ?? Array.Empty<ItemSaveData>();
            AttemptsUsed = attemptsUsed;
            RadarPlaced = radarPlaced;
            ArtifactCells = artifactCells ?? Array.Empty<int>();
            ArtifactAttempts = artifactAttempts ?? Array.Empty<int>();
            ScannedCells = scannedCells ?? Array.Empty<int>();
            SearchedCells = searchedCells ?? Array.Empty<int>();
        }

        /// <summary>Id записи спавна.</summary>
        public string SpawnId { get; }

        /// <summary>Забран ли объект (или побеждён противник).</summary>
        public bool Looted { get; }

        /// <summary>Оставшийся в объекте лут.</summary>
        public IReadOnlyList<ItemSaveData> Items { get; }

        /// <summary>Сколько попыток поиска потратила аномалия.</summary>
        public int AttemptsUsed { get; }

        /// <summary>Раскладка радара уже выполнена.</summary>
        public bool RadarPlaced { get; }

        /// <summary>Клетки с ненайденными артефактами.</summary>
        public IReadOnlyList<int> ArtifactCells { get; }

        /// <summary>Номера бросков, параллельно клеткам артефактов.</summary>
        public IReadOnlyList<int> ArtifactAttempts { get; }

        /// <summary>Отсканированные клетки.</summary>
        public IReadOnlyList<int> ScannedCells { get; }

        /// <summary>Обысканные клетки.</summary>
        public IReadOnlyList<int> SearchedCells { get; }

        /// <summary>Сериализует запись.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["spawnId"] = JsonValue.FromString(SpawnId);
            value.Members["looted"] = JsonValue.FromBool(Looted);
            value.Members["attemptsUsed"] = JsonValue.FromNumber(AttemptsUsed);
            value.Members["radarPlaced"] = JsonValue.FromBool(RadarPlaced);
            value.Members["artifactCells"] = IntArray(ArtifactCells);
            value.Members["artifactAttempts"] = IntArray(ArtifactAttempts);
            value.Members["scannedCells"] = IntArray(ScannedCells);
            value.Members["searchedCells"] = IntArray(SearchedCells);
            var items = JsonValue.NewArray();
            foreach (var item in Items)
            {
                items.Items.Add(item.ToJson());
            }

            value.Members["items"] = items;
            return value;
        }

        /// <summary>Читает запись объекта локации.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Запись объекта.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static LocationObjectSaveData FromJson(JsonValue value, string sourcePath)
        {
            var spawnId = DataMap.RequireString(value, "spawnId", sourcePath);
            var looted = DataMap.OptionalBool(value, "looted", false);
            var attemptsUsed = DataMap.OptionalInt(value, "attemptsUsed", 0);
            if (attemptsUsed < 0)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "attemptsUsed");
            }

            var radarPlaced = DataMap.OptionalBool(value, "radarPlaced", false);
            var artifactCells = ReadInts(value, "artifactCells", sourcePath);
            var artifactAttempts = ReadInts(value, "artifactAttempts", sourcePath);
            if (artifactCells.Count != artifactAttempts.Count)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "artifactCells");
            }
            var items = new List<ItemSaveData>();
            if (value.TryGet("items", out var itemsValue) && itemsValue.IsArray)
            {
                foreach (var item in itemsValue.Items)
                {
                    if (!item.IsObject)
                    {
                        throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                    }

                    items.Add(ItemSaveData.FromJson(item, sourcePath));
                }
            }

            return new LocationObjectSaveData(
                spawnId,
                looted,
                items,
                attemptsUsed,
                radarPlaced,
                artifactCells,
                artifactAttempts,
                ReadInts(value, "scannedCells", sourcePath),
                ReadInts(value, "searchedCells", sourcePath));
        }

        private static JsonValue IntArray(IReadOnlyList<int> values)
        {
            var array = JsonValue.NewArray();
            foreach (var value in values)
            {
                array.Items.Add(JsonValue.FromNumber(value));
            }

            return array;
        }

        private static List<int> ReadInts(JsonValue value, string field, string sourcePath)
        {
            var result = new List<int>();
            if (!value.TryGet(field, out var array) || !array.IsArray)
            {
                return result;
            }

            foreach (var item in array.Items)
            {
                if (item.Kind != JsonKind.Number)
                {
                    throw new DataException(sourcePath, CoreErrors.SaveMissingField, field);
                }

                var number = (int)System.Math.Round(item.Number, System.MidpointRounding.AwayFromZero);
                if (number < 0)
                {
                    throw new DataException(sourcePath, CoreErrors.SaveMissingField, field);
                }

                result.Add(number);
            }

            return result;
        }
    }
}
