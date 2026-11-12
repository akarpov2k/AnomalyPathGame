#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Цвета основного игрового экрана.</summary>
    public static class PlayPalette
    {
        /// <summary>Фон панели действий.</summary>
        public static readonly Color ActionPanel = new Color(0.06f, 0.08f, 0.07f, 0.92f);

        /// <summary>Фон области объекта.</summary>
        public static readonly Color Object = new Color(0.13f, 0.17f, 0.15f, 0.72f);

        /// <summary>Цвет сообщения об ошибке действия.</summary>
        public static readonly Color Error = new Color(0.95f, 0.55f, 0.45f, 1f);

        /// <summary>Светлая половина циферблата: день.</summary>
        public static readonly Color ClockDay = new Color(0.64f, 0.5f, 0.24f, 1f);

        /// <summary>Тёмная половина циферблата: ночь.</summary>
        public static readonly Color ClockNight = new Color(0.1f, 0.16f, 0.28f, 1f);

        /// <summary>Обводка циферблата.</summary>
        public static readonly Color ClockBorder = new Color(0.9f, 0.84f, 0.64f, 1f);

        /// <summary>Стрелка часов.</summary>
        public static readonly Color ClockHand = new Color(1f, 0.94f, 0.72f, 1f);

        /// <summary>Значок солнца на дневной половине.</summary>
        public static readonly Color ClockSun = new Color(1f, 0.86f, 0.38f, 1f);

        /// <summary>Значок луны на ночной половине.</summary>
        public static readonly Color ClockMoon = new Color(0.82f, 0.88f, 1f, 1f);

        /// <summary>Фон полосы опыта уровня.</summary>
        public static readonly Color XpBar = new Color(0.12f, 0.15f, 0.13f, 0.9f);

        /// <summary>Заполнение полосы опыта уровня.</summary>
        public static readonly Color XpFill = new Color(0.55f, 0.75f, 0.45f, 1f);

        /// <summary>Запасной цвет неба, если картинки фазы суток нет.</summary>
        public static readonly Color SkyFallback = new Color(0.18f, 0.22f, 0.27f, 1f);

        /// <summary>Запасной цвет дороги, если картинки дороги нет.</summary>
        public static readonly Color RoadFallback = new Color(0.10f, 0.11f, 0.10f, 1f);

        /// <summary>Яркость объекта-перехода без подсветки.</summary>
        public static readonly Color TransitionNormal = new Color(0.88f, 0.9f, 0.88f, 1f);

        /// <summary>Яркость объекта-перехода при наведении.</summary>
        public static readonly Color TransitionHover = Color.white;

        /// <summary>Цвет общего указателя у локации без вырезки: дерево, а не серая плашка.</summary>
        public static readonly Color Marker = new Color(0.42f, 0.31f, 0.19f, 1f);

        /// <summary>Свечение позади объекта-перехода при наведении.</summary>
        public static readonly Color TransitionGlow = new Color(0.55f, 0.95f, 0.6f, 0.55f);

        /// <summary>Контактная тень под фигурой на сцене локации.</summary>
        public static readonly Color SceneShadow = new Color(0f, 0f, 0f, 0.5f);

        /// <summary>Свечение позади фигуры сцены при наведении.</summary>
        public static readonly Color SceneGlow = new Color(1f, 0.92f, 0.72f, 0.35f);

        /// <summary>Цвет фигуры при наведении: тон сцены чуть светлее, чтобы фигура «оживала».</summary>
        /// <param name="tint">Цвет подгонки фигуры под тон сцены.</param>
        /// <returns>Осветлённый цвет с прежней прозрачностью.</returns>
        public static Color SceneHighlight(Color tint) => new Color(
            Mathf.Min(1f, tint.r * 1.18f),
            Mathf.Min(1f, tint.g * 1.18f),
            Mathf.Min(1f, tint.b * 1.18f),
            tint.a);

        /// <summary>
        /// Цвет подгонки фигуры: тон сцены из данных, умноженный на её яркость.
        /// Нет тона или он не читается — фигура остаётся как нарисована.
        /// </summary>
        /// <param name="hex">Тон в формате <c>#rrggbb</c>; <c>null</c> — без подгонки.</param>
        /// <param name="brightness">Множитель яркости, больше нуля.</param>
        /// <returns>Цвет фигуры.</returns>
        public static Color SceneTint(string? hex, double brightness)
        {
            var color = Color.white;
            if (!string.IsNullOrWhiteSpace(hex) && !ColorUtility.TryParseHtmlString(hex, out color))
            {
                color = Color.white;
            }

            var factor = brightness > 0 ? (float)brightness : 1f;
            return new Color(color.r * factor, color.g * factor, color.b * factor, 1f);
        }
    }
}
