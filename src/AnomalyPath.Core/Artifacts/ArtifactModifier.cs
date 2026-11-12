#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>
    /// Модификатор артефакта или его доп. свойства: характеристика, величина со знаком
    /// и, для защиты, тип урона. Множители редкости применяются при чтении экземпляра.
    /// </summary>
    public sealed class ArtifactModifier
    {
        /// <summary>Создаёт модификатор.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <param name="value">Величина со знаком; ноль запрещён.</param>
        /// <param name="damageTypeId">Тип урона; обязателен только для <see cref="ArtifactStat.Protection"/>.</param>
        /// <param name="mode">Как складывать повтор; по умолчанию с убывающей отдачей.</param>
        /// <exception cref="ArgumentException">Величина равна нулю.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Нулевое значение или неверный набор полей.</exception>
        public ArtifactModifier(
            ArtifactStat stat,
            double value,
            string? damageTypeId = null,
            ArtifactModifierMode mode = ArtifactModifierMode.Diminishing)
        {
            if (value == 0 || double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }

            var protection = stat == ArtifactStat.Protection;
            if (protection && string.IsNullOrWhiteSpace(damageTypeId))
            {
                throw new ArgumentOutOfRangeException(nameof(damageTypeId));
            }

            if (!protection && !string.IsNullOrWhiteSpace(damageTypeId))
            {
                throw new ArgumentOutOfRangeException(nameof(damageTypeId));
            }

            Stat = stat;
            Value = value;
            DamageTypeId = protection ? damageTypeId : null;
            Mode = mode;
        }

        /// <summary>Характеристика.</summary>
        public ArtifactStat Stat { get; }

        /// <summary>Величина со знаком: положительная усиливает, отрицательная ослабляет.</summary>
        public double Value { get; }

        /// <summary>Тип урона у защиты; у остальных характеристик <c>null</c>.</summary>
        public string? DamageTypeId { get; }

        /// <summary>Как складывать повтор модификатора.</summary>
        public ArtifactModifierMode Mode { get; }

        /// <summary>Ключ модификатора: характеристика и тип урона.</summary>
        public string Key => ArtifactStatCodec.ToCode(Stat) + "|" + (DamageTypeId ?? "-");

        /// <summary>Умножает величину на множители редкости: плюсы на один, минусы на другой.</summary>
        /// <param name="positiveFactor">Множитель положительных значений, больше нуля.</param>
        /// <param name="negativeFactor">Множитель отрицательных значений, больше нуля.</param>
        /// <returns>Модификатор той же характеристики с новой величиной.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Множитель не положителен.</exception>
        public ArtifactModifier Scale(double positiveFactor, double negativeFactor)
        {
            if (positiveFactor <= 0 || negativeFactor <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(positiveFactor));
            }

            var factor = Value > 0 ? positiveFactor : negativeFactor;
            return new ArtifactModifier(Stat, Value * factor, DamageTypeId, Mode);
        }

        /// <summary>Читает модификатор из записи данных.</summary>
        /// <param name="value">JSON-объект модификатора.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Разобранный модификатор.</returns>
        /// <exception cref="DataException">Поля отсутствуют, неизвестны или заданы неверно.</exception>
        public static ArtifactModifier FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var statCode = DataMap.RequireString(value, "stat", sourcePath);
            if (!ArtifactStatCodec.TryParse(statCode, out var stat))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownStat, statCode);
            }

            var damageTypeId = DataMap.OptionalString(value, "damageTypeId");
            if (stat == ArtifactStat.Protection && string.IsNullOrWhiteSpace(damageTypeId))
            {
                throw new DataException(sourcePath, CoreErrors.MissingStringField, "damageTypeId");
            }

            if (stat != ArtifactStat.Protection && !string.IsNullOrWhiteSpace(damageTypeId))
            {
                throw new DataException(sourcePath, CoreErrors.ModifierDamageType, statCode);
            }

            var number = DataMap.RequireDouble(value, "value", sourcePath);
            if (number == 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonZero, statCode);
            }

            var mode = ArtifactModifierMode.Diminishing;
            var modeCode = DataMap.OptionalString(value, "mode");
            if (modeCode != null && !SchemaEnum.TryParse(modeCode, out mode))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownModifierMode, modeCode);
            }

            return new ArtifactModifier(stat, number, damageTypeId, mode);
        }
    }
}
