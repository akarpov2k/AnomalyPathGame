#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>Действие, доступное в бою: скилл оружия.</summary>
    public sealed class CombatAction
    {
        /// <summary>Создаёт описание действия.</summary>
        /// <param name="kind">Вид действия.</param>
        /// <param name="skillId">Идентификатор скилла.</param>
        /// <param name="apCost">Цена в ОД.</param>
        /// <param name="ammoCost">Цена в патронах.</param>
        /// <param name="availability">Доступность и причина отказа.</param>
        public CombatAction(
            CombatActionKind kind,
            string? skillId,
            int apCost,
            int ammoCost,
            CombatActionAvailability availability)
        {
            Kind = kind;
            SkillId = skillId;
            ApCost = apCost;
            AmmoCost = ammoCost;
            Availability = availability;
        }

        /// <summary>Вид действия.</summary>
        public CombatActionKind Kind { get; }

        /// <summary>Идентификатор скилла.</summary>
        public string? SkillId { get; }

        /// <summary>Цена в ОД.</summary>
        public int ApCost { get; }

        /// <summary>Цена в патронах.</summary>
        public int AmmoCost { get; }

        /// <summary>Доступность и причина отказа.</summary>
        public CombatActionAvailability Availability { get; }

        /// <summary><c>true</c>, если действие можно выполнить сейчас.</summary>
        public bool IsAvailable => Availability == CombatActionAvailability.Available;
    }
}
