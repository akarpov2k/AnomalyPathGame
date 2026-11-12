#nullable enable
namespace AnomalyPath.Core.Templates
{
    /// <summary>Что делает эффект расходника при применении.</summary>
    public enum ConsumableEffectType
    {
        /// <summary>Лечит здоровье на <see cref="ConsumableEffect.Amount"/>.</summary>
        Heal,

        /// <summary>Восстанавливает очки действия на <see cref="ConsumableEffect.Amount"/>.</summary>
        RestoreAp,

        /// <summary>Восполняет еду (сытость) на <see cref="ConsumableEffect.Amount"/>.</summary>
        RestoreFood,

        /// <summary>Восполняет воду (питьё) на <see cref="ConsumableEffect.Amount"/>.</summary>
        RestoreWater,

        /// <summary>Даёт бойцу временный модификатор до конца боя.</summary>
        Modifier,
    }
}
