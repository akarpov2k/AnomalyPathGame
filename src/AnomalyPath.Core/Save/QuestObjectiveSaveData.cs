#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests.Story;

namespace AnomalyPath.Core.Save
{
    /// <summary>Снимок прогресса одной цели сюжетного квеста.</summary>
    public sealed class QuestObjectiveSaveData
    {
        /// <summary>Создаёт снимок прогресса цели.</summary>
        /// <param name="type">Тип цели.</param>
        /// <param name="current">Текущий счётчик.</param>
        /// <param name="countedIds">Учтённые цели.</param>
        public QuestObjectiveSaveData(
            QuestObjectiveType type,
            int current = 0,
            IReadOnlyList<string>? countedIds = null)
        {
            Type = type;
            Current = current;
            CountedIds = countedIds ?? Array.Empty<string>();
        }

        /// <summary>Тип цели.</summary>
        public QuestObjectiveType Type { get; }

        /// <summary>Текущий счётчик.</summary>
        public int Current { get; }

        /// <summary>Учтённые цели.</summary>
        public IReadOnlyList<string> CountedIds { get; }

        /// <summary>Сериализует снимок.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["type"] = JsonValue.FromString(QuestObjectiveTypeCodec.ToCode(Type));
            value.Members["current"] = JsonValue.FromNumber(Current);
            var ids = JsonValue.NewArray();
            foreach (var id in CountedIds)
            {
                ids.Items.Add(JsonValue.FromString(id));
            }

            value.Members["countedIds"] = ids;
            return value;
        }

        /// <summary>Читает снимок прогресса цели.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Снимок прогресса.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static QuestObjectiveSaveData FromJson(JsonValue value, string sourcePath)
        {
            var code = DataMap.RequireString(value, "type", sourcePath);
            if (!QuestObjectiveTypeCodec.TryParse(code, out var type))
            {
                throw new DataException(sourcePath, CoreErrors.StoryObjectiveType, code);
            }

            var current = DataMap.OptionalInt(value, "current", 0);
            if (current < 0)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "current");
            }

            return new QuestObjectiveSaveData(type, current, DataMap.OptionalStringArray(value, "countedIds"));
        }
    }
}
