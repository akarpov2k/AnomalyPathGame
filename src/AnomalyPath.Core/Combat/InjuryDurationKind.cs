#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Как истекает статус: ходами боя, часами мира или только лечением.</summary>
    public enum InjuryDurationKind
    {
        /// <summary>Часы мирового времени: статус тикает при переходах и возврате в хаб.</summary>
        Hours = 0,

        /// <summary>До лечения: сама не истекает, снимается только лечением.</summary>
        UntilHealed = 1,

        /// <summary>Ходы боя: тикает в конце хода владельца статуса.</summary>
        Turns = 2
    }
}
