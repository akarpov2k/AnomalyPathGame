#nullable enable
namespace AnomalyPath.Core.World
{
    /// <summary>Итог отдыха: доступность действия и нападение во время него.</summary>
    public enum RestStatus
    {
        /// <summary>Отдых прошёл, нападения не было.</summary>
        Success,

        /// <summary>Отдых прошёл, но вне хаба напал противник и начался бой.</summary>
        Encounter,

        /// <summary>Идёт бой: отдыхать нельзя.</summary>
        InCombat,

        /// <summary>Выносливость полна: восстанавливать нечего.</summary>
        FullStamina,

        /// <summary>Правила отдыха недоступны: нет топологии мира.</summary>
        Unavailable,

        /// <summary>Выбрано часов больше предела или отрицательное число.</summary>
        InvalidHours
    }
}
