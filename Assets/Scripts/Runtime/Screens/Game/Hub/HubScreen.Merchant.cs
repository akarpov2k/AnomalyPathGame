#nullable enable
using System;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Часть класса <see cref="HubScreen"/>: выбранный торговец. Пока хаб открыт, за стойкой
    /// стоит первый доступный торговец локации; его полноростовая картинка берётся из
    /// <c>hubPortrait</c> актёра, иначе из обычного портрета. Переключение торговцев —
    /// отдельная задача, клик по фигуре пока действий не открывает.
    /// </summary>
    public sealed partial class HubScreen
    {
        private string? FirstTrader(GameSession session, LocationTemplate template)
        {
            if (template.LocationKind != LocationKind.Hub)
            {
                return null;
            }

            foreach (var traderId in template.Traders)
            {
                if (string.Equals(traderId, _selectedTraderId, StringComparison.Ordinal) &&
                    Host.Trade.IsAvailable(session, traderId))
                {
                    return traderId;
                }
            }

            foreach (var traderId in template.Traders)
            {
                if (Host.Trade.IsAvailable(session, traderId))
                {
                    return traderId;
                }
            }

            return null;
        }

        private Sprite? MerchantSprite(ActorTemplate actor)
        {
            if (actor.HubPortrait != null &&
                Host.Sprites.TryGet(actor.HubPortrait, out var body) &&
                body != null)
            {
                return body;
            }

            if (actor.Portrait != null &&
                Host.Sprites.TryGet(actor.Portrait, out var portrait) &&
                portrait != null)
            {
                return portrait;
            }

            return null;
        }
    }
}
