#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Save
{
    /// <summary>Сохранённые потребности: отдельные шкалы еды и воды.</summary>
    public sealed class NeedsSaveData
    {
        /// <summary>Создаёт запись потребностей.</summary>
        /// <param name="hunger">Сытость.</param>
        /// <param name="thirst">Питьё.</param>
        /// <param name="foodMax">Максимум шкалы еды.</param>
        /// <param name="waterMax">Максимум шкалы воды.</param>
        /// <param name="foodCarry">Накопленная дробная часть еды, 0..1.</param>
        /// <param name="waterCarry">Накопленная дробная часть воды, 0..1.</param>
        public NeedsSaveData(
            int hunger,
            int thirst,
            int foodMax,
            int waterMax,
            double foodCarry = 0,
            double waterCarry = 0)
        {
            Hunger = hunger;
            Thirst = thirst;
            FoodMax = foodMax;
            WaterMax = waterMax;
            FoodCarry = foodCarry;
            WaterCarry = waterCarry;
        }

        /// <summary>Сытость.</summary>
        public int Hunger { get; }

        /// <summary>Питьё.</summary>
        public int Thirst { get; }

        /// <summary>Максимум шкалы еды.</summary>
        public int FoodMax { get; }

        /// <summary>Максимум шкалы воды.</summary>
        public int WaterMax { get; }

        /// <summary>Накопленная дробная часть еды, 0..1.</summary>
        public double FoodCarry { get; }

        /// <summary>Накопленная дробная часть воды, 0..1.</summary>
        public double WaterCarry { get; }

        /// <summary>Сериализует потребности.</summary>
        /// <returns>JSON-объект.</returns>
        public JsonValue ToJson()
        {
            var value = JsonValue.NewObject();
            value.Members["hunger"] = JsonValue.FromNumber(Hunger);
            value.Members["thirst"] = JsonValue.FromNumber(Thirst);
            value.Members["foodMax"] = JsonValue.FromNumber(FoodMax);
            value.Members["waterMax"] = JsonValue.FromNumber(WaterMax);
            value.Members["foodCarry"] = JsonValue.FromNumber(FoodCarry);
            value.Members["waterCarry"] = JsonValue.FromNumber(WaterCarry);
            return value;
        }

        /// <summary>Читает потребности.</summary>
        /// <param name="value">JSON-объект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Запись потребностей.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static NeedsSaveData FromJson(JsonValue value, string sourcePath)
        {
            var hunger = DataMap.RequireInt(value, "hunger", sourcePath);
            var thirst = DataMap.RequireInt(value, "thirst", sourcePath);
            var foodMax = DataMap.RequireInt(value, "foodMax", sourcePath);
            var waterMax = DataMap.RequireInt(value, "waterMax", sourcePath);
            if (foodMax < 1 || waterMax < 1 || hunger < 0 || hunger > foodMax ||
                thirst < 0 || thirst > waterMax)
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "needs");
            }

            var foodCarry = DataMap.OptionalDouble(value, "foodCarry") ?? 0;
            var waterCarry = DataMap.OptionalDouble(value, "waterCarry") ?? 0;
            if (foodCarry < 0 || foodCarry >= 1 || double.IsNaN(foodCarry) || double.IsInfinity(foodCarry) ||
                waterCarry < 0 || waterCarry >= 1 || double.IsNaN(waterCarry) || double.IsInfinity(waterCarry))
            {
                throw new DataException(sourcePath, CoreErrors.SaveMissingField, "needs");
            }

            return new NeedsSaveData(hunger, thirst, foodMax, waterMax, foodCarry, waterCarry);
        }
    }
}
