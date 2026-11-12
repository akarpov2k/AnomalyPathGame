#nullable enable
using System;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>
    /// Соглашение «характеристика артефакта ↔ код в JSON»: коды записаны в camelCase и
    /// не совпадают с именами членов перечисления, поэтому переводятся явно.
    /// </summary>
    public static class ArtifactStatCodec
    {
        /// <summary>Код характеристики в данных.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <returns>Код в JSON.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Характеристика неизвестна кодеку.</exception>
        public static string ToCode(ArtifactStat stat)
        {
            switch (stat)
            {
                case ArtifactStat.HealthMax: return "healthMax";
                case ArtifactStat.Accuracy: return "accuracy";
                case ArtifactStat.Stamina: return "stamina";
                case ArtifactStat.Protection: return "protection";
                default: throw new ArgumentOutOfRangeException(nameof(stat));
            }
        }

        /// <summary>Разбирает код характеристики из данных.</summary>
        /// <param name="code">Код в JSON.</param>
        /// <param name="stat">Разобранная характеристика.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string code, out ArtifactStat stat)
        {
            switch (code)
            {
                case "healthMax":
                    stat = ArtifactStat.HealthMax;
                    return true;
                case "accuracy":
                    stat = ArtifactStat.Accuracy;
                    return true;
                case "stamina":
                    stat = ArtifactStat.Stamina;
                    return true;
                case "protection":
                    stat = ArtifactStat.Protection;
                    return true;
                default:
                    stat = default;
                    return false;
            }
        }
    }
}
