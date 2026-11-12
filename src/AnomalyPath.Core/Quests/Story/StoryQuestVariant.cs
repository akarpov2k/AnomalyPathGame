#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>
    /// Вариант условий сюжетного квеста: свой список целей и своя награда. Стандартный вариант
    /// собирается из полей шаблона, остальные (например, <c>hard</c>) задаются в данных явно.
    /// </summary>
    public sealed class StoryQuestVariant
    {
        /// <summary>Создаёт вариант условий.</summary>
        /// <param name="id">Идентификатор варианта.</param>
        /// <param name="textKey">Ключ реплики варианта или <c>null</c>.</param>
        /// <param name="objectives">Цели варианта; не пусто.</param>
        /// <param name="rewards">Награда варианта.</param>
        /// <exception cref="ArgumentException">Идентификатор пуст.</exception>
        /// <exception cref="ArgumentNullException">Цели не заданы.</exception>
        public StoryQuestVariant(
            string id,
            string? textKey,
            IReadOnlyList<QuestObjective> objectives,
            QuestRewardTemplate rewards)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            Id = id;
            TextKey = textKey;
            Objectives = objectives ?? throw new ArgumentNullException(nameof(objectives));
            Rewards = rewards ?? QuestRewardTemplate.Empty;
        }

        /// <summary>Идентификатор варианта.</summary>
        public string Id { get; }

        /// <summary>Ключ реплики варианта или <c>null</c>.</summary>
        public string? TextKey { get; }

        /// <summary>Цели варианта.</summary>
        public IReadOnlyList<QuestObjective> Objectives { get; }

        /// <summary>Награда варианта.</summary>
        public QuestRewardTemplate Rewards { get; }

        /// <summary>Читает вариант из записи данных.</summary>
        /// <param name="value">Запись варианта.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Вариант условий.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static StoryQuestVariant FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            if (string.Equals(id, StoryQuestTemplate.StandardVariantId, StringComparison.Ordinal))
            {
                throw new DataException(sourcePath, CoreErrors.StoryVariantStandard, id);
            }

            var textKey = DataMap.OptionalString(value, "textKey");
            var objectives = QuestObjective.ReadList(value, sourcePath, CoreErrors.StoryVariantObjectives);
            var rewards = QuestRewardTemplate.Read(value, sourcePath);
            return new StoryQuestVariant(id, textKey, objectives, rewards);
        }
    }
}
