#nullable enable
namespace AnomalyPath.Core.Dialog
{
    /// <summary>Тип последствия варианта ответа; код в JSON — имя члена в нижнем регистре.</summary>
    public enum DialogEffectType
    {
        /// <summary>Выдать или принять сдачу сюжетного квеста.</summary>
        Quest,

        /// <summary>Выдать предмет.</summary>
        Item,

        /// <summary>Выдать открытие.</summary>
        Unlock
    }
}
