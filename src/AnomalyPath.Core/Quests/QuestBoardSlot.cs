#nullable enable
using System;

namespace AnomalyPath.Core.Quests
{
    /// <summary>Один слот доски в момент показа: настройка, таймер и предложение.</summary>
    public sealed class QuestBoardSlot
    {
        /// <summary>Создаёт снимок слота доски.</summary>
        /// <param name="index">Номер слота в настройке доски.</param>
        /// <param name="difficulty">Сложность слота.</param>
        /// <param name="refreshHours">Срок обновления слота в часах мира.</param>
        /// <param name="availableAt">Когда слот снова откроется; 0 — уже открыт.</param>
        /// <param name="offer">Предложение в слоте или <c>null</c>.</param>
        /// <exception cref="ArgumentOutOfRangeException">Номер, срок или время вне границ.</exception>
        public QuestBoardSlot(
            int index,
            QuestDifficulty difficulty,
            int refreshHours,
            double availableAt,
            QuestOffer? offer)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            if (refreshHours < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(refreshHours));
            }

            if (availableAt < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(availableAt));
            }

            Index = index;
            Difficulty = difficulty;
            RefreshHours = refreshHours;
            AvailableAt = availableAt;
            Offer = offer;
        }

        /// <summary>Номер слота в настройке доски.</summary>
        public int Index { get; }

        /// <summary>Сложность слота.</summary>
        public QuestDifficulty Difficulty { get; }

        /// <summary>Срок обновления слота в часах мира.</summary>
        public int RefreshHours { get; }

        /// <summary>Когда слот снова откроется; 0 — уже открыт.</summary>
        public double AvailableAt { get; }

        /// <summary>Предложение в слоте или <c>null</c>.</summary>
        public QuestOffer? Offer { get; }

        /// <summary>Слот закрыт таймером.</summary>
        /// <param name="now">Текущее абсолютное время мира в часах.</param>
        /// <returns><c>true</c>, если слот ещё не обновился.</returns>
        public bool IsLocked(double now) => Offer == null && now < AvailableAt;

        /// <summary>Сколько часов осталось до обновления слота.</summary>
        /// <param name="now">Текущее абсолютное время мира в часах.</param>
        /// <returns>Остаток в часах; 0, если слот уже открыт.</returns>
        public double HoursLeft(double now) => Math.Max(0, AvailableAt - now);
    }
}
