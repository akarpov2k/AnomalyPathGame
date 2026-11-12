#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Доступ к травмам: единственный путь к их данным из игрового кода.</summary>
    public interface IInjuryRepository
    {
        /// <summary>Все травмы, загруженные при старте, в порядке объявления.</summary>
        IReadOnlyList<InjuryTemplate> All { get; }

        /// <summary>Ищет травму по id.</summary>
        /// <param name="id">Id вида <c>injury_broken_leg</c>.</param>
        /// <param name="template">Найденная запись или <c>null</c>.</param>
        /// <returns><c>true</c>, если травма найдена.</returns>
        bool TryGet(string id, out InjuryTemplate template);

        /// <summary>Возвращает травму по id.</summary>
        /// <param name="id">Id вида <c>injury_broken_leg</c>.</param>
        /// <returns>Запись травмы.</returns>
        /// <exception cref="Data.DataException">Травма не найдена.</exception>
        InjuryTemplate Get(string id);

        /// <summary>Проверяет наличие травмы.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <returns><c>true</c>, если травма есть.</returns>
        bool Contains(string id);
    }
}
