#nullable enable
using System;
using System.Globalization;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>
    /// Идентификаторы экземпляров локаций квестовой цепочки: их строит генератор маршрута,
    /// и они должны совпадать с теми, что проверяет прогресс целей квеста.
    /// </summary>
    public static class QuestChainInstanceIds
    {
        /// <summary>Идентификатор экземпляра шага цепочки.</summary>
        /// <param name="chainId">Идентификатор цепочки.</param>
        /// <param name="index">Номер шага с нуля.</param>
        /// <returns>Идентификатор вида <c>chain_atp_00</c>.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Номер шага отрицателен.</exception>
        public static string For(string chainId, int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return (chainId ?? string.Empty) + "_" + index.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
