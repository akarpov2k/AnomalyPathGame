#nullable enable
namespace AnomalyPath.Core.Templates
{
    /// <summary>Характеристика, которую меняет эффект <see cref="ConsumableEffectType.Modifier"/>.</summary>
    public enum ConsumableStat
    {
        /// <summary>Прибавка к меткости: доля базового шанса.</summary>
        Accuracy,

        /// <summary>Прибавка к защите от типа урона <see cref="ConsumableEffect.DamageTypeId"/>.</summary>
        Protection,

        /// <summary>Прибавка к цене действия в ОД; может быть отрицательной.</summary>
        ActionApCost
    }
}
