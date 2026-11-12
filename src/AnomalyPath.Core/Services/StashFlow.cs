#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Loot;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: тайник текущей локации — вскрытие
    /// и кодовый замок. Вскрытие сообщает о себе сюжетным квестам.
    /// </summary>
    internal sealed class StashFlow : IStashFlow
    {
        private readonly IStashService? _stashes;
        private readonly IStoryQuestHooks _story;

        internal StashFlow(IStashService? stashes, IStoryQuestHooks story)
        {
            _stashes = stashes;
            _story = story ?? throw new ArgumentNullException(nameof(story));
        }

        /// <inheritdoc />
        public StashOpenResult OpenStash(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.State.World;
            var result = _stashes == null
                ? StashOpenResult.NoStash()
                : _stashes.Open(world, world.CurrentLocation);
            if (result.Status == StashOpenStatus.Opened)
            {
                _story.OnStashOpened(session, world.CurrentLocationId);
            }

            return result;
        }

        /// <inheritdoc />
        public StashLockState? LockState(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.State.World;
            return _stashes?.LockState(world, world.CurrentLocation);
        }

        /// <inheritdoc />
        public StashLockState? Guess(GameSession session, string guess)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.State.World;
            return _stashes?.Guess(world, world.CurrentLocation, guess);
        }
    }
}
