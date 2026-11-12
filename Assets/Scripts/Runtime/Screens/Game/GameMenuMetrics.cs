#nullable enable
namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Размеры меню игры (Esc) для сборщика префаба: колонка по центру, сводка состояния,
    /// счётчик смертей и кнопки прежней плашкой. Это стартовые значения разметки: в игре
    /// геометрия берётся из префаба, а не отсюда.
    /// </summary>
    public static class GameMenuMetrics
    {
        /// <summary>Ширина колонки меню по центру экрана.</summary>
        public const float ColumnWidth = 760f;

        /// <summary>Поле колонки со всех сторон.</summary>
        public const int ColumnPadding = 20;

        /// <summary>Зазор между элементами колонки.</summary>
        public const float ColumnSpacing = 10f;

        /// <summary>Кегль заголовка меню.</summary>
        public const int TitleSize = 40;

        /// <summary>Высота заголовка меню.</summary>
        public const float TitleHeight = 70f;

        /// <summary>Кегль строки состояния и счётчика смертей.</summary>
        public const int TextSize = 20;

        /// <summary>Высота строки состояния: четыре строки сводки.</summary>
        public const float StatusHeight = 160f;

        /// <summary>Высота строки счётчика смертей.</summary>
        public const float DeathsHeight = 32f;

        /// <summary>Высота кнопки меню: кнопки остаются прежней плашкой.</summary>
        public const float ButtonHeight = 54f;

        /// <summary>Кегль подписи кнопки меню.</summary>
        public const int ButtonLabelSize = 22;
    }
}
