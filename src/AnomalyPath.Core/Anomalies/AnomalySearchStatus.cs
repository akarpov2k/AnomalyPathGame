#nullable enable
namespace AnomalyPath.Core.Anomalies
{
    /// <summary>Итог попытки поиска в аномалии.</summary>
    public enum AnomalySearchStatus
    {
        /// <summary>Попытка выполнена: урон нанесён, бросок артефакта сделан.</summary>
        Success,

        /// <summary>Попытки аномалии кончились.</summary>
        NoAttempts,

        /// <summary>Объект не принадлежит текущей локации.</summary>
        WrongLocation,

        /// <summary>Игрок в бою.</summary>
        InCombat,

        /// <summary>Аномалия не найдена в данных.</summary>
        UnknownAnomaly,

        /// <summary>В состоянии прохождения нет игрока.</summary>
        NoPlayer,

        /// <summary>Клетка уже обыскана.</summary>
        AlreadySearched,

        /// <summary>Клетка уже отсканирована.</summary>
        AlreadyScanned,

        /// <summary>Клетка вне поля.</summary>
        WrongCell,

        /// <summary>Сканы кончились.</summary>
        NoScans
    }
}
