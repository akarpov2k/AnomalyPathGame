#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий актёров в памяти.</summary>
    public sealed class ActorRepository : IActorRepository
    {
        private readonly TemplateStore<ActorTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых актёров.</summary>
        /// <param name="templates">Актёры.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public ActorRepository(IEnumerable<ActorTemplate> templates)
        {
            _store = new TemplateStore<ActorTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.ItemNotFound));
        }

        /// <inheritdoc />
        public IReadOnlyList<ActorTemplate> All => _store.All;

        /// <inheritdoc />
        public ActorTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>actors</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static ActorRepository FromLoadedData(LoadedData data) =>
            new ActorRepository(TemplateStore<ActorTemplate>.Load(data, "actors", ActorTemplate.FromJson));
    }
}
