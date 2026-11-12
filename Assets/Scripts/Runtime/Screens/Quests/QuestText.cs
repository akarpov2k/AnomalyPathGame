#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Quests
{
    /// <summary>
    /// Тексты заданий для доски и журнала: названия сложности и типа, срок, прогресс и цели.
    /// Всё берётся из каталога строк, поэтому оба экрана показывают одинаковые формулировки.
    /// </summary>
    public static class QuestText
    {
        /// <summary>Ключ сообщения по результату действия с заданием.</summary>
        /// <param name="status">Результат действия.</param>
        /// <returns>Ключ текста ошибки или отказа.</returns>
        public static UiTextKey ResultKey(QuestResultStatus status) => status switch
        {
            QuestResultStatus.QuestNotFound => UiTextKey.Quest_Result_Not_Found,
            QuestResultStatus.NotRepeatable => UiTextKey.Quest_Result_Not_Repeatable,
            QuestResultStatus.NotOffered => UiTextKey.Quest_Result_Not_Offered,
            QuestResultStatus.SlotLocked => UiTextKey.Quest_Result_Slot_Locked,
            QuestResultStatus.SlotEmpty => UiTextKey.Quest_Result_Slot_Empty,
            QuestResultStatus.WrongLocation => UiTextKey.Quest_Result_Wrong_Location,
            QuestResultStatus.Unavailable => UiTextKey.Quest_Result_Unavailable,
            QuestResultStatus.RequirementsNotMet => UiTextKey.Quest_Result_Requirements,
            QuestResultStatus.AlreadyActive => UiTextKey.Quest_Result_Already_Active,
            QuestResultStatus.ActiveLimit => UiTextKey.Quest_Result_Active_Limit,
            QuestResultStatus.Cooldown => UiTextKey.Quest_Result_Cooldown,
            QuestResultStatus.NoTargets => UiTextKey.Quest_Result_No_Targets,
            QuestResultStatus.NotReady => UiTextKey.Quest_Result_Not_Ready,
            QuestResultStatus.WrongBoard => UiTextKey.Quest_Result_Wrong_Board,
            QuestResultStatus.NoRoom => UiTextKey.Quest_Result_No_Room,
            _ => UiTextKey.Quest_Result_Invalid
        };

        /// <summary>Название сложности задания.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="difficulty">Сложность.</param>
        /// <returns>Текст из каталога строк.</returns>
        public static string Difficulty(IUiHost host, QuestDifficulty difficulty) => difficulty switch
        {
            QuestDifficulty.Easy => UiText.Get(host.Texts, UiTextKey.Quest_Difficulty_Easy),
            QuestDifficulty.Medium => UiText.Get(host.Texts, UiTextKey.Quest_Difficulty_Medium),
            QuestDifficulty.Hard => UiText.Get(host.Texts, UiTextKey.Quest_Difficulty_Hard),
            _ => string.Empty
        };

        /// <summary>Название типа цели задания.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="type">Тип цели.</param>
        /// <returns>Текст из каталога строк.</returns>
        public static string TypeName(IUiHost host, QuestType type) => type switch
        {
            QuestType.ClearLocation => UiText.Get(host.Texts, UiTextKey.Quest_Type_Clear_Location),
            QuestType.KillMutants => UiText.Get(host.Texts, UiTextKey.Quest_Type_Kill_Mutants),
            QuestType.GatherArtifacts => UiText.Get(host.Texts, UiTextKey.Quest_Type_Gather_Artifacts),
            QuestType.FindItems => UiText.Get(host.Texts, UiTextKey.Quest_Type_Find_Items),
            _ => string.Empty
        };

        /// <summary>Строка условия провала задания: срок или смерть.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="quest">Состояние задания.</param>
        /// <param name="template">Шаблон задания.</param>
        /// <returns>Текст условия провала.</returns>
        public static string FailLine(
            IUiHost host,
            GameSession session,
            QuestState quest,
            QuestTemplate template)
        {
            if (template.FailCondition != QuestFailCondition.Time)
            {
                return UiText.Get(host.Texts, UiTextKey.Quest_Fail_Death);
            }

            var now = session.State.World.Clock.AbsoluteHours;
            var hours = Math.Ceiling(quest.HoursLeft(now, template.TimeLimitHours));
            return UiText.Format(host.Texts, UiTextKey.Quest_Fail_Time, hours);
        }

        /// <summary>Строка прогресса задания.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="template">Шаблон задания.</param>
        /// <param name="progress">Прогресс.</param>
        /// <returns>Текст «готово» или счётчик.</returns>
        public static string ProgressLine(IUiHost host, QuestTemplate template, QuestProgress progress)
        {
            if (progress.Ready)
            {
                return UiText.Get(host.Texts, UiTextKey.Quest_Ready);
            }

            return template.Type == QuestType.FindItems
                ? UiText.Format(host.Texts, UiTextKey.Quest_Npc_Progress, progress.NpcCurrent, progress.NpcRequired)
                : UiText.Format(host.Texts, UiTextKey.Quest_Progress, progress.Current, progress.Required);
        }

        /// <summary>Строка целей задания: локация, актёры и предметы.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="quest">Состояние задания.</param>
        /// <param name="template">Шаблон задания.</param>
        /// <param name="progress">Прогресс.</param>
        /// <returns>Текст целей, по строке на вид цели.</returns>
        public static string TargetsLine(
            IUiHost host,
            QuestState quest,
            QuestTemplate template,
            QuestProgress progress)
        {
            var parts = new List<string>();
            if (quest.LocationTemplateId != null && host.Locations.Contains(quest.LocationTemplateId))
            {
                var location = host.Locations.Get(quest.LocationTemplateId);
                parts.Add(UiText.Format(
                    host.Texts,
                    UiTextKey.Quest_Target_Location,
                    host.Texts.Get(location.TitleKey)));
            }

            if (quest.ActorIds.Count > 0)
            {
                var names = new List<string>();
                foreach (var actorId in quest.ActorIds)
                {
                    if (host.Actors.Contains(actorId))
                    {
                        names.Add(host.Texts.Get(host.Actors.Get(actorId).NameKey));
                    }
                }

                if (names.Count > 0)
                {
                    parts.Add(UiText.Format(host.Texts, UiTextKey.Quest_Target_Actors, string.Join(", ", names)));
                }
            }

            if (quest.Items.Count > 0)
            {
                var names = new List<string>();
                foreach (var item in quest.Items)
                {
                    var name = host.Items.TryGet(item.ItemId, out var itemTemplate)
                        ? host.Texts.Get(itemTemplate.NameKey)
                        : item.ItemId;
                    names.Add(name + " " + CurrentItem(progress, item.ItemId) + "/" + item.Count);
                }

                parts.Add(UiText.Format(host.Texts, UiTextKey.Quest_Target_Items, string.Join(", ", names)));
            }

            return string.Join("\n", parts);
        }

        /// <summary>Строка прогресса сюжетного задания: сколько целей выполнено.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="quest">Состояние сюжетного задания.</param>
        /// <param name="template">Шаблон сюжетного задания.</param>
        /// <param name="ready">Готово ли задание к сдаче.</param>
        /// <returns>Текст прогресса целей.</returns>
        public static string StoryProgressLine(
            IUiHost host,
            StoryQuestState quest,
            StoryQuestTemplate template,
            out bool ready)
        {
            var variant = template.TryGetVariant(quest.VariantId, out var found)
                ? found
                : template.Standard;
            var done = 0;
            var total = Math.Min(variant.Objectives.Count, quest.Progress.Count);
            for (var index = 0; index < total; index++)
            {
                if (quest.ObjectiveAt(index).Current >= variant.Objectives[index].Count)
                {
                    done++;
                }
            }

            ready = done >= total && total > 0;
            return UiText.Format(host.Texts, UiTextKey.Quest_Objective_Done, done, total);
        }

        /// <summary>Строка награды задания.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="rewards">Награда задания.</param>
        /// <returns>Текст награды.</returns>
        public static string RewardLine(IUiHost host, QuestRewardTemplate rewards) =>
            UiText.Format(host.Texts, UiTextKey.Quest_Reward, rewards.Money);

        private static int CurrentItem(QuestProgress progress, string itemId)
        {
            foreach (var item in progress.Items)
            {
                if (string.Equals(item.ItemId, itemId, StringComparison.Ordinal))
                {
                    return item.Current;
                }
            }

            return 0;
        }
    }
}
