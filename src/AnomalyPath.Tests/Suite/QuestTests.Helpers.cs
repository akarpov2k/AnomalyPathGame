#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Save;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;


namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="QuestTests"/>: помощники — слоты, сессия, игрок, путь.</summary>
    public sealed partial class QuestTests
    {
        private static QuestBoardSlot RequireSlot(
            IQuestService quests,
            GameSession session,
            QuestDifficulty difficulty)
        {
            foreach (var slot in quests.GetBoard(session, BoardId(session)))
            {
                if (slot.Offer != null && slot.Difficulty == difficulty)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException("Нет слота сложности " + difficulty);
        }

        private static QuestBoardSlot RequireSlot(
            IQuestService quests,
            GameSession session,
            QuestDifficulty difficulty,
            int excludeIndex)
        {
            foreach (var slot in quests.GetBoard(session, BoardId(session)))
            {
                if (slot.Index != excludeIndex && slot.Offer != null && slot.Difficulty == difficulty)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException("Нет второго слота сложности " + difficulty);
        }

        private static QuestBoardSlot RequireSlot(
            IQuestService quests,
            GameSession session,
            QuestType type)
        {
            foreach (var slot in quests.GetBoard(session, BoardId(session)))
            {
                if (slot.Offer != null && slot.Offer.Template.Type == type)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException("Нет квеста типа " + type);
        }

        private static QuestBoardSlot RequireConditionSlot(
            IQuestService quests,
            GameSession session,
            QuestFailCondition condition)
        {
            foreach (var slot in quests.GetBoard(session, BoardId(session)))
            {
                if (slot.Offer != null && slot.Offer.Template.FailCondition == condition)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException("Нет квеста с условием провала " + condition);
        }

        private static QuestBoardSlot FirstOffer(IQuestService quests, GameSession session)
        {
            foreach (var slot in quests.GetBoard(session, BoardId(session)))
            {
                if (slot.Offer != null)
                {
                    return slot;
                }
            }

            throw new InvalidOperationException("На доске нет предложений");
        }

        private static QuestState Accept(
            IQuestService quests,
            GameSession session,
            QuestBoardSlot slot)
        {
            var result = quests.Accept(session, BoardId(session), slot.Index);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException("Не удалось взять квест: " + result.Status);
            }

            if (!session.State.TryGetActiveQuest(slot.Offer!.QuestId, out var state) || state == null)
            {
                throw new InvalidOperationException("Квест не стал активным");
            }

            return state;
        }

        private static string BoardId(GameSession session) =>
            session.State.World.CurrentLocationId;

        private static GameSession NewSession(ServiceContainer services)
        {
            var generator = services.Resolve<IWorldGenerator>();
            var world = WorldState.Start(generator.Generate(42));
            return new GameSession(new GameState(world, NewPlayer(2000)));
        }

        private static PlayerState NewPlayer(int money)
        {
            return new PlayerState(
                "player",
                2,
                0,
                100,
                8,
                new GridContainer("player", 10, 6),
                new Equipment("player", 4),
                Array.Empty<string>(),
                new NeedsState(100, 100, 100),
                "Тест",
                money);
        }

        private static bool HasPath(GameSession session, string target)
        {
            var map = session.State.World.Map;
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var queue = new Queue<string>();
            queue.Enqueue(session.State.World.CurrentLocationId);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current))
                {
                    continue;
                }

                foreach (var edge in map.EdgesFrom(current))
                {
                    if (string.Equals(edge.ToId, target, StringComparison.Ordinal))
                    {
                        return true;
                    }

                    queue.Enqueue(edge.ToId);
                }
            }

            return false;
        }

        private static int CountItem(PlayerState player, string itemId)
        {
            var count = 0;
            foreach (var item in player.Inventory.Items)
            {
                if (item.ItemId == itemId)
                {
                    count += item.Stack;
                }
            }

            return count;
        }

        private static void WithSaveRoot(Action<string> body)
        {
            var root = Path.Combine(Path.GetTempPath(), "anomalypath-quests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                body(root);
            }
            finally
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                }
                catch (IOException)
                {
                    // временная папка не критична
                }
            }
        }
    }
}
