#nullable enable
using System;
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Повторяемые квесты: слоты доски объявлений хаба, таймеры обновления,
    /// взятие и отказ, прогресс по событиям и сдача на той же доске.
    /// </summary>
    public sealed partial class QuestService : IQuestService
    {
        private readonly IQuestRepository _quests;
        private readonly IActorRepository _actors;
        private readonly ILocationRepository _locations;
        private readonly ITopologyRepository _topology;
        private readonly IItemRepository _items;
        private readonly IGameRulesRepository _rules;

        /// <summary>Создаёт сервис квестов.</summary>
        /// <param name="quests">Шаблоны квестов.</param>
        /// <param name="actors">Актёры: пулы мутантов и NPC.</param>
        /// <param name="locations">Шаблоны локаций: пул целей и маршрут.</param>
        /// <param name="topology">Топология: способы перехода.</param>
        /// <param name="items">Предметы: пулы артефактов, оружия и брони, прочность наград.</param>
        /// <param name="rules">Правила: число предложений, откат и предел активных.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public QuestService(
            IQuestRepository quests,
            IActorRepository actors,
            ILocationRepository locations,
            ITopologyRepository topology,
            IItemRepository items,
            IGameRulesRepository rules)
        {
            _quests = quests ?? throw new ArgumentNullException(nameof(quests));
            _actors = actors ?? throw new ArgumentNullException(nameof(actors));
            _locations = locations ?? throw new ArgumentNullException(nameof(locations));
            _topology = topology ?? throw new ArgumentNullException(nameof(topology));
            _items = items ?? throw new ArgumentNullException(nameof(items));
            _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        }
    }
}
