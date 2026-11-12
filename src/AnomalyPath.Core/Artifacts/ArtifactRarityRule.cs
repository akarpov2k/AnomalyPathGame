#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>Строка таблицы редкости артефактов: шанс ступени, множители и число доп. свойств.</summary>
    public sealed class ArtifactRarityRule
    {
        /// <summary>Создаёт правило редкости артефактов.</summary>
        /// <param name="rarity">Ступень редкости.</param>
        /// <param name="chance">Шанс выпадения ступени, 0..1.</param>
        /// <param name="positiveFactor">Множитель положительных значений, больше нуля.</param>
        /// <param name="negativeFactor">Множитель отрицательных значений, больше нуля.</param>
        /// <param name="extraMin">Минимум доп. свойств, не меньше нуля.</param>
        /// <param name="extraMax">Максимум доп. свойств, не меньше минимума.</param>
        public ArtifactRarityRule(
            Rarity rarity,
            double chance,
            double positiveFactor,
            double negativeFactor,
            int extraMin,
            int extraMax)
        {
            Rarity = rarity;
            Chance = chance;
            PositiveFactor = positiveFactor;
            NegativeFactor = negativeFactor;
            ExtraMin = extraMin;
            ExtraMax = extraMax;
        }

        /// <summary>Ступень редкости.</summary>
        public Rarity Rarity { get; }

        /// <summary>Шанс выпадения ступени для найденного артефакта.</summary>
        public double Chance { get; }

        /// <summary>Множитель положительных значений модификаторов.</summary>
        public double PositiveFactor { get; }

        /// <summary>Множитель отрицательных значений модификаторов.</summary>
        public double NegativeFactor { get; }

        /// <summary>Минимум доп. свойств из пула артефакта.</summary>
        public int ExtraMin { get; }

        /// <summary>Максимум доп. свойств из пула артефакта.</summary>
        public int ExtraMax { get; }

        /// <summary>Читает правило из записи таблицы.</summary>
        /// <param name="value">JSON-объект правила.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Разобранное правило.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static ArtifactRarityRule FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var rarityCode = DataMap.RequireString(value, "rarity", sourcePath);
            if (!SchemaEnum.TryParse(rarityCode, out Rarity rarity))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownRarity, rarityCode);
            }

            var chance = DataMap.RequireDouble(value, "chance", sourcePath);
            if (chance < 0 || chance > 1)
            {
                throw new DataException(sourcePath, CoreErrors.ChanceRange, "chance");
            }

            var positive = DataMap.RequireDouble(value, "positiveFactor", sourcePath);
            var negative = DataMap.RequireDouble(value, "negativeFactor", sourcePath);
            if (positive <= 0 || negative <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.PositiveValue, "factor");
            }

            var extraMin = DataMap.OptionalInt(value, "extraMin", 0);
            var extraMax = DataMap.OptionalInt(value, "extraMax", 0);
            if (extraMin < 0 || extraMax < extraMin)
            {
                throw new DataException(sourcePath, CoreErrors.RangeOrder, "extra");
            }

            return new ArtifactRarityRule(rarity, chance, positive, negative, extraMin, extraMax);
        }
    }
}
