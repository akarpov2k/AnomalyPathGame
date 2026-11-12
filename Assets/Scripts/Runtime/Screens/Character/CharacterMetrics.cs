#nullable enable
namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Разметка окна «инвентарь» задаётся в префабе; здесь остаётся только зазор сетки,
    /// по которому считаются клетки, предметы и перетаскивание.
    /// </summary>
    public static class CharacterMetrics
    {
        /// <summary>Зазор между клетками сетки инвентаря.</summary>
        public const float GridGap = 4f;

        /// <summary>Минимальная сторона клетки: меньше — сетка уходит в прокрутку.</summary>
        public const float MinCellSize = 48f;
    }
}
