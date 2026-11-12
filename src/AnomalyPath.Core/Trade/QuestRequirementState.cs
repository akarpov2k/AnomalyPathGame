#nullable enable
namespace AnomalyPath.Core.Trade
{
    /// <summary>Состояние квеста, которое может требоваться в условиях диалога.</summary>
    public enum QuestRequirementState
    {
        /// <summary>Квест ещё не взят и не сдан.</summary>
        Available,

        /// <summary>Квест взят и выполняется.</summary>
        Active,

        /// <summary>Все цели квеста выполнены, можно сдавать.</summary>
        Ready,

        /// <summary>Квест сдан.</summary>
        Completed
    }
}
