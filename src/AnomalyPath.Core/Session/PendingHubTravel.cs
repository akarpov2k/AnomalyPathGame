#nullable enable
using System;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Session
{
    /// <summary>
    /// Начатый переход между хабами, который ждёт окончания боя на дороге.
    /// Живёт только в сессии: в бою сохраняться нельзя, а до отправления и после прибытия
    /// перехода уже нет, поэтому в сейв не попадает.
    /// </summary>
    public sealed class PendingHubTravel
    {
        /// <summary>Создаёт ожидающий переход.</summary>
        /// <param name="fromHubId">Экземпляр хаба отправления.</param>
        /// <param name="targetHubId">Экземпляр целевого хаба.</param>
        /// <param name="mode">Способ перехода.</param>
        /// <exception cref="ArgumentException">Идентификаторы пусты.</exception>
        public PendingHubTravel(string fromHubId, string targetHubId, HubTravelMode mode)
        {
            if (string.IsNullOrWhiteSpace(fromHubId))
            {
                throw new ArgumentException(nameof(fromHubId));
            }

            if (string.IsNullOrWhiteSpace(targetHubId))
            {
                throw new ArgumentException(nameof(targetHubId));
            }

            FromHubId = fromHubId;
            TargetHubId = targetHubId;
            Mode = mode;
        }

        /// <summary>Экземпляр хаба отправления.</summary>
        public string FromHubId { get; }

        /// <summary>Экземпляр целевого хаба.</summary>
        public string TargetHubId { get; }

        /// <summary>Способ перехода.</summary>
        public HubTravelMode Mode { get; }
    }
}
