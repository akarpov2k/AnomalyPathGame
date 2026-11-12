#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.World
{
    /// <summary>Цена перехода с проводником за шаг: часы мира и деньги.</summary>
    public sealed class GuideTravelSettings
    {
        /// <summary>Создаёт настройку перехода с проводником.</summary>
        /// <param name="hoursPerStep">Часы мира за один шаг; неотрицательны.</param>
        /// <param name="moneyPerStep">Деньги за один шаг; неотрицательны.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public GuideTravelSettings(double hoursPerStep, int moneyPerStep)
        {
            if (hoursPerStep < 0 || double.IsNaN(hoursPerStep) || double.IsInfinity(hoursPerStep))
            {
                throw new ArgumentOutOfRangeException(nameof(hoursPerStep));
            }

            if (moneyPerStep < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(moneyPerStep));
            }

            HoursPerStep = hoursPerStep;
            MoneyPerStep = moneyPerStep;
        }

        /// <summary>Часы мира за один шаг между хабами.</summary>
        public double HoursPerStep { get; }

        /// <summary>Деньги за один шаг.</summary>
        public int MoneyPerStep { get; }

        /// <summary>Читает настройку из объекта блока <c>travel.guide</c>.</summary>
        /// <param name="value">Объект настроек.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка перехода с проводником.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static GuideTravelSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "travel.guide");
            }

            var hours = value.TryGet("minutesPerStep", out _)
                ? DataMap.RequireInt(value, "minutesPerStep", sourcePath) / 60.0
                : DataMap.RequireDouble(value, "hoursPerStep", sourcePath);
            if (hours < 0 || double.IsNaN(hours) || double.IsInfinity(hours))
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "travel.guide.minutesPerStep");
            }

            var money = DataMap.RequireInt(value, "moneyPerStep", sourcePath);
            if (money < 0)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "travel.guide.moneyPerStep");
            }

            return new GuideTravelSettings(hours, money);
        }
    }
}
