#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.World;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Выбор боевого фона: упрощённая картинка по фону текущей локации
    /// (<c>backdrops/combat_&lt;backgroundId&gt;</c>), на дороге — придорожная.
    /// </summary>
    public static class CombatBackdrop
    {
        /// <summary>Префикс id боевых картинок в каталоге спрайтов.</summary>
        public const string Prefix = "backdrops/combat_";

        /// <summary>Фон локации, на которой проходит придорожная встреча.</summary>
        public const string Road = "bg_route_highway";

        /// <summary>Задник по умолчанию, если у локации нет фона.</summary>
        public const string Fallback = "backdrops/screen";

        /// <summary>Фон, на котором идёт бой: у встречи на дороге — дорога.</summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        /// <param name="world">Состояние мира; <c>null</c> — мира нет.</param>
        /// <returns>Id фона локации или <c>null</c>.</returns>
        public static string? BackgroundFor(CombatState? combat, WorldState? world)
        {
            if (combat?.Context?.OnRoad == true)
            {
                return Road;
            }

            return world?.CurrentLocation.Background;
        }

        /// <summary>Id боевой картинки для фона локации.</summary>
        /// <param name="backgroundId">Id фона локации; пусто — задник по умолчанию.</param>
        /// <returns>Id спрайта в каталоге.</returns>
        public static string IdFor(string? backgroundId) =>
            string.IsNullOrWhiteSpace(backgroundId) ? Fallback : Prefix + backgroundId;
    }
}
