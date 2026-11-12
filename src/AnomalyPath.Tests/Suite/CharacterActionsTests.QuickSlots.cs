#nullable enable
using System;
using AnomalyPath.Core.Character;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="CharacterActionsTests"/>: быстрые слоты.</summary>
    public sealed partial class CharacterActionsTests
    {
        private const string QuickStim = "stim_quick_test";
        private const string QuickFood = "food_quick_test";

        private const string QuickStimJson =
            "{\"kind\":\"items\",\"id\":\"stim_quick_test\",\"nameKey\":\"n\",\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"," +
            "\"consumable\":{\"useTime\":1,\"charges\":1,\"usableInCombat\":true,\"effects\":[{\"type\":\"heal\",\"amount\":5}]}}";

        private const string QuickFoodJson =
            "{\"kind\":\"items\",\"id\":\"food_quick_test\",\"nameKey\":\"n\",\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"," +
            "\"equipSlots\":[],\"consumable\":{\"useTime\":1,\"charges\":1,\"usableInCombat\":false,\"effects\":[{\"type\":\"restoreFood\",\"amount\":10}]}}";

        private void RunQuickSlots()
        {
            Check("действия: расходник из сетки встаёт в быстрый слот", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var stim = Add(inventory, grid, QuickStim);
                var state = NewEquipment();

                var result = actions.PutQuick(grid, state, stim.InstanceId, 1);

                Equal(CharacterActionResult.Ok, result, "итог");
                True(ReferenceEquals(stim, state.GetQuick(1)), "в слоте тот же экземпляр");
                True(grid.Find(stim.InstanceId) == null, "из сетки убран");
            });

            Check("действия: еда и небоевая медицина в быстрый слот не кладутся", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var food = Add(inventory, grid, QuickFood);
                var state = NewEquipment();

                var result = actions.PutQuick(grid, state, food.InstanceId, 0);

                Equal(CharacterActionResult.NotQuickItem, result, "итог");
                True(state.GetQuick(0) == null, "слот пуст");
                True(grid.Find(food.InstanceId) != null, "предмет остался в сетке");
            });

            Check("действия: занятый быстрый слот заменяется, прежний предмет возвращается", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var stim = Add(inventory, grid, QuickStim);
                var state = NewEquipment();
                Equal(CharacterActionResult.Ok, actions.PutQuick(grid, state, stim.InstanceId, 0), "первый предмет");
                var rifle = Add(inventory, grid, TestData.Rifle);

                var result = actions.PutQuick(grid, state, rifle.InstanceId, 0);

                Equal(CharacterActionResult.Ok, result, "итог");
                True(ReferenceEquals(rifle, state.GetQuick(0)), "в слоте новое оружие");
                True(grid.Find(stim.InstanceId) == null, "прежний предмет вернулся новым экземпляром");
                True(grid.Find(rifle.InstanceId) == null, "оружие ушло из сетки");
                Equal(1, grid.Items.Count, "в сетке только возвращённый предмет");
                Equal(QuickStim, grid.Items[0].ItemId, "вернулся тот же вид");
            });

            Check("действия: без места в сетке замена быстрого слота не выполняется", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 2);
                var part = Template(TestData.PartJson);
                for (var y = 0; y < 2; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        if (x == 7 && y == 1)
                        {
                            continue;
                        }

                        grid.Place(new ItemInstance("part_" + x + "_" + y, TestData.Part, 1), part, x, y, false);
                    }
                }

                var stimTemplate = Template(QuickStimJson);
                var stim = new ItemInstance("quick_stim_full", QuickStim, 1);
                grid.Place(stim, stimTemplate, 7, 1, false);
                var state = NewEquipment();
                var rifle = new ItemInstance("quick_rifle_full", TestData.Rifle, 1);
                state.SetQuick(0, rifle);

                var result = actions.PutQuick(grid, state, stim.InstanceId, 0);

                Equal(CharacterActionResult.NoRoom, result, "итог");
                True(ReferenceEquals(rifle, state.GetQuick(0)), "оружие осталось в слоте");
                True(grid.Find(stim.InstanceId) != null, "расходник остался в сетке");
            });

            Check("действия: предмет переставляется между быстрыми слотами", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var state = NewEquipment();
                var stim = new ItemInstance("quick_stim_swap", QuickStim, 1);
                var rifle = new ItemInstance("quick_rifle_swap", TestData.Rifle, 1);
                state.SetQuick(0, stim);
                state.SetQuick(1, rifle);

                var result = actions.PutQuick(grid, state, rifle.InstanceId, 0);

                Equal(CharacterActionResult.Ok, result, "итог");
                True(ReferenceEquals(rifle, state.GetQuick(0)), "оружие в первом слоте");
                True(ReferenceEquals(stim, state.GetQuick(1)), "расходник во втором слоте");
                Equal(0, grid.Items.Count, "сетка не менялась");
            });

            Check("действия: предмет убирается из быстрого слота в сетку", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var state = NewEquipment();
                state.SetQuick(2, new ItemInstance("quick_stim_take", QuickStim, 1));

                var result = actions.TakeQuick(grid, state, 2);

                Equal(CharacterActionResult.Ok, result, "итог");
                True(state.GetQuick(2) == null, "слот пуст");
                Equal(1, grid.Items.Count, "предмет вернулся в сетку");
                Equal(QuickStim, grid.Items[0].ItemId, "вернулся тот же вид");
            });

            Check("действия: пустой быстрый слот убирать нечего", () =>
            {
                var (actions, _, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var state = NewEquipment();

                var result = actions.TakeQuick(grid, state, 0);

                Equal(CharacterActionResult.ItemMissing, result, "итог");
            });

            Check("действия: без места в сетке предмет остаётся в быстром слоте", () =>
            {
                var (actions, inventory, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 2);
                var part = Template(TestData.PartJson);
                for (var y = 0; y < 2; y++)
                {
                    for (var x = 0; x < 8; x++)
                    {
                        grid.Place(new ItemInstance("fill_" + x + "_" + y, TestData.Part, 1), part, x, y, false);
                    }
                }

                var stim = new ItemInstance("quick_stim_noroom", QuickStim, 1);
                var state = NewEquipment();
                state.SetQuick(0, stim);

                var result = actions.TakeQuick(grid, state, 0);

                Equal(CharacterActionResult.NoRoom, result, "итог");
                True(ReferenceEquals(stim, state.GetQuick(0)), "предмет остался в слоте");
                Equal(16, grid.Items.Count, "сетка не менялась");
            });

            Check("действия: индекс быстрого слота проверяется", () =>
            {
                var (actions, _, _) = NewQuickActions();
                var grid = new GridContainer("g", 8, 4);
                var state = NewEquipment();

                Throws<ArgumentOutOfRangeException>(
                    () => actions.PutQuick(grid, state, "missing", 4),
                    "PutQuick за пределами слотов");
                Throws<ArgumentOutOfRangeException>(
                    () => actions.TakeQuick(grid, state, -1),
                    "TakeQuick за пределами слотов");
            });
        }

        private (CharacterActions Actions, InventoryService Inventory, EquipmentService Equipment) NewQuickActions()
        {
            var items = new ItemRepository(new[]
            {
                Template(QuickStimJson),
                Template(QuickFoodJson),
                Template(TestData.RifleJson),
                Template(TestData.PartJson)
            });
            var inventory = new InventoryService(items, new SequentialInstanceIdGenerator());
            var equipment = new EquipmentService(items, inventory, TestArtifacts.Service(items));
            return (new CharacterActions(items, inventory, equipment), inventory, equipment);
        }
    }
}
