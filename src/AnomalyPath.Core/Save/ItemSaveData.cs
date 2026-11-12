#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Сохранённый экземпляр предмета без привязки к контейнеру.</summary>
    public sealed class ItemSaveData
    {
        /// <summary>Создаёт запись предмета.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <param name="itemId">Id шаблона.</param>
        /// <param name="stack">Количество.</param>
        /// <param name="durability">Прочность.</param>
        /// <param name="durabilityMax">Максимум прочности.</param>
        /// <param name="variantKey">Вариант.</param>
        /// <param name="magazine">Патроны в магазине.</param>
        /// <param name="charges">Остаток применений расходника.</param>
        /// <param name="ammoItemId">Id патрона в магазине; <c>null</c> — магазин пуст.</param>
        public ItemSaveData(
            string instanceId,
            string itemId,
            int stack,
            int? durability,
            int? durabilityMax,
            string? variantKey,
            int? magazine,
            int? charges,
            string? ammoItemId = null)
        {
            InstanceId = instanceId;
            ItemId = itemId;
            Stack = stack;
            Durability = durability;
            DurabilityMax = durabilityMax;
            VariantKey = variantKey;
            Magazine = magazine;
            Charges = charges;
            AmmoItemId = ammoItemId;
        }

        /// <summary>Id экземпляра.</summary>
        public string InstanceId { get; }

        /// <summary>Id шаблона.</summary>
        public string ItemId { get; }

        /// <summary>Количество.</summary>
        public int Stack { get; }

        /// <summary>Прочность.</summary>
        public int? Durability { get; }

        /// <summary>Максимум прочности.</summary>
        public int? DurabilityMax { get; }

        /// <summary>Вариант.</summary>
        public string? VariantKey { get; }

        /// <summary>Патроны в магазине.</summary>
        public int? Magazine { get; }

        /// <summary>Остаток применений расходника.</summary>
        public int? Charges { get; }

        /// <summary>Id патрона в магазине; <c>null</c> — магазин пуст.</summary>
        public string? AmmoItemId { get; }

        /// <summary>Сериализует запись.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["instanceId"] = JsonValue.FromString(InstanceId);
            value.Members["itemId"] = JsonValue.FromString(ItemId);
            value.Members["stack"] = JsonValue.FromNumber(Stack);
            value.Members["durability"] = Durability.HasValue ? JsonValue.FromNumber(Durability.Value) : JsonValue.Null();
            value.Members["durabilityMax"] = DurabilityMax.HasValue
                ? JsonValue.FromNumber(DurabilityMax.Value)
                : JsonValue.Null();
            value.Members["variantKey"] = VariantKey == null ? JsonValue.Null() : JsonValue.FromString(VariantKey);
            value.Members["magazine"] = Magazine.HasValue ? JsonValue.FromNumber(Magazine.Value) : JsonValue.Null();
            value.Members["charges"] = Charges.HasValue ? JsonValue.FromNumber(Charges.Value) : JsonValue.Null();
            value.Members["ammoItemId"] = AmmoItemId == null ? JsonValue.Null() : JsonValue.FromString(AmmoItemId);
            return value;
        }

        /// <summary>Читает запись предмета.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Запись предмета.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static ItemSaveData FromJson(JsonValue value, string sourcePath)
        {
            var instanceId = DataMap.RequireString(value, "instanceId", sourcePath);
            var itemId = DataMap.RequireString(value, "itemId", sourcePath);
            var stack = DataMap.RequireInt(value, "stack", sourcePath);
            var durability = ReadNullableInt(value, "durability", sourcePath);
            var durabilityMax = ReadNullableInt(value, "durabilityMax", sourcePath);
            var variantKey = DataMap.OptionalString(value, "variantKey");
            var magazine = ReadNullableInt(value, "magazine", sourcePath);
            var charges = ReadNullableInt(value, "charges", sourcePath);
            var ammoItemId = DataMap.OptionalString(value, "ammoItemId");
            if (stack < 1 || (durability.HasValue && durability.Value < 0) ||
                (durabilityMax.HasValue && durabilityMax.Value < 0) ||
                (magazine.HasValue && magazine.Value < 0) ||
                (charges.HasValue && charges.Value < 0))
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "item");
            }

            return new ItemSaveData(
                instanceId,
                itemId,
                stack,
                durability,
                durabilityMax,
                variantKey,
                magazine,
                charges,
                ammoItemId);
        }

        private static int? ReadNullableInt(JsonValue value, string field, string sourcePath)
        {
            if (!value.TryGet(field, out var fieldValue) || fieldValue.Kind == JsonKind.Null)
            {
                return null;
            }

            if (fieldValue.Kind != JsonKind.Number)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, field);
            }

            return (int)System.Math.Round(fieldValue.Number);
        }
    }
}
