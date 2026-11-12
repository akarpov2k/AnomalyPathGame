#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Tests
{
    /// <summary>Сохранения: JSON-писатель, round-trip мира и игрока, слоты и ошибки.</summary>
    public sealed partial class SaveTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов сохранений.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public SaveTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            JsonWriterTests();
            var services = BuildServices();
            var generator = services.Resolve<IWorldGenerator>();
            var travel = services.Resolve<ITravelService>();
            var items = services.Resolve<IItemRepository>();
            var spawns = services.Resolve<ISpawnRepository>();
            SlotNameTests();
            RoundTripTests(generator, travel, items, spawns);
            MapperTests(generator);
            PlayerTests(generator, items);
            StorageTests(generator, items);
        }
    }
}
