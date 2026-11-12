#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Ремонт снаряжения у торговца-механика: чинит всю недостающую прочность предметов
    /// в инвентаре, надетом снаряжении и быстрых слотах, списывая деньги по доле цены.
    /// </summary>
    public sealed class RepairService : IRepairService
    {
        private readonly IActorRepository _actors;
        private readonly IItemRepository _items;
        private readonly ITradeService _trade;
        private readonly IEconomySettings _economy;

        /// <summary>Создаёт сервис ремонта.</summary>
        /// <param name="actors">Актёры: флаг механика у торговца.</param>
        /// <param name="items">Предметы: цены и максимум прочности.</param>
        /// <param name="trade">Торговля: доступность торговца и хаба.</param>
        /// <param name="economy">Округление, нижняя граница цены и коэффициент ремонта.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public RepairService(
            IActorRepository actors,
            IItemRepository items,
            ITradeService trade,
            IEconomySettings economy)
        {
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _trade = trade ?? throw new ArgumentNullException(nameof(trade));
            _economy = economy ?? throw new ArgumentNullException(nameof(economy));
        }

        /// <inheritdoc />
        public RepairQuote GetQuote(GameSession session, string actorId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!TryGetMechanic(actorId) || !_trade.IsAvailable(session, actorId))
            {
                return new RepairQuote(false, 0, 0);
            }

            var player = session.State.Player;
            if (player == null)
            {
                return new RepairQuote(false, 0, 0);
            }

            var count = 0;
            var cost = 0;
            foreach (var item in AllItems(player))
            {
                var itemCost = CostFor(item);
                if (!NeedsRepair(item))
                {
                    continue;
                }

                count++;
                cost = checked(cost + itemCost);
            }

            return new RepairQuote(true, count, cost);
        }

        /// <inheritdoc />
        public RepairResult Repair(GameSession session, string actorId)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (!TryGetMechanic(actorId))
            {
                return new RepairResult(
                    _actors.Contains(actorId) ? RepairStatus.NotMechanic : RepairStatus.TraderNotFound, 0, 0);
            }

            if (!_trade.IsAvailable(session, actorId))
            {
                return new RepairResult(RepairStatus.Unavailable, 0, 0);
            }

            var player = session.State.Player;
            if (player == null)
            {
                return new RepairResult(RepairStatus.Unavailable, 0, 0);
            }

            var damaged = new List<ItemInstance>();
            var total = 0;
            foreach (var item in AllItems(player))
            {
                if (!NeedsRepair(item))
                {
                    continue;
                }

                damaged.Add(item);
                total = checked(total + CostFor(item));
            }

            if (damaged.Count == 0)
            {
                return new RepairResult(RepairStatus.NothingToRepair, 0, 0);
            }

            if (!player.TrySpendMoney(total))
            {
                return new RepairResult(RepairStatus.NotEnoughMoney, 0, 0);
            }

            foreach (var item in damaged)
            {
                item.Durability = item.DurabilityMax;
            }

            return new RepairResult(RepairStatus.Ok, damaged.Count, total);
        }

        /// <summary>Проверяет, что актёр — торговец с включённым ремонтом.</summary>
        private bool TryGetMechanic(string actorId)
        {
            if (string.IsNullOrWhiteSpace(actorId) || !_actors.Contains(actorId))
            {
                return false;
            }

            var actor = _actors.Get(actorId);
            return actor.Type == ActorType.Trader && actor.Trader != null && actor.Trader.Repair;
        }

        /// <summary>Все предметы игрока: сетка, надетое и быстрые слоты.</summary>
        private static IEnumerable<ItemInstance> AllItems(PlayerState player)
        {
            foreach (var item in player.Inventory.Items)
            {
                if (!item.Overflow)
                {
                    yield return item;
                }
            }

            foreach (var pair in player.Equipment.Slots)
            {
                yield return pair.Value;
            }

            var equipment = player.Equipment;
            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                var quick = equipment.GetQuick(index);
                if (quick != null)
                {
                    yield return quick;
                }
            }
        }

        /// <summary>Нужна ли предмету починка: есть прочность и она ниже максимума.</summary>
        private static bool NeedsRepair(ItemInstance item) =>
            item.Durability.HasValue && item.DurabilityMax.HasValue &&
            item.DurabilityMax.Value > 0 && item.Durability.Value < item.DurabilityMax.Value;

        /// <summary>Цена ремонта: доля недостающей прочности от цены предмета.</summary>
        private int CostFor(ItemInstance item)
        {
            if (!NeedsRepair(item) || !_items.TryGet(item.ItemId, out var template))
            {
                return 0;
            }

            if (!ItemPrices.TryGetItemValue(template, out var basePrice))
            {
                return 0;
            }

            var max = item.DurabilityMax!.Value;
            var missing = max - item.Durability!.Value;
            var value = ((double)missing / max) * basePrice * _economy.RepairCostFactor;
            var step = _economy.Rounding;
            var rounded = (int)(Math.Round(value / step, MidpointRounding.AwayFromZero) * step);
            return Math.Max(rounded, _economy.MinPrice);
        }
    }
}
