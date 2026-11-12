#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Tests
{
    public sealed class EquipmentTests : DataTestBase
    {
        public EquipmentTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            var services = BuildServices();
            var equipment = services.Resolve<IEquipmentService>();
            var settings = services.Resolve<IEquipmentSettings>();

            Check("экипировка: число быстрых слотов берётся из данных", () =>
            {
                Equal(4, settings.QuickSlots, "quickSlots");
            });

            Check("экипировка: защита от аномалий суммируется по типам", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_helmet_light"), EquipmentSlot.Helmet, out _);
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                Equal(4, equipment.GetProtection(state, "dmg_electric"), "1 + 3");
                Equal(3, equipment.GetProtection(state, "dmg_rupture"), "1 + 2");
            });

            Check("экипировка: экзоскелет защищает от всех типов урона", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_armor_exoskeleton"), EquipmentSlot.Armor, out _);
                Equal(6, equipment.GetProtection(state, "dmg_bullet"), "пули");
                Equal(7, equipment.GetProtection(state, "dmg_electric"), "электричество");
                Equal(6, equipment.GetProtection(state, "dmg_fire"), "огонь");
                Equal(6, equipment.GetProtection(state, "dmg_rupture"), "разрыв");
            });

            Check("экипировка: защита суммируется по шлему и броне", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_helmet_light"), EquipmentSlot.Helmet, out _);
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                Equal(4, equipment.GetProtection(state, "dmg_bullet"), "1 + 3");
            });

            Check("экипировка: сломанная броня не участвует в защите", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_helmet_light"), EquipmentSlot.Helmet, out _);
                equipment.TryEquip(state, new ItemInstance("broken", "itm_armor_jacket", 1, 0, 40), EquipmentSlot.Armor, out _);
                Equal(1, equipment.GetProtection(state, "dmg_bullet"), "только шлем");
            });

            Check("экипировка: неизвестный тип урона даёт нулевую защиту", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                Equal(0, equipment.GetProtection(state, "dmg_wood"), "нет такой защиты");
            });

            Check("экипировка: защита артефакта суммируется с бронёй и шлемом", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_helmet_light"), EquipmentSlot.Helmet, out _);
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                True(equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out _), "артефакт");
                Equal(6, equipment.GetProtection(state, "dmg_fire"), "1 шлем + 2 броня + 3 артефакт");
                Equal(4, equipment.GetProtection(state, "dmg_bullet"), "артефакт огня не защищает от пуль");
            });

            Check("экипировка: снятый артефакт не участвует в защите", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                True(equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out var index), "артефакт");
                Equal(5, equipment.GetProtection(state, "dmg_fire"), "2 броня + 3 артефакт");

                equipment.TakeArtifact(state, index);
                Equal(2, equipment.GetProtection(state, "dmg_fire"), "осталась броня");
            });

            Check("экипировка: сломанный артефакт не участвует в защите", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                state.SetArtifact(0, new ItemInstance("broken_art", "art_fire_ember", 1, 0, 10));
                Equal(2, equipment.GetProtection(state, "dmg_fire"), "только броня");
            });

            Check("экипировка: основное оружие — любое", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                True(equipment.TryEquip(state, Instance("wpn_ak74"), EquipmentSlot.Main, out _), "автомат");
                True(equipment.TryEquip(state, Instance("wpn_pm"), EquipmentSlot.Main, out _), "пистолет");
            });

            Check("экипировка: пистолет встаёт в дополнительный слот", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                True(equipment.TryEquip(state, Instance("wpn_pm"), EquipmentSlot.Secondary, out _), "пистолет");
                Equal("wpn_pm", state.Get(EquipmentSlot.Secondary)!.ItemId, "слот");
            });

            Check("экипировка: автомат в дополнительный слот не встаёт", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                False(equipment.TryEquip(state, Instance("wpn_ak74"), EquipmentSlot.Secondary, out _), "автомат");
            });

            Check("экипировка: шлем, броня, рюкзак и пояс — по своим слотам", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                True(equipment.TryEquip(state, Instance("itm_helmet_light"), EquipmentSlot.Helmet, out _), "шлем");
                True(equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _), "броня");
                True(equipment.TryEquip(state, Instance("itm_backpack_small"), EquipmentSlot.Backpack, out _), "рюкзак");
                True(equipment.TryEquip(state, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _), "пояс");
                False(equipment.TryEquip(state, Instance("itm_helmet_light"), EquipmentSlot.Armor, out _), "шлем в броню");
            });

            Check("экипировка: замена возвращает прежний предмет", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("wpn_ak74"), EquipmentSlot.Main, out _);
                True(equipment.TryEquip(state, Instance("wpn_akm"), EquipmentSlot.Main, out var replaced), "замена");
                Equal("wpn_ak74", replaced!.ItemId, "снятый предмет");
                Equal("wpn_akm", state.Get(EquipmentSlot.Main)!.ItemId, "новый предмет");
            });

            Check("экипировка: снятие очищает слот", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("wpn_ak74"), EquipmentSlot.Main, out _);
                var removed = equipment.Unequip(state, EquipmentSlot.Main);
                Equal("wpn_ak74", removed!.ItemId, "снятый");
                True(state.Get(EquipmentSlot.Main) == null, "слот пуст");
            });

            Check("экипировка: слоты артефактов суммируются", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _);
                Equal(2, equipment.TotalArtifactSlots(state), "только пояс");
                equipment.TryEquip(state, Instance("itm_armor_jacket"), EquipmentSlot.Armor, out _);
                Equal(3, equipment.TotalArtifactSlots(state), "броня плюс пояс");
            });

            Check("экипировка: рюкзак добавляет лимит веса", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                Equal(20d, equipment.CarryCapacity(state, 20), "без рюкзака");
                equipment.TryEquip(state, Instance("itm_backpack_small"), EquipmentSlot.Backpack, out _);
                Equal(30d, equipment.CarryCapacity(state, 20), "с рюкзаком");
            });

            Check("экипировка: экзоскелет добавляет 40 кг", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                True(equipment.TryEquip(state, Instance("itm_armor_exoskeleton"), EquipmentSlot.Armor, out _),
                    "экзоскелет надет");
                Equal(60d, equipment.CarryCapacity(state, 20), "20 базовых + 40 экзоскелета");
            });

            Check("экипировка: быстрый слот берёт оружие", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                True(equipment.TryPutQuick(state, 0, Instance("wpn_pm"), out _), "оружие");
                Equal("wpn_pm", state.GetQuick(0)!.ItemId, "слот");
            });

            Check("экипировка: быстрый слот не берёт броню", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                False(equipment.TryPutQuick(state, 0, Instance("itm_armor_jacket"), out _), "броня");
            });

            Check("экипировка: индекс быстрого слота проверяется", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                Throws<ArgumentOutOfRangeException>(
                    () => equipment.TryPutQuick(state, settings.QuickSlots, Instance("wpn_pm"), out _),
                    "вне диапазона");
            });

            Check("артефакты: без пояса надеть нельзя", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                Equal(0, equipment.ArtifactCapacity(state), "слотов нет");
                False(equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out _), "надевание");
            });

            Check("артефакты: занимают слоты пояса, а не инвентаря", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _);
                Equal(2, equipment.ArtifactCapacity(state), "слотов пояса");

                True(equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out var first), "первый");
                True(equipment.TryEquipArtifact(state, Instance("art_electric_spark"), out var second), "второй");
                False(equipment.TryEquipArtifact(state, Instance("art_chemical_toxin"), out _), "третий лишний");

                Equal(0, first, "индекс первого");
                Equal(1, second, "индекс второго");
                Equal("art_fire_ember", state.Artifacts[0]!.ItemId, "артефакт в поясе");
            });

            Check("артефакты: снятие освобождает слот", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _);
                equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out var index);
                var taken = equipment.TakeArtifact(state, index);
                Equal("art_fire_ember", taken!.ItemId, "снятый артефакт");
                True(equipment.TryEquipArtifact(state, Instance("art_gravity_lens"), out _), "слот свободен");
            });

            Check("пояс: при снятии артефакты уходят в инвентарь", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _);
                equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out _);
                equipment.TryEquipArtifact(state, Instance("art_electric_spark"), out _);

                var grid = new GridContainer("inv", 4, 4);
                True(equipment.TryUnequip(state, EquipmentSlot.Belt, grid, out var removed), "снятие пояса");

                Equal("itm_belt_artifact", removed!.ItemId, "снятый пояс");
                True(state.Get(EquipmentSlot.Belt) == null, "слот пояса пуст");
                Equal(2, CountPlaced(grid), "артефактов в инвентаре");
            });

            Check("пояс: без места в инвентаре снятие блокируется", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _);
                equipment.TryEquipArtifact(state, Instance("art_fire_ember"), out _);
                equipment.TryEquipArtifact(state, Instance("art_electric_spark"), out _);

                var repository = services.Resolve<IItemRepository>();
                var grid = new GridContainer("inv", 1, 1);
                grid.Place(Instance("art_chemical_toxin"), repository.Get("art_chemical_toxin"), 0, 0, false);

                False(equipment.TryUnequip(state, EquipmentSlot.Belt, grid, out _), "снятие заблокировано");
                True(state.Get(EquipmentSlot.Belt) != null, "пояс остался");
                Equal(2, CountArtifacts(state), "артефакты на месте");
            });

            Check("рюкзак: снятие снижает лимит веса, предметы остаются", () =>
            {
                var state = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(state, Instance("itm_backpack_small"), EquipmentSlot.Backpack, out _);

                var repository = services.Resolve<IItemRepository>();
                var grid = new GridContainer("inv", 4, 4);
                grid.SetWeightLimit(equipment.CarryCapacity(state, 20));
                var helmet = Instance("itm_helmet_light");
                grid.Place(helmet, repository.Get("itm_helmet_light"), 0, 0, false);

                equipment.Unequip(state, EquipmentSlot.Backpack);
                grid.SetWeightLimit(equipment.CarryCapacity(state, 20));

                Equal(20d, grid.WeightLimit, "лимит без рюкзака");
                True(!helmet.Overflow, "предмет остался в сетке");
                Equal(1, grid.UsedCells > 0 ? 1 : 0, "предмет на месте");
            });
        }
    }
}
