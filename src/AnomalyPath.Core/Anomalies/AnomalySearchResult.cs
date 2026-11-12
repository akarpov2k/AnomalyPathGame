#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;

namespace AnomalyPath.Core.Anomalies
{
    /// <summary>Результат попытки поиска: урон, найденные артефакты и остаток попыток.</summary>
    public sealed class AnomalySearchResult
    {
        /// <summary>Создаёт результат попытки.</summary>
        /// <param name="status">Итог попытки.</param>
        /// <param name="damage">Нанесённый урон; <c>null</c>, если попытка не выполнена.</param>
        /// <param name="artifacts">Найденные артефакты.</param>
        /// <param name="attemptsLeft">Сколько попыток осталось.</param>
        /// <exception cref="ArgumentNullException">Список артефактов не задан.</exception>
        public AnomalySearchResult(
            AnomalySearchStatus status,
            PlayerDamageResult? damage,
            IReadOnlyList<ItemInstance>? artifacts,
            int attemptsLeft)
        {
            Status = status;
            Damage = damage;
            Artifacts = artifacts ?? Array.Empty<ItemInstance>();
            AttemptsLeft = attemptsLeft;
        }

        /// <summary>Итог попытки.</summary>
        public AnomalySearchStatus Status { get; }

        /// <summary>Нанесённый урон; <c>null</c>, если попытка не выполнена.</summary>
        public PlayerDamageResult? Damage { get; }

        /// <summary>Найденные артефакты.</summary>
        public IReadOnlyList<ItemInstance> Artifacts { get; }

        /// <summary>Сколько попыток осталось.</summary>
        public int AttemptsLeft { get; }

        /// <summary>Найден хотя бы один артефакт.</summary>
        public bool Found => Artifacts.Count > 0;

        /// <summary>Игрок погиб от урона аномалии.</summary>
        public bool Died => Damage != null && Damage.Died;

        /// <summary>Попытка не выполнена.</summary>
        /// <param name="status">Причина отказа.</param>
        /// <returns>Результат без урона и находок.</returns>
        public static AnomalySearchResult Failed(AnomalySearchStatus status) =>
            new AnomalySearchResult(status, null, null, 0);
    }
}
