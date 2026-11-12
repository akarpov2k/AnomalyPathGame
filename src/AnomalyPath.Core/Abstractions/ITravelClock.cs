#nullable enable
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Session;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Время мира в сессии: травмы, еда и вода, сроки квестов и жажда.</summary>
    internal interface ITravelClock
    {
        /// <summary>Действующая цена перехода в выносливости с учётом потребностей.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="baseStamina">Базовая цена.</param>
        /// <returns>Цена, которую заплатит игрок.</returns>
        int GetTravelStaminaCost(GameSession session, int baseStamina);

        /// <summary>Фиксированное время возврата в хаб в минутах.</summary>
        int ReturnMinutes { get; }

        /// <summary>Тратит прошедшие часы: травмы, еду и воду, сроки квестов.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="hours">Сколько часов прошло.</param>
        void Advance(GameSession session, double hours);

        /// <summary>Наносит урон жажды за переход.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Урон и его последствия; <c>null</c>, если жажды нет.</returns>
        PlayerDamageResult? Thirst(GameSession session);

        /// <summary>Прибавка к цене действий игрока в бою при нуле еды.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Прибавка к ОД; 0 — без голода.</returns>
        int ApCostBonus(GameSession session);

        /// <summary>Урон здоровью в начале хода игрока при нуле воды.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Сколько здоровья снять; 0 — без жажды.</returns>
        int TurnDamage(GameSession session);
    }
}
