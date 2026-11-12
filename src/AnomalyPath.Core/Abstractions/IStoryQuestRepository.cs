#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Quests.Story;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Доступ к шаблонам сюжетных квестов; единственный путь к ним из игрового кода.</summary>
    public interface IStoryQuestRepository
    {
        /// <summary>Все сюжетные квесты, загруженные при старте.</summary>
        IReadOnlyList<StoryQuestTemplate> All { get; }

        /// <summary>Ищет сюжетный квест по id.</summary>
        /// <param name="id">Идентификатор квеста.</param>
        /// <param name="template">Найденный шаблон или <c>null</c>.</param>
        /// <returns><c>true</c>, если квест найден.</returns>
        bool TryGet(string id, out StoryQuestTemplate template);

        /// <summary>Проверяет, существует ли сюжетный квест.</summary>
        /// <param name="id">Идентификатор квеста.</param>
        /// <returns><c>true</c>, если квест существует.</returns>
        bool Contains(string id);

        /// <summary>Возвращает сюжетный квест по id.</summary>
        /// <param name="id">Идентификатор квеста.</param>
        /// <returns>Шаблон квеста.</returns>
        /// <exception cref="Data.DataException">Квест не найден.</exception>
        StoryQuestTemplate Get(string id);
    }
}
