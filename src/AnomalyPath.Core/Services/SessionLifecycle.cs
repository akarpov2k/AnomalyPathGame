#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: сохранение, загрузка и новая игра.
    /// Старые сейвы копили опыт без повышения уровня — при загрузке уровень подтягивается.
    /// </summary>
    internal sealed class SessionLifecycle : ISessionLifecycle
    {
        private readonly ISaveService _saves;
        private readonly INewGameService _newGame;
        private readonly IProgressionService? _progression;
        private readonly ILocationEntry _entry;

        internal SessionLifecycle(
            ISaveService saves,
            INewGameService newGame,
            IProgressionService? progression,
            ILocationEntry entry)
        {
            _saves = saves ?? throw new ArgumentNullException(nameof(saves));
            _newGame = newGame ?? throw new ArgumentNullException(nameof(newGame));
            _progression = progression;
            _entry = entry ?? throw new ArgumentNullException(nameof(entry));
        }

        /// <inheritdoc />
        public void Save(
            GameSession session,
            string slot,
            double playtime = 0,
            DateTimeOffset? createdAt = null,
            string? title = null)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (session.InCombat)
            {
                throw new InvalidOperationException(CoreText.Get(CoreErrors.SaveInCombat));
            }

            _saves.Save(session.State, slot, playtime, createdAt, title);
        }

        /// <inheritdoc />
        public GameSession Load(string slot)
        {
            var state = _saves.Load(slot);
            if (_progression != null && state.Player != null)
            {
                // Старые сейвы копили опыт без повышения уровня: подтягиваем уровень и награды.
                _progression.SyncLevel(state.Player);
            }

            return new GameSession(state);
        }

        /// <inheritdoc />
        public GameSession StartNew(int seed, string loadoutId, NewGameSettings settings)
        {
            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var session = new GameSession(_newGame.Create(seed, loadoutId, settings));
            _entry.RollStash(session);
            return session;
        }
    }
}
