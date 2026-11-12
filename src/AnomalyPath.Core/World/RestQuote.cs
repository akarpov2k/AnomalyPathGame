#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>
    /// Предложение отдыха по текущему состоянию: доступность, скорость восстановления,
    /// предел часов и цена одного часа вне хаба в шансе нападения. Ничего не меняет.
    /// </summary>
    public sealed class RestQuote
    {
        /// <summary>Создаёт предложение отдыха.</summary>
        /// <param name="status">Доступность отдыха.</param>
        /// <param name="locationIsHub">Игрок в хабе, а не вне его.</param>
        /// <param name="stamina">Текущая выносливость.</param>
        /// <param name="staminaMax">Действующий максимум выносливости с бонусами.</param>
        /// <param name="staminaPerHour">Восстановление за час; 0 — отдых недоступен.</param>
        /// <param name="maxHours">Предел часов отдыха; 0 — восстанавливать нечего.</param>
        /// <param name="missing">Сколько выносливости не хватает до максимума.</param>
        /// <param name="attackChancePerHour">Прибавка шанса нападения за час вне хаба, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимых границ.</exception>
        public RestQuote(
            RestStatus status,
            bool locationIsHub,
            int stamina,
            int staminaMax,
            int staminaPerHour,
            int maxHours,
            int missing,
            double attackChancePerHour)
        {
            if (stamina < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stamina));
            }

            if (staminaMax < stamina)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaMax));
            }

            if (staminaPerHour < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaPerHour));
            }

            if (maxHours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxHours));
            }

            if (missing < 0 || missing > staminaMax)
            {
                throw new ArgumentOutOfRangeException(nameof(missing));
            }

            if (attackChancePerHour < 0 || attackChancePerHour > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(attackChancePerHour));
            }

            Status = status;
            LocationIsHub = locationIsHub;
            Stamina = stamina;
            StaminaMax = staminaMax;
            StaminaPerHour = staminaPerHour;
            MaxHours = maxHours;
            Missing = missing;
            AttackChancePerHour = attackChancePerHour;
        }

        /// <summary>Доступность отдыха.</summary>
        public RestStatus Status { get; }

        /// <summary><c>true</c>, если игрок в хабе: нападения там нет.</summary>
        public bool LocationIsHub { get; }

        /// <summary>Текущая выносливость.</summary>
        public int Stamina { get; }

        /// <summary>Действующий максимум выносливости с бонусами.</summary>
        public int StaminaMax { get; }

        /// <summary>Сколько выносливости восстанавливает один час отдыха.</summary>
        public int StaminaPerHour { get; }

        /// <summary>Сколько часов можно отдыхать: предел полного восстановления.</summary>
        public int MaxHours { get; }

        /// <summary>Сколько выносливости не хватает до максимума.</summary>
        public int Missing { get; }

        /// <summary>Прибавка шанса нападения за час вне хаба, 0..1.</summary>
        public double AttackChancePerHour { get; }

        /// <summary><c>true</c>, если отдых возможен: есть что восстанавливать и правила загружены.</summary>
        public bool Available => Status == RestStatus.Success;

        /// <summary>Шанс нападения за выбранное число часов: прибавка за час × часы, не больше 1.</summary>
        /// <param name="hours">Выбранные часы отдыха.</param>
        /// <returns>Шанс нападения, 0..1.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Часы отрицательны.</exception>
        public double AttackChance(int hours)
        {
            if (hours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours));
            }

            return Math.Min(1, AttackChancePerHour * hours);
        }
    }
}
