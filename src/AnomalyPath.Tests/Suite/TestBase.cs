#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    public abstract partial class TestBase
    {
        private int _passed;
        private int _failed;
        private readonly List<string> _failures = new List<string>();

        /// <summary>Куда писать строки результата.</summary>
        public Action<string> Log { get; set; } = Console.WriteLine;

        /// <summary>Сколько проверок прошло.</summary>
        public int Passed => _passed;

        /// <summary>Сколько проверок упало.</summary>
        public int Failed => _failed;

        /// <summary>Описания упавших проверок.</summary>
        public IReadOnlyList<string> Failures => _failures;

        /// <summary>Прогоняет проверки категории.</summary>
        public abstract void Run();
        protected IEnumerable<string> SourceFiles(string dataRoot)
        {
            var srcRoot = Path.GetFullPath(Path.Combine(dataRoot, "..", "..", "src"));
            if (!Directory.Exists(srcRoot))
            {
                yield break;
            }

            foreach (var file in Directory.EnumerateFiles(srcRoot, "*.cs", SearchOption.AllDirectories))
            {
                yield return file;
            }
        }

        protected ItemInstance Instance(string itemId) => new ItemInstance("inst_" + itemId, itemId, 1);

        /// <summary>Кладёт предмет в первую свободную клетку сетки игрока; без места — исключение.</summary>
        /// <param name="player">Состояние игрока.</param>
        /// <param name="items">Репозиторий предметов.</param>
        /// <param name="itemId">Id предмета.</param>
        /// <returns>Созданный экземпляр, уже лежащий в сетке.</returns>
        protected static ItemInstance AddToInventory(PlayerState player, IItemRepository items, string itemId)
        {
            var template = items.Get(itemId);
            var item = new ItemInstance("inst_" + itemId, itemId, 1, charges: template.Consumable?.Charges);
            for (var y = 0; y < player.Inventory.Rows; y++)
            {
                for (var x = 0; x < player.Inventory.Width; x++)
                {
                    if (player.Inventory.CanPlace(template, x, y, rotated: false))
                    {
                        player.Inventory.Place(item, template, x, y, rotated: false);
                        return item;
                    }
                }
            }

            throw new InvalidOperationException("Нет места для предмета: " + itemId);
        }

        protected int CountPlaced(GridContainer grid)
        {
            var count = 0;
            foreach (var item in grid.Items)
            {
                if (!item.Overflow)
                {
                    count++;
                }
            }

            return count;
        }

        protected int CountArtifacts(Equipment equipment)
        {
            var count = 0;
            foreach (var artifact in equipment.Artifacts)
            {
                if (artifact != null)
                {
                    count++;
                }
            }

            return count;
        }

        protected (IInventoryService Service, GridContainer Grid) NewInventory(int width, int rows, double overloadKg = 0)
        {
            var templates = new[]
            {
                Template(TestData.RifleJson),
                Template(TestData.AmmoJson),
                Template(TestData.CrateJson),
                Template(TestData.PartJson)
            };

            var repository = new ItemRepository(templates);
            var service = new InventoryService(repository, new SequentialInstanceIdGenerator(), overloadKg);
            return (service, new GridContainer("test", width, rows));
        }

        protected ItemTemplate Template(string json)
        {
            return ItemTemplate.FromJson(JsonParser.Parse(json, "test"), "test");
        }

        protected int PlacedCount(GridContainer grid)
        {
            var count = 0;
            foreach (var item in grid.Items)
            {
                if (!item.Overflow)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Путь к данным, переданный раннером (в Unity автопоиск от каталога редактора не работает).</summary>
    }
}
