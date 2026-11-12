#nullable enable
using System;

namespace AnomalyPath.Core.Templates
{
    /// <summary>Кодек типа эффекта расходника: код в JSON — camelCase, а не имя члена перечисления.</summary>
    public static class ConsumableEffectCodec
    {
        /// <summary>Возвращает код типа эффекта для JSON.</summary>
        /// <param name="type">Тип эффекта.</param>
        /// <returns>Код.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Тип неизвестен.</exception>
        public static string ToCode(ConsumableEffectType type) => type switch
        {
            ConsumableEffectType.Heal => "heal",
            ConsumableEffectType.RestoreAp => "restoreAp",
            ConsumableEffectType.RestoreFood => "restoreFood",
            ConsumableEffectType.RestoreWater => "restoreWater",
            ConsumableEffectType.Modifier => "modifier",
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        /// <summary>Разбирает код типа эффекта.</summary>
        /// <param name="code">Код из JSON.</param>
        /// <param name="type">Разобранный тип.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string? code, out ConsumableEffectType type)
        {
            switch (code)
            {
                case "heal":
                    type = ConsumableEffectType.Heal;
                    return true;
                case "restoreAp":
                    type = ConsumableEffectType.RestoreAp;
                    return true;
                case "restoreFood":
                    type = ConsumableEffectType.RestoreFood;
                    return true;
                case "restoreWater":
                    type = ConsumableEffectType.RestoreWater;
                    return true;
                case "modifier":
                    type = ConsumableEffectType.Modifier;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }
    }
}
