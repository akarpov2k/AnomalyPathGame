#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Правила радара поиска артефакта: общий размер поля, число сканов и пороги сигнала по тирам.
    /// Поле фиксировано для всех тиров: ширина и высота заданы один раз в блоке данных.
    /// </summary>
    public sealed class AnomalyFieldSettings
    {
        private readonly IReadOnlyList<AnomalyFieldTier> _tiers;

        /// <summary>Создаёт настройку радара.</summary>
        /// <param name="width">Ширина поля, от 2 до 10.</param>
        /// <param name="height">Высота поля, от 2 до 10.</param>
        /// <param name="tiers">Параметры поля по тирам; хотя бы один тир.</param>
        /// <exception cref="ArgumentOutOfRangeException">Размер поля вне границ.</exception>
        /// <exception cref="ArgumentNullException">Список тиров не задан.</exception>
        /// <exception cref="ArgumentException">Список пуст или тиры повторяются.</exception>
        public AnomalyFieldSettings(int width, int height, IReadOnlyList<AnomalyFieldTier> tiers)
        {
            if (width < 2 || width > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height < 2 || height > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            _tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            if (_tiers.Count == 0)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RulesRange, "anomalyField.tiers"),
                    nameof(tiers));
            }

            var seen = new HashSet<int>();
            foreach (var tier in _tiers)
            {
                if (!seen.Add(tier.Tier))
                {
                    throw new ArgumentException(
                        CoreText.Format(CoreErrors.RulesRange, "anomalyField.tiers"),
                        nameof(tiers));
                }
            }

            Width = width;
            Height = height;
        }

        /// <summary>Ширина поля в клетках; одинакова для всех тиров.</summary>
        public int Width { get; }

        /// <summary>Высота поля в клетках; одинакова для всех тиров.</summary>
        public int Height { get; }

        /// <summary>Параметры поля по тирам локаций.</summary>
        public IReadOnlyList<AnomalyFieldTier> Tiers => _tiers;

        /// <summary>Параметры для тира локации: точный тир, иначе ближайший меньший, иначе первый.</summary>
        /// <param name="locationTier">Тир локации.</param>
        /// <returns>Настройка поля для тира.</returns>
        public AnomalyFieldTier ForTier(int locationTier)
        {
            AnomalyFieldTier? nearest = null;
            var nearestDistance = int.MaxValue;
            foreach (var tier in _tiers)
            {
                if (tier.Tier == locationTier)
                {
                    return tier;
                }

                var distance = Math.Abs(tier.Tier - locationTier);
                if (distance < nearestDistance ||
                    (distance == nearestDistance && tier.Tier < (nearest?.Tier ?? int.MaxValue)))
                {
                    nearestDistance = distance;
                    nearest = tier;
                }
            }

            return nearest!;
        }

        /// <summary>Читает блок <c>anomalyField</c> записи правил.</summary>
        /// <param name="value">Объект блока.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка радара.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static AnomalyFieldSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "anomalyField");
            }

            var width = DataMap.RequireInt(value, "width", sourcePath);
            var height = DataMap.RequireInt(value, "height", sourcePath);
            if (width < 2 || width > 10 || height < 2 || height > 10)
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "anomalyField.width");
            }

            if (!value.TryGet("tiers", out var tiersValue) || !tiersValue.IsArray ||
                tiersValue.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "anomalyField.tiers");
            }

            var tiers = new List<AnomalyFieldTier>(tiersValue.Items.Count);
            var seen = new HashSet<int>();
            foreach (var item in tiersValue.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var tier = DataMap.RequireInt(item, "tier", sourcePath);
                var scans = DataMap.RequireInt(item, "scans", sourcePath);
                if (tier < 0 || scans < 1 || scans > 50)
                {
                    throw new DataException(sourcePath, CoreErrors.RulesRange, "anomalyField.tiers");
                }

                if (!seen.Add(tier))
                {
                    throw new DataException(sourcePath, CoreErrors.RulesRange, "anomalyField.tiers");
                }

                if (!item.TryGet("bands", out var bandsValue) || !bandsValue.IsArray ||
                    bandsValue.Items.Count != 3)
                {
                    throw new DataException(sourcePath, CoreErrors.MissingArrayField, "anomalyField.bands");
                }

                var bands = new List<int>(3);
                foreach (var band in bandsValue.Items)
                {
                    if (band.Kind != JsonKind.Number)
                    {
                        throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                    }

                    bands.Add((int)Math.Round(band.Number, MidpointRounding.AwayFromZero));
                }

                if (bands.Count != 3 || bands[0] < 0 || bands[0] >= bands[1] || bands[1] >= bands[2])
                {
                    throw new DataException(sourcePath, CoreErrors.RulesRange, "anomalyField.bands");
                }

                tiers.Add(new AnomalyFieldTier(tier, scans, bands));
            }

            return new AnomalyFieldSettings(width, height, tiers);
        }
    }
}
