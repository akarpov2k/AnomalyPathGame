#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Один эффект скилла: урон, наложение статуса или снятие статуса.</summary>
    public sealed class SkillEffect
    {
        private SkillEffect(
            SkillEffectType type,
            double multiplier,
            int hits,
            string? damageTypeId,
            string? statusId,
            SkillEffectTarget target,
            double accuracyBonus,
            int actionApCostBonus,
            double damageMultiplier,
            int turns)
        {
            Type = type;
            Multiplier = multiplier;
            Hits = hits;
            DamageTypeId = damageTypeId;
            StatusId = statusId;
            Target = target;
            AccuracyBonus = accuracyBonus;
            ActionApCostBonus = actionApCostBonus;
            DamageMultiplier = damageMultiplier;
            Turns = turns;
        }

        /// <summary>Тип эффекта.</summary>
        public SkillEffectType Type { get; }

        /// <summary>Множитель урона оружия для эффекта <see cref="SkillEffectType.Damage"/>.</summary>
        public double Multiplier { get; }

        /// <summary>Число отдельных выстрелов для эффекта <see cref="SkillEffectType.Damage"/>.</summary>
        public int Hits { get; }

        /// <summary>Тип урона; <c>null</c> — тип урона оружия.</summary>
        public string? DamageTypeId { get; }

        /// <summary>Id статуса для эффектов <see cref="SkillEffectType.Status"/> и <see cref="SkillEffectType.RemoveStatus"/>.</summary>
        public string? StatusId { get; }

        /// <summary>На кого направлен эффект статуса.</summary>
        public SkillEffectTarget Target { get; }

        /// <summary>Прибавка к меткости в статусе.</summary>
        public double AccuracyBonus { get; }

        /// <summary>Прибавка к цене действия в ОД в статусе.</summary>
        public int ActionApCostBonus { get; }

        /// <summary>Доля прибавки к урону в статусе; 0 — без прибавки.</summary>
        public double DamageMultiplier { get; }

        /// <summary>Сколько ходов действует статус.</summary>
        public int Turns { get; }

        /// <summary>Создаёт эффект урона.</summary>
        /// <param name="multiplier">Множитель урона оружия.</param>
        /// <param name="hits">Число отдельных выстрелов.</param>
        /// <param name="damageTypeId">Тип урона или <c>null</c> для типа оружия.</param>
        /// <returns>Эффект урона.</returns>
        public static SkillEffect Damage(double multiplier, int hits, string? damageTypeId) =>
            new SkillEffect(
                SkillEffectType.Damage,
                multiplier,
                hits,
                damageTypeId,
                null,
                SkillEffectTarget.Enemy,
                0,
                0,
                0,
                0);

        /// <summary>Создаёт эффект статуса.</summary>
        /// <param name="statusId">Id статуса.</param>
        /// <param name="target">На кого направлен статус.</param>
        /// <param name="accuracyBonus">Прибавка к меткости.</param>
        /// <param name="actionApCostBonus">Прибавка к цене действия в ОД.</param>
        /// <param name="damageMultiplier">Доля прибавки к урону.</param>
        /// <param name="turns">Сколько ходов действует.</param>
        /// <returns>Эффект статуса.</returns>
        public static SkillEffect Status(
            string statusId,
            SkillEffectTarget target,
            double accuracyBonus,
            int actionApCostBonus,
            double damageMultiplier,
            int turns) =>
            new SkillEffect(
                SkillEffectType.Status,
                0,
                0,
                null,
                statusId,
                target,
                accuracyBonus,
                actionApCostBonus,
                damageMultiplier,
                turns);

        /// <summary>Создаёт эффект снятия статуса.</summary>
        /// <param name="statusId">Id статуса.</param>
        /// <param name="target">У кого снять статус.</param>
        /// <returns>Эффект снятия.</returns>
        public static SkillEffect RemoveStatus(string statusId, SkillEffectTarget target) =>
            new SkillEffect(
                SkillEffectType.RemoveStatus,
                0,
                0,
                null,
                statusId,
                target,
                0,
                0,
                0,
                0);
    }
}
