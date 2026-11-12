#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>Ответ замка на введённый код: сколько символов на месте, сколько не там и подсказка по каждой цифре.</summary>
    public sealed class LockGuessResult
    {
        /// <summary>Создаёт результат проверки.</summary>
        /// <param name="guess">Введённый код.</param>
        /// <param name="exact">Сколько символов на своих местах.</param>
        /// <param name="partial">Сколько символов есть в коде, но не на своём месте.</param>
        /// <param name="solved">Совпал ли код целиком.</param>
        /// <param name="marks">Подсказка по каждой цифре; параллельно символам <paramref name="guess"/>.</param>
        /// <exception cref="ArgumentNullException">Введённый код или подсказки не заданы.</exception>
        public LockGuessResult(
            string guess,
            int exact,
            int partial,
            bool solved,
            IReadOnlyList<LockDigitMark> marks)
        {
            Guess = guess ?? throw new ArgumentNullException(nameof(guess));
            Marks = marks ?? throw new ArgumentNullException(nameof(marks));
            Exact = exact;
            Partial = partial;
            Solved = solved;
        }

        /// <summary>Введённый код.</summary>
        public string Guess { get; }

        /// <summary>Сколько символов на своих местах.</summary>
        public int Exact { get; }

        /// <summary>Сколько символов есть в коде, но не на своём месте.</summary>
        public int Partial { get; }

        /// <summary>Код угадан.</summary>
        public bool Solved { get; }

        /// <summary>Подсказка по каждой цифре введённого кода.</summary>
        public IReadOnlyList<LockDigitMark> Marks { get; }
    }
}
