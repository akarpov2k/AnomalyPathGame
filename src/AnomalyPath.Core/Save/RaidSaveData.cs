#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Снимок текущего рейда: идёт ли он и какие локации уже зачищены.</summary>
    public sealed class RaidSaveData
    {
        /// <summary>Создаёт снимок рейда.</summary>
        /// <param name="active">Идёт ли рейд.</param>
        /// <param name="clearedLocationIds">Зачищенные за рейд локации.</param>
        public RaidSaveData(bool active = false, IReadOnlyList<string>? clearedLocationIds = null)
        {
            Active = active;
            ClearedLocationIds = clearedLocationIds ?? new List<string>();
        }

        /// <summary>Идёт ли рейд.</summary>
        public bool Active { get; }

        /// <summary>Зачищенные за рейд локации.</summary>
        public IReadOnlyList<string> ClearedLocationIds { get; }

        /// <summary>Сериализует снимок.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["active"] = JsonValue.FromBool(Active);
            var ids = JsonValue.NewArray();
            foreach (var id in ClearedLocationIds)
            {
                ids.Items.Add(JsonValue.FromString(id));
            }

            value.Members["clearedLocationIds"] = ids;
            return value;
        }

        /// <summary>Читает снимок рейда.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Снимок рейда.</returns>
        /// <exception cref="DataException">Поля заданы неверно.</exception>
        public static RaidSaveData FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            return new RaidSaveData(
                DataMap.OptionalBool(value, "active", false),
                DataMap.OptionalStringArray(value, "clearedLocationIds"));
        }
    }
}
