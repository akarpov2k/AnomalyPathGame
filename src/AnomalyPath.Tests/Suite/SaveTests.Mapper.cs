#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="SaveTests"/>: маппер мира.</summary>
    public sealed partial class SaveTests
    {
        private void MapperTests(IWorldGenerator generator)
        {
            Check("маппер мира: снимок и восстановление", () =>
            {
                var map = generator.Generate(7);
                var state = WorldState.Start(map);
                state.NextLootSerial();
                state.SetHourCarry(0.5);
                var snapshot = WorldSaveMapper.ToSaveData(new GameState(state, null));
                var loaded = WorldSaveMapper.LoadWorld(snapshot, map.Seed, "slot_1", null);
                Equal(state.CurrentLocationId, loaded.CurrentLocationId, "локация");
                Equal(state.Clock.Hours, loaded.Clock.Hours, "часы");
                Equal(0.5, loaded.HourCarry, "неполный час травм");
                Equal(1, loaded.LootSerial, "счётчик бросков");
                Equal(map.Locations.Count, loaded.Map.Locations.Count, "локации");
            });
        }
    }
}
