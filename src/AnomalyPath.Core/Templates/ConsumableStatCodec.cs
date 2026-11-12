#nullable enable
using System;

namespace AnomalyPath.Core.Templates
{
    /// <summary>Кодек характеристики эффекта-модификатора расходника: код в JSON — camelCase.</summary>
    public static class ConsumableStatCodec
    {
        /// <summary>Возвращает код характеристики для JSON.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <returns>Код.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Характеристика неизвестна.</exception>
        public static string ToCode(ConsumableStat stat) => stat switch
        {
            ConsumableStat.Accuracy => "accuracy",
            ConsumableStat.Protection => "protection",
            ConsumableStat.ActionApCost => "actionApCost",
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };

        /// <summary>Разбирает код характеристики.</summary>
        /// <param name="code">Код из JSON.</param>
        /// <param name="stat">Разобранная характеристика.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string? code, out ConsumableStat stat)
        {
            switch (code)
            {
                case "accuracy":
                    stat = ConsumableStat.Accuracy;
                    return true;
                case "protection":
                    stat = ConsumableStat.Protection;
                    return true;
                case "actionApCost":
                    stat = ConsumableStat.ActionApCost;
                    return true;
                default:
                    stat = default;
                    return false;
            }
        }

        /// <summary>Проверяет, измеряется ли характеристика целыми единицами.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <returns><c>true</c>, если дробное значение недопустимо.</returns>
        public static bool IsInteger(ConsumableStat stat) => stat != ConsumableStat.Accuracy;
    }
}
