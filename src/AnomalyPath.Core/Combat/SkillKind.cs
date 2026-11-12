#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Назначение скилла: атака, защита или полезное действие.</summary>
    public enum SkillKind
    {
        /// <summary>Атака по противнику.</summary>
        Attack,

        /// <summary>Защитное действие.</summary>
        Defense,

        /// <summary>Полезное действие без атаки.</summary>
        Utility
    }
}
