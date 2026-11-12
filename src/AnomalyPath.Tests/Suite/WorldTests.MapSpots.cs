#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="WorldTests"/>: точки хабов на карте мира.</summary>
    public sealed partial class WorldTests
    {
        private void MapSpotTests(ILocationRepository locations)
        {
            Check("карта: оба хаба размечены на карте мира", () =>
            {
                True(Spot(locations, "loc_hub_settlement") != null, "деревня без точки на карте");
                True(Spot(locations, "loc_hub_depot") != null, "депо без точки на карте");
            });

            Check("карта: точка хаба задана в долях картинки и в правильном порядке", () =>
            {
                var spot = Spot(locations, "loc_hub_depot");
                True(spot != null, "депо без точки на карте");
                Near(0.443, spot!.Left, "левая граница");
                Near(0.355, spot.Bottom, "нижняя граница");
                Near(0.670, spot.Right, "правая граница");
                Near(0.657, spot.Top, "верхняя граница");
                True(spot.Left < spot.Right, "порядок по ширине");
                True(spot.Bottom < spot.Top, "порядок по высоте");
            });

            Check("карта: точка на карте есть только у хабов", () =>
            {
                foreach (var template in locations.All)
                {
                    if (template.WorldMapSpot == null)
                    {
                        continue;
                    }

                    True(template.IsHub, $"точка карты у не-хаба: {template.Id}");
                }
            });
        }

        private void Near(double expected, double actual, string what)
        {
            if (Math.Abs(expected - actual) > 1e-9)
            {
                throw new InvalidOperationException($"{what}: ожидалось {expected}, получено {actual}");
            }
        }

        private WorldMapSpot? Spot(ILocationRepository locations, string id)
        {
            True(locations.Contains(id), $"нет локации {id}");
            return locations.Get(id).WorldMapSpot;
        }
    }
}
