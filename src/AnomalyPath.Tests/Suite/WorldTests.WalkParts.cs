#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="WorldTests"/>: части фона похода в данных локаций.</summary>
    public sealed partial class WorldTests
    {
        private void WalkPartsTests(ILocationRepository locations)
        {
            Check("фон похода: у каждой дикой локации три части и часть с дорогой", () =>
            {
                var checkedCount = 0;
                foreach (var location in locations.All)
                {
                    if (location.LocationKind == LocationKind.Hub)
                    {
                        continue;
                    }

                    Equal(3, location.WalkParts.Count, location.Id + ": частей");
                    checkedCount++;
                }

                True(checkedCount >= 20, "диких локаций: " + checkedCount);
            });

            Check("фон похода: у частей задана доля вертикальной тропы", () =>
            {
                var withPaths = 0;
                foreach (var location in locations.All)
                {
                    if (location.WalkPaths.Count == 0)
                    {
                        continue;
                    }

                    Equal(3, location.WalkPaths.Count, location.Id + ": долей тропы");
                    foreach (var path in location.WalkPaths)
                    {
                        True(path is >= 0 and <= 1, location.Id + ": доля в границах 0..1");
                    }

                    withPaths++;
                }

                True(withPaths >= 5, "локаций с долями тропы: " + withPaths);
            });
        }
    }
}
