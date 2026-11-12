#nullable enable
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Вход на локацию: проверка тайника и спавн объектов.</summary>
    internal interface ILocationEntry
    {
        /// <summary>Спавнит объекты текущей локации и проверяет тайник; повторный вызов ничего не меняет.</summary>
        /// <param name="session">Игровая сессия.</param>
        void Ensure(GameSession session);

        /// <summary>Проверяет появление тайника на текущей локации.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns><c>true</c>, если на локации появился тайник.</returns>
        bool RollStash(GameSession session);
    }
}
