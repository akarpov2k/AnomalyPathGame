#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Тайники: проверка появления при входе с растущим шансом и вскрытие
    /// с пулом по тиру локации либо пулом джекпота, не привязанным к тирам.
    /// </summary>
    public sealed class StashService : IStashService
    {
        private readonly StashSettings? _settings;
        private readonly ILootService _loot;
        private readonly IGameRulesRepository? _rules;

        /// <summary>Создаёт сервис тайников.</summary>
        /// <param name="loot">Таблицы лута и настройки тайников.</param>
        /// <param name="lootService">Бросок лута по таблице.</param>
        /// <param name="rules">Правила прохождения: настройки кодового замка; <c>null</c> — без замка.</param>
        /// <exception cref="ArgumentNullException">Зависимость не задана.</exception>
        public StashService(
            ILootRepository loot,
            ILootService lootService,
            IGameRulesRepository? rules = null)
        {
            if (loot == null)
            {
                throw new ArgumentNullException(nameof(loot));
            }

            _settings = loot.Settings.Stashes;
            _loot = lootService ?? throw new ArgumentNullException(nameof(lootService));
            _rules = rules;
        }

        /// <inheritdoc />
        public bool Enabled => _settings != null;

        /// <inheritdoc />
        public bool LockEnabled => _rules?.Rules?.StashLock != null;

        /// <inheritdoc />
        public bool RollOnEntry(WorldState world, LocationInstance location)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (_settings == null || location.Subtype == LocationSubtype.Hub || location.StashRolled)
            {
                return false;
            }

            location.MarkStashRolled();
            var checks = world.StashChecks;
            var seed = StableHash.Combine(
                world.Map.Seed,
                location.InstanceId + "#stash_check#" + checks.ToString(CultureInfo.InvariantCulture));
            if (new SeededWorldRandom(seed).NextDouble() >= _settings.Chance(checks))
            {
                world.RegisterStashCheck();
                return false;
            }

            location.MarkStashPresent();
            world.ResetStashChecks();
            return true;
        }

        /// <inheritdoc />
        public StashLockState? LockState(WorldState world, LocationInstance location)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            var settings = _rules?.Rules?.StashLock;
            if (settings == null)
            {
                return null;
            }

            var tier = settings.ForTier(location.Tier);
            var puzzle = Puzzle(location, tier.CodeLength, tier.AlphabetSize);
            var history = new List<LockGuessResult>(location.StashLockGuesses.Count);
            foreach (var guess in location.StashLockGuesses)
            {
                var result = puzzle.Evaluate(guess);
                if (result != null)
                {
                    history.Add(result);
                }
            }

            return new StashLockState(
                tier.CodeLength,
                tier.AlphabetSize,
                settings.Attempts,
                location.StashLockAttemptsUsed,
                location.StashLockGuesses,
                location.StashLockSolved,
                location.StashLockFailed,
                history: history);
        }

        /// <inheritdoc />
        public StashLockState? Guess(WorldState world, LocationInstance location, string guess)
        {
            var state = LockState(world, location);
            var settings = _rules?.Rules?.StashLock;
            if (state == null || settings == null || !state.CanGuess)
            {
                return state;
            }

            if (!LockPuzzle.IsValidGuess(guess, state.CodeLength, state.AlphabetSize))
            {
                return state;
            }

            var puzzle = new LockPuzzle(
                StableHash.Combine(location.Seed, "stash_lock:" + location.InstanceId),
                state.CodeLength,
                state.AlphabetSize);
            var result = puzzle.Evaluate(guess);
            if (result == null)
            {
                return state;
            }

            if (result.Solved)
            {
                location.MarkStashLockSolved();
            }
            else
            {
                location.RegisterStashLockGuess(guess);
                if (location.StashLockAttemptsUsed >= settings.Attempts)
                {
                    location.MarkStashLockFailed();
                }
            }

            var after = LockState(world, location)!;
            return new StashLockState(
                after.CodeLength,
                after.AlphabetSize,
                after.AttemptsTotal,
                after.AttemptsUsed,
                after.Guesses,
                after.Solved,
                after.Failed,
                result,
                after.History);
        }

        private static LockPuzzle Puzzle(LocationInstance location, int codeLength, int alphabetSize) =>
            new LockPuzzle(
                StableHash.Combine(location.Seed, "stash_lock:" + location.InstanceId),
                codeLength,
                alphabetSize);

        /// <inheritdoc />
        public StashOpenResult Open(WorldState world, LocationInstance location)        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (location == null)
            {
                throw new ArgumentNullException(nameof(location));
            }

            if (location.StashOpened)
            {
                return StashOpenResult.AlreadyOpened();
            }

            if (_settings == null || !location.StashPresent)
            {
                return StashOpenResult.NoStash();
            }

            if (LockEnabled)
            {
                if (location.StashLockFailed)
                {
                    return StashOpenResult.LockFailed();
                }

                if (!location.StashLockSolved)
                {
                    return StashOpenResult.Locked();
                }
            }

            var openings = world.StashOpenings;
            var serial = world.NextLootSerial();
            var seed = StableHash.Combine(
                world.Map.Seed,
                location.InstanceId
                + "#stash_open#"
                + serial.ToString(CultureInfo.InvariantCulture)
                + "#"
                + openings.ToString(CultureInfo.InvariantCulture));
            var jackpot = new SeededWorldRandom(seed).NextDouble() < _settings.JackpotChance(openings);
            location.MarkStashOpened();
            if (jackpot)
            {
                world.ResetStashOpenings();
            }
            else
            {
                world.RegisterStashOpening();
            }

            if (jackpot)
            {
                var jackpotLoot = _loot.RollTable(_settings.JackpotLootTableId, location.Tier, seed, true);
                return StashOpenResult.Opened(true, _settings.JackpotLootTableId, jackpotLoot);
            }

            var tableId = _settings.PoolForTier(location.Tier);
            if (tableId == null)
            {
                return StashOpenResult.Opened(false, _settings.JackpotLootTableId, Array.Empty<ItemInstance>());
            }

            var loot = _loot.RollTable(tableId, location.Tier, seed, true);
            return StashOpenResult.Opened(false, tableId, loot);
        }
    }
}
