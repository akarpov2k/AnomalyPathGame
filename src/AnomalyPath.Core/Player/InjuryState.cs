#nullable enable
using System;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Player
{
    /// <summary>
    /// Активная травма игрока вне боя: вид, модификаторы берутся из данных при входе в бой,
    /// а здесь хранится только остаток длительности.
    /// </summary>
    public sealed class InjuryState
    {
        /// <summary>Создаёт активную травму.</summary>
        /// <param name="id">Id травмы из данных.</param>
        /// <param name="kind">Вид длительности, зафиксированный при наложении.</param>
        /// <param name="remaining">Остаток в шагах или часах; для «до лечения» — 0.</param>
        /// <exception cref="ArgumentException">Id пуст.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Остаток отрицателен.</exception>
        public InjuryState(string id, InjuryDurationKind kind, int remaining)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            if (remaining < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(remaining));
            }

            Id = id;
            Kind = kind;
            Remaining = remaining;
        }

        /// <summary>Id травмы.</summary>
        public string Id { get; }

        /// <summary>Вид длительности.</summary>
        public InjuryDurationKind Kind { get; }

        /// <summary>Остаток в шагах или часах; у «до лечения» не используется.</summary>
        public int Remaining { get; private set; }

        /// <summary><c>true</c>, если травма истекает сама.</summary>
        public bool IsTimed => InjuryDurationCodec.IsTimed(Kind);

        /// <summary><c>true</c>, если остаток исчерпан.</summary>
        public bool IsExpired => IsTimed && Remaining <= 0;

        /// <summary>Продлевает травму; «до лечения» продлить нельзя.</summary>
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

            Remaining = Math.Max(0, Remaining - units);
        }
    }
}
