#nullable enable
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Тайник на текущей локации: вскрытие и кодовый замок.</summary>
    internal interface IStashFlow
    {
        /// <summary>Вскрывает тайник текущей локации.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Статус, лут и признак джекпота.</returns>
        StashOpenResult OpenStash(GameSession session);

        /// <summary>Состояние кодового замка текущего тайника.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Снимок состояния замка или <c>null</c>.</returns>
        StashLockState? LockState(GameSession session);

        /// <summary>Проверяет введённый код замка.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="guess">Введённый код.</param>
        /// <returns>Состояние замка после попытки или <c>null</c>.</returns>
        StashLockState? Guess(GameSession session, string guess);
    }
}
