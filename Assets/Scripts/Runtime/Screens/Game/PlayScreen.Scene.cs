#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.World;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Часть класса <see cref="PlayScreen"/>: переходы как объекты на сцене локации.</summary>
    public sealed partial class PlayScreen
    {
        /// <summary>
        /// Строит объекты-переходы по видимым рёбрам: картинка — вырезка локации-цели
        /// из каталога спрайтов; без вырезки сцена нарисует общий указатель.
        /// Переход на дорогу считается продолжением пути.
        /// </summary>
        /// <param name="edges">Видимые переходы из текущей локации.</param>
        /// <param name="world">Состояние мира с картой маршрута.</param>
        /// <param name="forwardIndex">Номер перехода «дальше по дороге»; <c>-1</c> — такого нет.</param>
        /// <returns>Переходы в порядке показа.</returns>
        private IReadOnlyList<PlayTransition> BuildTransitions(
            IReadOnlyList<WorldEdge> edges,
            WorldState world,
            out int forwardIndex)
        {
            forwardIndex = -1;
            if (edges.Count == 0)
            {
                return Array.Empty<PlayTransition>();
            }

            var transitions = new List<PlayTransition>(edges.Count);
            foreach (var edge in edges)
            {
                var destination = world.Map.GetLocation(edge.ToId);
                var template = _host.Locations.Get(destination.TemplateId);
                var edgeId = edge.EdgeId;
                transitions.Add(new PlayTransition(
                    edgeId,
                    template.SceneArt,
                    () => Travel(edgeId)));

                if (forwardIndex < 0 && destination.Subtype == LocationSubtype.Path)
                {
                    forwardIndex = transitions.Count - 1;
                }
            }

            return transitions;
        }
    }
}
