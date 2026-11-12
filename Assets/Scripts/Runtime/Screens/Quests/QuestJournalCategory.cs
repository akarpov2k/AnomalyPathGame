#nullable enable
namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>Категория журнала заданий: переключатели списка над ним.</summary>
    public enum QuestJournalCategory
    {
        /// <summary>Сюжетные задания основной линии.</summary>
        Story,

        /// <summary>Побочные задания: сюжетные квесты других линий.</summary>
        Side,

        /// <summary>Повторяемые задания, взятые на доске объявлений.</summary>
        Repeatable
    }
}
