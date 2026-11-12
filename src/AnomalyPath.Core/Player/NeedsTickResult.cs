#nullable enable
using System;

namespace AnomalyPath.Core.Player
{
    /// <summary>Итог расхода еды и воды за прошедшие часы мира.</summary>
    public sealed class NeedsTickResult
    {
        /// <summary>Создаёт результат расхода потребностей.</summary>
        /// <param name="hours">Сколько часов прошло.</param>
        /// <param name="foodSpent">Сколько еды списано.</param>
        /// <param name="waterSpent">Сколько воды списано.</param>
        /// <param name="hunger">Сытость после расхода.</param>
        /// <param name="thirst">Питьё после расхода.</param>
        /// <param name="foodCarry">Накопленная дробная часть еды, 0..1.</param>
        /// <param name="waterCarry">Накопленная дробная часть воды, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимых границ.</exception>
        public NeedsTickResult(
            double hours,
            int foodSpent,
            int waterSpent,
            int hunger,
            int thirst,
            double foodCarry,
            double waterCarry)
        {
            if (hours < 0 || double.IsNaN(hours) || double.IsInfinity(hours))
            {
                throw new ArgumentOutOfRangeException(nameof(hours));
            }

            if (foodSpent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(foodSpent));
            }

            if (waterSpent < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(waterSpent));
            }

            if (hunger < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hunger));
            }

            if (thirst < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(thirst));
            }

            if (foodCarry < 0 || foodCarry >= 1 || double.IsNaN(foodCarry) || double.IsInfinity(foodCarry))
            {
                throw new ArgumentOutOfRangeException(nameof(foodCarry));
            }

            if (waterCarry < 0 || waterCarry >= 1 || double.IsNaN(waterCarry) || double.IsInfinity(waterCarry))
            {
                throw new ArgumentOutOfRangeException(nameof(waterCarry));
            }

            Hours = hours;
            FoodSpent = foodSpent;
            WaterSpent = waterSpent;
            Hunger = hunger;
            Thirst = thirst;
            FoodCarry = foodCarry;
            WaterCarry = waterCarry;
        }

        /// <summary>Сколько часов прошло.</summary>
        public double Hours { get; }

        /// <summary>Сколько еды списано.</summary>
        public int FoodSpent { get; }

        /// <summary>Сколько воды списано.</summary>
        public int WaterSpent { get; }

        /// <summary>Сытость после расхода.</summary>
        public int Hunger { get; }

        /// <summary>Питьё после расхода.</summary>
        public int Thirst { get; }

        /// <summary>Накопленная дробная часть еды, 0..1.</summary>
        public double FoodCarry { get; }

        /// <summary>Накопленная дробная часть воды, 0..1.</summary>
        public double WaterCarry { get; }
    }
}
