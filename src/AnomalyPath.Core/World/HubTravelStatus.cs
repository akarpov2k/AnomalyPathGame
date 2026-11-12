#nullable enable
namespace AnomalyPath.Core.World
{
    /// <summary>Итог проверки или выполнения перехода между хабами.</summary>
    public enum HubTravelStatus
    {
        /// <summary>Переход возможен и выполнен; игрок прибыл в целевой хаб.</summary>
        Success,

        /// <summary>Переход начат, но на дороге случилась встреча: идёт бой, прибытие после победы.</summary>
        Encounter,

        /// <summary>Игрок не в хабе.</summary>
        NotFromHub,

        /// <summary>Переход запрещён во время боя.</summary>
        InCombat,

        /// <summary>Целевой хаб не найден в карте мира.</summary>
        TargetNotFound,

        /// <summary>Целевая локация существует, но это не хаб.</summary>
        TargetNotHub,

        /// <summary>Игрок уже в этом хабе.</summary>
        SameHub,

        /// <summary>Не хватает выносливости на пеший переход.</summary>
        NotEnoughStamina,

        /// <summary>Не хватает денег на проводника.</summary>
        NotEnoughMoney,

        /// <summary>Целевая локация стала заглушкой после своего квеста: игра показывает экран «контент будет позже».</summary>
        InDevelopment,

        /// <summary>Правила перехода не загружены: топология отсутствует.</summary>
        Unavailable
    }
}
