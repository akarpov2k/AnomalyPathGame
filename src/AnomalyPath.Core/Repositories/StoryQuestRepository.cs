#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий шаблонов сюжетных квестов в памяти.</summary>
    public sealed class StoryQuestRepository : IStoryQuestRepository
    {
        private readonly TemplateStore<StoryQuestTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых шаблонов.</summary>
        /// <param name="templates">Шаблоны сюжетных квестов.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public StoryQuestRepository(IEnumerable<StoryQuestTemplate> templates)
        {
            _store = new TemplateStore<StoryQuestTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.StoryQuestNotFound));
        }

        /// <inheritdoc />
        public IReadOnlyList<StoryQuestTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out StoryQuestTemplate template) =>
            _store.TryGet(id, out template!);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <inheritdoc />
        public StoryQuestTemplate Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из записей вида <c>quests</c> с <c>questKind = story</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static StoryQuestRepository FromLoadedData(LoadedData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            var result = new List<StoryQuestTemplate>();
            foreach (var entity in data.All("quests"))
            {
                if (!IsStory(entity))
                {
                    continue;
                }

                var path = entity.TryGet("id", out var id) ? id.Text : "quests";
                result.Add(StoryQuestTemplate.FromJson(entity, path));
            }

            return new StoryQuestRepository(result);
        }

        private static bool IsStory(JsonValue entity) =>
            entity.TryGet("questKind", out var kind) &&
            kind.Kind == JsonKind.String &&
            SchemaEnum.TryParse(kind.Text, out QuestKind value) &&
            value == QuestKind.Story;
    }
}
