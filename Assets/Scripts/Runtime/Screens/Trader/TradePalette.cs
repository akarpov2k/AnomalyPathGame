#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>Цвета окна торговли: фон окна, панели, рамка предмета и подсветка выбранного.</summary>
    public static class TradePalette
    {
        /// <summary>Фон окна торговли.</summary>
        public static readonly Color Window = new Color(0.05f, 0.065f, 0.06f, 0.98f);

        /// <summary>Фон панели инвентаря.</summary>
        public static readonly Color Panel = new Color(0.08f, 0.10f, 0.09f, 0.92f);

        /// <summary>Фон области выбранного предмета.</summary>
        public static readonly Color Preview = new Color(0.06f, 0.08f, 0.07f, 0.90f);

        /// <summary>Фон рамки предмета в сетке.</summary>
        public static readonly Color Item = new Color(0.10f, 0.13f, 0.12f, 0.90f);

        /// <summary>Фон выбранного предмета.</summary>
        public static readonly Color ItemSelected = new Color(0.24f, 0.38f, 0.30f, 1f);

        /// <summary>Цвет сообщения об ошибке.</summary>
        public static readonly Color Error = new Color(0.95f, 0.55f, 0.45f, 1f);
    }
}
