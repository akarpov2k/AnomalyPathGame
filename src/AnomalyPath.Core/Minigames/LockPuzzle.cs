#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Кодовый замок тайника: детерминированный код, подсказки «на месте» и «есть, но не там».
    /// Правила не знают о UI: экран передаёт введённый код и показывает ответ.
    /// </summary>
    public sealed class LockPuzzle
    {
        private readonly int[] _code;

        /// <summary>Создаёт замок с кодом от seed; цифры в коде не повторяются.</summary>
        /// <param name="seed">Seed: локация и id тайника.</param>
        /// <param name="codeLength">Число символов в коде, от 1 до 8.</param>
        /// <param name="alphabetSize">Число символов, от 2 до 9; должно быть не меньше длины кода.</param>
        /// <exception cref="ArgumentOutOfRangeException">Длина или алфавит вне границ.</exception>
        public LockPuzzle(int seed, int codeLength, int alphabetSize)
        {
            if (codeLength < 1 || codeLength > 8)
            {
                throw new ArgumentOutOfRangeException(nameof(codeLength));
            }

            if (alphabetSize < 2 || alphabetSize > 9 || alphabetSize < codeLength)
            {
                throw new ArgumentOutOfRangeException(nameof(alphabetSize));
            }

            CodeLength = codeLength;
            AlphabetSize = alphabetSize;
            _code = new int[codeLength];
            var available = new List<int>(alphabetSize);
            for (var digit = 1; digit <= alphabetSize; digit++)
            {
                available.Add(digit);
            }

            var random = new SeededWorldRandom(seed);
            for (var index = 0; index < codeLength; index++)
            {
                var pick = random.NextInt(0, available.Count);
                _code[index] = available[pick];
                available.RemoveAt(pick);
            }
        }

        /// <summary>Число символов в коде.</summary>
        public int CodeLength { get; }

        /// <summary>Число доступных символов; символы — цифры от 1 до значения.</summary>
        public int AlphabetSize { get; }

        /// <summary>Код замка: нужен проверкам и отладке, экран его не показывает.</summary>
        public IReadOnlyList<int> Code => _code;

        /// <summary>Проверяет введённый код и считает подсказки; цифры в коде не должны повторяться.</summary>
        /// <param name="guess">Строка из цифр <c>1..AlphabetSize</c> длиной <see cref="CodeLength"/> без повторов.</param>
        /// <returns>Подсказки или <c>null</c>, если строка неполная, содержит чужие или повторяющиеся цифры.</returns>
        public LockGuessResult? Evaluate(string? guess)
        {
            if (guess == null || guess.Length != CodeLength)
            {
                return null;
            }

            var values = new int[CodeLength];
            var seen = new bool[10];
            for (var index = 0; index < CodeLength; index++)
            {
                var symbol = guess[index];
                if (symbol < '1' || symbol > (char)('0' + AlphabetSize))
                {
                    return null;
                }

                var digit = symbol - '0';
                if (seen[digit])
                {
                    return null;
                }

                seen[digit] = true;
                values[index] = digit;
            }

            var exact = 0;
            var partial = 0;
            var marks = new LockDigitMark[CodeLength];
            for (var index = 0; index < CodeLength; index++)
            {
                if (values[index] == _code[index])
                {
                    marks[index] = LockDigitMark.CorrectPlace;
                    exact++;
                }
                else if (Array.IndexOf(_code, values[index]) >= 0)
                {
                    marks[index] = LockDigitMark.WrongPlace;
                    partial++;
                }
                else
                {
                    marks[index] = LockDigitMark.None;
                }
            }

            return new LockGuessResult(guess, exact, partial, exact == CodeLength, marks);
        }

        /// <summary>Строит код как строку цифр; удобно для отладочных сообщений и тестов.</summary>
        /// <returns>Строка длиной <see cref="CodeLength"/>.</returns>
        public string CodeText()
        {
            var chars = new char[CodeLength];
            for (var index = 0; index < CodeLength; index++)
            {
                chars[index] = (char)('0' + _code[index]);
            }

            return new string(chars);
        }

        /// <summary>Проверяет, что строка состоит из допустимых неповторяющихся символов нужной длины.</summary>
        /// <param name="guess">Проверяемая строка.</param>
        /// <param name="codeLength">Длина кода.</param>
        /// <param name="alphabetSize">Размер алфавита.</param>
        /// <returns><c>true</c>, если строка корректна.</returns>
        public static bool IsValidGuess(string? guess, int codeLength, int alphabetSize)
        {
            if (guess == null || guess.Length != codeLength)
            {
                return false;
            }

            var seen = new bool[10];
            foreach (var symbol in guess)
            {
                if (symbol < '1' || symbol > (char)('0' + alphabetSize))
                {
                    return false;
                }

                var digit = symbol - '0';
                if (seen[digit])
                {
                    return false;
                }

                seen[digit] = true;
            }

            return true;
        }

        /// <summary>Возвращает код числом для отладочных сообщений.</summary>
        /// <returns>Строка кода.</returns>
        public override string ToString() =>
            CodeText() + " / " + CodeLength.ToString(CultureInfo.InvariantCulture);
    }
}
