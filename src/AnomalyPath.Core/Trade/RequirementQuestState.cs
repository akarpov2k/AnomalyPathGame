#nullable enable
using System;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Trade
{
    /// <summary>Условие «квест находится в указанном состоянии».</summary>
    public sealed class RequirementQuestState
    {
        /// <summary>Создаёт условие состояния квеста.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="state">Требуемое состояние.</param>
        /// <exception cref="ArgumentException">Идентификатор пуст.</exception>
        public RequirementQuestState(string questId, QuestRequirementState state)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(questId)),
                    nameof(questId));
            }

            QuestId = questId;
            State = state;
        }

        /// <summary>Идентификатор квеста.</summary>
        public string QuestId { get; }

        /// <summary>Требуемое состояние.</summary>
        public QuestRequirementState State { get; }
    }
}
