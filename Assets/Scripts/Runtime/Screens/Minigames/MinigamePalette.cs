#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Minigames
{
    /// <summary>Цвета мини-игр: окно, панели, клетки радара и сигналы.</summary>
    public static class MinigamePalette
    {
        /// <summary>Фон окна.</summary>
        public static readonly Color Window = new Color(0.05f, 0.065f, 0.06f, 0.98f);

        /// <summary>Фон панели внутри окна.</summary>
        public static readonly Color Panel = new Color(0.08f, 0.10f, 0.09f, 0.95f);

        /// <summary>Клетка без скана.</summary>
        public static readonly Color CellIdle = new Color(0.14f, 0.17f, 0.15f, 1f);

        /// <summary>Выбранная клетка.</summary>
        public static readonly Color CellSelected = new Color(0.36f, 0.48f, 0.30f, 1f);

        /// <summary>Обысканная клетка.</summary>
        public static readonly Color CellSearched = new Color(0.10f, 0.12f, 0.10f, 1f);

        /// <summary>Сигнал 1 — слабый.</summary>
        public static readonly Color Signal1 = new Color(0.20f, 0.26f, 0.20f, 1f);

        /// <summary>Сигнал 2.</summary>
        public static readonly Color Signal2 = new Color(0.30f, 0.40f, 0.26f, 1f);

        /// <summary>Сигнал 3.</summary>
        public static readonly Color Signal3 = new Color(0.44f, 0.58f, 0.34f, 1f);

        /// <summary>Сигнал 4 — сильный: яркий красно-оранжевый, чтобы клетка отличалась от зелёных 1–3.</summary>
        public static readonly Color Signal4 = new Color(0.98f, 0.30f, 0.12f, 1f);

        /// <summary>Цвет успеха.</summary>
        public static readonly Color Success = new Color(0.55f, 0.82f, 0.45f, 1f);

        /// <summary>Подсказка взлома: цифра есть в коде, но стоит не на своём месте.</summary>
        public static readonly Color WrongPlace = new Color(0.95f, 0.78f, 0.25f, 1f);

        /// <summary>Цвет ошибки.</summary>
        public static readonly Color Error = new Color(0.90f, 0.50f, 0.42f, 1f);

        /// <summary>Цвет текста.</summary>
        public static Color Text => AnomalyPath.Runtime.Ui.Core.UiFactory.TextColor;

        /// <summary>Цвет приглушённого текста.</summary>
        public static Color Muted => AnomalyPath.Runtime.Ui.Core.UiFactory.MutedTextColor;

        /// <summary>Цвет клетки по силе сигнала.</summary>
        /// <param name="signal">Сила сигнала 0..4.</param>
        /// <returns>Цвет клетки.</returns>
        public static Color ForSignal(int signal) => signal switch
        {
            4 => Signal4,
            3 => Signal3,
            2 => Signal2,
            1 => Signal1,
            _ => CellIdle,
        };
    }
}
