#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    /// <summary>Тестовые настройки артефактов: редкость, множители и пустые аномалии.</summary>
    internal static class TestArtifacts
    {
        internal static ArtifactSettings Settings() =>
            new ArtifactSettings(1, new[]
            {
                Rule(Rarity.Common, 0.45, 1.0, 1.0, 0, 0),
                Rule(Rarity.Rare, 0.25, 1.1, 0.9, 0, 0),
                Rule(Rarity.Special, 0.15, 1.25, 0.75, 0, 0),
                Rule(Rarity.Epic, 0.09, 1.4, 0.6, 0, 0),
                Rule(Rarity.Legendary, 0.045, 1.6, 0.4, 1, 2),
                Rule(Rarity.Unique, 0.015, 1.8, 0.2, 1, 2)
            });

        internal static ArtifactService Service(IItemRepository items) => new ArtifactService(items, Settings());

        internal static AnomalyRepository NoAnomalies() =>
            new AnomalyRepository(Array.Empty<AnomalyTemplate>());

        private static ArtifactRarityRule Rule(
            Rarity rarity,
            double chance,
            double positive,
            double negative,
            int extraMin,
            int extraMax) =>
            new ArtifactRarityRule(rarity, chance, positive, negative, extraMin, extraMax);
    }
}
