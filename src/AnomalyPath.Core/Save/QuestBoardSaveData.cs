#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Снимок доски объявлений хаба: слоты и их таймеры.</summary>
    public sealed class QuestBoardSaveData
    {
        /// <summary>Создаёт снимок доски.</summary>
        /// <param name="locationInstanceId">Экземпляр хаба.</param>
        /// <param name="slots">Снимки слотов; <c>null</c> — пусто.</param>
        /// <exception cref="ArgumentException">Идентификатор локации пуст.</exception>
        public QuestBoardSaveData(
            string locationInstanceId,
            IReadOnlyList<QuestBoardSlotSaveData>? slots = null)
        {
            if (string.IsNullOrWhiteSpace(locationInstanceId))
            {
                throw new ArgumentException(nameof(locationInstanceId));
            }

            LocationInstanceId = locationInstanceId;
            Slots = slots ?? new List<QuestBoardSlotSaveData>();
        }

        /// <summary>Экземпляр хаба.</summary>
        public string LocationInstanceId { get; }

        /// <summary>Снимки слотов в порядке настройки доски.</summary>
        public IReadOnlyList<QuestBoardSlotSaveData> Slots { get; }

        /// <summary>Сериализует снимок.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["locationInstanceId"] = JsonValue.FromString(LocationInstanceId);
            var slots = JsonValue.NewArray();
            foreach (var slot in Slots)
            {
                slots.Items.Add(slot.ToJson());
            }

            value.Members["slots"] = slots;
            return value;
        }

        /// <summary>Читает снимок доски.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Снимок доски.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static QuestBoardSaveData FromJson(JsonValue value, string sourcePath)
        {
            if (value == null || !value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var locationInstanceId = DataMap.RequireString(value, "locationInstanceId", sourcePath);
            var slots = new List<QuestBoardSlotSaveData>();
            if (value.TryGet("slots", out var slotsValue) && slotsValue.IsArray)
            {
                foreach (var item in slotsValue.Items)
                {
                    if (!item.IsObject)
                    {
                        throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                    }

                    slots.Add(QuestBoardSlotSaveData.FromJson(item, sourcePath));
                }
            }

            return new QuestBoardSaveData(locationInstanceId, slots);
        }
    }
}
