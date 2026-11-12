#nullable enable
using System;

namespace AnomalyPath.Core.Quests
{
    /// <summary>Настройка одного слота доски объявлений: сложность и срок обновления.</summary>
    public sealed class QuestBoardSlotTemplate
    {
        /// <summary>Создаёт настройку слота.</summary>
        /// <param name="difficulty">Сложность квестов, которые попадают в слот.</param>
        /// <param name="refreshHours">Через сколько часов слот обновляется после взятия или отказа.</param>
        /// <exception cref="ArgumentException">Сложность не определена.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Срок обновления меньше часа.</exception>
        public QuestBoardSlotTemplate(QuestDifficulty difficulty, int refreshHours)
        {
            if (!Enum.IsDefined(typeof(QuestDifficulty), difficulty))
            {
                throw new ArgumentException(nameof(difficulty));
            }

            if (refreshHours < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshHours));
            }

            Difficulty = difficulty;
            RefreshHours = refreshHours;
        }

        /// <summary>Сложность квестов слота.</summary>
        public QuestDifficulty Difficulty { get; }

        /// <summary>Срок обновления слота в часах мира.</summary>
        public int RefreshHours { get; }
    }
}
