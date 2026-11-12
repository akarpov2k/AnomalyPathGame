#nullable enable
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Потребности: расход еды и воды за часы мира, штрафы голода и урон жажды.
    /// </summary>
    public interface INeedsService
    {
        /// <summary>
        /// Тратит еду и воду за прошедшие часы с накоплением дробного остатка:
        /// неполный час не теряется, а переносится на следующий расход.
        /// </summary>
        /// <param name="state">Состояние прохождения: потребности игрока.</param>
        /// <param name="hours">Сколько часов мира прошло; не больше нуля — ничего не меняет.</param>
        /// <returns>Сколько списано, остаток шкал и накопленные дробные части.</returns>
        /// <exception cref="System.ArgumentNullException">Состояние не задано.</exception>
        NeedsTickResult Consume(GameState state, double hours);

        /// <summary>Цена перехода в выносливости: при нуле еды увеличивается на долю из данных.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <param name="baseCost">Базовая цена в выносливости.</param>
        /// <returns>Действующая цена; при голоде округлена вверх.</returns>
        int TravelStaminaCost(GameState state, int baseCost);

        /// <summary>Прибавка к цене действия игрока в бою: при нуле еды — из данных, иначе 0.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns>Прибавка к ОД; 0 — игрок не голоден или игрока нет.</returns>
        int ApCostBonus(GameState state);

        /// <summary>Урон здоровью в начале хода игрока в бою: при нуле воды — из данных, иначе 0.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns>Сколько здоровья теряется; 0 — жажды нет.</returns>
        int TurnDamage(GameState state);

        /// <summary>
        /// Применяет урон жажды за переход: при нуле воды здоровье падает на значение из данных,
        /// защита не помогает; нулевое здоровье запускает обычную смерть.
        /// </summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns>Прошедший урон и последствия смерти; нули, если жажды нет.</returns>
        /// <exception cref="System.ArgumentNullException">Состояние не задано.</exception>
        PlayerDamageResult ApplyThirstDamage(GameState state);
    }
}
