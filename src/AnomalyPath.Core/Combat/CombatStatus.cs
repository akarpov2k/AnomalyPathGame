#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Активный статус бойца: травма, эффект медицины или эффект скилла. Хранит id, модификаторы,
    /// вид длительности и остаток; остаток тикает часами или ходами.
    /// </summary>
    public sealed class CombatStatus
    {
        /// <summary>Создаёт статус без длительности: он действует до конца боя.</summary>
        /// <param name="id">Идентификатор статуса.</param>
        /// <param name="modifiers">Модификаторы, которые статус даёт участнику.</param>
        /// <exception cref="System.ArgumentNullException">Обязательное значение не передано.</exception>
        public CombatStatus(string id, CombatModifiers modifiers)
        {
            Id = id ?? throw new System.ArgumentNullException(nameof(id));
            Modifiers = modifiers ?? throw new System.ArgumentNullException(nameof(modifiers));
            Kind = InjuryDurationKind.UntilHealed;
            Remaining = 0;
            NameKey = null;
        }

        /// <summary>Создаёт временный статус скилла или медицины.</summary>
        /// <param name="id">Идентификатор статуса.</param>
        /// <param name="nameKey">Ключ названия; <c>null</c> — служебный статус.</param>
        /// <param name="modifiers">Модификаторы статуса.</param>
        /// <param name="kind">Вид длительности.</param>
        /// <param name="remaining">Остаток в часах или ходах; для «до лечения» — 0.</param>
        /// <exception cref="System.ArgumentNullException">Обязательное значение не передано.</exception>
        public CombatStatus(
            string id,
            string? nameKey,
            CombatModifiers modifiers,
            InjuryDurationKind kind,
            int remaining)
        {
            Id = id ?? throw new System.ArgumentNullException(nameof(id));
            NameKey = nameKey;
            Modifiers = modifiers ?? throw new System.ArgumentNullException(nameof(modifiers));
            Kind = kind;
            Remaining = remaining < 0 ? 0 : remaining;
            IsInjury = false;
        }

        /// <summary>Создаёт статус из шаблона травмы.</summary>
        /// <param name="template">Травма из данных.</param>
        /// <param name="remaining">Остаток в часах или ходах; для «до лечения» — 0.</param>
        /// <exception cref="System.ArgumentNullException">Шаблон не передан.</exception>
        public CombatStatus(Templates.InjuryTemplate template, int remaining)
        {
            if (template == null)
            {
                throw new System.ArgumentNullException(nameof(template));
            }

            Id = template.Id;
            Modifiers = template.Modifiers;
            Kind = template.DurationKind;
            Remaining = remaining < 0 ? 0 : remaining;
            NameKey = template.NameKey;
            IsInjury = true;
        }

        /// <summary>Идентификатор статуса.</summary>
        public string Id { get; }

        /// <summary>Ключ названия в каталоге строк; <c>null</c> — без названия (служебный статус).</summary>
        public string? NameKey { get; }

        /// <summary>Модификаторы статуса.</summary>
        public CombatModifiers Modifiers { get; }

        /// <summary>Вид длительности.</summary>
        public InjuryDurationKind Kind { get; }

        /// <summary>Остаток в часах или ходах; у «до лечения» не используется.</summary>
        public int Remaining { get; private set; }

        /// <summary>
        /// <c>true</c>, если статус — травма: только травмы переносятся в состояние игрока
        /// и пишутся в сейв; временные эффекты живут до конца боя.
        /// </summary>
        public bool IsInjury { get; }

        /// <summary><c>true</c>, если статус истекает сам.</summary>
        public bool IsTimed => InjuryDurationCodec.IsTimed(Kind);

        /// <summary><c>true</c>, если остаток исчерпан.</summary>
        public bool IsExpired => IsTimed && Remaining <= 0;

        /// <summary>Продлевает статус: повторный статус добавляет длительность, а не складывает модификаторы.</summary>
        /// <param name="amount">Сколько единиц добавить; не больше нуля — ничего не меняет.</param>
        public void Extend(int amount)
        {
            if (!IsTimed || amount <= 0)
            {
                return;
            }

            Remaining += amount;
        }

        /// <summary>Тратит единицы длительности; «до лечения» не тикает.</summary>
        /// <param name="units">Сколько единиц прошло; не больше нуля — ничего не меняет.</param>
        public void Tick(int units)
        {
            if (!IsTimed || units <= 0)
            {
                return;
            }

            Remaining = System.Math.Max(0, Remaining - units);
        }

        /// <summary>Копия статуса для расчёта плана ИИ: изменения копии не трогают бой.</summary>
        /// <returns>Независимая копия с тем же остатком.</returns>
        internal CombatStatus Clone() => new CombatStatus(Id, Modifiers, Kind, Remaining, NameKey, IsInjury);

        private CombatStatus(
            string id,
            CombatModifiers modifiers,
            InjuryDurationKind kind,
            int remaining,
            string? nameKey,
            bool isInjury)
        {
            Id = id;
            Modifiers = modifiers;
            Kind = kind;
            Remaining = remaining;
            NameKey = nameKey;
            IsInjury = isInjury;
        }
    }
}
