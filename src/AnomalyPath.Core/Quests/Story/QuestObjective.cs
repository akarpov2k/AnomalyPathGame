#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>
    /// Цель сюжетного квеста: тип, счётчик и параметры конкретной цели. Один класс на все типы,
    /// потому что цели отличаются набором необязательных полей, а не поведением.
    /// </summary>
    public sealed class QuestObjective
    {
        /// <summary>Создаёт цель сюжетного квеста.</summary>
        /// <param name="type">Тип цели.</param>
        /// <param name="count">Сколько раз выполнить цель; не меньше 1.</param>
        /// <param name="actorPool">Пул актёров для <see cref="QuestObjectiveType.KillActors"/>.</param>
        /// <param name="minTier">Минимальный тир цели.</param>
        /// <param name="maxTier">Максимальный тир цели.</param>
        /// <param name="objectKind">Вид объекта для <see cref="QuestObjectiveType.InspectObjects"/>.</param>
        /// <param name="excludeSubtypes">Подтипы локаций, которые не считаются.</param>
        /// <param name="chainId">Цепочка для <see cref="QuestObjectiveType.ClearChain"/>.</param>
        /// <param name="completion">Как завершается цепочка.</param>
        /// <param name="locationId">Локация для <see cref="QuestObjectiveType.ReachLocation"/>.</param>
        /// <param name="actorId">Персонаж для <see cref="QuestObjectiveType.TalkToActor"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Счётчик меньше единицы или диапазон тиров задан неверно.
        /// </exception>
        public QuestObjective(
            QuestObjectiveType type,
            int count = 1,
            IReadOnlyList<string>? actorPool = null,
            int minTier = 0,
            int maxTier = 0,
            LocationObjectKind? objectKind = null,
            IReadOnlyList<LocationSubtype>? excludeSubtypes = null,
            string? chainId = null,
            ChainCompletion? completion = null,
            string? locationId = null,
            string? actorId = null)
        {
            if (count < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (minTier < 0 || maxTier < minTier)
            {
                throw new ArgumentOutOfRangeException(nameof(minTier));
            }

            Type = type;
            Count = count;
            ActorPool = actorPool ?? Array.Empty<string>();
            MinTier = minTier;
            MaxTier = maxTier;
            ObjectKind = objectKind;
            ExcludeSubtypes = excludeSubtypes ?? Array.Empty<LocationSubtype>();
            ChainId = chainId;
            Completion = completion;
            LocationId = locationId;
            ActorId = actorId;
        }

        /// <summary>Тип цели.</summary>
        public QuestObjectiveType Type { get; }

        /// <summary>Сколько раз выполнить цель.</summary>
        public int Count { get; }

        /// <summary>Пул актёров для убийства; пусто — любые подходящие.</summary>
        public IReadOnlyList<string> ActorPool { get; }

        /// <summary>Минимальный тир цели.</summary>
        public int MinTier { get; }

        /// <summary>Максимальный тир цели.</summary>
        public int MaxTier { get; }

        /// <summary>Вид объекта для осмотра или <c>null</c>.</summary>
        public LocationObjectKind? ObjectKind { get; }

        /// <summary>Подтипы локаций, которые не считаются.</summary>
        public IReadOnlyList<LocationSubtype> ExcludeSubtypes { get; }

        /// <summary>Цепочка локаций или <c>null</c>.</summary>
        public string? ChainId { get; }

        /// <summary>Способ завершения цепочки или <c>null</c>.</summary>
        public ChainCompletion? Completion { get; }

        /// <summary>Целевая локация или <c>null</c>.</summary>
        public string? LocationId { get; }

        /// <summary>Целевой персонаж или <c>null</c>.</summary>
        public string? ActorId { get; }

        /// <summary>Читает цель из записи данных.</summary>
        /// <param name="value">Запись цели.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Цель квеста.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static QuestObjective FromJson(JsonValue value, string sourcePath)
        {
            var code = DataMap.RequireString(value, "type", sourcePath);
            if (!QuestObjectiveTypeCodec.TryParse(code, out var type))
            {
                throw new DataException(sourcePath, CoreErrors.StoryObjectiveType, code);
            }

            switch (type)
            {
                case QuestObjectiveType.ClearLocations:
                    return new QuestObjective(
                        type,
                        ReadCount(value, sourcePath),
                        excludeSubtypes: ReadSubtypes(value, sourcePath));

                case QuestObjectiveType.KillActors:
                {
                    var minTier = DataMap.OptionalInt(value, "minTier", 0);
                    var maxTier = DataMap.OptionalInt(value, "maxTier", minTier);
                    if (minTier < 0 || maxTier < minTier)
                    {
                        throw new DataException(sourcePath, CoreErrors.QuestTierRange);
                    }

                    return new QuestObjective(
                        type,
                        ReadCount(value, sourcePath),
                        DataMap.OptionalStringArray(value, "actorPool"),
                        minTier,
                        maxTier);
                }

                case QuestObjectiveType.InspectObjects:
                {
                    var kindCode = DataMap.RequireString(value, "objectKind", sourcePath);
                    if (!SchemaEnum.TryParse(kindCode, out LocationObjectKind objectKind))
                    {
                        throw new DataException(
                            sourcePath,
                            CoreErrors.StoryObjectiveObjectKind,
                            kindCode);
                    }

                    return new QuestObjective(
                        type,
                        ReadCount(value, sourcePath),
                        objectKind: objectKind);
                }

                case QuestObjectiveType.OpenStashes:
                    return new QuestObjective(type, ReadCount(value, sourcePath));

                case QuestObjectiveType.ClearChain:
                {
                    var chainId = DataMap.RequireString(value, "chainId", sourcePath);
                    var completionCode = DataMap.RequireString(value, "completion", sourcePath);
                    if (!ChainCompletionCodec.TryParse(completionCode, out var completion))
                    {
                        throw new DataException(
                            sourcePath,
                            CoreErrors.StoryObjectiveCompletion,
                            completionCode);
                    }

                    var count = DataMap.OptionalInt(value, "count", 1);
                    if (count < 1)
                    {
                        throw new DataException(sourcePath, CoreErrors.StoryObjectiveCount, "count");
                    }

                    return new QuestObjective(type, count, chainId: chainId, completion: completion);
                }

                case QuestObjectiveType.ReachLocation:
                    return new QuestObjective(
                        type,
                        locationId: DataMap.RequireString(value, "locationId", sourcePath));

                case QuestObjectiveType.TalkToActor:
                    return new QuestObjective(
                        type,
                        actorId: DataMap.RequireString(value, "actorId", sourcePath));

                default:
                    throw new DataException(sourcePath, CoreErrors.StoryObjectiveType, code);
            }
        }

        /// <summary>Читает список целей из поля <c>objectives</c>.</summary>
        /// <param name="value">Объект, в котором ищется поле.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <param name="emptyErrorCode">Код ошибки, если список пуст или отсутствует.</param>
        /// <returns>Цели в порядке данных.</returns>
        /// <exception cref="DataException">Поле отсутствует, пусто или задано неверно.</exception>
        public static IReadOnlyList<QuestObjective> ReadList(
            JsonValue value,
            string sourcePath,
            string emptyErrorCode)
        {
            if (!value.TryGet("objectives", out var objectives) ||
                !objectives.IsArray ||
                objectives.Items.Count == 0)
            {
                throw new DataException(sourcePath, emptyErrorCode, "objectives");
            }

            var result = new List<QuestObjective>();
            foreach (var item in objectives.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                result.Add(FromJson(item, sourcePath));
            }

            return result;
        }

        private static int ReadCount(JsonValue value, string sourcePath)
        {
            var count = DataMap.RequireInt(value, "count", sourcePath);
            if (count < 1)
            {
                throw new DataException(sourcePath, CoreErrors.StoryObjectiveCount, "count");
            }

            return count;
        }

        private static IReadOnlyList<LocationSubtype> ReadSubtypes(
            JsonValue value,
            string sourcePath)
        {
            var result = new List<LocationSubtype>();
            foreach (var code in DataMap.OptionalStringArray(value, "excludeSubtypes"))
            {
                if (!SchemaEnum.TryParse(code, out LocationSubtype subtype))
                {
                    throw new DataException(sourcePath, CoreErrors.StoryObjectiveSubtype, code);
                }

                if (!result.Contains(subtype))
                {
                    result.Add(subtype);
                }
            }

            return result;
        }
    }
}
