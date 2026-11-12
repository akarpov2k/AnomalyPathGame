#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Сценарий главы 1: какие цели остались у активного сюжетного квеста и какую
    /// последовательность фраз диалога нужно пройти, чтобы взять или сдать квест.
    /// Данные целей читаются из шаблона квеста, а не дублируются числами.
    /// </summary>
    internal static class AutoPlayScenario
    {
        /// <summary>Активный сюжетный квест основной линии; <c>null</c> — ни одного.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Состояние активного квеста или <c>null</c>.</returns>
        public static StoryQuestState? ActiveStory(GameSession session)
        {
            var player = session.State.Player;
            if (player == null)
            {
                return null;
            }

            foreach (var state in player.StoryQuests)
            {
                if (state.Status != QuestStatus.Active)
                {
                    continue;
                }

                foreach (var questId in AutoPlayIds.StoryQuestOrder)
                {
                    if (string.Equals(state.QuestId, questId, StringComparison.Ordinal))
                    {
                        return state;
                    }
                }
            }

            return null;
        }

        /// <summary>Первый несданный сюжетный квест, который выдающий может предложить сейчас.</summary>
        /// <param name="screen">Игровой экран с сервисами.</param>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="questId">Id квеста, если он найден.</param>
        /// <returns><c>true</c>, если есть квест для взятия.</returns>
        public static bool TryGetNextAvailable(
            AnomalyPath.Runtime.GameScreen screen,
            GameSession session,
            out string questId)
        {
            questId = string.Empty;
            var player = session.State.Player;
            if (player == null)
            {
                return false;
            }

            foreach (var candidate in AutoPlayIds.StoryQuestOrder)
            {
                if (player.HasCompletedQuest(candidate))
                {
                    continue;
                }

                if (screen.StoryQuests.IsAvailable(session, candidate, AutoPlayIds.SmugglerActorId))
                {
                    questId = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Глава 1 пройдена: последний сюжетный квест сдан.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns><c>true</c>, если S5 сдан.</returns>
        public static bool IsChapterComplete(GameSession session)
        {
            var player = session.State.Player;
            return player != null && player.HasCompletedQuest(
                AutoPlayIds.StoryQuestOrder[AutoPlayIds.StoryQuestOrder.Length - 1]);
        }

        /// <summary>Номер квеста в линии нужен для подписи шага: S1..S5.</summary>
        /// <param name="questId">Id квеста.</param>
        /// <returns>Номер от 1 или 0, если квест не из линии.</returns>
        public static int StoryNumber(string questId)
        {
            for (var index = 0; index < AutoPlayIds.StoryQuestOrder.Length; index++)
            {
                if (string.Equals(AutoPlayIds.StoryQuestOrder[index], questId, StringComparison.Ordinal))
                {
                    return index + 1;
                }
            }

            return 0;
        }

        /// <summary>Собирает фразы диалога для взятия или сдачи активного квеста.</summary>
        /// <param name="screen">Игровой экран.</param>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quest">Квест, который нужно взять или сдать.</param>
        /// <param name="turnIn">Сдача или взятие.</param>
        /// <returns>План диалога.</returns>
        public static DialoguePlan PlanFor(string questId, bool turnIn)
        {
            // S5 выдаёт контрабандист, а принимает Гвоздь в Депо.
            var npc = turnIn && IsGvozdQuest(questId);
            var options = turnIn ? TurnInOptions(questId) : AcceptOptions(questId);
            return new DialoguePlan(questId, turnIn, npc, options);
        }

        /// <summary>Взять ли S5 у Гвоздя: у него сдача, а выдача всё равно у контрабандиста.</summary>
        /// <param name="questId">Id квеста.</param>
        /// <returns><c>true</c>, если квест связан с Гвоздём.</returns>
        public static bool IsGvozdQuest(string questId) =>
            string.Equals(questId, AutoPlayIds.StoryQuestOrder[4], StringComparison.Ordinal);

        /// <summary>Где сейчас игрок: в Депо или в поселении.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Шаблон текущей локации.</returns>
        public static string CurrentTemplateId(GameSession session) =>
            session.State.World.CurrentLocation.TemplateId;

        /// <summary>Цели активного квеста, которые ещё не выполнены.</summary>
        /// <param name="screen">Игровой экран.</param>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quest">Активный квест.</param>
        /// <returns>Потребности локации; пустые, если целей нет.</returns>
        public static QuestNeeds Needs(
            AnomalyPath.Runtime.GameScreen screen,
            GameSession session,
            StoryQuestState quest)
        {
            var needs = new QuestNeeds();
            if (!screen.StoryQuests.TryGetTemplate(quest.QuestId, out var template) ||
                !template.TryGetVariant(quest.VariantId, out var variant))
            {
                return needs;
            }

            var objectives = variant.Objectives;
            for (var index = 0; index < objectives.Count && index < quest.Progress.Count; index++)
            {
                var objective = objectives[index];
                var progress = quest.Progress[index];
                if (progress.IsDone(objective.Count))
                {
                    continue;
                }

                switch (objective.Type)
                {
                    case QuestObjectiveType.KillActors:
                        needs.NeedKills = true;
                        foreach (var actorId in objective.ActorPool)
                        {
                            needs.KillPool.Add(actorId);
                        }

                        needs.KillsLeft += objective.Count - progress.Current;
                        break;
                    case QuestObjectiveType.InspectObjects:
                        needs.NeedLoot = true;
                        needs.LootLeft += objective.Count - progress.Current;
                        break;
                    case QuestObjectiveType.OpenStashes:
                        needs.NeedStash = true;
                        needs.StashLeft += objective.Count - progress.Current;
                        break;
                    case QuestObjectiveType.ClearLocations:
                        needs.NeedClear = true;
                        break;
                    case QuestObjectiveType.ClearChain:
                        needs.NeedChain = true;
                        needs.ChainId = objective.ChainId;
                        break;
                }
            }

            return needs;
        }

        /// <summary>Фразы сдачи квеста в порядке нажатия.</summary>
        private static string[] TurnInOptions(string questId)
        {
            switch (StoryNumber(questId))
            {
                case 1:
                    return new[] { "s1_ready", "std", "thanks" };
                case 2:
                    return new[] { "s2_ready", "std", "thanks" };
                case 3:
                    return new[] { "s3_ready", "std", "thanks" };
                case 4:
                    return new[] { "s4_ready", "flash", "thanks" };
                default:
                    return new[] { "ready", "thanks" };
            }
        }

        /// <summary>Фразы взятия квеста в порядке нажатия.</summary>
        private static string[] AcceptOptions(string questId)
        {
            switch (StoryNumber(questId))
            {
                case 1:
                    return new[] { "s1_std", "ok" };
                case 2:
                    return new[] { "s2", "accept", "ok" };
                case 3:
                    return new[] { "s3", "accept", "ok" };
                case 4:
                    return new[] { "s4", "accept", "ok" };
                default:
                    return new[] { "s5", "accept", "ok" };
            }
        }
    }

    /// <summary>План прохождения диалога: кто говорит, какие фразы нажать и что это даёт.</summary>
    internal sealed class DialoguePlan
    {
        /// <summary>Создаёт план диалога.</summary>
        /// <param name="questId">Квест, который берут или сдают.</param>
        /// <param name="turnIn">Сдача или взятие.</param>
        /// <param name="npc">Диалог NPC (Гвоздь) или комната торговца.</param>
        /// <param name="options">Id фраз в порядке нажатия.</param>
        public DialoguePlan(string questId, bool turnIn, bool npc, string[] options)
        {
            QuestId = questId;
            TurnIn = turnIn;
            Npc = npc;
            Options = options;
        }

        /// <summary>Квест плана.</summary>
        public string QuestId { get; }

        /// <summary>Сдача или взятие.</summary>
        public bool TurnIn { get; }

        /// <summary>Диалог NPC вместо комнаты торговца.</summary>
        public bool Npc { get; }

        /// <summary>Id фраз в порядке нажатия.</summary>
        public string[] Options { get; }

        /// <summary>Следующая фраза.</summary>
        public int Index { get; set; }
    }

    /// <summary>Незакрытые цели активного квеста, влияющие на поведение на локации.</summary>
    internal sealed class QuestNeeds
    {
        /// <summary>Нужно убивать актёров из пула.</summary>
        public bool NeedKills { get; set; }

        /// <summary>Пул актёров, чьи убийства засчитываются.</summary>
        public HashSet<string> KillPool { get; } = new HashSet<string>(StringComparer.Ordinal);

        /// <summary>Сколько убийств осталось.</summary>
        public int KillsLeft { get; set; }

        /// <summary>Нужно осматривать объекты лута.</summary>
        public bool NeedLoot { get; set; }

        /// <summary>Сколько осмотров осталось.</summary>
        public int LootLeft { get; set; }

        /// <summary>Нужно вскрывать тайники.</summary>
        public bool NeedStash { get; set; }

        /// <summary>Сколько тайников осталось.</summary>
        public int StashLeft { get; set; }

        /// <summary>Нужно зачищать локации (не дороги).</summary>
        public bool NeedClear { get; set; }

        /// <summary>Нужно пройти квестовую цепочку.</summary>
        public bool NeedChain { get; set; }

        /// <summary>Id цепочки; <c>null</c> — цепочки нет.</summary>
        public string? ChainId { get; set; }
    }
}
