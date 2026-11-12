#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Services;

namespace AnomalyPath.Core
{
    /// <summary>
    /// Сборка приложения: проверка данных, репозитории, сервисы, контейнер.
    /// UI и MonoBehaviour сюда не входят: всё это чистый C#.
    /// </summary>
    public static partial class Bootstrap
    {
        /// <summary>
        /// Собирает игровую сессию из готовых сервисов: единственное место, где известны
        /// соисполнители фасада. Вызывается композиционным корнем — <c>GameBootstrap</c>
        /// и тесты, которым нужна сессия; потребители получают <see cref="IGameSessionService"/>.
        /// </summary>
        /// <param name="travel">Переходы по миру.</param>
        /// <param name="combat">Боевой расчёт.</param>
        /// <param name="saves">Сохранение и загрузка состояния.</param>
        /// <param name="newGame">Создание новой игры.</param>
        /// <param name="death">Последствия смерти для вещей.</param>
        /// <param name="outcomes">Итог боя: опыт, лут и респавн.</param>
        /// <param name="quests">Квесты: учёт убийств в бою; <c>null</c> — не учитывать.</param>
        /// <param name="talents">Таланты: бонусы игрока в бою; <c>null</c> — без бонусов.</param>
        /// <param name="progression">Прогрессия: подтягивание уровня при загрузке; <c>null</c> — не подтягивать.</param>
        /// <param name="stashes">Тайники на локациях; <c>null</c> — тайников нет.</param>
        /// <param name="locationObjects">Объекты локаций; <c>null</c> — только тайники.</param>
        /// <param name="worldGenerator">Достройка маршрута при переходах; <c>null</c> — маршрут не растёт.</param>
        /// <param name="topology">Топология: цена возврата в хаб; <c>null</c> — бесплатно.</param>
        /// <param name="ai">ИИ противника; <c>null</c> — ход противника не исполняется.</param>
        /// <param name="anomalies">Поиск в аномалиях; <c>null</c> — аномалии не обрабатываются.</param>
        /// <param name="artifacts">Артефакты: бонусы надетого пояса в бою; <c>null</c> — без бонусов.</param>
        /// <param name="storyQuests">Сюжетные квесты: прогресс по событиям мира; <c>null</c> — без учёта.</param>
        /// <param name="spawns">Записи спавна: пулы встреч на дороге; <c>null</c> — встреч нет.</param>
        /// <param name="actors">Актёры: противник и игрок для боя на дороге; <c>null</c> — встреч нет.</param>
        /// <param name="rules">Правила прохождения: id актёра игрока; <c>null</c> — встреч нет.</param>
        /// <param name="needs">Потребности: расход еды и воды, штрафы голода и урон жажды; <c>null</c> — без них.</param>
        /// <param name="locations">
        /// Шаблоны локаций: признак «контент в разработке» у целевого хаба; <c>null</c> — переходы
        /// не ограничиваются.
        /// </param>
        /// <returns>Готовый фасад сессии.</returns>
        /// <exception cref="ArgumentNullException">Обязательная зависимость не задана.</exception>
        public static GameSessionService CreateSession(
            ITravelService travel,
            ICombatService combat,
            ISaveService saves,
            INewGameService newGame,
            IDeathService death,
            ICombatOutcomeService outcomes,
            IQuestService? quests = null,
            ITalentService? talents = null,
            IProgressionService? progression = null,
            IStashService? stashes = null,
            ILocationObjectService? locationObjects = null,
            IWorldGenerator? worldGenerator = null,
            ITopologyRepository? topology = null,
            ICombatAi? ai = null,
            IAnomalyService? anomalies = null,
            IArtifactService? artifacts = null,
            IStoryQuestService? storyQuests = null,
            ISpawnRepository? spawns = null,
            IActorRepository? actors = null,
            IGameRulesRepository? rules = null,
            INeedsService? needs = null,
            ILocationRepository? locations = null)
        {
            var hooks = new StoryQuestHooks(storyQuests, quests);
            var clock = new TravelClock(needs, quests, hooks, rules);
            var entry = new LocationEntry(stashes, locationObjects);
            var route = new RouteTravel(travel, worldGenerator, topology, clock, hooks, entry);
            var combatFlow = new CombatFlow(combat, ai, talents, artifacts, clock, hooks);
            var encounters = new RoadEncounterService(spawns, actors, rules, combatFlow);
            var hubTravel = new HubTravelFlow(topology, encounters, combatFlow, clock, hooks, locations);
            var rest = new RestFlow(topology, talents, artifacts, encounters, clock, combatFlow);
            var stashFlow = new StashFlow(stashes, hooks);
            var anomalyFlow = new AnomalyFlow(anomalies, hooks);
            var outcomeFlow = new SessionOutcomes(death, outcomes, combatFlow, hubTravel, hooks);
            var lifecycle = new SessionLifecycle(saves, newGame, progression, entry);
            return new GameSessionService(
                lifecycle,
                route,
                combatFlow,
                outcomeFlow,
                hubTravel,
                rest,
                entry,
                stashFlow,
                anomalyFlow,
                clock);
        }
    }
}
