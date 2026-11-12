#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Снимок торговца в сохранении: время пополнения и остатки товаров.</summary>
    public sealed class TraderSaveData
    {
        /// <summary>Создаёт снимок торговца.</summary>
        /// <param name="actorId">Идентификатор торговца.</param>
        /// <param name="lastRestockAt">Абсолютное время мира последнего пополнения в часах.</param>
        /// <param name="stock">Остатки товаров.</param>
        public TraderSaveData(
            string actorId,
            double lastRestockAt,
            IReadOnlyList<TraderStockSaveData> stock)
        {
            ActorId = actorId;
            LastRestockAt = lastRestockAt;
            Stock = stock;
        }

        /// <summary>Идентификатор торговца.</summary>
        public string ActorId { get; }

        /// <summary>Абсолютное время мира последнего пополнения в часах.</summary>
        public double LastRestockAt { get; }

        /// <summary>Остатки товаров.</summary>
        public IReadOnlyList<TraderStockSaveData> Stock { get; }

        /// <summary>Сериализует снимок.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["actorId"] = JsonValue.FromString(ActorId);
            value.Members["lastRestockAt"] = JsonValue.FromNumber(LastRestockAt);
            var stock = JsonValue.NewArray();
            foreach (var item in Stock)
            {
                stock.Items.Add(item.ToJson());
            }

            value.Members["stock"] = stock;
            return value;
        }

        /// <summary>Читает снимок торговца.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Снимок торговца.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static TraderSaveData FromJson(JsonValue value, string sourcePath)
        {
            var actorId = DataMap.RequireString(value, "actorId", sourcePath);
            var lastRestockAt = DataMap.RequireDouble(value, "lastRestockAt", sourcePath);
            if (lastRestockAt < -1)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "traders");
            }

            var stock = new List<TraderStockSaveData>();
            if (value.TryGet("stock", out var stockValue) && stockValue.IsArray)
            {
                foreach (var item in stockValue.Items)
                {
                    if (!item.IsObject)
                    {
                        throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                    }

                    stock.Add(TraderStockSaveData.FromJson(item, sourcePath));
                }
            }

            return new TraderSaveData(actorId, lastRestockAt, stock);
        }
    }
}
