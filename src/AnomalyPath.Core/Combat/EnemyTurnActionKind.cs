#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Вид действия в плане противника.</summary>
    public enum EnemyTurnActionKind
    {
        /// <summary>Скилл оружия или скилл мутанта.</summary>
        Skill,

        /// <summary>Перезарядка из бесконечного запаса противника.</summary>
        Reload,

        /// <summary>Применение аптечки противника по порогу здоровья.</summary>
        Medkit
    }
}
