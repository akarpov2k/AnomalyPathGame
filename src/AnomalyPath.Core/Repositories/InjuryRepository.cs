#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий травм в памяти: строится один раз при загрузке данных.</summary>
    public sealed class InjuryRepository : IInjuryRepository
    {
        private readonly TemplateStore<InjuryTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых записей.</summary>
        /// <param name="templates">Записи травм.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public InjuryRepository(IEnumerable<InjuryTemplate> templates)
        {
            _store = new TemplateStore<InjuryTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.InjuryNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<InjuryTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out InjuryTemplate template) => _store.TryGet(id, out template!);

        /// <inheritdoc />
        public InjuryTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>injuries</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Сущность не соответствует схеме травмы.</exception>
        public static InjuryRepository FromLoadedData(LoadedData data) =>
            new InjuryRepository(TemplateStore<InjuryTemplate>.Load(data, "injuries", InjuryTemplate.FromJson));
    }
}
