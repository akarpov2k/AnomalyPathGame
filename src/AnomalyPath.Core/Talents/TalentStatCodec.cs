#nullable enable
using System;

namespace AnomalyPath.Core.Talents
{
    /// <summary>Код характеристики таланта в JSON: читаемый camelCase, а не имя члена перечисления.</summary>
    public static class TalentStatCodec
    {
        /// <summary>Возвращает код характеристики для JSON.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <returns>Код.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Характеристика неизвестна.</exception>
        public static string ToCode(TalentStat stat) => stat switch
        {
            TalentStat.ApMax => "apMax",
            TalentStat.HealthMax => "healthMax",
            TalentStat.Accuracy => "accuracy",
            TalentStat.StaminaMax => "staminaMax",
            TalentStat.StaminaRecovery => "staminaRecovery",
            TalentStat.MaxFood => "maxFood",
            TalentStat.MaxWater => "maxWater",
            _ => throw new ArgumentOutOfRangeException(nameof(stat))
        };

        /// <summary>Разбирает код характеристики.</summary>
        /// <param name="code">Код из JSON.</param>
        /// <param name="stat">Разобранная характеристика.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string? code, out TalentStat stat)
        {
            switch (code)
            {
                case "apMax":
                    stat = TalentStat.ApMax;
                    return true;
                case "healthMax":
                    stat = TalentStat.HealthMax;
                    return true;
                case "accuracy":
                    stat = TalentStat.Accuracy;
                    return true;
                case "staminaMax":
                    stat = TalentStat.StaminaMax;
                    return true;
                case "staminaRecovery":
                    stat = TalentStat.StaminaRecovery;
                    return true;
                case "maxFood":
                    stat = TalentStat.MaxFood;
                    return true;
                case "maxWater":
                    stat = TalentStat.MaxWater;
                    return true;
                default:
                    stat = default;
                    return false;
            }
        }

        /// <summary>Проверяет, что характеристика измеряется целыми единицами.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <returns><c>true</c>, если дробное значение недопустимо.</returns>
        public static bool IsInteger(TalentStat stat) => stat != TalentStat.Accuracy;
    }
}
