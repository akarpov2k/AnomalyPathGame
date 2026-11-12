#nullable enable
using System;
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Core.Data
{
    /// <summary>
    /// Выбор одного элемента по весам: один бросок, пропорциональный весам.
    /// Один код на все взвешенные выборы — генерацию веток, спавн объектов, квесты и встречи.
    /// </summary>
    public static class WeightedPick
    {
        /// <summary>Выбирает индекс элемента по весам.</summary>
        /// <param name="count">Сколько элементов участвует в выборе.</param>
        /// <param name="weight">Вес элемента по индексу; вес 0 элемент не выбирает.</param>
        /// <param name="random">Источник случайных чисел.</param>
        /// <returns>Индекс выбранного элемента или -1, если сумма весов не положительна.</returns>
        /// <exception cref="ArgumentNullException">Вес или источник не заданы.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Количество отрицательное.</exception>
        public static int ChooseIndex(int count, Func<int, double> weight, IWorldRandom random)
        {
            if (weight == null)
            {
                throw new ArgumentNullException(nameof(weight));
            }

            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var total = 0.0;
            for (var index = 0; index < count; index++)
            {
                total += weight(index);
            }

            if (total <= 0)
            {
                return -1;
            }

            var roll = random.NextDouble() * total;
            for (var index = 0; index < count; index++)
            {
                var current = weight(index);
                if (roll < current)
                {
                    return index;
                }

                roll -= current;
            }

            return count - 1;
        }
    }
}
