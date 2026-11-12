#nullable enable
namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Цены действий боя из данных: интерфейс показывает их в подсказках, а не хранит у себя.
    /// </summary>
    public interface ICombatSettings
    {
        /// <summary>Цена перезарядки в очках действия.</summary>
        int ReloadApCost { get; }

        /// <summary>Цена смены оружия в очках действия.</summary>
        int WeaponSwapApCost { get; }
    }
}
