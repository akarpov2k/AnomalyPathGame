#nullable enable
namespace AnomalyPath.Core.Artifacts
{
    /// <summary>Характеристика, которую меняет надетый артефакт.</summary>
    public enum ArtifactStat
    {
        /// <summary>Максимум здоровья персонажа.</summary>
        HealthMax,

        /// <summary>Меткость персонажа.</summary>
        Accuracy,

        /// <summary>Максимум выносливости.</summary>
        Stamina,

        /// <summary>Защита от одного типа урона; требует <c>damageTypeId</c>.</summary>
        Protection
    }
}
