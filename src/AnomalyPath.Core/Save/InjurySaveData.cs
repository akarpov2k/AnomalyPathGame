#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Активная травма в сейве: id, вид длительности и остаток.</summary>
    public sealed class InjurySaveData
    {
        /// <summary>Создаёт запись травмы.</summary>
        /// <param name="id">Id травмы из данных.</param>
        /// <param name="kind">Вид длительности.</param>
        /// <param name="remaining">Остаток в шагах или часах; для «до лечения» — 0.</param>
        public InjurySaveData(string id, InjuryDurationKind kind, int remaining)
        {
            Id = id;
            Kind = kind;
            Remaining = remaining;
        }

        /// <summary>Id травмы.</summary>
        public string Id { get; }

        /// <summary>Вид длительности.</summary>
        public InjuryDurationKind Kind { get; }

        /// <summary>Остаток в шагах или часах; у «до лечения» не используется.</summary>
        public int Remaining { get; }

        /// <summary>Сериализует травму.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["id"] = JsonValue.FromString(Id);
            value.Members["kind"] = JsonValue.FromString(InjuryDurationCodec.ToCode(Kind));
            value.Members["remaining"] = JsonValue.FromNumber(Remaining);
            return value;
        }

        /// <summary>Читает травму.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Запись травмы.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static InjurySaveData FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var kindCode = DataMap.RequireString(value, "kind", sourcePath);
            if (!InjuryDurationCodec.TryParse(kindCode, out var kind))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownInjuryDuration, kindCode);
            }

            var remaining = DataMap.RequireInt(value, "remaining", sourcePath);
            if (remaining < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "remaining");
            }

            return new InjurySaveData(id, kind, remaining);
        }
    }
}
