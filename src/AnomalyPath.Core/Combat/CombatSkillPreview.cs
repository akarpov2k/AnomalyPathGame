#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Предпросмотр скилла без бросков: доступность, цена и ожидаемый результат.
    /// Считается тем же кодом, что и исполнение, поэтому ИИ и интерфейс не дублируют формулы.
    /// </summary>
    public readonly struct CombatSkillPreview
    {
        /// <summary>Создаёт предпросмотр скилла.</summary>
        /// <param name="available">Доступен ли скилл сейчас.</param>
        /// <param name="availability">Причина недоступности или <see cref="CombatActionAvailability.Available"/>.</param>
        /// <param name="apCost">Цена вызова в ОД.</param>
        /// <param name="ammoCost">Расход патронов.</param>
        /// <param name="hits">Сколько отдельных выстрелов нанесёт скилл.</param>
        /// <param name="hitChance">Шанс первого выстрела 0..1 с текущим «пожалей»-бонусом.</param>
        /// <param name="expectedDamage">Ожидаемый урон с обрезкой остатком здоровья цели.</param>
        /// <param name="killChance">Вероятность, что цель погибнет.</param>
        public CombatSkillPreview(
            bool available,
            CombatActionAvailability availability,
            int apCost,
            int ammoCost,
            int hits,
            double hitChance,
            double expectedDamage,
            double killChance)
        {
            Available = available;
            Availability = availability;
            ApCost = apCost;
            AmmoCost = ammoCost;
            Hits = hits;
            HitChance = hitChance;
            ExpectedDamage = expectedDamage;
            KillChance = killChance;
        }

        /// <summary><c>true</c>, если скилл сейчас доступен.</summary>
        public bool Available { get; }

        /// <summary>Причина недоступности или <see cref="CombatActionAvailability.Available"/>.</summary>
        public CombatActionAvailability Availability { get; }

        /// <summary>Цена вызова в ОД.</summary>
        public int ApCost { get; }

        /// <summary>Расход патронов.</summary>
        public int AmmoCost { get; }

        /// <summary>Сколько отдельных выстрелов нанесёт скилл.</summary>
        public int Hits { get; }

        /// <summary>Шанс первого выстрела 0..1 с текущим «пожалей»-бонусом.</summary>
        public double HitChance { get; }

        /// <summary>Ожидаемый урон с обрезкой остатком здоровья цели.</summary>
        public double ExpectedDamage { get; }

        /// <summary>Вероятность, что цель погибнет.</summary>
        public double KillChance { get; }

        /// <summary>Создаёт недоступный предпросмотр.</summary>
        /// <param name="availability">Причина недоступности.</param>
        /// <param name="apCost">Цена вызова в ОД, если известна.</param>
        /// <param name="ammoCost">Расход патронов, если известен.</param>
        /// <returns>Предпросмотр с нулевым результатом.</returns>
        public static CombatSkillPreview Failed(
            CombatActionAvailability availability,
            int apCost = 0,
            int ammoCost = 0) =>
            new CombatSkillPreview(false, availability, apCost, ammoCost, 0, 0, 0, 0);
    }
}
