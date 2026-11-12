#nullable enable
namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Область прогресса сюжетного квеста: когда счётчики целей сбрасываются.</summary>
    public enum QuestProgressScope
    {
        /// <summary>Сбрасывается при возврате в хаб и при смерти.</summary>
        Raid,

        /// <summary>Сбрасывается только при смерти.</summary>
        Life,

        /// <summary>Не сбрасывается.</summary>
        Cumulative
    }
}
