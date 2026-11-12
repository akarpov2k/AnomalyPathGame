#nullable enable
using AnomalyPath.Core.Templates;
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>
    /// Цвета окантовки по редкости: обычная без рамки, редкая зелёная, особая синяя,
    /// эпическая фиолетовая, легендарная жёлто-рыжая, уникальная красная.
    /// </summary>
    public static class RarityPalette
    {
        /// <summary>Редкая ступень: зелёная окантовка.</summary>
        public static readonly Color Rare = new Color(0.36f, 0.82f, 0.44f, 1f);

        /// <summary>Особая ступень: синяя окантовка.</summary>
        public static readonly Color Special = new Color(0.34f, 0.62f, 0.98f, 1f);

        /// <summary>Эпическая ступень: фиолетовая окантовка.</summary>
        public static readonly Color Epic = new Color(0.74f, 0.44f, 0.96f, 1f);

        /// <summary>Легендарная ступень: жёлто-рыжая окантовка.</summary>
        public static readonly Color Legendary = new Color(0.98f, 0.72f, 0.26f, 1f);

        /// <summary>Уникальная ступень: красная окантовка.</summary>
        public static readonly Color Unique = new Color(0.94f, 0.33f, 0.30f, 1f);

        /// <summary>Возвращает цвет окантовки ступени.</summary>
        /// <param name="rarity">Ступень редкости.</param>
        /// <param name="color">Цвет окантовки.</param>
        /// <returns><c>false</c> у обычной ступени: рамки нет.</returns>
        public static bool TryGet(Rarity rarity, out Color color)
        {
            switch (rarity)
            {
                case Rarity.Rare:
                    color = Rare;
                    return true;
                case Rarity.Special:
                    color = Special;
                    return true;
                case Rarity.Epic:
                    color = Epic;
                    return true;
                case Rarity.Legendary:
                    color = Legendary;
                    return true;
                case Rarity.Unique:
                    color = Unique;
                    return true;
                default:
                    color = Color.clear;
                    return false;
            }
        }
    }
}
