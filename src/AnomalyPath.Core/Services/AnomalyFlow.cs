#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Часть класса <see cref="GameSessionService"/>: поиск артефакта в аномалии. Обыск клетки
    /// может убить игрока, поэтому смерть сообщается миру оттуда же.
    /// </summary>
    internal sealed class AnomalyFlow : IAnomalyFlow
    {
        private readonly IAnomalyService? _anomalies;
        private readonly IStoryQuestHooks _story;

        internal AnomalyFlow(IAnomalyService? anomalies, IStoryQuestHooks story)
        {
            _anomalies = anomalies;
            _story = story ?? throw new ArgumentNullException(nameof(story));
        }

        /// <inheritdoc />
        public RadarState? OpenRadar(GameSession session, AnomalyLocationObject anomaly)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            return _anomalies?.OpenRadar(session, anomaly);
        }

        /// <inheritdoc />
        public RadarScanResult Scan(GameSession session, AnomalyLocationObject anomaly, int cell)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (_anomalies == null)
            {
                return RadarScanResult.Failed(AnomalySearchStatus.UnknownAnomaly);
            }

            return _anomalies.Scan(session, anomaly, cell);
        }

        /// <inheritdoc />
        public AnomalySearchResult SearchCell(GameSession session, AnomalyLocationObject anomaly, int cell)
        {
            if (session == null)
            {
                throw new ArgumentNullException(nameof(session));
            }

            if (anomaly == null)
            {
                throw new ArgumentNullException(nameof(anomaly));
            }

            if (_anomalies == null)
            {
                return AnomalySearchResult.Failed(AnomalySearchStatus.UnknownAnomaly);
            }

            var result = _anomalies.SearchCell(session, anomaly, cell);
            if (result.Died)
            {
                _story.OnPlayerDeath(session);
            }

            return result;
        }
    }
}
