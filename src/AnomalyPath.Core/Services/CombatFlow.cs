#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: бой, ход противника и побег.
    /// Побег с дороги доводит до конца начатый переход между хабами — это делает
    /// <see cref="SessionOutcomes"/>, бой о переходах не знает.
    /// </summary>
    internal sealed class CombatFlow : ICombatFlow
    {
        private readonly ICombatService _combat;
        private readonly ICombatAi? _ai;
        private readonly ITalentService? _talents;
        private readonly IArtifactService? _artifacts;
        private readonly ITravelClock _clock;
        private readonly IStoryQuestHooks _story;

        internal CombatFlow(
            ICombatService combat,
            ICombatAi? ai,
            ITalentService? talents,
            IArtifactService? artifacts,
            ITravelClock clock,
            IStoryQuestHooks story)
        {
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
            _ai = ai;
            _talents = talents;
            _artifacts = artifacts;
            _clock = clock ?? throw new ArgumentNullException(nameof(clock));
            _story = story ?? throw new ArgumentNullException(nameof(story));
        }

        /// <inheritdoc />
        public bool StartCombat(
            GameSession session,
            ActorTemplate playerActor,
            ActorTemplate enemy,
            CombatScenario? scenario = null,
            CombatEncounterContext? context = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.InCombat)
            {
                return false;
            }

            var player = session.State.Player;
            if (player == null)
            {
                return false;
            }

            session.SetCombat(_combat.Start(
                playerActor,
                player.Equipment,
                enemy,
                scenario,
                _talents?.GetModifiers(player),
                player.LevelHealthBonus,
                playerInjuries: player.Injuries,
                playerArtifacts: _artifacts?.GetEquippedModifiers(player.Equipment),
                phase: session.State.World.Clock.Phase,
                nightAccuracyPenalty: session.State.Settings?.NightAccuracyPenalty ?? 0,
                context: context,
                playerApCostBonus: _clock.ApCostBonus(session),
                playerTurnDamage: _clock.TurnDamage(session),
                enemyWeaponSeed: EnemyWeaponSeed(session, enemy, context)));
            return true;
        }

        /// <summary>
        /// Seed выбора оружия противника: seed мира, актёр, ключ боя и номер маршрута.
        /// Один и тот же бой на одной и той же локации даёт одно и то же оружие.
        /// </summary>
        private static int EnemyWeaponSeed(
            GameSession session,
            ActorTemplate enemy,
            CombatEncounterContext? context)
        {
            var world = session.State.World;
            var key = context != null ? context.Key : world.CurrentLocation.InstanceId;
            return StableHash.Combine(
                world.Map.Seed,
                "enemy_weapon:" + enemy.Id + ":" + key + ":" + world.Map.RouteSerial);
        }

        /// <inheritdoc />
        public bool TryAttack(GameSession session, ItemInstance weapon, out CombatAttackResult result)
        {
            result = default;
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var attacked = session.Combat != null && _combat.TryAttack(session.Combat, weapon, out result);
            if (attacked && result.Killed && session.Combat != null)
            {
                _story.OnActorKilled(session, session.Combat.Enemy.Template.Id);
            }

            return attacked;
        }

        /// <inheritdoc />
        public IReadOnlyList<CombatAction> GetCombatActions(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var combat = session.Combat;
            return combat == null || combat.IsFinished || !ReferenceEquals(combat.Current, combat.Player)
                ? Array.Empty<CombatAction>()
                : _combat.GetActions(combat, _combat.PlayerWeapon(combat));
        }

        /// <inheritdoc />
        public bool TryUseCombatSkill(GameSession session, string skillId, out CombatSkillResult result)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            result = CombatSkillResult.Failed(CombatActionAvailability.Finished);
            var combat = session.Combat;
            if (combat == null || combat.IsFinished || !ReferenceEquals(combat.Current, combat.Player))
            {
                return false;
            }

            var used = _combat.TryUseSkill(
                combat,
                _combat.PlayerWeapon(combat),
                skillId,
                out result);
            if (used && result.Killed)
            {
                _story.OnActorKilled(session, combat.Enemy.Template.Id);
            }

            return used;
        }

        /// <inheritdoc />
        public bool TryReload(GameSession session, ItemInstance weapon)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var player = session.State.Player;
            return session.Combat != null &&
                   player != null &&
                   _combat.TryReload(session.Combat, weapon, player.Inventory);
        }

        /// <inheritdoc />
        public void EndTurn(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.Combat != null)
            {
                _combat.EndTurn(session.Combat);
            }
        }

        /// <inheritdoc />
        public bool TryRunEnemyTurnStep(GameSession session, out EnemyTurnAction action)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            action = default!;
            var combat = session.Combat;
            if (combat == null || _ai == null || combat.IsFinished ||
                !ReferenceEquals(combat.Current, combat.Enemy))
            {
                return false;
            }

            return _ai.TryExecuteNext(combat, out action);
        }

        /// <inheritdoc />
        public EnemyTurnResult RunEnemyTurn(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var combat = session.Combat;
            if (combat == null || _ai == null || combat.IsFinished ||
                !ReferenceEquals(combat.Current, combat.Enemy))
            {
                return EnemyTurnResult.Empty;
            }

            var actions = new List<EnemyTurnAction>();
            while (TryRunEnemyTurnStep(session, out var action))
            {
                actions.Add(action);
            }

            var after = session.Combat;
            if (after != null && !after.IsFinished)
            {
                _combat.EndTurn(after);
            }

            return actions.Count == 0
                ? EnemyTurnResult.Empty
                : new EnemyTurnResult(actions, after?.Player.IsDead ?? false);
        }

        /// <inheritdoc />
        public bool TrySwapWeapon(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var combat = session.Combat;
            return combat != null && _combat.TrySwapWeapon(combat);
        }

        /// <inheritdoc />
        public bool TryUseQuickWeapon(GameSession session, int quickIndex)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var combat = session.Combat;
            return combat != null && _combat.TryUseQuickWeapon(combat, quickIndex);
        }

        /// <inheritdoc />
        public bool CanUseMedicine(GameSession session, ItemInstance item)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            var combat = session.Combat;
            return combat != null && _combat.CanUseMedicine(combat, item);
        }

        /// <inheritdoc />
        public bool TryUseMedicine(GameSession session, ItemInstance item, out ConsumableUseResult result)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            result = ConsumableUseResult.Failed;
            var combat = session.Combat;
            if (combat == null)
            {
                return false;
            }

            result = _combat.TryUseMedicine(combat, item);
            return result.Succeeded;
        }

        /// <inheritdoc />
        public bool TryFlee(GameSession session, out FleeResult result)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var combat = session.Combat;
            if (combat == null)
            {
                result = FleeResult.NotInCombat();
                return false;
            }

            if (!_combat.TryFlee(combat, out result))
            {
                return false;
            }

            if (result.Died)
            {
                // Бой остаётся завершённым: вызывающий применяет обычный итог поражения.
                return true;
            }

            session.State.Player?.SetHealth(result.Health);
            var location = session.State.World.CurrentLocation;
            if (combat.Context?.OnRoad != true)
            {
                location.MarkEnemyGone();
                location.MarkEnemyDefeated(combat.Enemy.Template.Id);
            }

            session.SetCombat(null);
            return true;
        }

        /// <inheritdoc />
        public void EndCombat(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            session.SetCombat(null);
        }
    }
}
