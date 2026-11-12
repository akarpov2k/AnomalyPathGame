#nullable enable
using System;

namespace AnomalyPath.Core.Quests
{
    /// <summary>Изменяемое состояние одного слота доски: текущее предложение и таймер.</summary>
    public sealed class QuestBoardSlotState
    {
        /// <summary>Создаёт состояние слота, в том числе из сохранения.</summary>
        /// <param name="questId">Предложение в слоте или <c>null</c>.</param>
        /// <param name="availableAt">Когда слот откроется; 0 — уже открыт.</param>
        /// <exception cref="ArgumentOutOfRangeException">Время отрицательно.</exception>
        public QuestBoardSlotState(string? questId = null, double availableAt = 0)
        {
            if (availableAt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableAt));
            }

            QuestId = string.IsNullOrWhiteSpace(questId) ? null : questId;
            AvailableAt = availableAt;
        }

        /// <summary>Предложение в слоте или <c>null</c>.</summary>
        public string? QuestId { get; private set; }

        /// <summary>Когда слот откроется; 0 — уже открыт.</summary>
        public double AvailableAt { get; private set; }

        /// <summary>Слот готов принять новое предложение.</summary>
        /// <param name="now">Текущее абсолютное время мира в часах.</param>
        /// <returns><c>true</c>, если таймер истёк и предложения нет.</returns>
        public bool IsReady(double now) => QuestId == null && now >= AvailableAt;

        /// <summary>Слот закрыт таймером.</summary>
        /// <param name="now">Текущее абсолютное время мира в часах.</param>
        /// <returns><c>true</c>, если таймер ещё идёт.</returns>
        public bool IsLocked(double now) => QuestId == null && now < AvailableAt;

        /// <summary>Кладёт в слот предложение и снимает таймер.</summary>
        /// <param name="questId">Идентификатор предложенного квеста.</param>
        /// <exception cref="ArgumentException">Идентификатор пуст.</exception>
        public void SetOffer(string questId)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                throw new ArgumentException(nameof(questId));
            }

            QuestId = questId;
            AvailableAt = 0;
        }

        /// <summary>Освобождает слот и запускает таймер обновления.</summary>
        /// <param name="availableAt">Когда слот снова откроется.</param>
        /// <exception cref="ArgumentOutOfRangeException">Время отрицательно.</exception>
        public void Close(double availableAt)
        {
            if (availableAt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableAt));
            }

            QuestId = null;
            AvailableAt = availableAt;
        }
    }
}
