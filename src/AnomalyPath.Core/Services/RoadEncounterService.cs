#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: встреча на дороге. Бросок детерминирован
    /// ключом перехода, а старт боя общий для дороги между хабами и нападения во время отдыха.
    /// </summary>
    internal sealed class RoadEncounterService : IRoadEncounterService
    {
        private readonly ISpawnRepository? _spawns;
        private readonly IActorRepository? _actors;
        private readonly IGameRulesRepository? _rules;
        private readonly ICombatFlow _combat;

        internal RoadEncounterService(
            ISpawnRepository? spawns,
            IActorRepository? actors,
            IGameRulesRepository? rules,
            ICombatFlow combat)
        {
            _spawns = spawns;
            _actors = actors;
            _rules = rules;
            _combat = combat ?? throw new ArgumentNullException(nameof(combat));
        }

        /// <inheritdoc />
        public RoadEncounter? Roll(
            int firstTier,
            int secondTier,
            int fallbackTier,
            double chance,
            string key,
            int seed)
        {
            if (chance <= 0)
            {
                return null;
            }

            var random = new SeededWorldRandom(StableHash.Combine(seed, key));
            if (random.NextDouble() >= chance)
            {
                return null;
            }

            var minTier = Math.Min(firstTier, secondTier);
            var maxTier = Math.Max(firstTier, secondTier);
            var spawn = PickRoadEnemy(minTier, maxTier, random);
            if (spawn?.ActorId == null)
            {
                return null;
            }

            var tier = fallbackTier;
            if (_actors != null && _actors.Contains(spawn.ActorId) && _actors.Get(spawn.ActorId).Combat != null)
            {
                tier = _actors.Get(spawn.ActorId).Combat!.Tier;
            }

            return new RoadEncounter(spawn.ActorId, tier, key);
        }

        /// <inheritdoc />
        public bool TryStart(GameSession session, RoadEncounter encounter)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (encounter == null)
            {
                throw new ArgumentNullException(nameof(encounter));
            }

            if (_actors == null || _rules == null || !_actors.Contains(encounter.ActorId))
            {
                return false;
            }

            var playerActor = _actors.Get(_rules.Require().PlayerActorId);
            var enemy = _actors.Get(encounter.ActorId);
            return _combat.StartCombat(
                session,
                playerActor,
                enemy,
                context: new CombatEncounterContext(encounter.Tier, encounter.Key));
        }

        /// <summary>Выбирает запись спавна врага из пулов тиров, которые пересекает переход.</summary>
        /// <param name="minTier">Меньший тир.</param>
        /// <param name="maxTier">Больший тир.</param>
        /// <param name="random">Источник случайных чисел.</param>
        /// <returns>Запись спавна или <c>null</c>, если пул пуст.</returns>
        private SpawnEntry? PickRoadEnemy(int minTier, int maxTier, IWorldRandom random)
        {
            if (_spawns == null || _actors == null)
            {
                return null;
            }

            var pool = new List<SpawnEntry>();
            foreach (var entry in _spawns.All)
            {
                if (entry.ObjectKind != LocationObjectKind.Enemy ||
                    string.IsNullOrWhiteSpace(entry.ActorId) ||
                    entry.MaxTier < minTier ||
                    entry.MinTier > maxTier ||
                    !_actors.Contains(entry.ActorId!))
                {
                    continue;
                }

                pool.Add(entry);
            }

            var index = WeightedPick.ChooseIndex(pool.Count, position => pool[position].Weight, random);
            return index >= 0 ? pool[index] : null;
        }
    }
}
