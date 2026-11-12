#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.World
{
    /// <summary>Цена пешего перехода между хабами за шаг и шанс встречи на дороге.</summary>
    public sealed class OnFootTravelSettings
    {
        /// <summary>Создаёт настройку пешего перехода.</summary>
        /// <param name="hoursPerStep">Часы мира за один шаг; неотрицательны.</param>
        /// <param name="staminaPerStep">Выносливость за шаг; неотрицательна.</param>
        /// <param name="encounterChancePerStep">Шанс встречи за шаг, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public OnFootTravelSettings(double hoursPerStep, int staminaPerStep, double encounterChancePerStep)
        {
            if (hoursPerStep < 0 || double.IsNaN(hoursPerStep) || double.IsInfinity(hoursPerStep))
            {
                throw new ArgumentOutOfRangeException(nameof(hoursPerStep));
            }

            if (staminaPerStep < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaPerStep));
            }

            if (encounterChancePerStep < 0 || encounterChancePerStep > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(encounterChancePerStep));
            }

            HoursPerStep = hoursPerStep;
            StaminaPerStep = staminaPerStep;
            EncounterChancePerStep = encounterChancePerStep;
        }

        /// <summary>Часы мира за один шаг между хабами.</summary>
        public double HoursPerStep { get; }

        /// <summary>Выносливость за один шаг.</summary>
        public int StaminaPerStep { get; }

        /// <summary>Шанс встречи за один шаг, 0..1.</summary>
        public double EncounterChancePerStep { get; }

        /// <summary>Читает настройку из объекта блока <c>travel.onFoot</c>.</summary>
        /// <param name="value">Объект настроек.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка пешего перехода.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static OnFootTravelSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "travel.onFoot");
            }

            var hours = value.TryGet("minutesPerStep", out _)
                ? DataMap.RequireInt(value, "minutesPerStep", sourcePath) / 60.0
                : DataMap.RequireDouble(value, "hoursPerStep", sourcePath);
            if (hours < 0 || double.IsNaN(hours) || double.IsInfinity(hours))
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "travel.onFoot.minutesPerStep");
            }

            var stamina = DataMap.RequireInt(value, "staminaPerStep", sourcePath);
            if (stamina < 0)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "travel.onFoot.staminaPerStep");
            }

            var chance = DataMap.RequireDouble(value, "encounterChancePerStep", sourcePath);
            if (chance < 0 || chance > 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldChance, "travel.onFoot.encounterChancePerStep");
            }

            return new OnFootTravelSettings(hours, stamina, chance);
        }
    }
}
