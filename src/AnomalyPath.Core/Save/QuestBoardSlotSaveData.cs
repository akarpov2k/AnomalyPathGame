#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Снимок одного слота доски объявлений.</summary>
    public sealed class QuestBoardSlotSaveData
    {
        /// <summary>Создаёт снимок слота.</summary>
        /// <param name="questId">Предложение в слоте или <c>null</c>.</param>
        /// <param name="availableAt">Когда слот откроется; 0 — уже открыт.</param>
        /// <exception cref="ArgumentOutOfRangeException">Время отрицательно.</exception>
        public QuestBoardSlotSaveData(string? questId, double availableAt)
        {
            if (availableAt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableAt));
            }

            QuestId = string.IsNullOrWhiteSpace(questId) ? null : questId;
            AvailableAt = availableAt;
        }

        /// <summary>Предложение в слоте или <c>null</c>.</summary>
        public string? QuestId { get; }

        /// <summary>Когда слот откроется; 0 — уже открыт.</summary>
        public double AvailableAt { get; }

        /// <summary>Сериализует снимок.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            if (QuestId != null)
            {
                value.Members["questId"] = JsonValue.FromString(QuestId);
            }

            value.Members["availableAt"] = JsonValue.FromNumber(AvailableAt);
            return value;
        }

        /// <summary>Читает снимок слота.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Снимок слота.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static QuestBoardSlotSaveData FromJson(JsonValue value, string sourcePath)
        {
            if (value == null || !value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var availableAt = DataMap.RequireDouble(value, "availableAt", sourcePath);
            if (availableAt < 0)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "availableAt");
            }

            return new QuestBoardSlotSaveData(DataMap.OptionalString(value, "questId"), availableAt);
        }
    }
}
