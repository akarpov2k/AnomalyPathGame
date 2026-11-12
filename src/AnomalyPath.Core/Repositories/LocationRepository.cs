#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий шаблонов локаций в памяти.</summary>
    public sealed class LocationRepository : ILocationRepository
    {
        private readonly TemplateStore<LocationTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых шаблонов.</summary>
        /// <param name="templates">Шаблоны локаций.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public LocationRepository(IEnumerable<LocationTemplate> templates)
        {
            _store = new TemplateStore<LocationTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.WorldInstanceNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<LocationTemplate> All => _store.All;

        /// <inheritdoc />
        public LocationTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>locations</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static LocationRepository FromLoadedData(LoadedData data) =>
            new LocationRepository(TemplateStore<LocationTemplate>.Load(
                data,
                "locations",
                LocationTemplate.FromJson));
    }
}
