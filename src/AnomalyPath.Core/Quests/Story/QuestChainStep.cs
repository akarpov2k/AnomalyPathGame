#nullable enable
using System;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests.Story
{
    /// <summary>Одна локация квестовой цепочки: шаблон и принудительные объекты.</summary>
    public sealed class QuestChainStep
    {
        /// <summary>Создаёт шаг цепочки.</summary>
        /// <param name="locationId">Шаблон локации.</param>
        /// <param name="enemySpawnId">Запись спавна противника или <c>null</c>.</param>
        /// <param name="npcSpawnId">Запись спавна NPC или <c>null</c>.</param>
        /// <exception cref="ArgumentException">Шаблон локации пуст.</exception>
        public QuestChainStep(
            string locationId,
            string? enemySpawnId = null,
            string? npcSpawnId = null)
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(locationId)),
                    nameof(locationId));
            }

            LocationId = locationId;
            EnemySpawnId = enemySpawnId;
            NpcSpawnId = npcSpawnId;
        }

        /// <summary>Шаблон локации.</summary>
        public string LocationId { get; }

        /// <summary>Запись спавна противника или <c>null</c>.</summary>
        public string? EnemySpawnId { get; }

        /// <summary>Запись спавна NPC или <c>null</c>.</summary>
        public string? NpcSpawnId { get; }
    }
}
