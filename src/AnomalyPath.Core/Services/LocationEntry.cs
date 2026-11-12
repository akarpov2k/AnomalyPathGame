#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: вход на локацию. Тайник проверяется
    /// первым — он занимает одно из двух мест сцены, поэтому число объектов пула зависит
    /// от того, появился ли он.
    /// </summary>
    internal sealed class LocationEntry : ILocationEntry
    {
        private readonly IStashService? _stashes;
        private readonly ILocationObjectService? _objects;

        internal LocationEntry(IStashService? stashes, ILocationObjectService? objects)
        {
            _stashes = stashes;
            _objects = objects;
        }

        /// <inheritdoc />
        public void Ensure(GameSession session)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            var world = session.State.World;
            var location = world.CurrentLocation;
            _stashes?.RollOnEntry(world, location);
            _objects?.Spawn(location);
        }

        /// <inheritdoc />
        public bool RollStash(GameSession session)
        {
            Ensure(session);
            return session.State.World.CurrentLocation.StashPresent;
        }
    }
}
