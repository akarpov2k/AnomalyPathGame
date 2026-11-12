#nullable enable
using System;

namespace AnomalyPath.Core.Trade
{
    /// <summary>Итог ремонта: что сделано и сколько списано.</summary>
    public sealed class RepairResult
    {
        /// <summary>Создаёт итог ремонта.</summary>
        /// <param name="status">Состояние ремонта.</param>
        /// <param name="repairedCount">Сколько предметов починено.</param>
        /// <param name="cost">Сколько денег списано.</param>
        /// <exception cref="ArgumentOutOfRangeException">Отрицательные числа.</exception>
        public RepairResult(RepairStatus status, int repairedCount, int cost)
        {
            if (repairedCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(repairedCount));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            Status = status;
            RepairedCount = repairedCount;
            Cost = cost;
        }

        /// <summary>Состояние ремонта.</summary>
        public RepairStatus Status { get; }

        /// <summary>Сколько предметов починено.</summary>
        public int RepairedCount { get; }

        /// <summary>Сколько денег списано.</summary>
        public int Cost { get; }

        /// <summary><c>true</c>, если ремонт выполнен.</summary>
        public bool Succeeded => Status == RepairStatus.Ok;
    }
}
