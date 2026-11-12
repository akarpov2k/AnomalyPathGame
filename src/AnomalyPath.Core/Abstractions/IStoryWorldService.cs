#nullable enable
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Квестовые локации: цепочка создаётся событием квеста, а не пулом.</summary>
    public interface IStoryWorldService
    {
        /// <summary>Создаёт локации цепочки, если их ещё нет на карте, и принудительные объекты.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="chainId">Идентификатор цепочки.</param>
        /// <returns><c>true</c>, если цепочка есть на карте после вызова.</returns>
        bool EnsureChain(GameSession session, string chainId);

        /// <summary>Убирает локации цепочки с карты: квест пройден, цепочка больше не нужна.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="chainId">Идентификатор цепочки.</param>
        /// <returns>Сколько локаций удалено.</returns>
        int RemoveChain(GameSession session, string chainId);

        /// <summary>Проверяет, создана ли цепочка на карте.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="chainId">Идентификатор цепочки.</param>
        /// <returns><c>true</c>, если все локации цепочки есть на карте.</returns>
        bool HasChain(GameSession session, string chainId);

        /// <summary>Создаёт одну уникальную квестовую локацию, если её ещё нет.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="locationId">Шаблон локации.</param>
        /// <returns><c>true</c>, если локация есть на карте после вызова.</returns>
        bool EnsureLocation(GameSession session, string locationId);
    }
}
