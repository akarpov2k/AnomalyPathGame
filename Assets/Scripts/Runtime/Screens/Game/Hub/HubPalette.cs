#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>Цвета экрана хаба: подсказка, плашка под значком и тонировка торговца.</summary>
    public static class HubPalette
    {
        /// <summary>Фон плашки подсказки.</summary>
        public static readonly Color Tooltip = new Color(0.02f, 0.03f, 0.025f, 0.88f);

        /// <summary>Тонировка фигуры торговца под тёплый свет интерьера.</summary>
        public static readonly Color MerchantTint = new Color(0.73f, 0.64f, 0.50f, 1f);
    }
}
