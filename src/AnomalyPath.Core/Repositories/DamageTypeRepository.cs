#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий типов урона в памяти: строится один раз при загрузке данных.</summary>
    public sealed class DamageTypeRepository : IDamageTypeRepository
    {
        private readonly TemplateStore<DamageTypeTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых типов урона.</summary>
        /// <param name="templates">Типы урона.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public DamageTypeRepository(IEnumerable<DamageTypeTemplate> templates)
        {
            _store = new TemplateStore<DamageTypeTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.ItemNotFound));
        }

        /// <inheritdoc />
        public IReadOnlyList<DamageTypeTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out DamageTypeTemplate template) => _store.TryGet(id, out template!);

        /// <inheritdoc />
        public DamageTypeTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>damageTypes</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Сущность не соответствует схеме типа урона.</exception>
        public static DamageTypeRepository FromLoadedData(LoadedData data) =>
            new DamageTypeRepository(TemplateStore<DamageTypeTemplate>.Load(
                data,
                "damageTypes",
                DamageTypeTemplate.FromJson));
    }
}
