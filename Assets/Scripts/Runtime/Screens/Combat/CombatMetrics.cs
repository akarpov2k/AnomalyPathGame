#nullable enable
namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Числа поведения боевого экрана, которые не задаются разметкой префаба:
    /// сколько строк журнала держать и с какой паузой показывать действия противника.
    /// Размеры и отступы живут в префабе.
    /// </summary>
    public static class CombatMetrics
    {
        /// <summary>Сколько строк журнала хранится и показывается.</summary>
        public const int LogLines = 12;

        /// <summary>Пауза между действиями противника, чтобы каждое было видно в журнале.</summary>
        public const float EnemyActionPause = 0.75f;
    }
}
