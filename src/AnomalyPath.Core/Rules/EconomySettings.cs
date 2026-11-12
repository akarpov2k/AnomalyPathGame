#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Rules
{
    /// <summary>Экономика из данных: округление, нижняя граница цены и стоимость ремонта.</summary>
    public sealed class EconomySettings : IEconomySettings
    {
        /// <summary>Настройки по умолчанию: округление до рубля, без нижней границы, ремонт — половина цены.</summary>
        public static readonly EconomySettings Default = new EconomySettings(1, 0, 0.5);

        /// <summary>Создаёт настройки экономики.</summary>
        /// <param name="rounding">Шаг округления, не меньше единицы.</param>
        /// <param name="minPrice">Минимальная цена, не меньше нуля.</param>
        /// <param name="repairCostFactor">Доля цены предмета за полный ремонт, больше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значения вне допустимых границ.</exception>
        public EconomySettings(int rounding, int minPrice, double repairCostFactor)
        {
            if (rounding < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(rounding),
                    CoreText.Format(CoreErrors.EconomyRounding, rounding));
            }

            if (minPrice < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(minPrice),
                    CoreText.Format(CoreErrors.EconomyMinPrice, minPrice));
            }

            if (repairCostFactor <= 0 || double.IsNaN(repairCostFactor) || double.IsInfinity(repairCostFactor))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(repairCostFactor),
                    CoreText.Format(CoreErrors.EconomyRepairFactor, repairCostFactor));
            }

            Rounding = rounding;
            MinPrice = minPrice;
            RepairCostFactor = repairCostFactor;
        }

        /// <inheritdoc />
        public int Rounding { get; }

        /// <inheritdoc />
        public int MinPrice { get; }

        /// <inheritdoc />
        public double RepairCostFactor { get; }

        /// <summary>Читает настройки экономики из записи данных.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройки экономики.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static EconomySettings FromJson(JsonValue value, string sourcePath)
        {
            var rounding = DataMap.RequireInt(value, "rounding", sourcePath);
            if (rounding < 1)
            {
                throw new DataException(sourcePath, CoreErrors.EconomyRounding, rounding);
            }

            var minPrice = DataMap.OptionalInt(value, "minPrice", 0);
            if (minPrice < 0)
            {
                throw new DataException(sourcePath, CoreErrors.EconomyMinPrice, minPrice);
            }

            var repairCostFactor = DataMap.OptionalDouble(value, "repairCostFactor") ?? Default.RepairCostFactor;
            if (repairCostFactor <= 0 || double.IsNaN(repairCostFactor) || double.IsInfinity(repairCostFactor))
            {
                throw new DataException(sourcePath, CoreErrors.EconomyRepairFactor, repairCostFactor);
            }

            return new EconomySettings(rounding, minPrice, repairCostFactor);
        }
    }
}
