#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Встреча на дороге: детерминированный бросок по пулам тиров и старт боя.</summary>
    internal interface IRoadEncounterService
    {
        /// <summary>Бросает встречу по пулам тиров между хабами.</summary>
        /// <param name="firstTier">Тир отправления.</param>
        /// <param name="secondTier">Тир назначения.</param>
        /// <param name="fallbackTier">Тир лута, если у актёра нет боевого профиля.</param>
        /// <param name="chance">Шанс встречи, 0..1.</param>
        /// <param name="key">Ключ броска: одинаковый ключ и seed дают один результат.</param>
        /// <param name="seed">Seed мира.</param>
        /// <returns>Найденный противник или <c>null</c>, если встреча не случилась.</returns>
        RoadEncounter? Roll(int firstTier, int secondTier, int fallbackTier, double chance, string key, int seed);

        /// <summary>Начинает бой со встреченным противником.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="encounter">Найденный противник.</param>
        /// <returns><c>true</c>, если бой начат.</returns>
        bool TryStart(GameSession session, RoadEncounter encounter);
    }
}
