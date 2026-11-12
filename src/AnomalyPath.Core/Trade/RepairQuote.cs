#nullable enable
using System;

namespace AnomalyPath.Core.Trade
{
    /// <summary>Предварительная оценка ремонта: сколько предметов чинится и сколько это стоит.</summary>
    public sealed class RepairQuote
    {
        /// <summary>Создаёт оценку ремонта.</summary>
        /// <param name="available">У торговца вообще есть ремонт и он доступен.</param>
        /// <param name="itemCount">Сколько предметов требует починки.</param>
        /// <param name="cost">Общая стоимость ремонта, не меньше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Отрицательное число предметов или денег.</exception>
        public RepairQuote(bool available, int itemCount, int cost)
        {
            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount));
            }

            if (cost < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(cost));
            }

            Available = available;
            ItemCount = itemCount;
            Cost = cost;
        }

        /// <summary>У торговца есть ремонт и он доступен.</summary>
        public bool Available { get; }

        /// <summary>Сколько предметов требует починки.</summary>
        public int ItemCount { get; }

        /// <summary>Общая стоимость ремонта.</summary>
        public int Cost { get; }

        /// <summary><c>true</c>, если есть что чинить и ремонт доступен.</summary>
        public bool HasWork => Available && ItemCount > 0;
    }
}
