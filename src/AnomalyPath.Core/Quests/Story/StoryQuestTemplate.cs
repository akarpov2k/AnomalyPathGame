#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>
    /// Шаблон сюжетного квеста: линия, кто выдаёт и принимает, цели, варианты условий
    /// и область прогресса. Выдаётся и сдаётся только в диалоге.
    /// </summary>
    public sealed class StoryQuestTemplate
    {
        /// <summary>Идентификатор стандартного варианта; в данных его не задают.</summary>
        public const string StandardVariantId = "standard";

        /// <summary>Идентификатор сложного варианта: цели больше, награда меньше.</summary>
        public const string HardVariantId = "hard";

        private readonly List<StoryQuestVariant> _variants;

        /// <summary>Создаёт шаблон сюжетного квеста.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="storyLine">Линия, к которой относится квест.</param>
        /// <param name="titleKey">Ключ названия.</param>
        /// <param name="descriptionKey">Ключ описания.</param>
        /// <param name="giverActorId">Кто выдаёт квест.</param>
        /// <param name="turnInActorId">Кто принимает сдачу; <c>null</c> — выдающий.</param>
        /// <param name="nextQuestId">Следующий квест линии или <c>null</c>.</param>
        /// <param name="progressScope">Область прогресса.</param>
        /// <param name="requirements">Условия выдачи.</param>
        /// <param name="objectives">Цели стандартного варианта.</param>
        /// <param name="rewards">Награда стандартного варианта.</param>
        /// <param name="variants">Дополнительные варианты условий.</param>
        /// <param name="turnInItems">Предметы, которые изымаются при сдаче.</param>
        /// <exception cref="ArgumentException">Обязательная строка пуста.</exception>
        /// <exception cref="ArgumentNullException">Цели не заданы.</exception>
        public StoryQuestTemplate(
            string id,
            string storyLine,
            string titleKey,
            string descriptionKey,
            string giverActorId,
            string? turnInActorId,
            string? nextQuestId,
            QuestProgressScope progressScope,
            RequirementsTemplate requirements,
            IReadOnlyList<QuestObjective> objectives,
            QuestRewardTemplate rewards,
            IReadOnlyList<StoryQuestVariant>? variants = null,
            IReadOnlyList<QuestItemTarget>? turnInItems = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(storyLine))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(storyLine)),
                    nameof(storyLine));
            }

            if (string.IsNullOrWhiteSpace(giverActorId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(giverActorId)),
                    nameof(giverActorId));
            }

            Id = id;
            StoryLine = storyLine;
            TitleKey = titleKey;
            DescriptionKey = descriptionKey;
            GiverActorId = giverActorId;
            TurnInActorId = turnInActorId;
            NextQuestId = nextQuestId;
            ProgressScope = progressScope;
            Requirements = requirements ?? RequirementsTemplate.Empty;
            Objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
            Rewards = rewards ?? QuestRewardTemplate.Empty;
            _variants = variants == null ? new List<StoryQuestVariant>() : new List<StoryQuestVariant>(variants);
            TurnInItems = turnInItems ?? Array.Empty<QuestItemTarget>();
            Standard = new StoryQuestVariant(StandardVariantId, null, Objectives, Rewards);
        }

        /// <summary>Идентификатор.</summary>
        public string Id { get; }

        /// <summary>Линия, к которой относится квест.</summary>
        public string StoryLine { get; }

        /// <summary>Ключ названия.</summary>
        public string TitleKey { get; }

        /// <summary>Ключ описания.</summary>
        public string DescriptionKey { get; }

        /// <summary>Кто выдаёт квест.</summary>
        public string GiverActorId { get; }

        /// <summary>Кто принимает сдачу; <c>null</c> — выдающий персонаж.</summary>
        public string? TurnInActorId { get; }

        /// <summary>Следующий квест линии или <c>null</c>.</summary>
        public string? NextQuestId { get; }

        /// <summary>Область прогресса.</summary>
        public QuestProgressScope ProgressScope { get; }

        /// <summary>Условия выдачи.</summary>
        public RequirementsTemplate Requirements { get; }

        /// <summary>Цели стандартного варианта.</summary>
        public IReadOnlyList<QuestObjective> Objectives { get; }

        /// <summary>Награда стандартного варианта.</summary>
        public QuestRewardTemplate Rewards { get; }

        /// <summary>Дополнительные варианты условий.</summary>
        public IReadOnlyList<StoryQuestVariant> Variants => _variants;

        /// <summary>Предметы, которые изымаются при сдаче квеста.</summary>
        public IReadOnlyList<QuestItemTarget> TurnInItems { get; }

        /// <summary>Стандартный вариант, собранный из полей шаблона.</summary>
        public StoryQuestVariant Standard { get; }

        /// <summary>Кто принимает сдачу с учётом значения по умолчанию.</summary>
        public string TurnInActor => TurnInActorId ?? GiverActorId;

        /// <summary>Ищет вариант условий по идентификатору.</summary>
        /// <param name="id">Идентификатор варианта.</param>
        /// <param name="variant">Найденный вариант или стандартный.</param>
        /// <returns><c>true</c>, если вариант существует.</returns>
        public bool TryGetVariant(string id, out StoryQuestVariant variant)
        {
            if (string.Equals(id, StandardVariantId, StringComparison.Ordinal))
            {
                variant = Standard;
                return true;
            }

            foreach (var candidate in _variants)
            {
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    variant = candidate;
                    return true;
                }
            }

            variant = Standard;
            return false;
        }

        /// <summary>Читает шаблон сюжетного квеста из записи данных.</summary>
        /// <param name="value">Запись данных.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Шаблон квеста.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static StoryQuestTemplate FromJson(JsonValue value, string sourcePath)
        {
            var kindCode = DataMap.RequireString(value, "questKind", sourcePath);
            if (!SchemaEnum.TryParse(kindCode, out QuestKind kind) || kind != QuestKind.Story)
            {
                throw new DataException(sourcePath, CoreErrors.StoryQuestKind, kindCode);
            }

            var id = DataMap.RequireString(value, "id", sourcePath);
            var storyLine = DataMap.RequireString(value, "storyLine", sourcePath);
            var titleKey = DataMap.RequireString(value, "titleKey", sourcePath);
            var descriptionKey = DataMap.RequireString(value, "descriptionKey", sourcePath);
            var giverActorId = DataMap.RequireString(value, "giverActorId", sourcePath);
            var turnInActorId = DataMap.OptionalString(value, "turnInActorId");
            var nextQuestId = DataMap.OptionalString(value, "nextQuestId");

            var scopeCode = DataMap.RequireString(value, "progressScope", sourcePath);
            if (!SchemaEnum.TryParse(scopeCode, out QuestProgressScope progressScope))
            {
                throw new DataException(sourcePath, CoreErrors.StoryProgressScope, scopeCode);
            }

            var requirements = RequirementsTemplate.Read(value, sourcePath);
            var objectives = QuestObjective.ReadList(value, sourcePath, CoreErrors.StoryObjectivesEmpty);
            var rewards = QuestRewardTemplate.Read(value, sourcePath);
            var variants = ReadVariants(value, sourcePath);
            var turnInItems = QuestRewardTemplate.ReadItems(value, "turnInItems", sourcePath);

            return new StoryQuestTemplate(
                id,
                storyLine,
                titleKey,
                descriptionKey,
                giverActorId,
                turnInActorId,
                nextQuestId,
                progressScope,
                requirements,
                objectives,
                rewards,
                variants,
                turnInItems);
        }

        private static IReadOnlyList<StoryQuestVariant> ReadVariants(
            JsonValue value,
            string sourcePath)
        {
            var result = new List<StoryQuestVariant>();
            if (!value.TryGet("variants", out var variants) || !variants.IsArray)
            {
                return result;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in variants.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var variant = StoryQuestVariant.FromJson(item, sourcePath);
                if (!ids.Add(variant.Id))
                {
                    throw new DataException(sourcePath, CoreErrors.StoryVariantDuplicate, variant.Id);
                }

                result.Add(variant);
            }

            return result;
        }
    }
}
