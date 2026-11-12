#nullable enable
using System;

namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Контекст боя вне локации: тир противника и ключ броска лута. Встреча на дороге
    /// между хабами не привязана к экземпляру локации, поэтому опыт и лут считаются
    /// по тиру противника, а объекты локации не трогаются.
    /// </summary>
    public sealed class CombatEncounterContext
    {
        /// <summary>Создаёт контекст боя.</summary>
        /// <param name="tier">Тир противника для опыта и лута; неотрицателен.</param>
        /// <param name="key">Ключ броска лута: пара хабов или id локации.</param>
        /// <param name="locationId">Экземпляр локации; <c>null</c> — бой вне локации (дорога).</param>
        /// <exception cref="ArgumentOutOfRangeException">Тир отрицателен.</exception>
        /// <exception cref="ArgumentException">Ключ пуст.</exception>
        public CombatEncounterContext(int tier, string key, string? locationId = null)
        {
            if (tier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tier));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(nameof(key));
            }

            Tier = tier;
            Key = key;
            LocationId = locationId;
        }

        /// <summary>Тир противника для опыта и лута.</summary>
        public int Tier { get; }

        /// <summary>Ключ броска лута.</summary>
        public string Key { get; }

        /// <summary>Экземпляр локации; <c>null</c> — бой вне локации.</summary>
        public string? LocationId { get; }

        /// <summary><c>true</c>, если бой идёт вне локации.</summary>
        public bool OnRoad => LocationId == null;
    }
}
