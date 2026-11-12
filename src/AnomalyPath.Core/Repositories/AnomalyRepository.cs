#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Аномалии из загруженных данных.</summary>
    public sealed class AnomalyRepository : IAnomalyRepository
    {
        private readonly TemplateStore<AnomalyTemplate> _store;

        /// <summary>Создаёт репозиторий аномалий.</summary>
        /// <param name="templates">Аномалии.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public AnomalyRepository(IEnumerable<AnomalyTemplate> templates)
        {
            _store = new TemplateStore<AnomalyTemplate>(
                templates,
                template => template.Id,
                id => new DataException("anomalies", CoreErrors.DuplicateEntity, id),
                id => new DataException(id, CoreErrors.AnomalyNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<AnomalyTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out AnomalyTemplate? template) => _store.TryGet(id, out template);

        /// <inheritdoc />
        public AnomalyTemplate Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из загруженных данных.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Запись вида <c>anomalies</c> разобрана с ошибкой.</exception>
        public static AnomalyRepository FromLoadedData(LoadedData data) =>
            new AnomalyRepository(TemplateStore<AnomalyTemplate>.Load(data, "anomalies", AnomalyTemplate.FromJson));
    }
}
