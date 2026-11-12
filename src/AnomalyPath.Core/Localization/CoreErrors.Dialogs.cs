#nullable enable
namespace AnomalyPath.Core.Localization
{
    /// <summary>Часть класса <see cref="CoreErrors"/>: диалоги.</summary>
    public static partial class CoreErrors
    {
        /// <summary>Диалог указан у врага.</summary>
        public const string DialogOnEnemy = "error.dialog_on_enemy";

        /// <summary>Диалог не найден.</summary>
        public const string DialogNotFound = "error.dialog_not_found";

        /// <summary>Неизвестное состояние квеста в условиях диалога.</summary>
        public const string DialogRequirementState = "error.dialog_requirement_state";

        /// <summary>Неверное условие варианта квеста в диалоге.</summary>
        public const string DialogRequirementVariant = "error.dialog_requirement_variant";

        /// <summary>Начальный узел диалога не найден.</summary>
        public const string DialogNodeMissing = "error.dialog_node_missing";

        /// <summary>Идентификаторы узлов диалога повторяются.</summary>
        public const string DialogNodeDuplicate = "error.dialog_node_duplicate";

        /// <summary>Идентификаторы вариантов ответа повторяются в узле.</summary>
        public const string DialogOptionDuplicate = "error.dialog_option_duplicate";

        /// <summary>У варианта нет ни следующего узла, ни признака закрытия.</summary>
        public const string DialogOptionEnd = "error.dialog_option_end";

        /// <summary>Следующий узел варианта не найден.</summary>
        public const string DialogOptionNextMissing = "error.dialog_option_next_missing";

        /// <summary>У узла нет ни одного варианта ответа.</summary>
        public const string DialogNodeDeadEnd = "error.dialog_node_dead_end";

        /// <summary>Узел недостижим из начального.</summary>
        public const string DialogNodeUnreachable = "error.dialog_node_unreachable";

        /// <summary>Говорящий узел не найден среди актёров.</summary>
        public const string DialogSpeakerMissing = "error.dialog_speaker_missing";

        /// <summary>Неизвестный тип последствия варианта.</summary>
        public const string DialogUnknownEffect = "error.dialog_unknown_effect";

        /// <summary>Последствие выдаёт или принимает несуществующий квест.</summary>
        public const string DialogEffectQuest = "error.dialog_effect_quest";

        /// <summary>Неизвестное действие с квестом в последствии.</summary>
        public const string DialogEffectAction = "error.dialog_effect_action";

        /// <summary>Последствие выдаёт несуществующий предмет.</summary>
        public const string DialogEffectItem = "error.dialog_effect_item";

        /// <summary>Последствие выдаёт пустое открытие.</summary>
        public const string DialogEffectUnlock = "error.dialog_effect_unlock";

    }
}
