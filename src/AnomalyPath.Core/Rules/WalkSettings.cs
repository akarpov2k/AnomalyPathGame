#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Rules
{
    /// <summary>
    /// Правила бокового прохода рейда: скорость, длина дороги, раскладка объектов,
    /// перевод пути в минуты мира, фиксированный возврат в хаб, откат назад и временное
    /// отключение выносливости.
    /// </summary>
    public sealed class WalkSettings
    {
        /// <summary>Правила по умолчанию: дорога в два раза дольше прежней.</summary>
        public static WalkSettings Default { get; } = new WalkSettings(
            enabled: false,
            speed: 420,
            laneLength: 7200,
            laneMeters: 60,
            objectStart: 2200,
            objectGap: 500,
            minutesPerMeter: 0.5,
            returnMinutes: 30,
            useStamina: true,
            backtrackFactor: 3,
            runMultiplier: 2);

        /// <summary>Создаёт правила прохода.</summary>
        /// <param name="enabled">Включён ли режим похода.</param>
        /// <param name="speed">Скорость, единиц канваса в секунду; больше 0.</param>
        /// <param name="laneLength">Длина дороги в единицах канваса; больше 0.</param>
        /// <param name="laneMeters">Длина дороги в метрах мира; больше 0.</param>
        /// <param name="objectStart">Где стоит первый объект; не меньше 0.</param>
        /// <param name="objectGap">Расстояние между объектами; больше 0.</param>
        /// <param name="minutesPerMeter">Сколько минут мира стоит метр пути; не меньше 0.</param>
        /// <param name="returnMinutes">Фиксированное время возврата в хаб, минут; не меньше 0.</param>
        /// <param name="useStamina">Тратится ли выносливость на переходы; по умолчанию да.</param>
        /// <param name="backtrackFactor">Во сколько раз назад можно отойти от максимально пройденного; больше 0.</param>
        /// <param name="runMultiplier">Во сколько раз бег ускоряет шаг и расход; больше 0.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public WalkSettings(
            bool enabled,
            double speed,
            double laneLength,
            double laneMeters,
            double objectStart,
            double objectGap,
            double minutesPerMeter,
            double returnMinutes,
            bool useStamina,
            double backtrackFactor = 3,
            double runMultiplier = 2)
        {
            if (speed <= 0 || double.IsNaN(speed) || double.IsInfinity(speed))
            {
                throw new ArgumentOutOfRangeException(nameof(speed));
            }

            if (laneLength <= 0 || double.IsNaN(laneLength) || double.IsInfinity(laneLength))
            {
                throw new ArgumentOutOfRangeException(nameof(laneLength));
            }

            if (laneMeters <= 0 || double.IsNaN(laneMeters) || double.IsInfinity(laneMeters))
            {
                throw new ArgumentOutOfRangeException(nameof(laneMeters));
            }

            if (objectStart < 0 || double.IsNaN(objectStart) || double.IsInfinity(objectStart))
            {
                throw new ArgumentOutOfRangeException(nameof(objectStart));
            }

            if (objectGap <= 0 || double.IsNaN(objectGap) || double.IsInfinity(objectGap))
            {
                throw new ArgumentOutOfRangeException(nameof(objectGap));
            }

            if (minutesPerMeter < 0 || double.IsNaN(minutesPerMeter) || double.IsInfinity(minutesPerMeter))
            {
                throw new ArgumentOutOfRangeException(nameof(minutesPerMeter));
            }

            if (returnMinutes < 0 || double.IsNaN(returnMinutes) || double.IsInfinity(returnMinutes))
            {
                throw new ArgumentOutOfRangeException(nameof(returnMinutes));
            }

            if (backtrackFactor <= 0 || double.IsNaN(backtrackFactor) || double.IsInfinity(backtrackFactor))
            {
                throw new ArgumentOutOfRangeException(nameof(backtrackFactor));
            }

            if (runMultiplier <= 0 || double.IsNaN(runMultiplier) || double.IsInfinity(runMultiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(runMultiplier));
            }

            Enabled = enabled;
            Speed = speed;
            LaneLength = laneLength;
            LaneMeters = laneMeters;
            ObjectStart = objectStart;
            ObjectGap = objectGap;
            MinutesPerMeter = minutesPerMeter;
            ReturnMinutes = returnMinutes;
            UseStamina = useStamina;
            BacktrackFactor = backtrackFactor;
            RunMultiplier = runMultiplier;
        }

        /// <summary>Включён ли режим похода.</summary>
        public bool Enabled { get; }

        /// <summary>Скорость ходьбы в единицах канваса в секунду.</summary>
        public double Speed { get; }

        /// <summary>Длина дороги в единицах канваса.</summary>
        public double LaneLength { get; }

        /// <summary>Длина дороги в метрах мира.</summary>
        public double LaneMeters { get; }

        /// <summary>Где стоит первый объект на дороге.</summary>
        public double ObjectStart { get; }

        /// <summary>Расстояние между объектами.</summary>
        public double ObjectGap { get; }

        /// <summary>Сколько минут мира стоит метр пути.</summary>
        public double MinutesPerMeter { get; }

        /// <summary>Фиксированное время возврата в хаб, минут.</summary>
        public double ReturnMinutes { get; }

        /// <summary>Тратится ли выносливость на переходы.</summary>
        public bool UseStamina { get; }

        /// <summary>Во сколько раз назад можно отойти от максимально пройденного.</summary>
        public double BacktrackFactor { get; }

        /// <summary>Во сколько раз бег ускоряет шаг и расход.</summary>
        public double RunMultiplier { get; }

        /// <summary>Насколько далеко назад можно вернуться от максимального прохода.</summary>
        /// <param name="forwardMax">Максимальное пройденное расстояние; не меньше 0.</param>
        /// <returns>Минимальная позиция камеры.</returns>
        public double BackLimit(double forwardMax) =>
            Math.Max(0, forwardMax - (forwardMax / BacktrackFactor));

        /// <summary>Читает блок <c>walk</c>; отсутствующие поля получают значения по умолчанию.</summary>
        /// <param name="value">Объект блока <c>walk</c>.</param>
        /// <param name="path">Путь для сообщения об ошибке.</param>
        /// <returns>Правила прохода.</returns>
        /// <exception cref="DataException">Поле задано неверно.</exception>
        public static WalkSettings FromJson(JsonValue value, string path)
        {
            var enabled = DataMap.OptionalBool(value, "enabled", false);
            var speed = DataMap.OptionalDouble(value, "speed") ?? 420;
            var laneLength = DataMap.OptionalDouble(value, "laneLength") ?? 7200;
            var laneMeters = DataMap.OptionalDouble(value, "laneMeters") ?? 60;
            var objectStart = DataMap.OptionalDouble(value, "objectStart") ?? 2200;
            var objectGap = DataMap.OptionalDouble(value, "objectGap") ?? 500;
            var minutesPerMeter = DataMap.OptionalDouble(value, "minutesPerMeter") ?? 0.5;
            var returnMinutes = DataMap.OptionalDouble(value, "returnMinutes") ?? 30;
            var useStamina = DataMap.OptionalBool(value, "useStamina", true);
            var backtrackFactor = DataMap.OptionalDouble(value, "backtrackFactor") ?? 3;
            var runMultiplier = DataMap.OptionalDouble(value, "runMultiplier") ?? 2;

            if (speed <= 0 || laneLength <= 0 || laneMeters <= 0 || objectGap <= 0 ||
                objectStart < 0 || minutesPerMeter < 0 || returnMinutes < 0 || backtrackFactor <= 0 ||
                runMultiplier <= 0 ||
                double.IsNaN(speed) || double.IsNaN(laneLength) ||
                double.IsNaN(minutesPerMeter) || double.IsNaN(returnMinutes) ||
                double.IsNaN(backtrackFactor) || double.IsNaN(runMultiplier))
            {
                throw new DataException(path, CoreErrors.RulesRange, "walk");
            }

            return new WalkSettings(
                enabled,
                speed,
                laneLength,
                laneMeters,
                objectStart,
                objectGap,
                minutesPerMeter,
                returnMinutes,
                useStamina,
                backtrackFactor,
                runMultiplier);
        }
    }
}
