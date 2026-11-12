#nullable enable
using System;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Trade
{
    /// <summary>Условие «квест взят в указанном варианте».</summary>
    public sealed class RequirementQuestVariant
    {
        /// <summary>Создаёт условие варианта квеста.</summary>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="variantId">Идентификатор варианта.</param>
        /// <exception cref="ArgumentException">Обязательная строка пуста.</exception>
        public RequirementQuestVariant(string questId, string variantId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(questId)),
                    nameof(questId));
            }

            if (string.IsNullOrWhiteSpace(variantId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(variantId)),
                    nameof(variantId));
            }

            QuestId = questId;
            VariantId = variantId;
        }

        /// <summary>Идентификатор квеста.</summary>
        public string QuestId { get; }

        /// <summary>Идентификатор варианта.</summary>
        public string VariantId { get; }
    }
}
