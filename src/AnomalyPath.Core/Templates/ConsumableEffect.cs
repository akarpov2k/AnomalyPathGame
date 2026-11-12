#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Templates
{
    /// <summary>
    /// Один эффект расходника: лечение, восстановление ОД, восполнение еды или воды,
    /// либо временный модификатор бойца. Формат общий со скиллами, но набор типов
    /// у расходников свой.
    /// </summary>
    public sealed class ConsumableEffect
    {
        private ConsumableEffect(
            ConsumableEffectType type,
            int amount,
            ConsumableStat? stat,
            double value,
            string? damageTypeId)
        {
            Type = type;
            Amount = amount;
            Stat = stat;
            Value = value;
            DamageTypeId = damageTypeId;
        }

        /// <summary>Тип эффекта.</summary>
        public ConsumableEffectType Type { get; }

        /// <summary>
        /// Сколько здоровья лечит, сколько ОД восстанавливает или сколько еды/воды восполняет;
        /// у модификатора 0.
        /// </summary>
        public int Amount { get; }

        /// <summary>Характеристика модификатора; <c>null</c> у лечения и восстановления ОД.</summary>
        public ConsumableStat? Stat { get; }

        /// <summary>Величина модификатора; 0 у лечения и восстановления ОД.</summary>
        public double Value { get; }

        /// <summary>Тип урона для модификатора защиты; <c>null</c> у остальных эффектов.</summary>
        public string? DamageTypeId { get; }

        /// <summary>Создаёт эффект лечения здоровья.</summary>
        /// <param name="amount">Сколько здоровья лечит; больше нуля.</param>
        /// <returns>Эффект лечения.</returns>
        public static ConsumableEffect Heal(int amount) =>
            new ConsumableEffect(ConsumableEffectType.Heal, amount, null, 0, null);

        /// <summary>Создаёт эффект восстановления очков действия.</summary>
        /// <param name="amount">Сколько ОД восстанавливает; больше нуля.</param>
        /// <returns>Эффект восстановления ОД.</returns>
        public static ConsumableEffect RestoreAp(int amount) =>
            new ConsumableEffect(ConsumableEffectType.RestoreAp, amount, null, 0, null);

        /// <summary>Создаёт эффект восполнения еды.</summary>
        /// <param name="amount">Сколько еды восполняет; больше нуля.</param>
        /// <returns>Эффект восполнения еды.</returns>
        public static ConsumableEffect RestoreFood(int amount) =>
            new ConsumableEffect(ConsumableEffectType.RestoreFood, amount, null, 0, null);

        /// <summary>Создаёт эффект восполнения воды.</summary>
        /// <param name="amount">Сколько воды восполняет; больше нуля.</param>
        /// <returns>Эффект восполнения воды.</returns>
        public static ConsumableEffect RestoreWater(int amount) =>
            new ConsumableEffect(ConsumableEffectType.RestoreWater, amount, null, 0, null);

        /// <summary>Создаёт эффект временного модификатора бойца.</summary>
        /// <param name="stat">Характеристика.</param>
        /// <param name="value">Величина; не ноль.</param>
        /// <param name="damageTypeId">Тип урона для защиты; обязателен для <see cref="ConsumableStat.Protection"/>.</param>
        /// <returns>Эффект модификатора.</returns>
        public static ConsumableEffect Modifier(ConsumableStat stat, double value, string? damageTypeId = null) =>
            new ConsumableEffect(ConsumableEffectType.Modifier, 0, stat, value, damageTypeId);

        /// <summary>Читает эффект расходника из записи.</summary>
        /// <param name="value">Загруженный эффект.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Эффект расходника.</returns>
        /// <exception cref="DataException">Поля заданы неверно.</exception>
        public static ConsumableEffect FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var typeCode = DataMap.RequireString(value, "type", sourcePath);
            if (!ConsumableEffectCodec.TryParse(typeCode, out var type))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownConsumableEffect, typeCode);
            }

            if (type == ConsumableEffectType.Modifier)
            {
                var statCode = DataMap.RequireString(value, "stat", sourcePath);
                if (!ConsumableStatCodec.TryParse(statCode, out var stat))
                {
                    throw new DataException(sourcePath, CoreErrors.UnknownConsumableStat, statCode);
                }

                var modifierValue = DataMap.RequireDouble(value, "value", sourcePath);
                if (modifierValue == 0 ||
                    (ConsumableStatCodec.IsInteger(stat) && modifierValue != System.Math.Round(modifierValue)))
                {
                    throw new DataException(sourcePath, CoreErrors.ConsumableStatValue, statCode);
                }

                var damageTypeId = DataMap.OptionalString(value, "damageTypeId");
                if (stat == ConsumableStat.Protection && string.IsNullOrWhiteSpace(damageTypeId))
                {
                    throw new DataException(sourcePath, CoreErrors.ConsumableProtectionType, statCode);
                }

                return Modifier(stat, modifierValue, damageTypeId);
            }

            var amount = DataMap.RequireInt(value, "amount", sourcePath);
            if (amount <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.ConsumableEffectAmount, amount);
            }

            return type switch
            {
                ConsumableEffectType.Heal => Heal(amount),
                ConsumableEffectType.RestoreAp => RestoreAp(amount),
                ConsumableEffectType.RestoreFood => RestoreFood(amount),
                ConsumableEffectType.RestoreWater => RestoreWater(amount),
                _ => throw new DataException(sourcePath, CoreErrors.UnknownConsumableEffect, typeCode)
            };
        }
    }
}
