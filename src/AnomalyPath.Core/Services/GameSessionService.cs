#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Death;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Действия сессии поверх сохранений, переходов и боя; состояние хранится в <see cref="GameSession"/>.
    /// Фасад только переадресует вызовы соисполнителям: жизненный цикл, переходы по маршруту,
    /// переходы между хабами, отдых, бой, итоги, тайник, аномалии и часы мира. Собирается
    /// в <see cref="Bootstrap.CreateSession"/> — единственном месте, где известны реализации.
    /// </summary>
    public sealed class GameSessionService : IGameSessionService
    {
        private readonly ISessionLifecycle _lifecycle;
        private readonly IRouteTravel _route;
        private readonly ICombatFlow _combat;
        private readonly ISessionOutcomes _outcomes;
        private readonly IHubTravelFlow _hubTravel;
        private readonly IRestFlow _rest;
        private readonly ILocationEntry _entry;
        private readonly IStashFlow _stashes;
        private readonly IAnomalyFlow _anomalies;
        private readonly ITravelClock _clock;

        /// <summary>Создаёт фасад из готовых соисполнителей; сборку выполняет <see cref="Bootstrap.CreateSession"/>.</summary>
        /// <param name="lifecycle">Сохранение, загрузка и новая игра.</param>
        /// <param name="route">Переходы по маршруту.</param>
        /// <param name="combat">Бой и ход противника.</param>
        /// <param name="outcomes">Смерть, итог боя и побег.</param>
        /// <param name="hubTravel">Переходы между хабами.</param>
        /// <param name="rest">Отдых.</param>
        /// <param name="entry">Вход на локацию: тайник и объекты.</param>
        /// <param name="stashes">Тайник: вскрытие и замок.</param>
        /// <param name="anomalies">Поиск артефакта в аномалии.</param>
        /// <param name="clock">Время мира и потребности.</param>
        /// <exception cref="ArgumentNullException">Соисполнитель не задан.</exception>
        internal GameSessionService(
            ISessionLifecycle lifecycle,
            IRouteTravel route,
            ICombatFlow combat,
            ISessionOutcomes outcomes,
            IHubTravelFlow hubTravel,
            IRestFlow rest,
            ILocationEntry entry,
            IStashFlow stashes,
            IAnomalyFlow anomalies,
            ITravelClock clock)
        {
            _lifecycle = lifecycle ?? throw new ArgumentNullException(nameof(lifecycle));
            _route = route ?? throw new ArgumentNullException(nameof(route));
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _outcomes = outcomes ?? throw new ArgumentNullException(nameof(outcomes));
            _hubTravel = hubTravel ?? throw new ArgumentNullException(nameof(hubTravel));
            _rest = rest ?? throw new ArgumentNullException(nameof(rest));
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
            _stashes = stashes ?? throw new ArgumentNullException(nameof(stashes));
            _anomalies = anomalies ?? throw new ArgumentNullException(nameof(anomalies));
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        }

        /// <inheritdoc />
        public void Save(
            GameSession session,
            string slot,
            double playtime = 0,
            DateTimeOffset? createdAt = null,
            string? title = null) =>
            _lifecycle.Save(session, slot, playtime, createdAt, title);

        /// <inheritdoc />
        public GameSession Load(string slot) => _lifecycle.Load(slot);

        /// <inheritdoc />
        public GameSession StartNew(int seed, string loadoutId, NewGameSettings settings) =>
            _lifecycle.StartNew(seed, loadoutId, settings);

        /// <inheritdoc />
        public DeathResult ApplyDeath(GameSession session) => _outcomes.ApplyDeath(session);

        /// <inheritdoc />
        public CombatOutcome ApplyCombatOutcome(GameSession session) => _outcomes.ApplyCombatOutcome(session);

        /// <inheritdoc />
        public IReadOnlyList<WorldEdge> AvailableEdges(GameSession session) => _route.AvailableEdges(session);

        /// <inheritdoc />
        public TravelResult Travel(GameSession session, string edgeId) => _route.Travel(session, edgeId);

        /// <inheritdoc />
        public TravelResult LeaveHub(GameSession session) => _route.LeaveHub(session);

        /// <inheritdoc />
        public TravelResult ReturnToHub(GameSession session) => _route.ReturnToHub(session);

        /// <inheritdoc />
        public HubTravelQuote GetHubTravelQuote(GameSession session, string targetHubId, HubTravelMode mode) =>
            _hubTravel.GetHubTravelQuote(session, targetHubId, mode);

        /// <inheritdoc />
        public int GetTravelStaminaCost(GameSession session, int baseStamina) =>
            _clock.GetTravelStaminaCost(session, baseStamina);

        /// <inheritdoc />
        public HubTravelResult TravelToHub(GameSession session, string targetHubId, HubTravelMode mode) =>
            _hubTravel.TravelToHub(session, targetHubId, mode);

        /// <inheritdoc />
        public RestQuote GetRestQuote(GameSession session) => _rest.GetRestQuote(session);

        /// <inheritdoc />
        public RestResult Rest(GameSession session, int hours) => _rest.Rest(session, hours);

        /// <inheritdoc />
        public bool RollLocationStash(GameSession session) => _entry.RollStash(session);

        /// <inheritdoc />
        public StashOpenResult OpenStash(GameSession session) => _stashes.OpenStash(session);

        /// <inheritdoc />
        public StashLockState? LockState(GameSession session) => _stashes.LockState(session);

        /// <inheritdoc />
        public StashLockState? GuessStashLock(GameSession session, string guess) =>
            _stashes.Guess(session, guess);

        /// <inheritdoc />
        public RadarState? OpenAnomalyRadar(GameSession session, AnomalyLocationObject anomaly) =>
            _anomalies.OpenRadar(session, anomaly);

        /// <inheritdoc />
        public RadarScanResult ScanAnomaly(GameSession session, AnomalyLocationObject anomaly, int cell) =>
            _anomalies.Scan(session, anomaly, cell);

        /// <inheritdoc />
        public AnomalySearchResult SearchAnomalyCell(GameSession session, AnomalyLocationObject anomaly, int cell) =>
            _anomalies.SearchCell(session, anomaly, cell);

        /// <inheritdoc />
        public bool StartCombat(
            GameSession session,
            ActorTemplate playerActor,
            ActorTemplate enemy,
            CombatScenario? scenario = null,
            CombatEncounterContext? context = null) =>
            _combat.StartCombat(session, playerActor, enemy, scenario, context);

        /// <inheritdoc />
        public bool TryAttack(GameSession session, ItemInstance weapon, out CombatAttackResult result) =>
            _combat.TryAttack(session, weapon, out result);

        /// <inheritdoc />
        public IReadOnlyList<CombatAction> GetCombatActions(GameSession session) =>
            _combat.GetCombatActions(session);

        /// <inheritdoc />
        public bool TryUseCombatSkill(GameSession session, string skillId, out CombatSkillResult result) =>
            _combat.TryUseCombatSkill(session, skillId, out result);

        /// <inheritdoc />
        public bool TryReload(GameSession session, ItemInstance weapon) => _combat.TryReload(session, weapon);

        /// <inheritdoc />
        public void EndTurn(GameSession session) => _combat.EndTurn(session);

        /// <inheritdoc />
        public EnemyTurnResult RunEnemyTurn(GameSession session) => _combat.RunEnemyTurn(session);

        /// <inheritdoc />
        public bool TryRunEnemyTurnStep(GameSession session, out EnemyTurnAction action) =>
            _combat.TryRunEnemyTurnStep(session, out action);

        /// <inheritdoc />
        public bool TrySwapWeapon(GameSession session) => _combat.TrySwapWeapon(session);

        /// <inheritdoc />
        public bool TryUseQuickWeapon(GameSession session, int quickIndex) =>
            _combat.TryUseQuickWeapon(session, quickIndex);

        /// <inheritdoc />
        public bool CanUseMedicine(GameSession session, ItemInstance item) =>
            _combat.CanUseMedicine(session, item);

        /// <inheritdoc />
        public bool TryUseMedicine(GameSession session, ItemInstance item, out ConsumableUseResult result) =>
            _combat.TryUseMedicine(session, item, out result);

        /// <inheritdoc />
        public bool TryFlee(GameSession session, out FleeResult result) => _outcomes.TryFlee(session, out result);

        /// <inheritdoc />
        public PlayerDamageResult? AdvanceWalk(GameSession session, double minutes)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.InCombat || minutes <= 0 || double.IsNaN(minutes) || double.IsInfinity(minutes))
            {
                return null;
            }

            var hours = minutes / 60.0;
            session.State.World.Clock.Advance(hours);
            _clock.Advance(session, hours);
            return _clock.Thirst(session);
        }

        /// <inheritdoc />
        public void EndCombat(GameSession session) => _combat.EndCombat(session);
    }
}
