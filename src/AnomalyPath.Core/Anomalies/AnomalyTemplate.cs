#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Anomalies
{
    /// <summary>
    /// Аномалия как источник артефактов: тип артефактов, фиксированный урон, число попыток,
    /// шанс находки, пул артефактов и тиры локаций, где она может появиться.
    /// </summary>
    public sealed class AnomalyTemplate
    {
        private readonly IReadOnlyList<int> _tiers;

        /// <summary>Создаёт аномалию.</summary>
        /// <param name="id">Идентификатор аномалии.</param>
        /// <param name="artifactType">Тип артефактов аномалии.</param>
        /// <param name="damageTypeId">Тип урона аномалии.</param>
        /// <param name="damage">Фиксированный урон за попытку, не меньше нуля.</param>
        /// <param name="attempts">Число попыток поиска, не меньше одной.</param>
        /// <param name="artifactChance">Шанс находки за попытку, 0..1.</param>
        /// <param name="artifactTableId">Таблица лута с артефактами аномалии.</param>
        /// <param name="tiers">Тиры локаций, где аномалия может появиться; не пусто.</param>
        /// <exception cref="ArgumentException">Обязательная строка пуста.</exception>
        /// <exception cref="ArgumentNullException">Список тиров не задан.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Число вне допустимых границ.</exception>
        public AnomalyTemplate(
            string id,
            string artifactType,
            string damageTypeId,
            int damage,
            int attempts,
            double artifactChance,
            string artifactTableId,
            IReadOnlyList<int> tiers)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            if (string.IsNullOrWhiteSpace(artifactType))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(artifactType)),
                    nameof(artifactType));
            }

            if (string.IsNullOrWhiteSpace(damageTypeId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(damageTypeId)),
                    nameof(damageTypeId));
            }

            if (string.IsNullOrWhiteSpace(artifactTableId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(artifactTableId)),
                    nameof(artifactTableId));
            }

            if (damage < 0 || attempts < 1 || artifactChance < 0 || artifactChance > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(damage));
            }

            _tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            if (_tiers.Count == 0)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.EmptyCollection, "tiers"),
                    nameof(tiers));
            }

            Id = id;
            ArtifactType = artifactType;
            DamageTypeId = damageTypeId;
            Damage = damage;
            Attempts = attempts;
            ArtifactChance = artifactChance;
            ArtifactTableId = artifactTableId;
        }

        /// <summary>Идентификатор аномалии.</summary>
        public string Id { get; }

        /// <summary>Тип артефактов аномалии: <c>fire</c>, <c>electric</c>, <c>chemical</c>, <c>gravity</c>.</summary>
        public string ArtifactType { get; }

        /// <summary>Тип урона аномалии.</summary>
        public string DamageTypeId { get; }

        /// <summary>Фиксированный урон за попытку.</summary>
        public int Damage { get; }

        /// <summary>Число попыток поиска.</summary>
        public int Attempts { get; }

        /// <summary>Шанс найти артефакт за попытку.</summary>
        public double ArtifactChance { get; }

        /// <summary>Таблица лута с артефактами аномалии.</summary>
        public string ArtifactTableId { get; }

        /// <summary>Тиры локаций, где аномалия может появиться.</summary>
        public IReadOnlyList<int> Tiers => _tiers;

        /// <summary>Проверяет, может ли аномалия появиться на локации указанного тира.</summary>
        /// <param name="tier">Тир локации.</param>
        /// <returns><c>true</c>, если тир входит в список аномалии.</returns>
        public bool AllowsTier(int tier)
        {
            foreach (var value in _tiers)
            {
                if (value == tier)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Читает аномалию из записи данных.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Разобранная аномалия.</returns>
        /// <exception cref="DataException">Поля отсутствуют, значения неизвестны или вне границ.</exception>
        public static AnomalyTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var artifactType = DataMap.RequireString(value, "artifactType", sourcePath);
            var damageTypeId = DataMap.RequireString(value, "damageTypeId", sourcePath);
            var damage = DataMap.OptionalInt(value, "damage", 0);
            if (damage < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "damage");
            }

            var attempts = DataMap.RequireInt(value, "attempts", sourcePath);
            if (attempts < 1)
            {
                throw new DataException(sourcePath, CoreErrors.MinimumOne, "attempts");
            }

            var artifactChance = DataMap.RequireDouble(value, "artifactChance", sourcePath);
            if (artifactChance < 0 || artifactChance > 1)
            {
                throw new DataException(sourcePath, CoreErrors.ChanceRange, "artifactChance");
            }

            var artifactTable = DataMap.RequireString(value, "artifactTable", sourcePath);
            var tiers = ReadTiers(value, sourcePath);
            return new AnomalyTemplate(
                id,
                artifactType,
                damageTypeId,
                damage,
                attempts,
                artifactChance,
                artifactTable,
                tiers);
        }

        private static IReadOnlyList<int> ReadTiers(JsonValue value, string sourcePath)
        {
            if (!value.TryGet("tiers", out var array) || !array.IsArray)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "tiers");
            }

            var tiers = new List<int>();
            foreach (var item in array.Items)
            {
                if (item.Kind != JsonKind.Number || item.Number < 0)
                {
                    throw new DataException(sourcePath, CoreErrors.AnomalyTiers, "tiers");
                }

                var tier = (int)Math.Round(item.Number, MidpointRounding.AwayFromZero);
                if (Math.Abs(item.Number - tier) > 1e-9 || tiers.Contains(tier))
                {
                    throw new DataException(
                        sourcePath,
                        CoreErrors.AnomalyTiers,
                        item.Number.ToString(CultureInfo.InvariantCulture));
                }

                tiers.Add(tier);
            }

            if (tiers.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.AnomalyTiers, "empty");
            }

            return tiers;
        }
    }
}
