#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.World
{
    /// <summary>
    /// Правила отдыха: сколько выносливости восстанавливает час отдыха (одинаково в хабе и вне его)
    /// и насколько растёт риск нападения за каждый час отдыха вне хаба.
    /// </summary>
    public sealed class RestSettings
    {
        /// <summary>Создаёт настройку отдыха.</summary>
        /// <param name="staminaPerHour">Выносливость за час отдыха, не меньше 1.</param>
        /// <param name="raidAttackChancePerHour">Прибавка шанса нападения за час отдыха вне хаба, 0..1.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public RestSettings(int staminaPerHour, double raidAttackChancePerHour)
        {
            if (staminaPerHour < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaPerHour));
            }

            if (raidAttackChancePerHour < 0 || raidAttackChancePerHour > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(raidAttackChancePerHour));
            }

            PerHour = staminaPerHour;
            RaidAttackChancePerHour = raidAttackChancePerHour;
        }

        /// <summary>Выносливость, восстанавливаемая за час отдыха.</summary>
        public int PerHour { get; }

        /// <summary>Прибавка шанса нападения за каждый час отдыха вне хаба, 0..1.</summary>
        public double RaidAttackChancePerHour { get; }

        /// <summary>
        /// Сколько выносливости восстанавливает один час отдыха. Значение не зависит от максимума
        /// и от того, где идёт отдых: это одна и та же ставка из данных.
        /// </summary>
        /// <param name="staminaMax">Действующий максимум выносливости (для совместимости расчёта).</param>
        /// <param name="inHub">Отдых в хабе, а не вне его (на ставку не влияет).</param>
        /// <param name="recoveryBonus">Прибавка к ставке от талантов; 0 — без неё.</param>
        /// <returns>Выносливость за один час отдыха.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Максимум отрицателен.</exception>
        public int StaminaPerHour(int staminaMax, bool inHub, int recoveryBonus = 0)
        {
            if (staminaMax < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(staminaMax));
            }

            return PerHour + Math.Max(0, recoveryBonus);
        }

        /// <summary>
        /// Сколько часов нужно отдыхать, чтобы восстановить нехватку целиком:
        /// часы округляются вверх, чтобы ползунок доходил до полного восстановления.
        /// </summary>
        /// <param name="missing">Нехватка выносливости; не больше нуля — отдых не нужен.</param>
        /// <param name="staminaMax">Действующий максимум выносливости с бонусами.</param>
        /// <param name="inHub">Отдых в хабе, а не вне его.</param>
        /// <param name="recoveryBonus">Прибавка к ставке от талантов; 0 — без неё.</param>
        /// <returns>Предел часов отдыха; 0 — восстанавливать нечего.</returns>
        public int MaxHours(int missing, int staminaMax, bool inHub, int recoveryBonus = 0)
        {
            if (missing <= 0)
            {
                return 0;
            }

            var perHour = StaminaPerHour(staminaMax, inHub, recoveryBonus);
            return ((missing - 1) / perHour) + 1;
        }

        /// <summary>Шанс нападения за весь отдых вне хаба: прибавка за час × часы, не больше 1.</summary>
        /// <param name="hours">Выбранные часы отдыха.</param>
        /// <returns>Шанс нападения, 0..1.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Часы отрицательны.</exception>
        public double AttackChance(int hours)
        {
            if (hours < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(hours));
            }

            return Math.Min(1, RaidAttackChancePerHour * hours);
        }

        /// <summary>Читает блок <c>rest</c> записи топологии.</summary>
        /// <param name="value">Объект блока <c>rest</c>.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка отдыха.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static RestSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "rest");
            }

            var perHour = DataMap.OptionalInt(value, "staminaPerHour", 2);
            if (perHour < 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldRange, "rest.staminaPerHour");
            }

            var chance = DataMap.RequireDouble(value, "raidAttackChancePerHour", sourcePath);
            if (chance < 0 || chance > 1)
            {
                throw new DataException(sourcePath, CoreErrors.WorldChance, "rest.raidAttackChancePerHour");
            }

            return new RestSettings(perHour, chance);
        }
    }
}
