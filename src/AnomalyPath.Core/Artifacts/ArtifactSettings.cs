#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>
    /// Настройка редкости артефактов: одна таблица шансов на все артефакты, множители
    /// положительных и отрицательных значений, число доп. свойств и затухание модификаторов.
    /// </summary>
    public sealed class ArtifactSettings
    {
        /// <summary>Вид записи в данных.</summary>
        public const string RecordId = "artifactSettings";

        /// <summary>Единственный допустимый id настройки.</summary>
        public const string IdValue = "artifacts";

        /// <summary>Погрешность суммы шансов редкости.</summary>
        private const double ChanceEpsilon = 1e-6;

        private readonly IReadOnlyList<ArtifactRarityRule> _rarities;
        private readonly Dictionary<Rarity, ArtifactRarityRule> _byRarity;

        /// <summary>
        /// Настройка по умолчанию: без множителей, все находки обычные. Нужна данным без файла
        /// `artifacts.json` (тестовые наборы); в контенте таблица задана явно.
        /// </summary>
        public static ArtifactSettings Default { get; } = new ArtifactSettings(1, DefaultRules());

        /// <summary>Создаёт настройку редкости артефактов.</summary>
        /// <param name="falloff">Коэффициент убывающей отдачи модификаторов, (0, 1].</param>
        /// <param name="rarities">Таблица ступеней без повторов.</param>
        /// <exception cref="ArgumentNullException">Таблица не задана.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Коэффициент вне диапазона.</exception>
        public ArtifactSettings(double falloff, IReadOnlyList<ArtifactRarityRule> rarities)
        {
            if (rarities == null)
            {
                throw new ArgumentNullException(nameof(rarities));
            }

            if (falloff <= 0 || falloff > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(falloff));
            }

            Falloff = falloff;
            _rarities = rarities;
            _byRarity = new Dictionary<Rarity, ArtifactRarityRule>();
            foreach (var rule in rarities)
            {
                _byRarity[rule.Rarity] = rule;
            }
        }

        /// <summary>Коэффициент убывающей отдачи одинаковых модификаторов.</summary>
        public double Falloff { get; }

        /// <summary>Таблица ступеней в порядке данных.</summary>
        public IReadOnlyList<ArtifactRarityRule> Rarities => _rarities;

        /// <summary>Ищет правило ступени.</summary>
        /// <param name="rarity">Ступень редкости.</param>
        /// <param name="rule">Правило или <c>null</c>.</param>
        /// <returns><c>true</c>, если ступень описана в таблице.</returns>
        public bool TryGet(Rarity rarity, out ArtifactRarityRule? rule) => _byRarity.TryGetValue(rarity, out rule);

        /// <summary>Возвращает правило ступени.</summary>
        /// <param name="rarity">Ступень редкости.</param>
        /// <returns>Правило редкости.</returns>
        /// <exception cref="DataException">Ступень не описана в таблице.</exception>
        public ArtifactRarityRule Get(Rarity rarity) =>
            _byRarity.TryGetValue(rarity, out var rule)
                ? rule
                : throw new DataException(RecordId, CoreErrors.RarityMissing, SchemaEnum.ToCode(rarity));

        /// <summary>Бросает ступень редкости по таблице шансов.</summary>
        /// <param name="random">Детерминированный источник бросков.</param>
        /// <returns>Выпавшая ступень.</returns>
        /// <exception cref="ArgumentNullException">Источник бросков не задан.</exception>
        public ArtifactRarityRule RollRarity(SeededWorldRandom random)
        {
            if (random == null)
            {
                throw new ArgumentNullException(nameof(random));
            }

            if (_rarities.Count == 0)
            {
                throw new DataException(RecordId, CoreErrors.EmptyCollection, "rarities");
            }

            var roll = random.NextDouble();
            var accumulated = 0.0;
            foreach (var rule in _rarities)
            {
                accumulated += rule.Chance;
                if (roll < accumulated)
                {
                    return rule;
                }
            }

            return _rarities[_rarities.Count - 1];
        }

        /// <summary>Правила по умолчанию: все находки обычные, множители 1, доп. свойств нет.</summary>
        private static IReadOnlyList<ArtifactRarityRule> DefaultRules()
        {
            var rules = new List<ArtifactRarityRule>();
            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                var chance = rarity == Rarity.Common ? 1 : 0;
                rules.Add(new ArtifactRarityRule(rarity, chance, 1, 1, 0, 0));
            }

            return rules;
        }

        /// <summary>Читает настройку редкости артефактов из данных.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка редкости.</returns>
        /// <exception cref="DataException">Поля отсутствуют, значения неизвестны или вне границ.</exception>
        public static ArtifactSettings FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            if (!string.Equals(id, IdValue, StringComparison.Ordinal))
            {
                throw new DataException(sourcePath, CoreErrors.ArtifactSettingsId, id);
            }

            var falloff = DataMap.OptionalDouble(value, "falloff") ?? 1;
            if (falloff <= 0 || falloff > 1)
            {
                throw new DataException(sourcePath, CoreErrors.RangeOrder, "falloff");
            }

            if (!value.TryGet("rarities", out var array) || !array.IsArray)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "rarities");
            }

            var rules = new List<ArtifactRarityRule>();
            var seen = new HashSet<Rarity>();
            var total = 0.0;
            foreach (var item in array.Items)
            {
                var rule = ArtifactRarityRule.FromJson(item, sourcePath);
                if (!seen.Add(rule.Rarity))
                {
                    throw new DataException(
                        sourcePath,
                        CoreErrors.DuplicateEntity,
                        SchemaEnum.ToCode(rule.Rarity));
                }

                rules.Add(rule);
                total += rule.Chance;
            }

            foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
            {
                if (!seen.Contains(rarity))
                {
                    throw new DataException(sourcePath, CoreErrors.RarityMissing, SchemaEnum.ToCode(rarity));
                }
            }

            if (Math.Abs(total - 1) > ChanceEpsilon)
            {
                throw new DataException(sourcePath, CoreErrors.ArtifactRarityChances, total);
            }

            return new ArtifactSettings(falloff, rules);
        }
    }
}
