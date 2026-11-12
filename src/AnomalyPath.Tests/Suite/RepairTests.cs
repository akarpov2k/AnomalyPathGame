#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Trade;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Ремонт у торговца-механика: флаг из данных, цена, списание денег и границы.</summary>
    public sealed class RepairTests : DataTestBase
    {
        private const string MechanicId = "trd_hub_smuggler";
        private const string OrdinaryTraderId = "trd_hub_medic";

        /// <summary>Создаёт категорию тестов ремонта.</summary>
        /// <param name="dataRoot">Корень данных.</param>
        public RepairTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var actors = services.Resolve<IActorRepository>();
            var items = services.Resolve<IItemRepository>();
            var repair = services.Resolve<IRepairService>();

            Check("ремонт: флаг механика живёт в данных торговца", () =>
            {
                True(actors.Get(MechanicId).Trader!.Repair, "контрабандист — механик");
                False(actors.Get(OrdinaryTraderId).Trader!.Repair, "фельдшер не механик");
            });

            Check("ремонт: цена — доля недостающей прочности от цены предмета", () =>
            {
                var session = NewSession(services, money: 1000);
                var player = session.State.Player!;
                var weapon = Damaged(items, "wpn_ak74", durability: 50);
                player.Equipment.Set(EquipmentSlot.Main, weapon);

                var quote = repair.GetQuote(session, MechanicId);
                True(quote.Available, "ремонт доступен");
                True(quote.HasWork, "есть что чинить");
                Equal(1, quote.ItemCount, "предметов к ремонту");
                Equal(300, quote.Cost, "50% прочности от 1200 × 0.5");

                var result = repair.Repair(session, MechanicId);
                True(result.Succeeded, "ремонт выполнен");
                Equal(1, result.RepairedCount, "починено предметов");
                Equal(300, result.Cost, "списано");
                Equal(700, player.Money, "деньги после ремонта");
                Equal<int?>(100, weapon.Durability, "прочность восстановлена");
            });

            Check("ремонт: чинится и инвентарь, и быстрое снаряжение", () =>
            {
                var session = NewSession(services, money: 5000);
                var player = session.State.Player!;
                var armor = Damaged(items, "itm_armor_jacket", durability: 0);
                player.Equipment.Set(EquipmentSlot.Armor, armor);
                var helmet = Damaged(items, "itm_helmet_light", durability: 5);
                player.Equipment.Set(EquipmentSlot.Helmet, helmet);
                var inGrid = Damaged(items, "itm_armor_plate", durability: 1);
                player.Inventory.Place(inGrid, items.Get("itm_armor_plate"), 0, 0, false);

                var quote = repair.GetQuote(session, MechanicId);
                Equal(3, quote.ItemCount, "три изношенных предмета");

                var result = repair.Repair(session, MechanicId);
                True(result.Succeeded, "ремонт выполнен: " + result.Status + " / " + quote.Cost);
                Equal(3, result.RepairedCount, "починено три");
                Equal(armor.DurabilityMax, armor.Durability, "броня целая");
                Equal(helmet.DurabilityMax, helmet.Durability, "шлем целый");
                Equal(inGrid.DurabilityMax, inGrid.Durability, "предмет в сетке целый");
                Equal(5000 - result.Cost, session.State.Player!.Money, "деньги списаны на сумму ремонта");
            });

            Check("ремонт: не хватает денег — предмет и деньги не меняются", () =>
            {
                var session = NewSession(services, money: 10);
                var player = session.State.Player!;
                var weapon = Damaged(items, "wpn_ak74", durability: 50);
                player.Equipment.Set(EquipmentSlot.Main, weapon);

                var result = repair.Repair(session, MechanicId);
                Equal(RepairStatus.NotEnoughMoney, result.Status, "статус");
                Equal(10, player.Money, "деньги не списаны");
                Equal<int?>(50, weapon.Durability, "прочность не изменилась");
            });

            Check("ремонт: целое снаряжение — чинить нечего", () =>
            {
                var session = NewSession(services, money: 1000);
                var player = session.State.Player!;
                player.Equipment.Set(EquipmentSlot.Main, New(items, "wpn_ak74", durability: 100));

                var quote = repair.GetQuote(session, MechanicId);
                False(quote.HasWork, "работы нет");
                Equal(0, quote.Cost, "цена нулевая");
                Equal(RepairStatus.NothingToRepair, repair.Repair(session, MechanicId).Status, "статус");
            });

            Check("ремонт: у обычного торговца кнопки нет", () =>
            {
                var session = NewSession(services, money: 1000);
                False(repair.GetQuote(session, OrdinaryTraderId).Available, "оценка недоступна");
                var result = repair.Repair(session, OrdinaryTraderId);
                Equal(RepairStatus.NotMechanic, result.Status, "статус");
                Equal(0, result.Cost, "денег не списано");
            });

            Check("ремонт: вне хаба недоступен", () =>
            {
                var session = NewSession(services, money: 1000);
                var path = string.Empty;
                foreach (var location in session.State.World.Map.Locations)
                {
                    if (location.Subtype != LocationSubtype.Hub)
                    {
                        path = location.InstanceId;
                        break;
                    }
                }

                True(!string.IsNullOrEmpty(path), "в мире есть локация вне хаба");
                session.State.World.MoveTo(path);
                var result = repair.Repair(session, MechanicId);
                Equal(RepairStatus.Unavailable, result.Status, "статус");
            });
        }

        private static ItemInstance New(IItemRepository items, string itemId, int? durability)
        {
            var template = items.Get(itemId);
            var max = template.Weapon?.DurabilityMax ?? template.ArmorDurabilityMax;
            return new ItemInstance(
                "repair_" + itemId + "_" + durability,
                itemId,
                1,
                durability,
                durability.HasValue ? max : null);
        }

        private static ItemInstance Damaged(IItemRepository items, string itemId, int durability) =>
            New(items, itemId, durability);

        private static GameSession NewSession(ServiceContainer services, int money = 1000)
        {
            var generator = services.Resolve<IWorldGenerator>();
            var world = WorldState.Start(generator.Generate(42));
            var player = new PlayerState(
                "player",
                1,
                0,
                100,
                8,
                new GridContainer("player", 8, 4),
                new Equipment("player", 4),
                Array.Empty<string>(),
                new NeedsState(100, 100, 100),
                "Тест",
                money);
            return new GameSession(new GameState(world, player));
        }
    }
}
