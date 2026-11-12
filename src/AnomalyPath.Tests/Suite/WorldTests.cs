#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Tests
{
    /// <summary>Мир: шаблоны, топология, генерация маршрутов, переходы и время.</summary>
    public sealed partial class WorldTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов мира.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public WorldTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            var services = BuildServices();
            var generator = services.Resolve<IWorldGenerator>();
            var travel = services.Resolve<ITravelService>();
            var locations = services.Resolve<ILocationRepository>();
            var topology = services.Resolve<ITopologyRepository>().Topology;

            TemplateTests();
            WalkPartsTests(locations);
            MapSpotTests(locations);
            GenerationTests(generator, locations, topology);
            BranchTests(generator, topology);
            DepartureTests(generator);
            PruneTests();
            MapMutationTests(generator);
            TravelTests(generator, travel);
            ValidationTests();
        }
    }
}
