#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>Цвета экрана персонажа.</summary>
    public static class CharacterPalette
    {
        /// <summary>
        /// Запасной цвет фрейма персонажа: в префабе панель закрыта текстурой ржавой рамы,
        /// как окна диалогов, поэтому цвет виден только без картинки.
        /// </summary>
        public static readonly Color Frame = new Color(0.07f, 0.08f, 0.08f, 1f);

        /// <summary>
        /// Запасной цвет слота: в префабе у слота рамка из текстуры ржавой панели,
        /// цвет виден только без картинки.
        /// </summary>
        public static readonly Color Slot = new Color(0.10f, 0.11f, 0.11f, 1f);

        /// <summary>Фон клетки инвентаря.</summary>
        public static readonly Color Cell = new Color(0.10f, 0.12f, 0.13f, 1f);

        /// <summary>Фон фигуры персонажа.</summary>
        public static readonly Color Figure = new Color(0.17f, 0.21f, 0.20f, 1f);

        /// <summary>Фон фигуры под ней: прозрачный, фигура стоит прямо на фоне локации.</summary>
        public static readonly Color FigureArea = new Color(0f, 0f, 0f, 0f);

        /// <summary>Цвет сообщения об ошибке.</summary>
        public static readonly Color Error = new Color(0.95f, 0.55f, 0.45f, 1f);

        /// <summary>Полоса здоровья.</summary>
        public static readonly Color Health = new Color(0.78f, 0.32f, 0.30f, 1f);

        /// <summary>Полоса выносливости.</summary>
        public static readonly Color Stamina = new Color(0.38f, 0.66f, 0.42f, 1f);

        /// <summary>Полоса питья.</summary>
        public static readonly Color Thirst = new Color(0.36f, 0.56f, 0.78f, 1f);

        /// <summary>Полоса еды.</summary>
        public static readonly Color Hunger = new Color(0.72f, 0.58f, 0.30f, 1f);

        /// <summary>Фон полосы.</summary>
        public static readonly Color Bar = new Color(0.16f, 0.18f, 0.18f, 1f);

        /// <summary>
        /// Запасной фон панели статистики: в префабе панель закрыта ржавой рамой,
        /// как окна диалогов, поэтому цвет виден только без картинки.
        /// </summary>
        public static readonly Color StatsPanel = new Color(0.05f, 0.07f, 0.07f, 0.92f);

        /// <summary>Заголовок раздела на панели статистики.</summary>
        public static readonly Color StatsTitle = new Color(0.78f, 0.82f, 0.79f, 1f);

        /// <summary>Значение характеристики на панели статистики.</summary>
        public static readonly Color StatsValue = new Color(0.45f, 0.76f, 0.43f, 1f);

        /// <summary>Линия под заголовком раздела.</summary>
        public static readonly Color StatsLine = new Color(0.45f, 0.52f, 0.48f, 0.55f);

        /// <summary>Приглушённая иконка выключенной кнопки-глаза.</summary>
        public static readonly Color StatsEyeMuted = new Color(0.45f, 0.50f, 0.47f, 1f);
    }
}
