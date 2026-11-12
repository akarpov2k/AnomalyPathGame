#nullable enable
namespace AnomalyPath.Core.Quests
{
    /// <summary>Условие провала повторяемого квеста.</summary>
    public enum QuestFailCondition
    {
        /// <summary>Квест проваливается смертью игрока: один заход на жизнь.</summary>
        Death,

        /// <summary>Квест проваливается по истечении срока с момента взятия.</summary>
        Time
    }
}
