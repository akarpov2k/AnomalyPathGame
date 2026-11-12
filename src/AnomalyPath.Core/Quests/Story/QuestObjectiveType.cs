#nullable enable
namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Тип цели сюжетного квеста; код в JSON задаёт <see cref="QuestObjectiveTypeCodec"/>.</summary>
    public enum QuestObjectiveType
    {
        /// <summary>Зачистить N локаций.</summary>
        ClearLocations,

        /// <summary>Убить N конкретных актёров.</summary>
        KillActors,

        /// <summary>Осмотреть N объектов локации.</summary>
        InspectObjects,

        /// <summary>Вскрыть N тайников.</summary>
        OpenStashes,

        /// <summary>Зачистить цепочку локаций.</summary>
        ClearChain,

        /// <summary>Дойти до локации.</summary>
        ReachLocation,

        /// <summary>Поговорить с персонажем.</summary>
        TalkToActor
    }
}
