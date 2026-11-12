#nullable enable
namespace AnomalyPath.Core.Trade
{
    /// <summary>Состояние ремонта у торговца-механика.</summary>
    public enum RepairStatus
    {
        /// <summary>Ремонт выполнен.</summary>
        Ok,

        /// <summary>Торговец не найден.</summary>
        TraderNotFound,

        /// <summary>Торговец не механик: ремонта у него нет.</summary>
        NotMechanic,

        /// <summary>Торговец недоступен здесь или не выполнены его условия.</summary>
        Unavailable,

        /// <summary>Чинить нечего: всё снаряжение целое.</summary>
        NothingToRepair,

        /// <summary>Не хватает денег; ничего не изменено.</summary>
        NotEnoughMoney
    }
}
