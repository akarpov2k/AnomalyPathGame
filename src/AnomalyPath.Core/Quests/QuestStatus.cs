#nullable enable
namespace AnomalyPath.Core.Quests
{
    /// <summary>Состояние квеста в журнале игрока.</summary>
    public enum QuestStatus
    {
        /// <summary>Квест предложен доской объявлений и ещё не взят.</summary>
        Offered,

        /// <summary>Квест взят и выполняется.</summary>
        Active,

        /// <summary>Квест сдан.</summary>
        Completed,

        /// <summary>Квест отменён игроком.</summary>
        Cancelled,

        /// <summary>Квест провален.</summary>
        Failed
    }
}
