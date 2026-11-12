#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Anomalies;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Аномалии из данных: поиск по id и полный список.</summary>
    public interface IAnomalyRepository
    {
        /// <summary>Все аномалии в порядке данных.</summary>
        IReadOnlyList<AnomalyTemplate> All { get; }

        /// <summary>Ищет аномалию по id.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="template">Найденная аномалия или <c>null</c>.</param>
        /// <returns><c>true</c>, если аномалия найдена.</returns>
        bool TryGet(string id, out AnomalyTemplate? template);

        /// <summary>Возвращает аномалию по id.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <returns>Аномалия.</returns>
        /// <exception cref="AnomalyPath.Core.Data.DataException">Аномалия не найдена.</exception>
        AnomalyTemplate Get(string id);
    }
}
