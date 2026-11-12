#nullable enable
using System;

namespace AnomalyPath.Core.World
{
    /// <summary>Стоимость перехода между хабами: часы, выносливость и деньги.</summary>
    public sealed class HubTravelCost
    {
        /// <summary>Создаёт стоимость перехода.</summary>
        /// <param name="hours">Часы мира; неотрицательны.</param>
        /// <param name="stamina">Выносливость; неотрицательна.</param>
        /// <param name="money">Деньги; неотрицательны.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение отрицательное или не число.</exception>
        public HubTravelCost(double hours, int stamina, int money)
        {
            if (hours < 0 || double.IsNaN(hours) || double.IsInfinity(hours))
            {
                throw new ArgumentOutOfRangeException(nameof(hours));
            }

            if (stamina < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stamina));
            }

            if (money < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(money));
            }

            Hours = hours;
            Stamina = stamina;
            Money = money;
        }

        /// <summary>Часы мира.</summary>
        public double Hours { get; }

        /// <summary>Минуты мира (округление до минуты).</summary>
        public int Minutes => (int)Math.Round(Hours * 60, MidpointRounding.AwayFromZero);

        /// <summary>Выносливость.</summary>
        public int Stamina { get; }

        /// <summary>Деньги.</summary>
        public int Money { get; }

        /// <summary>Нулевая стоимость.</summary>
        public static HubTravelCost Zero { get; } = new HubTravelCost(0, 0, 0);

        /// <summary>Умножает стоимость на число шагов между хабами.</summary>
        /// <param name="steps">Число шагов; должно быть больше нуля.</param>
        /// <returns>Стоимость перехода через указанное число шагов.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Шагов не больше нуля.</exception>
        public HubTravelCost Scale(int steps)
        {
            if (steps <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(steps));
            }

            return new HubTravelCost(Hours * steps, Stamina * steps, Money * steps);
        }
    }
}
