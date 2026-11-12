#nullable enable
using System;
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Core.Character
{
    /// <summary>
    /// Действия с предметами игрока: надеть и снять предмет, надеть и снять артефакт, зарядить и разрядить оружие,
    /// разобрать и выбросить. Все проверки делаются до изменения состояния, чтобы ошибка ничего не ломала.
    /// </summary>
    public sealed partial class CharacterActions : ICharacterActions
    {
        private readonly IItemRepository _items;
        private readonly IInventoryService _inventory;
        private readonly IEquipmentService _equipment;
        private readonly double _baseCarryWeight;

        /// <summary>Создаёт действия поверх репозитория предметов, инвентаря и экипировки.</summary>
        /// <param name="items">Репозиторий шаблонов.</param>
        /// <param name="inventory">Сервис инвентаря.</param>
        /// <param name="equipment">Сервис экипировки.</param>
        /// <param name="baseCarryWeight">
        /// Базовый лимит веса из правил, кг; <see cref="double.PositiveInfinity"/> — не ограничивать
        /// (тесты и инструменты без правил).
        /// </param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public CharacterActions(
            IItemRepository items,
            IInventoryService inventory,
            IEquipmentService equipment,
            double baseCarryWeight = double.PositiveInfinity)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _baseCarryWeight = baseCarryWeight;
        }
    }
}
