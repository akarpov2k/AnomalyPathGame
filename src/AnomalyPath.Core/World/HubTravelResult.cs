#nullable enable
using System;
using AnomalyPath.Core.Player;

namespace AnomalyPath.Core.World
{
    /// <summary>Итог попытки перехода между хабами.</summary>
    public sealed class HubTravelResult
    {
        /// <summary>Создаёт результат перехода.</summary>
        /// <param name="status">Итог перехода.</param>
        /// <param name="steps">Число шагов между хабами.</param>
        /// <param name="cost">Списанная цена.</param>
        /// <param name="encounterActorId">Id противника, если на дороге случилась встреча.</param>
        /// <param name="encounterTier">Тир противника для опыта и лута; 0 — встречи нет.</param>
        /// <param name="damage">Урон жажды за переход; <c>null</c> — воды хватило.</param>
        /// <exception cref="ArgumentNullException">Цена не задана.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Шаги или тир отрицательны.</exception>
        public HubTravelResult(
            HubTravelStatus status,
            int steps,
            HubTravelCost cost,
            string? encounterActorId = null,
            int encounterTier = 0,
            PlayerDamageResult? damage = null)
        {
            if (cost == null)
            {
                throw new ArgumentNullException(nameof(cost));
            }

            if (steps < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps));
            }

            if (encounterTier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(encounterTier));
            }

            Status = status;
            Steps = steps;
            Cost = cost;
            EncounterActorId = encounterActorId;
            EncounterTier = encounterTier;
            Damage = damage;
        }

        /// <summary>Итог перехода.</summary>
        public HubTravelStatus Status { get; }

        /// <summary>Число шагов между хабами.</summary>
        public int Steps { get; }

        /// <summary>Списанная цена.</summary>
        public HubTravelCost Cost { get; }

        /// <summary>Id противника, если на дороге случилась встреча.</summary>
        public string? EncounterActorId { get; }

        /// <summary>Тир противника для опыта и лута; 0 — встречи нет.</summary>
        public int EncounterTier { get; }

        /// <summary>Урон жажды за переход; <c>null</c> — воды хватило.</summary>
        public PlayerDamageResult? Damage { get; }

        /// <summary><c>true</c>, если переход закончился смертью от жажды.</summary>
        public bool Died => Damage?.Died == true;

        /// <summary><c>true</c>, если переход начат (с прибытием или встречей).</summary>
        public bool Succeeded => Status == HubTravelStatus.Success || Status == HubTravelStatus.Encounter;
    }
}
