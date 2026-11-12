#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Выполненное действие хода противника: шаг плана и фактический исход.</summary>
    public sealed class EnemyTurnAction
    {
        /// <summary>Создаёт выполненное действие.</summary>
        /// <param name="step">Шаг плана.</param>
        /// <param name="damage">Фактический урон; 0 у шага, перезарядки и аптечки.</param>
        /// <param name="killed"><c>true</c>, если действие убило игрока.</param>
        /// <param name="hit"><c>true</c>, если скилл попал; у остальных действий — <c>false</c>.</param>
        /// <param name="healed">Фактически вылеченное здоровье противника; 0 у остальных действий.</param>
        public EnemyTurnAction(EnemyTurnStep step, int damage, bool killed, bool hit, int healed = 0)
        {
            Step = step;
            Damage = damage;
            Killed = killed;
            Hit = hit;
            Healed = healed;
        }

        /// <summary>Шаг плана с ожидаемыми значениями.</summary>
        public EnemyTurnStep Step { get; }

        /// <summary>Фактический урон; 0 у шага, перезарядки и аптечки.</summary>
        public int Damage { get; }

        /// <summary><c>true</c>, если действие убило игрока.</summary>
        public bool Killed { get; }

        /// <summary><c>true</c>, если скилл попал; у остальных действий — <c>false</c>.</summary>
        public bool Hit { get; }

        /// <summary>Сколько здоровья противник вылечил аптечкой; 0 у остальных действий.</summary>
        public int Healed { get; }
    }
}
