#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;

namespace AnomalyPath.Core.Save
{
    /// <summary>Снимок взятого сюжетного квеста: вариант, область прогресса и счётчики целей.</summary>
    public sealed class StoryQuestSaveData
    {
        /// <summary>Создаёт снимок сюжетного квеста.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="storyLine">Линия квеста.</param>
        /// <param name="variantId">Выбранный вариант.</param>
        /// <param name="scope">Область прогресса.</param>
        /// <param name="status">Состояние.</param>
        /// <param name="acceptedAt">Когда взят.</param>
        /// <param name="completedAt">Когда сдан.</param>
        /// <param name="objectives">Прогресс целей.</param>
        public StoryQuestSaveData(
            string questId,
            string storyLine,
            string variantId,
            QuestProgressScope scope,
            QuestStatus status,
            double acceptedAt = 0,
            double completedAt = 0,
            IReadOnlyList<QuestObjectiveSaveData>? objectives = null)
        {
            QuestId = questId;
            StoryLine = storyLine;
            VariantId = variantId;
            Scope = scope;
            Status = status;
            AcceptedAt = acceptedAt;
            CompletedAt = completedAt;
            Objectives = objectives ?? new List<QuestObjectiveSaveData>();
        }

        /// <summary>Идентификатор квеста.</summary>
        public string QuestId { get; }

        /// <summary>Линия квеста.</summary>
        public string StoryLine { get; }

        /// <summary>Выбранный вариант.</summary>
        public string VariantId { get; }

        /// <summary>Область прогресса.</summary>
        public QuestProgressScope Scope { get; }

        /// <summary>Состояние квеста.</summary>
        public QuestStatus Status { get; }

        /// <summary>Когда взят.</summary>
        public double AcceptedAt { get; }

        /// <summary>Когда сдан.</summary>
        public double CompletedAt { get; }

        /// <summary>Прогресс целей.</summary>
        public IReadOnlyList<QuestObjectiveSaveData> Objectives { get; }

        /// <summary>Сериализует снимок.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["questId"] = JsonValue.FromString(QuestId);
            value.Members["storyLine"] = JsonValue.FromString(StoryLine);
            value.Members["variantId"] = JsonValue.FromString(VariantId);
            value.Members["progressScope"] = JsonValue.FromString(SchemaEnum.ToCode(Scope));
            value.Members["status"] = JsonValue.FromString(SchemaEnum.ToCode(Status));
            value.Members["acceptedAt"] = JsonValue.FromNumber(AcceptedAt);
            value.Members["completedAt"] = JsonValue.FromNumber(CompletedAt);
            var objectives = JsonValue.NewArray();
            foreach (var objective in Objectives)
            {
                objectives.Items.Add(objective.ToJson());
            }

            value.Members["objectives"] = objectives;
            return value;
        }

        /// <summary>Читает снимок сюжетного квеста.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Снимок квеста.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static StoryQuestSaveData FromJson(JsonValue value, string sourcePath)
        {
            var questId = DataMap.RequireString(value, "questId", sourcePath);
            var storyLine = DataMap.RequireString(value, "storyLine", sourcePath);
            var variantId = DataMap.OptionalString(value, "variantId") ?? StoryQuestTemplate.StandardVariantId;

            var scopeCode = DataMap.RequireString(value, "progressScope", sourcePath);
            if (!SchemaEnum.TryParse(scopeCode, out QuestProgressScope scope))
            {
                throw new DataException(sourcePath, CoreErrors.StoryProgressScope, scopeCode);
            }

            var statusCode = DataMap.OptionalString(value, "status") ?? SchemaEnum.ToCode(QuestStatus.Active);
            if (!SchemaEnum.TryParse(statusCode, out QuestStatus status))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownQuestStatus, statusCode);
            }

            var acceptedAt = DataMap.OptionalDouble(value, "acceptedAt") ?? 0;
            var completedAt = DataMap.OptionalDouble(value, "completedAt") ?? 0;
            if (acceptedAt < 0 || completedAt < 0)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "storyQuests");
            }

            return new StoryQuestSaveData(
                questId,
                storyLine,
                variantId,
                scope,
                status,
                acceptedAt,
                completedAt,
                ReadObjectives(value, sourcePath));
        }

        private static IReadOnlyList<QuestObjectiveSaveData> ReadObjectives(
            JsonValue value,
            string sourcePath)
        {
            var result = new List<QuestObjectiveSaveData>();
            if (!value.TryGet("objectives", out var objectives) || !objectives.IsArray)
            {
                return result;
            }

            foreach (var item in objectives.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                result.Add(QuestObjectiveSaveData.FromJson(item, sourcePath));
            }

            return result;
        }
    }
}
