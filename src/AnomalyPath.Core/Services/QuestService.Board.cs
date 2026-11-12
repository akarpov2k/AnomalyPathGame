#nullable enable
using System;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Services
{
    /// <summary>Часть класса <see cref="QuestService"/>: проверка доски и поиск предложений.</summary>
    public sealed partial class QuestService
    {
        private bool TryGetBoard(
            GameSession? session,
            string boardLocationId,
            out QuestBoardTemplate? board)
        {
            board = null;
            if (session?.State.Player == null || string.IsNullOrWhiteSpace(boardLocationId))
            {
                return false;
            }

            var world = session.State.World;
            if (!string.Equals(world.CurrentLocationId, boardLocationId, StringComparison.Ordinal))
            {
                return false;
            }

            if (!world.Map.TryGetLocation(boardLocationId, out var location) || location == null)
            {
                return false;
            }

            if (location.Subtype != LocationSubtype.Hub)
            {
                return false;
            }

            if (!_locations.Contains(location.TemplateId))
            {
                return false;
            }

            board = _locations.Get(location.TemplateId).QuestBoard;
            return board != null;
        }

        private QuestOffer? FindOffer(GameSession session, string boardLocationId, int slot)
        {
            var offered = FindOfferedQuest(session, boardLocationId, slot);
            if (offered == null || !_quests.TryGet(offered.QuestId, out var template))
            {
                return null;
            }

            return new QuestOffer(template, offered);
        }

        private static QuestState? FindOfferedQuest(
            GameSession session,
            string boardLocationId,
            int slot)
        {
            foreach (var quest in session.State.Quests)
            {
                if (quest.Status == QuestStatus.Offered &&
                    quest.Slot == slot &&
                    string.Equals(quest.BoardLocationId, boardLocationId, StringComparison.Ordinal))
                {
                    return quest;
                }
            }

            return null;
        }

        private static bool IsOffered(GameSession session, string questId)
        {
            foreach (var quest in session.State.Quests)
            {
                if (quest.Status == QuestStatus.Offered &&
                    string.Equals(quest.QuestId, questId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
