#nullable enable
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Ремонт у торговца-механика: у торговца с флагом <c>trader.repair</c> можно починить
    /// всё изношенное снаряжение разом; цена зависит от недостающей прочности.
    /// </summary>
    public interface IRepairService
    {
        /// <summary>Оценивает ремонт: сколько предметов и за сколько.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">Id торговца.</param>
        /// <returns>Оценка; недоступный или не-механик даёт <c>Available = false</c>.</returns>
        RepairQuote GetQuote(GameSession session, string actorId);

        /// <summary>
        /// Чинит всё изношенное снаряжение игрока: инвентарь, надетое и быстрые слоты.
        /// Деньги списываются разом; при нехватке не меняется ничего.
        /// </summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="actorId">Id торговца.</param>
        /// <returns>Итог ремонта.</returns>
        RepairResult Repair(GameSession session, string actorId);
    }
}
