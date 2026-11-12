#nullable enable
using System;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Сложность кодового замка для одного тира локации: длина кода и размер алфавита.
    /// </summary>
    public sealed class StashLockTier
    {
        /// <summary>Создаёт настройку тира.</summary>
        /// <param name="tier">Тир локации, не меньше нуля.</param>
        /// <param name="codeLength">Число символов в коде, от 1 до 8.</param>
        /// <param name="alphabetSize">Число доступных символов, от 2 до 9.</param>
        /// <exception cref="ArgumentOutOfRangeException">Значение вне допустимых границ.</exception>
        public StashLockTier(int tier, int codeLength, int alphabetSize)
        {
            if (tier < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tier));
            }

            if (codeLength < 1 || codeLength > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(codeLength));
            }

            if (alphabetSize < 2 || alphabetSize > 9 || alphabetSize < codeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(alphabetSize));
            }

            Tier = tier;
            CodeLength = codeLength;
            AlphabetSize = alphabetSize;
        }

        /// <summary>Тир локации.</summary>
        public int Tier { get; }

        /// <summary>Число символов в коде.</summary>
        public int CodeLength { get; }

        /// <summary>Число доступных символов; символы — цифры от 1 до значения.</summary>
        public int AlphabetSize { get; }
    }
}
