#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Шаг плана противника с ожидаемым результатом; применяется боевым сервисом.</summary>
    public readonly struct EnemyTurnStep
    {
        /// <summary>Создаёт шаг плана.</summary>
        /// <param name="kind">Вид действия.</param>
        /// <param name="skillId">Скилл; <c>null</c> у перезарядки и аптечки.</param>
        /// <param name="apCost">Цена в ОД.</param>
        /// <param name="ammoCost">Расход патронов.</param>
        /// <param name="hitChance">Шанс первого выстрела; 0 у перезарядки и аптечки.</param>
        /// <param name="expectedDamage">Ожидаемый урон; 0 у перезарядки и аптечки.</param>
        /// <param name="killChance">Вероятность добить цель; 0 у перезарядки и аптечки.</param>
        public EnemyTurnStep(
            EnemyTurnActionKind kind,
            string? skillId,
            int apCost,
            int ammoCost,
            double hitChance,
            double expectedDamage,
            double killChance)
        {
            Kind = kind;
            SkillId = skillId;
            ApCost = apCost;
            AmmoCost = ammoCost;
            HitChance = hitChance;
            ExpectedDamage = expectedDamage;
            KillChance = killChance;
        }

        /// <summary>Вид действия.</summary>
        public EnemyTurnActionKind Kind { get; }

        /// <summary>Скилл; <c>null</c> у перезарядки и аптечки.</summary>
        public string? SkillId { get; }

        /// <summary>Цена в ОД.</summary>
        public int ApCost { get; }

        /// <summary>Расход патронов.</summary>
        public int AmmoCost { get; }

        /// <summary>Шанс первого выстрела; 0 у перезарядки и аптечки.</summary>
        public double HitChance { get; }

        /// <summary>Ожидаемый урон; 0 у перезарядки и аптечки.</summary>
        public double ExpectedDamage { get; }

        /// <summary>Вероятность добить цель; 0 у перезарядки и аптечки.</summary>
        public double KillChance { get; }
    }
}
