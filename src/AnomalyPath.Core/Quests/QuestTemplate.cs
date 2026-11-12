#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Quests
{
    /// <summary>
    /// Шаблон повторяемого квеста: тип цели, пул целей, награда и условия выдачи.
    /// Конкретная цель (локация, мутанты, предметы) выбирается при создании предложения.
    /// </summary>
    public sealed class QuestTemplate
    {
        /// <summary>Создаёт шаблон квеста.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="questKind">Вид: сюжетный или повторяемый.</param>
        /// <param name="type">Тип цели.</param>
        /// <param name="titleKey">Ключ названия.</param>
        /// <param name="descriptionKey">Ключ описания.</param>
        /// <param name="difficulty">Сложность: слот доски подбирает квест по ней.</param>
        /// <param name="cooldownHours">Откат после сдачи; 0 — значение из правил.</param>
        /// <param name="failCondition">Условие провала квеста.</param>
        /// <param name="timeLimitHours">Срок с момента взятия для провала по времени; 0 — без срока.</param>
        /// <param name="requirements">Условия выдачи.</param>
        /// <param name="rewards">Награда.</param>
        /// <param name="clearLocation">Цель зачистки или <c>null</c>.</param>
        /// <param name="killMutants">Цель убийства или <c>null</c>.</param>
        /// <param name="gatherArtifacts">Цель сбора артефактов или <c>null</c>.</param>
        /// <param name="findItems">Цель поиска предметов и NPC или <c>null</c>.</param>
        /// <param name="art">Id картинки квеста в каталоге спрайтов или <c>null</c>.</param>
        public QuestTemplate(
            string id,
            QuestKind questKind,
            QuestType type,
            string titleKey,
            string descriptionKey,
            QuestDifficulty difficulty,
            int cooldownHours,
            QuestFailCondition failCondition,
            int timeLimitHours,
            RequirementsTemplate requirements,
            QuestRewardTemplate rewards,
            ClearLocationTarget? clearLocation,
            KillMutantsTarget? killMutants,
            GatherArtifactsTarget? gatherArtifacts,
            FindItemsTarget? findItems,
            string? art = null)
        {
            Id = id;
            QuestKind = questKind;
            Type = type;
            TitleKey = titleKey;
            DescriptionKey = descriptionKey;
            Difficulty = difficulty;
            CooldownHours = cooldownHours;
            FailCondition = failCondition;
            TimeLimitHours = timeLimitHours;
            Requirements = requirements;
            Rewards = rewards;
            ClearLocationTarget = clearLocation;
            KillMutantsTarget = killMutants;
            GatherArtifactsTarget = gatherArtifacts;
            FindItemsTarget = findItems;
            Art = string.IsNullOrWhiteSpace(art) ? null : art;
        }

        /// <summary>Идентификатор.</summary>
        public string Id { get; }

        /// <summary>Вид квеста.</summary>
        public QuestKind QuestKind { get; }

        /// <summary>Тип цели.</summary>
        public QuestType Type { get; }

        /// <summary>Ключ названия.</summary>
        public string TitleKey { get; }

        /// <summary>Ключ описания.</summary>
        public string DescriptionKey { get; }

        /// <summary>
        /// Id картинки квеста в каталоге спрайтов (<c>quests/...</c>) или <c>null</c>:
        /// без арта доска показывает заглушку по типу цели.
        /// </summary>
        public string? Art { get; }

        /// <summary>Сложность: слот доски подбирает квест по ней.</summary>
        public QuestDifficulty Difficulty { get; }

        /// <summary>Откат после сдачи в часах; 0 — значение из правил.</summary>
        public int CooldownHours { get; }

        /// <summary>Условие провала: смерть игрока или истечение срока.</summary>
        public QuestFailCondition FailCondition { get; }

        /// <summary>Срок с момента взятия в часах; 0 — срок не задан.</summary>
        public int TimeLimitHours { get; }

        /// <summary>Условия выдачи.</summary>
        public RequirementsTemplate Requirements { get; }

        /// <summary>Награда.</summary>
        public QuestRewardTemplate Rewards { get; }

        /// <summary>Цель зачистки локации.</summary>
        public ClearLocationTarget? ClearLocation => ClearLocationTarget;

        /// <summary>Цель убийства мутантов.</summary>
        public KillMutantsTarget? KillMutants => KillMutantsTarget;

        /// <summary>Цель сбора артефактов.</summary>
        public GatherArtifactsTarget? GatherArtifacts => GatherArtifactsTarget;

        /// <summary>Цель поиска предметов и NPC.</summary>
        public FindItemsTarget? FindItems => FindItemsTarget;

        private ClearLocationTarget? ClearLocationTarget { get; }

        private KillMutantsTarget? KillMutantsTarget { get; }

        private GatherArtifactsTarget? GatherArtifactsTarget { get; }

        private FindItemsTarget? FindItemsTarget { get; }

        /// <summary>Читает шаблон квеста.</summary>
        /// <param name="value">Запись данных.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Шаблон квеста.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static QuestTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var kindCode = DataMap.RequireString(value, "questKind", sourcePath);
            if (!SchemaEnum.TryParse(kindCode, out QuestKind questKind))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownQuestKind, kindCode);
            }

            var typeCode = DataMap.RequireString(value, "questType", sourcePath);
            if (!QuestTypeCodec.TryParse(typeCode, out QuestType type))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownQuestType, typeCode);
            }

            var titleKey = DataMap.RequireString(value, "titleKey", sourcePath);
            var descriptionKey = DataMap.RequireString(value, "descriptionKey", sourcePath);
            var difficultyCode = DataMap.RequireString(value, "difficulty", sourcePath);
            if (!SchemaEnum.TryParse(difficultyCode, out QuestDifficulty difficulty))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownQuestDifficulty, difficultyCode);
            }

            var cooldownHours = DataMap.OptionalInt(value, "cooldownHours", 0);
            if (cooldownHours < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "cooldownHours");
            }

            var failCondition = QuestFailCondition.Death;
            var failCode = DataMap.OptionalString(value, "failCondition");
            if (failCode != null && !SchemaEnum.TryParse(failCode, out failCondition))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownQuestFailCondition, failCode);
            }

            var timeLimitHours = DataMap.OptionalInt(value, "timeLimitHours", 0);
            if (timeLimitHours < 0 ||
                (failCondition == QuestFailCondition.Time && timeLimitHours < 1) ||
                (failCondition == QuestFailCondition.Death && timeLimitHours > 0))
            {
                throw new DataException(sourcePath, CoreErrors.QuestTimeLimit, id);
            }

            var requirements = RequirementsTemplate.Read(value, sourcePath);
            var rewards = ReadRewards(value, sourcePath);
            var art = DataMap.OptionalString(value, "art");
            if (!value.TryGet("target", out var target) || !target.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingObjectField, "target");
            }

            ClearLocationTarget? clearLocation = null;
            KillMutantsTarget? killMutants = null;
            GatherArtifactsTarget? gatherArtifacts = null;
            FindItemsTarget? findItems = null;
            switch (type)
            {
                case QuestType.ClearLocation:
                    clearLocation = ReadClearLocation(target, sourcePath);
                    break;
                case QuestType.KillMutants:
                    killMutants = ReadKillMutants(target, sourcePath);
                    break;
                case QuestType.GatherArtifacts:
                    gatherArtifacts = ReadGatherArtifacts(target, sourcePath);
                    break;
                case QuestType.FindItems:
                    findItems = ReadFindItems(target, sourcePath);
                    break;
            }

            return new QuestTemplate(
                id,
                questKind,
                type,
                titleKey,
                descriptionKey,
                difficulty,
                cooldownHours,
                failCondition,
                timeLimitHours,
                requirements,
                rewards,
                clearLocation,
                killMutants,
                gatherArtifacts,
                findItems,
                art);
        }

        private static QuestRewardTemplate ReadRewards(JsonValue value, string sourcePath) =>
            QuestRewardTemplate.Read(value, sourcePath);

        private static ClearLocationTarget ReadClearLocation(JsonValue target, string sourcePath)
        {
            var pool = DataMap.OptionalStringArray(target, "locationPool");
            var minTier = DataMap.OptionalInt(target, "minTier", 0);
            var maxTier = DataMap.OptionalInt(target, "maxTier", minTier);
            ValidateTier(minTier, maxTier, sourcePath);
            return new ClearLocationTarget(pool, minTier, maxTier);
        }

        private static KillMutantsTarget ReadKillMutants(JsonValue target, string sourcePath)
        {
            var count = RequirePositive(target, "count", sourcePath);
            var poolSize = DataMap.OptionalInt(target, "poolSize", 1);
            if (poolSize < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "poolSize");
            }

            var minTier = DataMap.OptionalInt(target, "minTier", 0);
            var maxTier = DataMap.OptionalInt(target, "maxTier", minTier);
            ValidateTier(minTier, maxTier, sourcePath);
            return new KillMutantsTarget(
                DataMap.OptionalStringArray(target, "actorPool"),
                count,
                poolSize,
                minTier,
                maxTier);
        }

        private static GatherArtifactsTarget ReadGatherArtifacts(JsonValue target, string sourcePath)
        {
            var count = RequirePositive(target, "count", sourcePath);
            var poolSize = DataMap.OptionalInt(target, "poolSize", 1);
            if (poolSize < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "poolSize");
            }

            var minTier = DataMap.OptionalInt(target, "minTier", 0);
            var maxTier = DataMap.OptionalInt(target, "maxTier", minTier);
            ValidateTier(minTier, maxTier, sourcePath);
            return new GatherArtifactsTarget(
                DataMap.OptionalStringArray(target, "itemPool"),
                count,
                poolSize,
                minTier,
                maxTier);
        }

        private static FindItemsTarget ReadFindItems(JsonValue target, string sourcePath)
        {
            var itemKinds = DataMap.OptionalInt(target, "itemKinds", 1);
            if (itemKinds < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "itemKinds");
            }

            var itemCount = DataMap.OptionalInt(target, "itemCount", 1);
            if (itemCount < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "itemCount");
            }

            var npcCount = RequirePositive(target, "npcCount", sourcePath);
            var poolSize = DataMap.OptionalInt(target, "poolSize", 1);
            if (poolSize < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "poolSize");
            }

            var minTier = DataMap.OptionalInt(target, "minTier", 0);
            var maxTier = DataMap.OptionalInt(target, "maxTier", minTier);
            ValidateTier(minTier, maxTier, sourcePath);
            var categories = ReadCategories(target, sourcePath);
            if (categories.Count == 0)
            {
                categories = new[] { ItemCategory.Weapon, ItemCategory.Armor };
            }

            return new FindItemsTarget(
                DataMap.OptionalStringArray(target, "itemPool"),
                categories,
                itemKinds,
                itemCount,
                DataMap.OptionalStringArray(target, "npcPool"),
                npcCount,
                poolSize,
                minTier,
                maxTier);
        }

        private static IReadOnlyList<ItemCategory> ReadCategories(JsonValue target, string sourcePath)
        {
            var result = new List<ItemCategory>();
            foreach (var code in DataMap.OptionalStringArray(target, "itemCategories"))
            {
                if (!SchemaEnum.TryParse(code, out ItemCategory category))
                {
                    throw new DataException(sourcePath, CoreErrors.UnknownItemCategory, code);
                }

                result.Add(category);
            }

            return result;
        }

        private static int RequirePositive(JsonValue value, string field, string sourcePath)
        {
            var number = DataMap.RequireInt(value, field, sourcePath);
            if (number < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, field);
            }

            return number;
        }

        private static void ValidateTier(int minTier, int maxTier, string sourcePath)
        {
            if (minTier < 0 || maxTier < minTier)
            {
                throw new DataException(sourcePath, CoreErrors.QuestTierRange);
            }
        }
    }
}
