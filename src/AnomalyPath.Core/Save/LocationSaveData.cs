#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Save
{
    /// <summary>Сохранённое состояние одного экземпляра локации.</summary>
    public sealed class LocationSaveData
    {
        /// <summary>Создаёт запись локации.</summary>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <param name="templateId">Id шаблона.</param>
        /// <param name="seed">Seed наполнения.</param>
        /// <param name="background">Фон.</param>
        /// <param name="subtype">Подтип локации в маршруте.</param>
        /// <param name="tier">Тир.</param>
        /// <param name="danger">Сложность.</param>
        /// <param name="visited">Был ли игрок.</param>
        /// <param name="cleared">Зачищена ли.</param>
        /// <param name="enemyGone">Покинул ли противник локацию после побега.</param>
        /// <param name="stashRolled">Проверялось ли появление тайника.</param>
        /// <param name="stashPresent">Есть ли на локации тайник.</param>
        /// <param name="stashOpened">Вскрыт ли тайник.</param>
        /// <param name="objectsSpawned">Выполнен ли бросок спавна объектов локации.</param>
        /// <param name="objects">Объекты, появившиеся на локации, с оставшимся лутом.</param>
        /// <param name="routeStep">Сколько шагов от хаба; у хаба 0.</param>
        /// <param name="routeEdge">Номер звена маршрута; у хаба 0.</param>
        /// <param name="stashLockAttempts">Сколько попыток взлома тайника потрачено.</param>
        /// <param name="stashLockSolved">Замок тайника вскрыт верным кодом.</param>
        /// <param name="stashLockFailed">Попытки взлома кончились, тайник потерян.</param>
        /// <param name="stashLockGuesses">Введённые коды в порядке попыток.</param>
        public LocationSaveData(
            string instanceId,
            string templateId,
            int seed,
            string background,
            LocationSubtype subtype,
            int tier,
            double danger,
            bool visited,
            bool cleared,
            bool enemyGone = false,
            bool stashRolled = false,
            bool stashPresent = false,
            bool stashOpened = false,
            bool objectsSpawned = false,
            IReadOnlyList<LocationObjectSaveData>? objects = null,
            int routeStep = 0,
            int routeEdge = 0,
            int stashLockAttempts = 0,
            bool stashLockSolved = false,
            bool stashLockFailed = false,
            IReadOnlyList<string>? stashLockGuesses = null)
        {
            InstanceId = instanceId;
            TemplateId = templateId;
            Seed = seed;
            Background = background;
            Subtype = subtype;
            Tier = tier;
            Danger = danger;
            Visited = visited;
            Cleared = cleared;
            EnemyGone = enemyGone;
            StashRolled = stashRolled;
            StashPresent = stashPresent;
            StashOpened = stashOpened;
            ObjectsSpawned = objectsSpawned;
            Objects = objects ?? System.Array.Empty<LocationObjectSaveData>();
            RouteStep = routeStep;
            RouteEdge = routeEdge;
            StashLockAttempts = stashLockAttempts;
            StashLockSolved = stashLockSolved;
            StashLockFailed = stashLockFailed;
            StashLockGuesses = stashLockGuesses ?? System.Array.Empty<string>();
        }

        /// <summary>Id экземпляра.</summary>
        public string InstanceId { get; }

        /// <summary>Id шаблона.</summary>
        public string TemplateId { get; }

        /// <summary>Seed наполнения.</summary>
        public int Seed { get; }

        /// <summary>Фон.</summary>
        public string Background { get; }

        /// <summary>Подтип локации.</summary>
        public LocationSubtype Subtype { get; }

        /// <summary>Сколько шагов от хаба; у хаба 0.</summary>
        public int RouteStep { get; }

        /// <summary>Номер звена маршрута; у хаба 0.</summary>
        public int RouteEdge { get; }

        /// <summary>Тир.</summary>
        public int Tier { get; }

        /// <summary>Сложность.</summary>
        public double Danger { get; }

        /// <summary>Был ли игрок.</summary>
        public bool Visited { get; }

        /// <summary>Зачищена ли.</summary>
        public bool Cleared { get; }

        /// <summary>Покинул ли противник локацию после успешного побега.</summary>
        public bool EnemyGone { get; }

        /// <summary>Проверялось ли появление тайника.</summary>
        public bool StashRolled { get; }

        /// <summary>Есть ли на локации тайник.</summary>
        public bool StashPresent { get; }

        /// <summary>Вскрыт ли тайник.</summary>
        public bool StashOpened { get; }

        /// <summary>Выполнен ли бросок спавна объектов локации.</summary>
        public bool ObjectsSpawned { get; }

        /// <summary>Объекты, появившиеся на локации, с оставшимся лутом.</summary>
        public IReadOnlyList<LocationObjectSaveData> Objects { get; }

        /// <summary>Сколько попыток взлома тайника потрачено.</summary>
        public int StashLockAttempts { get; }

        /// <summary>Замок тайника вскрыт верным кодом.</summary>
        public bool StashLockSolved { get; }

        /// <summary>Попытки взлома кончились, тайник потерян.</summary>
        public bool StashLockFailed { get; }

        /// <summary>Введённые коды в порядке попыток.</summary>
        public IReadOnlyList<string> StashLockGuesses { get; }

        /// <summary>Сериализует запись.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["instanceId"] = JsonValue.FromString(InstanceId);
            value.Members["templateId"] = JsonValue.FromString(TemplateId);
            value.Members["seed"] = JsonValue.FromNumber(Seed);
            value.Members["background"] = JsonValue.FromString(Background);
            value.Members["subtype"] = JsonValue.FromString(SchemaEnum.ToCode(Subtype));
            value.Members["routeStep"] = JsonValue.FromNumber(RouteStep);
            value.Members["routeEdge"] = JsonValue.FromNumber(RouteEdge);
            value.Members["tier"] = JsonValue.FromNumber(Tier);
            value.Members["danger"] = JsonValue.FromNumber(Danger);
            value.Members["visited"] = JsonValue.FromBool(Visited);
            value.Members["cleared"] = JsonValue.FromBool(Cleared);
            value.Members["enemyGone"] = JsonValue.FromBool(EnemyGone);
            value.Members["stashRolled"] = JsonValue.FromBool(StashRolled);
            value.Members["stashPresent"] = JsonValue.FromBool(StashPresent);
            value.Members["stashOpened"] = JsonValue.FromBool(StashOpened);
            value.Members["stashLockAttempts"] = JsonValue.FromNumber(StashLockAttempts);
            value.Members["stashLockSolved"] = JsonValue.FromBool(StashLockSolved);
            value.Members["stashLockFailed"] = JsonValue.FromBool(StashLockFailed);
            var guesses = JsonValue.NewArray();
            foreach (var guess in StashLockGuesses)
            {
                guesses.Items.Add(JsonValue.FromString(guess));
            }

            value.Members["stashLockGuesses"] = guesses;
            value.Members["objectsSpawned"] = JsonValue.FromBool(ObjectsSpawned);
            var objects = JsonValue.NewArray();
            foreach (var locationObject in Objects)
            {
                objects.Items.Add(locationObject.ToJson());
            }

            value.Members["objects"] = objects;
            return value;
        }

        /// <summary>Читает запись локации.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Запись локации.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static LocationSaveData FromJson(JsonValue value, string sourcePath)
        {
            var instanceId = DataMap.RequireString(value, "instanceId", sourcePath);
            var templateId = DataMap.RequireString(value, "templateId", sourcePath);
            var seed = DataMap.RequireInt(value, "seed", sourcePath);
            var background = DataMap.OptionalString(value, "background") ?? string.Empty;
            var subtypeCode = DataMap.RequireString(value, "subtype", sourcePath);
            if (!SchemaEnum.TryParse(subtypeCode, out LocationSubtype subtype))
            {
                throw new DataException(sourcePath, CoreErrors.WorldUnknownSubtype, subtypeCode);
            }

            var routeStep = DataMap.OptionalInt(value, "routeStep", 0);
            var routeEdge = DataMap.OptionalInt(value, "routeEdge", 0);
            var tier = DataMap.OptionalInt(value, "tier", 0);
            var danger = DataMap.OptionalDouble(value, "danger") ?? 0;
            var visited = DataMap.OptionalBool(value, "visited", false);
            var cleared = DataMap.OptionalBool(value, "cleared", false);
            var enemyGone = DataMap.OptionalBool(value, "enemyGone", false);
            var stashRolled = DataMap.OptionalBool(value, "stashRolled", false);
            var stashPresent = DataMap.OptionalBool(value, "stashPresent", false);
            var stashOpened = DataMap.OptionalBool(value, "stashOpened", false);
            var stashLockAttempts = DataMap.OptionalInt(value, "stashLockAttempts", 0);
            var stashLockSolved = DataMap.OptionalBool(value, "stashLockSolved", false);
            var stashLockFailed = DataMap.OptionalBool(value, "stashLockFailed", false);
            var stashLockGuesses = new List<string>();
            if (value.TryGet("stashLockGuesses", out var guessesValue) && guessesValue.IsArray)
            {
                foreach (var guess in guessesValue.Items)
                {
                    if (guess.Kind != JsonKind.String)
                    {
                        throw new DataException(sourcePath, CoreErrors.SaveMissingField, "stashLockGuesses");
                    }

                    stashLockGuesses.Add(guess.Text);
                }
            }
            var objectsSpawned = DataMap.OptionalBool(value, "objectsSpawned", false);
            var objects = new List<LocationObjectSaveData>();
            if (value.TryGet("objects", out var objectsValue) && objectsValue.IsArray)
            {
                foreach (var item in objectsValue.Items)
                {
                    if (!item.IsObject)
                    {
                        throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                    }

                    objects.Add(LocationObjectSaveData.FromJson(item, sourcePath));
                }
            }

            if (tier < 0 || danger < 0 || danger > 1 || routeStep < 0 || routeEdge < 0 ||
                (stashPresent && !stashRolled) || (stashOpened && !stashPresent) ||
                (!objectsSpawned && objects.Count > 0) ||
                stashLockAttempts < 0 || (stashLockSolved && stashLockFailed) ||
                stashLockGuesses.Count > stashLockAttempts)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "location");
            }

            return new LocationSaveData(
                instanceId,
                templateId,
                seed,
                background,
                subtype,
                tier,
                danger,
                visited,
                cleared,
                enemyGone,
                stashRolled,
                stashPresent,
                stashOpened,
                objectsSpawned,
                objects,
                routeStep,
                routeEdge,
                stashLockAttempts,
                stashLockSolved,
                stashLockFailed,
                stashLockGuesses);
        }
    }
}
