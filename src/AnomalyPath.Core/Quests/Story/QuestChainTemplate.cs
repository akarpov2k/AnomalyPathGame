#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Квестовая цепочка локаций: порядок шагов и принудительные объекты на них.</summary>
    public sealed class QuestChainTemplate
    {
        /// <summary>Создаёт цепочку.</summary>
        /// <param name="id">Идентификатор цепочки.</param>
        /// <param name="locations">Шаги в порядке прохождения.</param>
        /// <param name="completionItemId">Предмет за полную зачистку цепочки или <c>null</c>.</param>
        /// <exception cref="ArgumentException">Идентификатор пуст.</exception>
        /// <exception cref="ArgumentNullException">Шаги не заданы.</exception>
        public QuestChainTemplate(
            string id,
            IReadOnlyList<QuestChainStep> locations,
            string? completionItemId = null)
        {
            Id = id;
            Locations = locations ?? throw new ArgumentNullException(nameof(locations));
            CompletionItemId = completionItemId;
        }

        /// <summary>Идентификатор цепочки.</summary>
        public string Id { get; }

        /// <summary>Шаги в порядке прохождения.</summary>
        public IReadOnlyList<QuestChainStep> Locations { get; }

        /// <summary>Предмет, который выдаётся за полную зачистку цепочки.</summary>
        public string? CompletionItemId { get; }

        /// <summary>Ищет шаг по шаблону локации.</summary>
        /// <param name="locationTemplateId">Шаблон локации.</param>
        /// <returns>Найденный шаг или <c>null</c>.</returns>
        public QuestChainStep? FindStep(string locationTemplateId)
        {
            foreach (var step in Locations)
            {
                if (string.Equals(step.LocationId, locationTemplateId, StringComparison.Ordinal))
                {
                    return step;
                }
            }

            return null;
        }

        /// <summary>Читает цепочку из записи данных.</summary>
        /// <param name="value">Запись цепочки.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Цепочка.</returns>
        /// <exception cref="DataException">Поле <c>locations</c> отсутствует или пусто.</exception>
        public static QuestChainTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            if (!value.TryGet("locations", out var locations) ||
                !locations.IsArray ||
                locations.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.ChainEmpty, id);
            }

            var result = new List<QuestChainStep>();
            foreach (var item in locations.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                result.Add(new QuestChainStep(
                    DataMap.RequireString(item, "locationId", sourcePath),
                    DataMap.OptionalString(item, "enemySpawnId"),
                    DataMap.OptionalString(item, "npcSpawnId")));
            }

            return new QuestChainTemplate(
                id,
                result,
                DataMap.OptionalString(value, "completionItemId"));
        }
    }
}
