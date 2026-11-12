#nullable enable
using System;

namespace AnomalyPath.Core.Player
{
    /// <summary>Потребности персонажа: еда и вода.</summary>
    public sealed class NeedsState
    {
        /// <summary>Создаёт состояние потребностей с общей шкалой.</summary>
        /// <param name="hunger">Сытость.</param>
        /// <param name="thirst">Питьё.</param>
        /// <param name="max">Максимум обеих шкал.</param>
        /// <param name="foodCarry">Накопленная дробная часть еды после неполного часа; 0..1.</param>
        /// <param name="waterCarry">Накопленная дробная часть воды после неполного часа; 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимых границ.</exception>
        public NeedsState(
            int hunger,
            int thirst,
            int max,
            double foodCarry = 0,
            double waterCarry = 0)
            : this(hunger, thirst, max, max, foodCarry, waterCarry)
        {
        }

        /// <summary>Создаёт состояние потребностей с отдельными максимумами еды и воды.</summary>
        /// <param name="hunger">Сытость.</param>
        /// <param name="thirst">Питьё.</param>
        /// <param name="foodMax">Максимум шкалы еды, не меньше 1.</param>
        /// <param name="waterMax">Максимум шкалы воды, не меньше 1.</param>
        /// <param name="foodCarry">Накопленная дробная часть еды после неполного часа; 0..1.</param>
        /// <param name="waterCarry">Накопленная дробная часть воды после неполного часа; 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимых границ.</exception>
        public NeedsState(
            int hunger,
            int thirst,
            int foodMax,
            int waterMax,
            double foodCarry,
            double waterCarry)
        {
            if (foodMax < 1 || waterMax < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(foodMax));
            }

            if (hunger < 0 || hunger > foodMax)
            {
                throw new ArgumentOutOfRangeException(nameof(hunger));
            }

            if (thirst < 0 || thirst > waterMax)
            {
                throw new ArgumentOutOfRangeException(nameof(thirst));
            }

            ValidateCarry(foodCarry, nameof(foodCarry));
            ValidateCarry(waterCarry, nameof(waterCarry));

            Hunger = hunger;
            Thirst = thirst;
            FoodMax = foodMax;
            WaterMax = waterMax;
            FoodCarry = foodCarry;
            WaterCarry = waterCarry;
        }

        /// <summary>Сытость.</summary>
        public int Hunger { get; private set; }

        /// <summary>Питьё.</summary>
        public int Thirst { get; private set; }

        /// <summary>Максимум шкалы еды.</summary>
        public int FoodMax { get; private set; }

        /// <summary>Максимум шкалы воды.</summary>
        public int WaterMax { get; private set; }

        /// <summary>Накопленная дробная часть еды, 0..1: остаток неполного часа.</summary>
        public double FoodCarry { get; private set; }

        /// <summary>Накопленная дробная часть воды, 0..1: остаток неполного часа.</summary>
        public double WaterCarry { get; private set; }

        /// <summary>
        /// Меняет максимумы шкал (например, от талантов). Текущие значения не растут,
        /// только подрезаются новым пределом, если он стал ниже.
        /// </summary>
        /// <param name="foodMax">Новый максимум еды, не меньше 1.</param>
        /// <param name="waterMax">Новый максимум воды, не меньше 1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Максимум меньше 1.</exception>
        public void SetMax(int foodMax, int waterMax)
        {
            if (foodMax < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(foodMax));
            }

            if (waterMax < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(waterMax));
            }

            FoodMax = foodMax;
            WaterMax = waterMax;
            Hunger = Clamp(Hunger, FoodMax);
            Thirst = Clamp(Thirst, WaterMax);
        }

        /// <summary>Меняет сытость в пределах шкалы еды.</summary>
        /// <param name="value">Новое значение.</param>
        public void SetHunger(int value) => Hunger = Clamp(value, FoodMax);

        /// <summary>Меняет питьё в пределах шкалы воды.</summary>
        /// <param name="value">Новое значение.</param>
        public void SetThirst(int value) => Thirst = Clamp(value, WaterMax);

        /// <summary>Запоминает дробную часть еды после расхода за неполный час.</summary>
        /// <param name="value">Остаток от 0 до 1 (не включая 1).</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне 0..1.</exception>
        public void SetFoodCarry(double value)
        {
            ValidateCarry(value, nameof(value));
            FoodCarry = value;
        }

        /// <summary>Запоминает дробную часть воды после расхода за неполный час.</summary>
        /// <param name="value">Остаток от 0 до 1 (не включая 1).</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне 0..1.</exception>
        public void SetWaterCarry(double value)
        {
            ValidateCarry(value, nameof(value));
            WaterCarry = value;
        }

        private static void ValidateCarry(double value, string parameter)
        {
            if (value < 0 || value >= 1 || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(parameter);
            }
        }

        private static int Clamp(int value, int max) => value < 0 ? 0 : value > max ? max : value;
    }
}
