#nullable enable
namespace AnomalyPath.Core.Quests
{
    /// <summary>Результат действия с квестом.</summary>
    public enum QuestResultStatus
    {
        /// <summary>Успех.</summary>
        Ok,

        /// <summary>Квест не найден.</summary>
        QuestNotFound,

        /// <summary>Квест не повторяемый.</summary>
        NotRepeatable,

        /// <summary>В слоте доски нет такого предложения.</summary>
        NotOffered,

        /// <summary>Слот закрыт таймером обновления.</summary>
        SlotLocked,

        /// <summary>Слот пуст: подходящих квестов в пуле нет.</summary>
        SlotEmpty,

        /// <summary>Действие доступно только на доске в хабе.</summary>
        WrongLocation,

        /// <summary>Доска недоступна.</summary>
        Unavailable,

        /// <summary>Условия выдачи не выполнены.</summary>
        RequirementsNotMet,

        /// <summary>Этот квест уже взят.</summary>
        AlreadyActive,

        /// <summary>Достигнут предел активных квестов.</summary>
        ActiveLimit,

        /// <summary>Квест на откате.</summary>
        Cooldown,

        /// <summary>В данных нет подходящих целей.</summary>
        NoTargets,

        /// <summary>Цель ещё не выполнена.</summary>
        NotReady,

        /// <summary>Квест взят на другой доске.</summary>
        WrongBoard,

        /// <summary>Нет места для награды.</summary>
        NoRoom,

        /// <summary>Действие недоступно.</summary>
        Invalid,

        /// <summary>Сюжетный квест сейчас недоступен.</summary>
        NotAvailable,

        /// <summary>На линии уже есть активный сюжетный квест.</summary>
        LineBusy,

        /// <summary>У сюжетного квеста нет такого варианта условий.</summary>
        VariantNotFound,

        /// <summary>Сюжетный квест уже сдан.</summary>
        AlreadyCompleted
    }
}
