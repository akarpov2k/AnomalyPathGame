#nullable enable
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Переходы между хабами по карте мира.</summary>
    internal interface IHubTravelFlow
    {
        /// <summary>Предложение перехода между хабами: шаги, цена и шанс встречи.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        /// <param name="mode">Способ перехода: пешком или с проводником.</param>
        /// <returns>Предложение перехода.</returns>
        HubTravelQuote GetHubTravelQuote(GameSession session, string targetHubId, HubTravelMode mode);

        /// <summary>Выполняет переход между хабами; встреча начинает бой на дороге.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        /// <param name="mode">Способ перехода.</param>
        /// <returns>Итог перехода.</returns>
        HubTravelResult TravelToHub(GameSession session, string targetHubId, HubTravelMode mode);

        /// <summary>Завершает начатый переход: игрок прибывает в целевой хаб.</summary>
        /// <param name="session">Игровая сессия.</param>
        void CompletePending(GameSession session);

        /// <summary>Сбрасывает начатый переход: бой на дороге закончился поражением.</summary>
        /// <param name="session">Игровая сессия.</param>
        void CancelPending(GameSession session);
    }
}
