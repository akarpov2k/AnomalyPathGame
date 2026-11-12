#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Quests
{
    /// <summary>Состояние доски объявлений конкретного хаба: слоты и их таймеры.</summary>
    public sealed class QuestBoardState
    {
        private readonly List<QuestBoardSlotState> _slots;

        /// <summary>Создаёт состояние доски.</summary>
        /// <param name="locationInstanceId">Экземпляр хаба, которому принадлежит доска.</param>
        /// <param name="slots">Состояния слотов; <c>null</c> — пусто.</param>
        /// <exception cref="ArgumentException">Идентификатор локации пуст.</exception>
        public QuestBoardState(
            string locationInstanceId,
            IReadOnlyList<QuestBoardSlotState>? slots = null)
        {
            if (string.IsNullOrWhiteSpace(locationInstanceId))
            {
                throw new ArgumentException(nameof(locationInstanceId));
            }

            LocationInstanceId = locationInstanceId;
            _slots = slots == null
                ? new List<QuestBoardSlotState>()
                : new List<QuestBoardSlotState>(slots);
        }

        /// <summary>Экземпляр хаба, которому принадлежит доска.</summary>
        public string LocationInstanceId { get; }

        /// <summary>Состояния слотов в порядке настройки доски.</summary>
        public IReadOnlyList<QuestBoardSlotState> Slots => _slots;

        /// <summary>Дополняет список слотов до нужного числа.</summary>
        /// <param name="count">Сколько слотов должно быть; не меньше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Количество отрицательно.</exception>
        public void EnsureSlots(int count)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            while (_slots.Count < count)
            {
                _slots.Add(new QuestBoardSlotState());
            }
        }

        /// <summary>Возвращает состояние слота.</summary>
        /// <param name="index">Номер слота.</param>
        /// <returns>Состояние слота.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Слот вне списка.</exception>
        public QuestBoardSlotState GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _slots[index];
        }
    }
}
