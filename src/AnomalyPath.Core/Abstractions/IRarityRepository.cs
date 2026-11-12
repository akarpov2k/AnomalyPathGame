#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Доступ к редкостям: единственный путь к их данным из игрового кода.</summary>
    public interface IRarityRepository
    {
        /// <summary>Все редкости, загруженные при старте, в порядке объявления.</summary>
        IReadOnlyList<RarityTemplate> All { get; }

        /// <summary>Ищет редкость по id.</summary>
        /// <param name="id">Id вида <c>rar_rare</c>.</param>
        /// <param name="template">Найденная запись или <c>null</c>.</param>
        /// <returns><c>true</c>, если редкость найдена.</returns>
        bool TryGet(string id, out RarityTemplate template);

        /// <summary>Возвращает редкость по id.</summary>
        /// <param name="id">Id вида <c>rar_rare</c>.</param>
        /// <returns>Запись редкости.</returns>
        /// <exception cref="Data.DataException">Редкость не найдена.</exception>
        RarityTemplate Get(string id);

        /// <summary>Возвращает редкость по ступени.</summary>
        /// <param name="rarity">Ступень.</param>
        /// <returns>Запись редкости.</returns>
        /// <exception cref="Data.DataException">Для ступени нет записи.</exception>
        RarityTemplate Get(Rarity rarity);
    }
}
