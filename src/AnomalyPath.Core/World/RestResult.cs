#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>Итог отдыха: сколько часов прошло, сколько восстановлено и кто напал.</summary>
    public sealed class RestResult
    {
        /// <summary>Создаёт результат отдыха.</summary>
        /// <param name="status">Итог отдыха.</param>
        /// <param name="hours">Сколько часов длился отдых.</param>
        /// <param name="staminaRestored">Сколько выносливости восстановлено.</param>
        /// <param name="stamina">Выносливость после отдыха.</param>
        /// <param name="attackChance">Шанс нападения за весь отдых, 0..1.</param>
        /// <param name="encounterActorId">Id напавшего противника; <c>null</c> — нападения не было.</param>
        /// <param name="encounterTier">Тир напавшего противника; 0 — нападения не было.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимых границ.</exception>
        /// <exception cref="ArgumentException">Id противника пуст.</exception>
        public RestResult(
            RestStatus status,
            int hours,
            int staminaRestored,
            int stamina,
            double attackChance,
            string? encounterActorId = null,
            int encounterTier = 0)
        {
            if (hours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours));
            }

            if (staminaRestored < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaRestored));
            }

            if (stamina < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stamina));
            }

            if (attackChance < 0 || attackChance > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(attackChance));
            }

            if (encounterActorId != null && string.IsNullOrWhiteSpace(encounterActorId))
            {
                throw new ArgumentException(nameof(encounterActorId));
            }

            if (encounterTier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(encounterTier));
            }

            Status = status;
            Hours = hours;
            StaminaRestored = staminaRestored;
            Stamina = stamina;
            AttackChance = attackChance;
            EncounterActorId = encounterActorId;
            EncounterTier = encounterTier;
        }

        /// <summary>Итог отдыха.</summary>
        public RestStatus Status { get; }

        /// <summary>Сколько часов длился отдых.</summary>
        public int Hours { get; }

        /// <summary>Сколько выносливости восстановлено.</summary>
        public int StaminaRestored { get; }

        /// <summary>Выносливость после отдыха.</summary>
        public int Stamina { get; }

        /// <summary>Шанс нападения за весь отдых, 0..1.</summary>
        public double AttackChance { get; }

        /// <summary>Id напавшего противника; <c>null</c> — нападения не было.</summary>
        public string? EncounterActorId { get; }

        /// <summary>Тир напавшего противника; 0 — нападения не было.</summary>
        public int EncounterTier { get; }

        /// <summary><c>true</c>, если отдых прошёл (с нападением или без).</summary>
        public bool Succeeded => Status == RestStatus.Success || Status == RestStatus.Encounter;
    }
}
