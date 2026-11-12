#nullable enable
namespace AnomalyPath.Core.Talents
{
    /// <summary>Характеристика, на которую влияет талант.</summary>
    public enum TalentStat
    {
        /// <summary>Максимум очков действия.</summary>
        ApMax,

        /// <summary>Максимум здоровья.</summary>
        HealthMax,

        /// <summary>Меткость.</summary>
        Accuracy,

        /// <summary>Максимум выносливости.</summary>
        StaminaMax,

        /// <summary>Сколько выносливости восстанавливает час отдыха.</summary>
        StaminaRecovery,

        /// <summary>Максимум шкалы еды.</summary>
        MaxFood,

        /// <summary>Максимум шкалы воды.</summary>
        MaxWater
    }
}
