#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests.Story;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Доступ к квестовым цепочкам локаций.</summary>
    public interface IQuestChainRepository
    {
        /// <summary>Все цепочки, загруженные при старте.</summary>
        IReadOnlyList<QuestChainTemplate> All { get; }

        /// <summary>Ищет цепочку по id.</summary>
        /// <param name="id">Идентификатор цепочки.</param>
        /// <param name="chain">Найденная цепочка или <c>null</c>.</param>
        /// <returns><c>true</c>, если цепочка найдена.</returns>
        bool TryGet(string id, out QuestChainTemplate chain);

        /// <summary>Проверяет, существует ли цепочка.</summary>
        /// <param name="id">Идентификатор цепочки.</param>
        /// <returns><c>true</c>, если цепочка существует.</returns>
        bool Contains(string id);

        /// <summary>Возвращает цепочку по id.</summary>
        /// <param name="id">Идентификатор цепочки.</param>
        /// <returns>Цепочка.</returns>
        /// <exception cref="Data.DataException">Цепочка не найдена.</exception>
        QuestChainTemplate Get(string id);
    }
}
