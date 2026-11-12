#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий формул в памяти.</summary>
    public sealed class FormulaRepository : IFormulaRepository
    {
        private readonly TemplateStore<FormulaTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых формул.</summary>
        /// <param name="templates">Формулы.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public FormulaRepository(IEnumerable<FormulaTemplate> templates)
        {
            _store = new TemplateStore<FormulaTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.FormulaNotFound));
        }

        /// <inheritdoc />
        public IReadOnlyList<FormulaTemplate> All => _store.All;

        /// <inheritdoc />
        public FormulaTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>formulas</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static FormulaRepository FromLoadedData(LoadedData data) =>
            new FormulaRepository(TemplateStore<FormulaTemplate>.Load(data, "formulas", FormulaTemplate.FromJson));
    }
}
