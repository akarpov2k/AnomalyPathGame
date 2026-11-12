#nullable enable
namespace AnomalyPath.Core.Quests
{
    /// <summary>Сложность квеста и тип слота доски объявлений.</summary>
    public enum QuestDifficulty
    {
        /// <summary>Простой квест: слот обновляется через 24 часа.</summary>
        Easy,

        /// <summary>Средний квест: слот обновляется через 48 часов.</summary>
        Medium,

        /// <summary>Сложный и высокооплачиваемый квест: слот обновляется через 72 часа.</summary>
        Hard
    }
}
