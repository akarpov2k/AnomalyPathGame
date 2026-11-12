#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>Вариант найденного артефакта: выпавшая редкость и номера доп. свойств.</summary>
    public sealed class ArtifactVariant
    {
        /// <summary>Создаёт вариант артефакта.</summary>
        /// <param name="rarity">Выпавшая ступень редкости.</param>
        /// <param name="extraIndexes">Номера доп. свойств из пула шаблона.</param>
        /// <exception cref="ArgumentNullException">Список номеров не задан.</exception>
        public ArtifactVariant(Rarity rarity, IReadOnlyList<int> extraIndexes)
        {
            Rarity = rarity;
            ExtraIndexes = extraIndexes ?? throw new ArgumentNullException(nameof(extraIndexes));
        }

        /// <summary>Выпавшая ступень редкости.</summary>
        public Rarity Rarity { get; }

        /// <summary>Номера доп. свойств из пула шаблона.</summary>
        public IReadOnlyList<int> ExtraIndexes { get; }
    }
}
