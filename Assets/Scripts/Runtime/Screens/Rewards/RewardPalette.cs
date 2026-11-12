#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Rewards
{
    /// <summary>Цвета экрана наград: фон окна, панели и сообщение о переполнении.</summary>
    public static class RewardPalette
    {
        /// <summary>
        /// Фон окна наград: прозрачный. Панели наград и инвентаря несут собственную текстуру,
        /// а чёрная плашка во весь экран закрывала фон локации и читалась как «плохой фон».
        /// </summary>
        public static readonly Color Window = new Color(0.05f, 0.065f, 0.06f, 0f);

        /// <summary>Фон панели, если текстура окна инвентаря не нашлась.</summary>
        public static readonly Color Panel = new Color(0.08f, 0.10f, 0.09f, 0.92f);

        /// <summary>Цвет сообщения о том, что вещь не влезла в инвентарь.</summary>
        public static readonly Color Error = new Color(0.95f, 0.55f, 0.45f, 1f);
    }
}
