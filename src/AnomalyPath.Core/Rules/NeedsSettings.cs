#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Rules
{
    /// <summary>
    /// Правила потребностей: сколько еды и воды уходит за час мира, чем оборачивается голод
    /// и сколько здоровья теряется при нуле воды.
    /// </summary>
    public sealed class NeedsSettings
    {
        /// <summary>Создаёт настройку потребностей.</summary>
        /// <param name="foodPerHour">Еда за час мира; не отрицательна.</param>
        /// <param name="waterPerHour">Вода за час мира; не отрицательна.</param>
        /// <param name="hungryStaminaCostMultiplier">
        /// Множитель цены перехода в выносливости при нуле еды; не меньше 1.
        /// </param>
        /// <param name="hungryApCostBonus">Прибавка к цене любого действия игрока в бою при нуле еды; не меньше 0.</param>
        /// <param name="thirstDamage">Здоровье, теряемое за переход и за ход игрока при нуле воды; не меньше 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public NeedsSettings(
            double foodPerHour,
            double waterPerHour,
            double hungryStaminaCostMultiplier,
            int hungryApCostBonus,
            int thirstDamage)
        {
            if (foodPerHour < 0 || double.IsNaN(foodPerHour) || double.IsInfinity(foodPerHour))
            {
                throw new ArgumentOutOfRangeException(nameof(foodPerHour));
            }

            if (waterPerHour < 0 || double.IsNaN(waterPerHour) || double.IsInfinity(waterPerHour))
            {
                throw new ArgumentOutOfRangeException(nameof(waterPerHour));
            }

            if (hungryStaminaCostMultiplier < 1 ||
                double.IsNaN(hungryStaminaCostMultiplier) ||
                double.IsInfinity(hungryStaminaCostMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(hungryStaminaCostMultiplier));
            }

            if (hungryApCostBonus < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hungryApCostBonus));
            }

            if (thirstDamage < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(thirstDamage));
            }

            FoodPerHour = foodPerHour;
            WaterPerHour = waterPerHour;
            HungryStaminaCostMultiplier = hungryStaminaCostMultiplier;
            HungryApCostBonus = hungryApCostBonus;
            ThirstDamage = thirstDamage;
        }

        /// <summary>Сколько еды уходит за час мира.</summary>
        public double FoodPerHour { get; }

        /// <summary>Сколько воды уходит за час мира.</summary>
        public double WaterPerHour { get; }

        /// <summary>Множитель цены перехода в выносливости при нуле еды; не меньше 1.</summary>
        public double HungryStaminaCostMultiplier { get; }

        /// <summary>Прибавка к цене любого действия игрока в бою при нуле еды.</summary>
        public int HungryApCostBonus { get; }

        /// <summary>Здоровье, теряемое за переход и за ход игрока при нуле воды.</summary>
        public int ThirstDamage { get; }

        /// <summary>
        /// Цена перехода в выносливости при голоде: базовая цена × множитель,
        /// округлённая вверх до целого.
        /// </summary>
        /// <param name="baseCost">Базовая цена в выносливости; не положительная — без изменений.</param>
        /// <returns>Цена с надбавкой; для базы 2 и множителя 1.2 — 3.</returns>
        public int HungryStaminaCost(int baseCost)
        {
            if (baseCost <= 0)
            {
                return baseCost;
            }

            return (int)Math.Ceiling(baseCost * HungryStaminaCostMultiplier);
        }

        /// <summary>Читает блок <c>needs</c> записи правил.</summary>
        /// <param name="value">Объект блока <c>needs</c>.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка потребностей.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static NeedsSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "needs");
            }

            var food = DataMap.RequireDouble(value, "foodPerHour", sourcePath);
            if (food < 0 || double.IsNaN(food) || double.IsInfinity(food))
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "needs.foodPerHour");
            }

            var water = DataMap.RequireDouble(value, "waterPerHour", sourcePath);
            if (water < 0 || double.IsNaN(water) || double.IsInfinity(water))
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "needs.waterPerHour");
            }

            var multiplier = DataMap.RequireDouble(value, "hungryStaminaCostMultiplier", sourcePath);
            if (multiplier < 1 || double.IsNaN(multiplier) || double.IsInfinity(multiplier))
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "needs.hungryStaminaCostMultiplier");
            }

            var apBonus = DataMap.RequireInt(value, "hungryApCostBonus", sourcePath);
            if (apBonus < 0)
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "needs.hungryApCostBonus");
            }

            var thirstDamage = DataMap.RequireInt(value, "thirstDamage", sourcePath);
            if (thirstDamage < 0)
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "needs.thirstDamage");
            }

            return new NeedsSettings(food, water, multiplier, apBonus, thirstDamage);
        }
    }
}
