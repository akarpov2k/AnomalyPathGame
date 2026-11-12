#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Minigames
{
    /// <summary>
    /// Состояние кодового замка: настройки тира, потраченные попытки, история и последний ответ.
    /// </summary>
    public sealed class StashLockState
    {
        /// <summary>Создаёт снимок состояния замка.</summary>
        /// <param name="codeLength">Длина кода.</param>
        /// <param name="alphabetSize">Размер алфавита.</param>
        /// <param name="attemptsTotal">Всего попыток.</param>
        /// <param name="attemptsUsed">Потрачено попыток.</param>
        /// <param name="guesses">Введённые коды.</param>
        /// <param name="solved">Замок вскрыт.</param>
        /// <param name="failed">Попытки кончились.</param>
        /// <param name="lastGuess">Ответ на последнюю попытку; <c>null</c> — попытки ещё не было.</param>
        /// <param name="history">Ответы на все неудачные попытки; <c>null</c> — пусто.</param>
        /// <exception cref="ArgumentNullException">Списки не заданы.</exception>
        public StashLockState(
            int codeLength,
            int alphabetSize,
            int attemptsTotal,
            int attemptsUsed,
            IReadOnlyList<string> guesses,
            bool solved,
            bool failed,
            LockGuessResult? lastGuess = null,
            IReadOnlyList<LockGuessResult>? history = null)
        {
            CodeLength = codeLength;
            AlphabetSize = alphabetSize;
            AttemptsTotal = attemptsTotal;
            AttemptsUsed = attemptsUsed;
            Guesses = guesses ?? throw new ArgumentNullException(nameof(guesses));
            Solved = solved;
            Failed = failed;
            LastGuess = lastGuess;
            History = history ?? Array.Empty<LockGuessResult>();
        }

        /// <summary>Длина кода.</summary>
        public int CodeLength { get; }

        /// <summary>Размер алфавита.</summary>
        public int AlphabetSize { get; }

        /// <summary>Всего попыток.</summary>
        public int AttemptsTotal { get; }

        /// <summary>Потрачено попыток.</summary>
        public int AttemptsUsed { get; }

        /// <summary>Сколько попыток осталось.</summary>
        public int AttemptsLeft => Math.Max(0, AttemptsTotal - AttemptsUsed);

        /// <summary>Введённые коды в порядке попыток.</summary>
        public IReadOnlyList<string> Guesses { get; }

        /// <summary>Замок вскрыт.</summary>
        public bool Solved { get; }

        /// <summary>Попытки кончились, тайник потерян.</summary>
        public bool Failed { get; }

        /// <summary>Можно ли вводить код.</summary>
        public bool CanGuess => !Solved && !Failed && AttemptsLeft > 0;

        /// <summary>Ответ на последнюю попытку.</summary>
        public LockGuessResult? LastGuess { get; }

        /// <summary>Ответы на все неудачные попытки.</summary>
        public IReadOnlyList<LockGuessResult> History { get; }
    }
}
