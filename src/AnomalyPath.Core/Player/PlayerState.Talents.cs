#nullable enable
using System;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Player
{
    /// <summary>Часть класса <see cref="PlayerState"/>: таланты и свободные очки.</summary>
    public sealed partial class PlayerState
    {
        /// <summary>Возвращает ранг таланта; 0, если талант не изучен.</summary>
        /// <param name="talentId">Идентификатор таланта.</param>
        /// <returns>Вложенный ранг.</returns>
        public int GetTalentRank(string talentId) =>
            !string.IsNullOrWhiteSpace(talentId) && _talentRanks.TryGetValue(talentId, out var rank) ? rank : 0;

        /// <summary>Записывает ранг таланта; 0 убирает запись.</summary>
        /// <param name="talentId">Идентификатор таланта.</param>
        /// <param name="rank">Новый ранг; не меньше нуля.</param>
        /// <exception cref="ArgumentException">Идентификатор пуст.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Ранг отрицателен.</exception>
        public void SetTalentRank(string talentId, int rank)
        {
            if (string.IsNullOrWhiteSpace(talentId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(talentId)),
                    nameof(talentId));
            }

            if (rank < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rank));
            }

            if (rank == 0)
            {
                _talentRanks.Remove(talentId);
                return;
            }

            _talentRanks[talentId] = rank;
        }

        /// <summary>Добавляет свободные очки талантов.</summary>
        /// <param name="amount">Сколько добавить; не меньше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Количество отрицательно.</exception>
        public void AddTalentPoints(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount));
            }

            TalentPoints += amount;
        }

        /// <summary>Списывает одно свободное очко таланта.</summary>
        /// <returns><c>true</c>, если очко было и списано.</returns>
        public bool TrySpendTalentPoint()
        {
            if (TalentPoints <= 0)
            {
                return false;
            }

            TalentPoints--;
            return true;
        }
    }
}
