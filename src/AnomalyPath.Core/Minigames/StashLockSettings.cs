#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Правила кодового замка тайника: общее число попыток и сложность по тирам локаций.
    /// </summary>
    public sealed class StashLockSettings
    {
        private readonly IReadOnlyList<StashLockTier> _tiers;

        /// <summary>Создаёт настройку взлома.</summary>
        /// <param name="attempts">Сколько неверных попыток допускается, от 1 до 20.</param>
        /// <param name="tiers">Сложность по тирам; хотя бы один тир.</param>
        /// <exception cref="ArgumentOutOfRangeException">Число попыток вне границ.</exception>
        /// <exception cref="ArgumentNullException">Список тиров не задан.</exception>
        /// <exception cref="ArgumentException">Тиры повторяются или список пуст.</exception>
        public StashLockSettings(int attempts, IReadOnlyList<StashLockTier> tiers)
        {
            if (attempts < 1 || attempts > 20)
            {
                throw new ArgumentOutOfRangeException(nameof(attempts));
            }

            _tiers = tiers ?? throw new ArgumentNullException(nameof(tiers));
            if (_tiers.Count == 0)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RulesRange, "stashLock.tiers"),
                    nameof(tiers));
            }

            var seen = new HashSet<int>();
            foreach (var tier in _tiers)
            {
                if (!seen.Add(tier.Tier))
                {
                    throw new ArgumentException(
                        CoreText.Format(CoreErrors.RulesRange, "stashLock.tiers"),
                        nameof(tiers));
                }
            }

            Attempts = attempts;
        }

        /// <summary>Сколько попыток даётся на один тайник.</summary>
        public int Attempts { get; }

        /// <summary>Сложность по тирам локаций.</summary>
        public IReadOnlyList<StashLockTier> Tiers => _tiers;

        /// <summary>Сложность для тира локации: точный тир, иначе ближайший меньший, иначе первый.</summary>
        /// <param name="locationTier">Тир локации.</param>
        /// <returns>Настройка кода для тира.</returns>
        public StashLockTier ForTier(int locationTier)
        {
            StashLockTier? nearest = null;
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

        /// <summary>Читает блок <c>stashLock</c> записи правил.</summary>
        /// <param name="value">Объект блока.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка взлома.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static StashLockSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "stashLock");
            }

            var attempts = DataMap.RequireInt(value, "attempts", sourcePath);
            if (attempts < 1 || attempts > 20)
            {
                throw new DataException(sourcePath, CoreErrors.RulesRange, "stashLock.attempts");
            }

            if (!value.TryGet("tiers", out var tiersValue) || !tiersValue.IsArray ||
                tiersValue.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "stashLock.tiers");
            }

            var tiers = new List<StashLockTier>(tiersValue.Items.Count);
            var seen = new HashSet<int>();
            foreach (var item in tiersValue.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var tier = DataMap.RequireInt(item, "tier", sourcePath);
                var codeLength = DataMap.RequireInt(item, "codeLength", sourcePath);
                var alphabet = DataMap.RequireInt(item, "alphabetSize", sourcePath);
                if (tier < 0 || codeLength < 1 || codeLength > 8 ||
                    alphabet < 2 || alphabet > 9 || alphabet < codeLength)
                {
                    throw new DataException(sourcePath, CoreErrors.RulesRange, "stashLock.tiers");
                }

                if (!seen.Add(tier))
                {
                    throw new DataException(sourcePath, CoreErrors.RulesRange, "stashLock.tiers");
                }

                tiers.Add(new StashLockTier(tier, codeLength, alphabet));
            }

            return new StashLockSettings(attempts, tiers);
        }
    }
}
