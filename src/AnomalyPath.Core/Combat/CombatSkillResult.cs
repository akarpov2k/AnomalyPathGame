#nullable enable
using System.Collections.Generic;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Результат применения скилла: все выстрелы, урон и смерть цели.</summary>
    public sealed class CombatSkillResult
    {
        /// <summary>Создаёт результат применения скилла.</summary>
        /// <param name="executed">Скилл выполнен.</param>
        /// <param name="availability">Доступность: <see cref="CombatActionAvailability.Available"/> при успехе.</param>
        /// <param name="hits">Отдельные выстрелы в порядке выполнения.</param>
        /// <param name="totalDamage">Суммарный урон.</param>
        /// <param name="killed">Цель убита.</param>
        /// <param name="apSpent">Потрачено ОД.</param>
        /// <param name="ammoSpent">Потрачено патронов.</param>
        /// <param name="injuries">Травмы, нанесённые за применение, в порядке попаданий; <c>null</c> — ни одной.</param>
        /// <exception cref="System.ArgumentNullException">Список выстрелов не передан.</exception>
        public CombatSkillResult(
            bool executed,
            CombatActionAvailability availability,
            IReadOnlyList<CombatAttackResult> hits,
            int totalDamage,
            bool killed,
            int apSpent,
            int ammoSpent,
            IReadOnlyList<string>? injuries = null)
        {
            Executed = executed;
            Availability = availability;
            Hits = hits ?? throw new System.ArgumentNullException(nameof(hits));
            TotalDamage = totalDamage;
            Killed = killed;
            ApSpent = apSpent;
            AmmoSpent = ammoSpent;
            Injuries = injuries ?? System.Array.Empty<string>();
        }

        /// <summary><c>true</c>, если скилл выполнен.</summary>
        public bool Executed { get; }

        /// <summary>Доступность: <see cref="CombatActionAvailability.Available"/> при успехе.</summary>
        public CombatActionAvailability Availability { get; }

        /// <summary>Отдельные выстрелы в порядке выполнения.</summary>
        public IReadOnlyList<CombatAttackResult> Hits { get; }

        /// <summary>Суммарный урон.</summary>
        public int TotalDamage { get; }

        /// <summary>Цель убита.</summary>
        public bool Killed { get; }

        /// <summary>Потрачено ОД.</summary>
        public int ApSpent { get; }

        /// <summary>Потрачено патронов.</summary>
        public int AmmoSpent { get; }

        /// <summary>Травмы, нанесённые за применение, в порядке попаданий.</summary>
        public IReadOnlyList<string> Injuries { get; }

        /// <summary>Пустой результат с причиной отказа.</summary>
        /// <param name="availability">Почему скилл не выполнен.</param>
        /// <returns>Результат без выстрелов.</returns>
        public static CombatSkillResult Failed(CombatActionAvailability availability) =>
            new CombatSkillResult(
                false,
                availability,
                System.Array.Empty<CombatAttackResult>(),
                0,
                false,
                0,
                0);
    }
}
