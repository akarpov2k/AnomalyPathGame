#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>Предложение перехода между хабами: доступность, шаги, цена и шанс встречи.</summary>
    public sealed class HubTravelQuote
    {
        /// <summary>Создаёт предложение перехода.</summary>
        /// <param name="status">Доступность перехода.</param>
        /// <param name="steps">Число шагов между хабами (разница тиров).</param>
        /// <param name="cost">Цена перехода.</param>
        /// <param name="encounterChance">Шанс встречи на дороге, 0..1.</param>
        /// <exception cref="ArgumentNullException">Цена не задана.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Шаги или шанс вне допустимых границ.</exception>
        public HubTravelQuote(HubTravelStatus status, int steps, HubTravelCost cost, double encounterChance)
        {
            if (cost == null)
            {
                throw new ArgumentNullException(nameof(cost));
            }

            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps));
            }

            if (encounterChance < 0 || encounterChance > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encounterChance));
            }

            Status = status;
            Steps = steps;
            Cost = cost;
            EncounterChance = encounterChance;
        }

        /// <summary>Доступность перехода.</summary>
        public HubTravelStatus Status { get; }

        /// <summary>Число шагов между хабами.</summary>
        public int Steps { get; }

        /// <summary>Цена перехода.</summary>
        public HubTravelCost Cost { get; }

        /// <summary>Шанс встречи на дороге, 0..1.</summary>
        public double EncounterChance { get; }

        /// <summary><c>true</c>, если переход доступен.</summary>
        public bool Available => Status == HubTravelStatus.Success;
    }
}
