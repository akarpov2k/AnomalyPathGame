#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Combat;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Доступ к скиллам из данных.</summary>
    public interface ISkillRepository
    {
        /// <summary>Все скиллы.</summary>
        IReadOnlyList<SkillTemplate> All { get; }

        /// <summary>Проверяет, есть ли скилл с таким id.</summary>
        /// <param name="id">Идентификатор скилла.</param>
        /// <returns><c>true</c>, если скилл есть.</returns>
        bool Contains(string id);

        /// <summary>Возвращает скилл по id.</summary>
        /// <param name="id">Идентификатор скилла.</param>
        /// <returns>Скилл.</returns>
        /// <exception cref="AnomalyPath.Core.Data.DataException">Скилл не найден.</exception>
        SkillTemplate Get(string id);

        /// <summary>Пробует найти скилл по id.</summary>
        /// <param name="id">Идентификатор скилла.</param>
        /// <param name="skill">Найденный скилл или <c>null</c>.</param>
        /// <returns><c>true</c>, если скилл есть.</returns>
        bool TryGet(string id, out SkillTemplate? skill);
    }
}
