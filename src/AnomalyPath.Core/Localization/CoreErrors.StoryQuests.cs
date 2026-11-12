#nullable enable
namespace AnomalyPath.Core.Localization
{
    /// <summary>Часть класса <see cref="CoreErrors"/>: сюжетные квесты.</summary>
    public static partial class CoreErrors
    {
        /// <summary>Запись вида <c>quests</c> не является сюжетным квестом.</summary>
        public const string StoryQuestKind = "error.story_quest_kind";

        /// <summary>У сюжетного квеста нет ни одной цели.</summary>
        public const string StoryObjectivesEmpty = "error.story_objectives_empty";

        /// <summary>Неизвестная область прогресса сюжетного квеста.</summary>
        public const string StoryProgressScope = "error.story_progress_scope";

        /// <summary>Неизвестный тип цели сюжетного квеста.</summary>
        public const string StoryObjectiveType = "error.story_objective_type";

        /// <summary>Счётчик цели не положителен.</summary>
        public const string StoryObjectiveCount = "error.story_objective_count";

        /// <summary>Неизвестный вид объекта в цели осмотра.</summary>
        public const string StoryObjectiveObjectKind = "error.story_objective_object_kind";

        /// <summary>Неизвестный подтип локации в исключениях цели.</summary>
        public const string StoryObjectiveSubtype = "error.story_objective_subtype";

        /// <summary>Неизвестный способ завершения цепочки.</summary>
        public const string StoryObjectiveCompletion = "error.story_objective_completion";

        /// <summary>Актёр цели не найден или не является врагом.</summary>
        public const string StoryObjectiveActor = "error.story_objective_actor";

        /// <summary>Локация цели не найдена.</summary>
        public const string StoryObjectiveLocation = "error.story_objective_location";

        /// <summary>Идентификатор варианта условий повторяется.</summary>
        public const string StoryVariantDuplicate = "error.story_variant_duplicate";

        /// <summary>Вариант <c>standard</c> задан в данных явно.</summary>
        public const string StoryVariantStandard = "error.story_variant_standard";

        /// <summary>У варианта условий нет целей.</summary>
        public const string StoryVariantObjectives = "error.story_variant_objectives";

        /// <summary>Выдающий персонаж не найден или является врагом.</summary>
        public const string StoryGiverNotNpc = "error.story_giver_not_npc";

        /// <summary>Принимающий сдачу персонаж не найден или является врагом.</summary>
        public const string StoryTurnInNotNpc = "error.story_turn_in_not_npc";

        /// <summary>Следующий квест линии не найден.</summary>
        public const string StoryNextQuestMissing = "error.story_next_quest_missing";

        /// <summary>Цепочка сюжетных квестов зациклена.</summary>
        public const string StoryNextQuestCycle = "error.story_next_quest_cycle";

        /// <summary>Два квеста указывают на один следующий.</summary>
        public const string StoryNextQuestDuplicate = "error.story_next_quest_duplicate";

        /// <summary>Следующий квест относится к другой линии.</summary>
        public const string StoryNextQuestLine = "error.story_next_quest_line";

        /// <summary>Цели сложного варианта не соответствуют стандартным.</summary>
        public const string StoryHardObjectives = "error.story_hard_objectives";

        /// <summary>Награда сложного варианта больше стандартной.</summary>
        public const string StoryHardReward = "error.story_hard_reward";

        /// <summary>Сюжетный квест не найден.</summary>
        public const string StoryQuestNotFound = "error.story_quest_not_found";

        /// <summary>У цепочки нет локаций.</summary>
        public const string ChainEmpty = "error.chain_empty";

        /// <summary>Квестовая цепочка не найдена.</summary>
        public const string ChainNotFound = "error.chain_not_found";

        /// <summary>Локация цепочки не найдена.</summary>
        public const string ChainLocationMissing = "error.chain_location_missing";

        /// <summary>Запись спавна цепочки не найдена.</summary>
        public const string ChainSpawnMissing = "error.chain_spawn_missing";

        /// <summary>Запись спавна цепочки не того вида.</summary>
        public const string ChainSpawnKind = "error.chain_spawn_kind";

        /// <summary>Счётчик цели цепочки не равен числу её локаций.</summary>
        public const string ChainQuestCount = "error.chain_quest_count";

    }
}
