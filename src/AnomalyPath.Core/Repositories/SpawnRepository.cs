#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Записи спавна объектов локаций из загруженных данных.</summary>
    public sealed class SpawnRepository : ISpawnRepository
    {
        private readonly TemplateStore<SpawnEntry> _store;

        /// <summary>Создаёт репозиторий спавна.</summary>
        /// <param name="entries">Записи спавна.</param>
        /// <exception cref="DataException">Дублирующийся id записи.</exception>
        public SpawnRepository(IEnumerable<SpawnEntry> entries)
        {
            _store = new TemplateStore<SpawnEntry>(
                entries,
                entry => entry.Id,
                id => new DataException("spawns", CoreErrors.DuplicateEntity, id),
                id => new DataException(id, CoreErrors.SpawnNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<SpawnEntry> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out SpawnEntry? entry) => _store.TryGet(id, out entry);

        /// <inheritdoc />
        public SpawnEntry Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из загруженных данных.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Запись вида <c>spawns</c> разобрана с ошибкой.</exception>
        public static SpawnRepository FromLoadedData(LoadedData data) =>
            new SpawnRepository(TemplateStore<SpawnEntry>.Load(data, "spawns", SpawnEntry.FromJson));
    }
}
