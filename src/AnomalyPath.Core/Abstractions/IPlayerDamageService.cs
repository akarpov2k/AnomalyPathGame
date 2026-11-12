#nullable enable
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Урон игроку вне боя: аномалии и будущая мини-игра поиска. Урон уменьшается защитой
    /// персонажа от типа урона; смерть обрабатывается как поражение в бою.
    /// </summary>
    public interface IPlayerDamageService
    {
        /// <summary>Максимум здоровья игрока с учётом уровня, талантов и надетых артефактов.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <returns>Максимум здоровья; 0, если игрока нет.</returns>
        int MaxHealth(GameState state);

        /// <summary>Защита персонажа от типа урона: база актёра плюс снаряжение и артефакты.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <param name="damageTypeId">Тип урона.</param>
        /// <returns>Целая защита, не меньше нуля.</returns>
        int Protection(GameState state, string damageTypeId);

        /// <summary>Наносит урон по здоровью игрока и применяет смерть, если здоровье дошло до нуля.</summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <param name="damage">Заявленный урон; меньше нуля считается нулём.</param>
        /// <param name="damageTypeId">Тип урона; защита берётся по нему.</param>
        /// <returns>Прошедший урон, остаток здоровья и последствия смерти.</returns>
        /// <exception cref="System.ArgumentNullException">Состояние не задано.</exception>
        PlayerDamageResult Apply(GameState state, int damage, string damageTypeId);

        /// <summary>
        /// Наносит прямой урон по здоровью без защиты: жажда и другие небоевые потери.
        /// Смерть обрабатывается так же, как у обычного урона.
        /// </summary>
        /// <param name="state">Состояние прохождения.</param>
        /// <param name="damage">Сколько здоровья снять; меньше нуля считается нулём.</param>
        /// <returns>Прошедший урон, остаток здоровья и последствия смерти.</returns>
        /// <exception cref="System.ArgumentNullException">Состояние не задано.</exception>
        PlayerDamageResult ApplyDirect(GameState state, int damage);
    }
}
