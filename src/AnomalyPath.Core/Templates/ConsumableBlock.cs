#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Templates
{
    /// <summary>
    /// Блок расходника: цена применения в ОД, число применений, разрешение боя,
    /// эффекты и травмы, которые предмет снимает.
    /// </summary>
    public sealed class ConsumableBlock
    {
        /// <summary>Создаёт блок расходника.</summary>
        /// <param name="useTime">Цена применения в бою, в ОД; 0 — мгновенно.</param>
        /// <param name="charges">Сколько применений выдерживает предмет; не меньше одного.</param>
        /// <param name="usableInCombat">Можно ли применять в бою и класть в быстрый слот.</param>
        /// <param name="effects">Эффекты применения.</param>
        /// <param name="healsInjuries">Id травм, которые предмет снимает.</param>
        /// <exception cref="ArgumentNullException">Списки не переданы.</exception>
        public ConsumableBlock(
            int useTime,
            int charges,
            bool usableInCombat,
            IReadOnlyList<ConsumableEffect> effects,
            IReadOnlyList<string> healsInjuries)
        {
            UseTime = useTime;
            Charges = charges;
            UsableInCombat = usableInCombat;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            HealsInjuries = healsInjuries ?? throw new ArgumentNullException(nameof(healsInjuries));
        }

        /// <summary>Цена применения в бою, в ОД; 0 — мгновенно.</summary>
        public int UseTime { get; }

        /// <summary>Сколько применений выдерживает предмет.</summary>
        public int Charges { get; }

        /// <summary>
        /// <c>true</c>, если предмет можно применять в бою и держать в слоте быстрого доступа.
        /// Еда, вода и медицина «только вне боя» имеют <c>false</c>.
        /// </summary>
        public bool UsableInCombat { get; }

        /// <summary>Эффекты применения.</summary>
        public IReadOnlyList<ConsumableEffect> Effects { get; }

        /// <summary>Id травм, которые предмет снимает: расходник перечисляет травмы, а не части тела.</summary>
        public IReadOnlyList<string> HealsInjuries { get; }

        /// <summary>Сколько здоровья лечит предмет за одно применение.</summary>
        /// <returns>Сумма эффектов лечения.</returns>
        public int TotalHeal() => Total(ConsumableEffectType.Heal);

        /// <summary>Сколько очков действия восстанавливает предмет за одно применение.</summary>
        /// <returns>Сумма эффектов восстановления ОД.</returns>
        public int TotalApRestore() => Total(ConsumableEffectType.RestoreAp);

        /// <summary>Сколько еды восполняет предмет за одно применение.</summary>
        /// <returns>Сумма эффектов восполнения еды.</returns>
        public int TotalFoodRestore() => Total(ConsumableEffectType.RestoreFood);

        /// <summary>Сколько воды восполняет предмет за одно применение.</summary>
        /// <returns>Сумма эффектов восполнения воды.</returns>
        public int TotalWaterRestore() => Total(ConsumableEffectType.RestoreWater);

        private int Total(ConsumableEffectType type)
        {
            var total = 0;
            foreach (var effect in Effects)
            {
                if (effect.Type == type)
                {
                    total += effect.Amount;
                }
            }

            return total;
        }

        /// <summary>Читает блок расходника из записи предмета.</summary>
        /// <param name="value">Блок <c>consumable</c>.</param>
        /// <param name="id">Id предмета для сообщения.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Блок расходника.</returns>
        /// <exception cref="DataException">Поля заданы неверно или эффектов нет.</exception>
        public static ConsumableBlock FromJson(JsonValue value, string id, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.ConsumableWithoutBlock, id);
            }

            var useTime = DataMap.RequireInt(value, "useTime", sourcePath);
            if (useTime < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "useTime");
            }

            var charges = DataMap.OptionalInt(value, "charges", 1);
            if (charges < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "charges");
            }

            var usableInCombat = DataMap.OptionalBool(value, "usableInCombat", false);
            var effects = ReadEffects(value, sourcePath);
            var healsInjuries = ReadInjuries(value, sourcePath);
            if (effects.Count == 0 && healsInjuries.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.ConsumableNoEffects, id);
            }

            return new ConsumableBlock(useTime, charges, usableInCombat, effects, healsInjuries);
        }

        private static IReadOnlyList<ConsumableEffect> ReadEffects(JsonValue value, string sourcePath)
        {
            var effects = new List<ConsumableEffect>();
            if (!value.TryGet("effects", out var field) || !field.IsArray)
            {
                return effects;
            }

            foreach (var item in field.Items)
            {
                effects.Add(ConsumableEffect.FromJson(item, sourcePath));
            }

            return effects;
        }

        private static IReadOnlyList<string> ReadInjuries(JsonValue value, string sourcePath)
        {
            var injuries = new List<string>();
            if (!value.TryGet("healsInjuries", out var field) || !field.IsArray)
            {
                return injuries;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in field.Items)
            {
                if (item.Kind != JsonKind.String)
                {
                    throw new DataException(sourcePath, CoreErrors.ArrayItemNotString, "healsInjuries");
                }

                if (!seen.Add(item.Text))
                {
                    throw new DataException(sourcePath, CoreErrors.DuplicateInjuryId, item.Text);
                }

                injuries.Add(item.Text);
            }

            return injuries;
        }
    }
}
