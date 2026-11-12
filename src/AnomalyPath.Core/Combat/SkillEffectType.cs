#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Тип эффекта скилла.</summary>
    public enum SkillEffectType
    {
        /// <summary>Урон по цели.</summary>
        Damage,

        /// <summary>Наложить статус на себя или противника.</summary>
        Status,

        /// <summary>Снять статус по id.</summary>
        RemoveStatus
    }
}
