#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Loadouts;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий наборов снаряжения в памяти.</summary>
    public sealed class LoadoutRepository : ILoadoutRepository
    {
        private readonly TemplateStore<LoadoutTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых наборов.</summary>
        /// <param name="loadouts">Наборы снаряжения.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public LoadoutRepository(IEnumerable<LoadoutTemplate> loadouts)
        {
            _store = new TemplateStore<LoadoutTemplate>(
                loadouts,
                loadout => loadout.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.LoadoutNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<LoadoutTemplate> All => _store.All;

        /// <inheritdoc />
        public LoadoutTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>loadouts</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static LoadoutRepository FromLoadedData(LoadedData data) =>
            new LoadoutRepository(TemplateStore<LoadoutTemplate>.Load(
                data,
                "loadouts",
                LoadoutTemplate.FromJson));
    }
}
