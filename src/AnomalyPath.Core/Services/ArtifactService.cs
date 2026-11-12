#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Артефакты: бросок варианта по таблице редкости и сборка модификаторов надетого пояса.
    /// Бросок детерминирован seed-ом и не тратит случайную последовательность пула лута.
    /// </summary>
    public sealed class ArtifactService : IArtifactService
    {
        private readonly IItemRepository _items;
        private readonly ArtifactSettings _settings;

        /// <summary>Создаёт сервис артефактов.</summary>
        /// <param name="items">Репозиторий предметов: модификаторы и пул доп. свойств.</param>
        /// <param name="settings">Настройка редкости артефактов и затухания модификаторов.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public ArtifactService(IItemRepository items, ArtifactSettings settings)
        {
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <inheritdoc />
        public bool IsArtifact(string itemId) =>
            !string.IsNullOrWhiteSpace(itemId) &&
            _items.TryGet(itemId, out var template) &&
            template != null &&
            template.Category == ItemCategory.Artifact;

        /// <inheritdoc />
        public string? RollVariantKey(string itemId, int seed)
        {
            if (!IsArtifact(itemId) || !_items.TryGet(itemId, out var template) || template == null)
            {
                return null;
            }

            var random = new SeededWorldRandom(seed);
            var rule = _settings.RollRarity(random);
            var pool = template.ExtraProperties.Count;
            var count = ExtraCount(rule, pool, random);
            var indexes = PickDistinct(pool, count, random);
            return ArtifactVariantCodec.Format(rule.Rarity, indexes);
        }

        /// <inheritdoc />
        public ArtifactModifiers GetModifiers(ItemInstance artifact)
        {
            if (artifact == null)
            {
                throw new ArgumentNullException(nameof(artifact));
            }

            var parts = new List<ArtifactModifier>();
            Collect(artifact, parts);
            return new ArtifactModifiers(parts, _settings.Falloff);
        }

        /// <inheritdoc />
        public ArtifactModifiers GetEquippedModifiers(EquipmentState equipment)
        {
            if (equipment == null)
            {
                throw new ArgumentNullException(nameof(equipment));
            }

            var parts = new List<ArtifactModifier>();
            foreach (var artifact in equipment.Artifacts)
            {
                if (artifact == null || artifact.IsBroken)
                {
                    continue;
                }

                Collect(artifact, parts);
            }

            return new ArtifactModifiers(parts, _settings.Falloff);
        }

        /// <summary>Добавляет в список действующие модификаторы экземпляра с учётом редкости.</summary>
        private void Collect(ItemInstance artifact, List<ArtifactModifier> parts)
        {
            if (!_items.TryGet(artifact.ItemId, out var template) || template == null ||
                template.Category != ItemCategory.Artifact)
            {
                return;
            }

            var positive = 1.0;
            var negative = 1.0;
            ArtifactVariant? variant = null;
            if (ArtifactVariantCodec.TryParse(artifact.VariantKey, out var parsed) && parsed != null &&
                _settings.TryGet(parsed.Rarity, out var rule) && rule != null)
            {
                variant = parsed;
                positive = rule.PositiveFactor;
                negative = rule.NegativeFactor;
            }

            foreach (var modifier in template.Modifiers)
            {
                parts.Add(modifier.Scale(positive, negative));
            }

            if (variant == null)
            {
                return;
            }

            foreach (var index in variant.ExtraIndexes)
            {
                if (index >= 0 && index < template.ExtraProperties.Count)
                {
                    parts.Add(template.ExtraProperties[index].Scale(positive, negative));
                }
            }
        }

        /// <summary>Сколько доп. свойств взять: в пределах пула и диапазона ступени.</summary>
        private static int ExtraCount(ArtifactRarityRule rule, int pool, SeededWorldRandom random)
        {
            if (pool <= 0 || rule.ExtraMax <= 0)
            {
                return 0;
            }

            var minimum = Math.Min(rule.ExtraMin, pool);
            var maximum = Math.Min(rule.ExtraMax, pool);
            return minimum >= maximum ? minimum : random.NextInt(minimum, maximum + 1);
        }

        /// <summary>Выбирает разные номера из пула: детерминированная перестановка.</summary>
        private static IReadOnlyList<int> PickDistinct(int pool, int count, SeededWorldRandom random)
        {
            var indexes = new List<int>(count);
            if (count <= 0 || pool <= 0)
            {
                return indexes;
            }

            var available = new List<int>(pool);
            for (var index = 0; index < pool; index++)
            {
                available.Add(index);
            }

            var take = Math.Min(count, pool);
            for (var index = 0; index < take; index++)
            {
                var chosen = random.NextInt(index, available.Count);
                var value = available[chosen];
                available[chosen] = available[index];
                available[index] = value;
                indexes.Add(value);
            }

            return indexes;
        }
    }
}
