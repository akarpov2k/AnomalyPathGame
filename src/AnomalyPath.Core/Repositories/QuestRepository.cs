#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий шаблонов квестов в памяти.</summary>
    public sealed class QuestRepository : IQuestRepository
    {
        private readonly TemplateStore<QuestTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых шаблонов.</summary>
        /// <param name="templates">Шаблоны квестов.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public QuestRepository(IEnumerable<QuestTemplate> templates)
        {
            _store = new TemplateStore<QuestTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.QuestNotFound));
        }

        /// <inheritdoc />
        public IReadOnlyList<QuestTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out QuestTemplate template) => _store.TryGet(id, out template!);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <inheritdoc />
        public QuestTemplate Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из записей вида <c>quests</c> с <c>questKind = repeatable</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static QuestRepository FromLoadedData(LoadedData data)
        {
            var result = new List<QuestTemplate>();
            foreach (var entity in data.All("quests"))
            {
                if (IsStory(entity))
                {
                    continue;
                }

                var path = entity.TryGet("id", out var id) ? id.Text : "quests";
                result.Add(QuestTemplate.FromJson(entity, path));
            }

            return new QuestRepository(result);
        }

        private static bool IsStory(JsonValue entity) =>
            entity.TryGet("questKind", out var kind) &&
            kind.Kind == JsonKind.String &&
            SchemaEnum.TryParse(kind.Text, out QuestKind value) &&
            value == QuestKind.Story;
    }
}
