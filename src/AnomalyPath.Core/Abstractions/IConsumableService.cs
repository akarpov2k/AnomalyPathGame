#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Применение расходников вне боя: медицина лечит здоровье и травмы, еда и вода
    /// восполняют потребности. В бою расходники применяет боевой сервис, потому что
    /// там применение тратит ОД и меняет состояние боя.
    /// </summary>
    public interface IConsumableService
    {
        /// <summary>Проверяет, можно ли применить предмет в указанной обстановке.</summary>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="inCombat">Применение идёт в бою: расходник «только вне боя» не подходит.</param>
        /// <returns><c>true</c>, если предмет — расходник с остатком применений.</returns>
        bool CanUse(ItemInstance item, bool inCombat);

        /// <summary>
        /// Применяет расходник вне боя: лечит здоровье до предела, снимает перечисленные травмы
        /// и восполняет еду и воду, списывая одно применение. Если восполнять нечего, предмет
        /// не тратится. Последнее применение убирает предмет из инвентаря или слота.
        /// </summary>
        /// <param name="player">Состояние игрока.</param>
        /// <param name="item">Экземпляр предмета.</param>
        /// <param name="maxHealth">Предел здоровья; больше него лечение не поднимает.</param>
        /// <returns>Итог применения; при неудаче состояние не меняется.</returns>
        ConsumableUseResult UseOutOfCombat(PlayerState player, ItemInstance item, int maxHealth);
    }
}
