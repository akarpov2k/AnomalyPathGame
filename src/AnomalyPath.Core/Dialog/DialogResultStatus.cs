#nullable enable
namespace AnomalyPath.Core.Dialog
{
    /// <summary>Результат шага диалога.</summary>
    public enum DialogResultStatus
    {
        /// <summary>Шаг выполнен.</summary>
        Ok,

        /// <summary>Диалог не найден.</summary>
        DialogNotFound,

        /// <summary>Узел не найден.</summary>
        NodeNotFound,

        /// <summary>Вариант ответа не найден.</summary>
        OptionNotFound,

        /// <summary>Вариант недоступен по условиям.</summary>
        Unavailable,

        /// <summary>Выдача или сдача квеста не удалась.</summary>
        QuestFailed,

        /// <summary>Награда не помещается в инвентарь.</summary>
        NoRoom
    }
}
