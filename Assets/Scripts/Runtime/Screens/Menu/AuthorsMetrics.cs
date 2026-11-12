#nullable enable
namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Размеры и скорость экрана авторов в единицах канваса: заголовок игры сверху,
    /// окно титров по центру и кнопка выхода в меню снизу.
    /// Это стартовые значения разметки: в игре геометрия берётся из префаба, а не отсюда.
    /// </summary>
    public static class AuthorsMetrics
    {
        /// <summary>Кегль заголовка игры сверху экрана.</summary>
        public const int GameTitleSize = 64;

        /// <summary>Высота заголовка игры.</summary>
        public const float GameTitleHeight = 84f;

        /// <summary>Ширина заголовка игры.</summary>
        public const float GameTitleWidth = 1000f;

        /// <summary>Отступ заголовка игры от верхнего края экрана.</summary>
        public const float GameTitleTop = 40f;

        /// <summary>Ширина окна титров по центру экрана: уже портретной опорной ширины 1080.</summary>
        public const float WindowWidth = 1000f;

        /// <summary>Высота окна титров по центру экрана.</summary>
        public const float WindowHeight = 560f;

        /// <summary>Кегль текста титров.</summary>
        public const int CreditsFontSize = 24;

        /// <summary>Межстрочный интервал титров.</summary>
        public const float CreditsLineSpacing = 1.3f;

        /// <summary>Отступ полотна титров от боковых краёв окна.</summary>
        public const float CreditsSideInset = 40f;

        /// <summary>Поле над первой и под последней строкой титров.</summary>
        public const float CreditsPadding = 20f;

        /// <summary>Скорость титров: единицы канваса в секунду.</summary>
        public const float CreditsSpeed = 45f;

        /// <summary>Ширина кнопки выхода в меню: кадр кнопки держит пропорцию 2:1.</summary>
        public const float ButtonWidth = 240f;

        /// <summary>Высота кнопки выхода в меню.</summary>
        public const float ButtonHeight = 120f;

        /// <summary>Отступ кнопки выхода от нижнего края экрана.</summary>
        public const float ButtonBottom = 40f;
    }
}
