#nullable enable
namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Размеры стартового меню, экранов загрузки и сохранения для сборщика префабов.
    /// Это стартовые значения разметки: в игре геометрия берётся из префаба, а не отсюда.
    /// </summary>
    public static class MenuScreenMetrics
    {
        /// <summary>Ширина колонки кнопок стартового меню.</summary>
        public const float MenuColumnWidth = 480f;

        /// <summary>Ширина кнопки стартового меню: кадр кнопки держит пропорцию 2:1.</summary>
        public const float MenuButtonWidth = 240f;

        /// <summary>Высота кнопки стартового меню: кадр кнопки держит пропорцию 2:1.</summary>
        public const float MenuButtonHeight = 140f;

        /// <summary>Зазор между кнопками стартового меню.</summary>
        public const float MenuButtonSpacing = 16f;

        /// <summary>Кегль подписи кнопки стартового меню: самая длинная надпись помещается в табличку.</summary>
        public const int MenuButtonFontSize = 26;

        /// <summary>Отступ колонки кнопок от левого края окна.</summary>
        public const float MenuButtonInset = 120f;

        /// <summary>Кегль названия игры над кнопками стартового меню.</summary>
        public const int GameTitleSize = 64;

        /// <summary>Высота названия игры над кнопками стартового меню.</summary>
        public const float GameTitleHeight = 96f;

        /// <summary>Отступ картинки меню снизу.</summary>
        public const float ArtworkInsetBottom = 48f;

        /// <summary>Отступ картинки меню сверху.</summary>
        public const float ArtworkInsetTop = 68f;

        /// <summary>Доля ширины окна, с которой начинается картинка меню.</summary>
        public const float ArtworkLeft = 0.17f;

        /// <summary>Доля ширины окна, на которой кончается картинка меню.</summary>
        public const float ArtworkRight = 0.725f;

        /// <summary>Отступ подписи версии от правого края.</summary>
        public const float VersionInsetRight = 32f;

        /// <summary>Отступ подписи версии от нижнего края.</summary>
        public const float VersionInsetBottom = 20f;

        /// <summary>Боковой отступ списка сохранений.</summary>
        public const float ListPadding = 24f;

        /// <summary>Отступ списка сохранений сверху.</summary>
        public const float ListTopInset = 16f;

        /// <summary>Отступ списка сохранений снизу: над листалкой и строкой состояния.</summary>
        public const float ListBottomInset = 120f;

        /// <summary>Высота карточки сохранения в префабе; в игре считается по числу карточек на экран.</summary>
        public const float RowHeight = 148f;

        /// <summary>Минимальная высота карточки сохранения.</summary>
        public const float RowMinHeight = 96f;

        /// <summary>Максимальная высота карточки сохранения.</summary>
        public const float RowMaxHeight = 240f;

        /// <summary>Высота поля названия внутри новой карточки.</summary>
        public const float RowNameHeight = 44f;

        /// <summary>Зазор между строками сохранений.</summary>
        public const float RowGap = 12f;

        /// <summary>Ширина картинки строки.</summary>
        public const float RowThumbnailWidth = 196f;

        /// <summary>Высота картинки строки.</summary>
        public const float RowThumbnailHeight = 92f;

        /// <summary>Отступ картинки строки от краёв.</summary>
        public const float RowThumbnailInset = 16f;

        /// <summary>Левый край текстов строки: правее картинки.</summary>
        public const float RowTextLeft = 232f;

        /// <summary>Отступ текстов строки от верхнего края.</summary>
        public const float RowTextTop = 14f;

        /// <summary>Высота строки названия в карточке.</summary>
        public const float RowTitleHeight = 32f;

        /// <summary>Высота строки прогресса в карточке.</summary>
        public const float RowLineHeight = 26f;

        /// <summary>Кегль названия сохранения.</summary>
        public const int RowTitleSize = 24;

        /// <summary>Кегль прогресса сохранения.</summary>
        public const int RowLineSize = 18;

        /// <summary>Ширина кнопок строки на экране загрузки.</summary>
        public const float RowButtonWidth = 220f;

        /// <summary>Высота кнопок строки на экране загрузки.</summary>
        public const float RowButtonHeight = 46f;

        /// <summary>Ширина кнопки сохранения в строке.</summary>
        public const float RowSaveButtonWidth = 240f;

        /// <summary>Высота кнопки сохранения в строке.</summary>
        public const float RowSaveButtonHeight = 52f;

        /// <summary>Отступ кнопок строки от правого края.</summary>
        public const float RowButtonInset = 16f;

        /// <summary>Зазор между верхней и нижней кнопками строки.</summary>
        public const float RowButtonGap = 8f;

        /// <summary>Ширина полосы прокрутки списка.</summary>
        public const float ScrollbarWidth = 12f;

        /// <summary>Ширина листалки страниц.</summary>
        public const float PagerWidth = 520f;

        /// <summary>Высота листалки страниц.</summary>
        public const float PagerHeight = 56f;

        /// <summary>Ширина кнопки-стрелки листалки.</summary>
        public const float PagerArrowWidth = 56f;

        /// <summary>Высота кнопки-стрелки листалки.</summary>
        public const float PagerArrowHeight = 52f;

        /// <summary>Ширина подписи номера страницы.</summary>
        public const float PagerLabelWidth = 180f;

        /// <summary>Смещение стрелки от края листалки.</summary>
        public const float PagerArrowInset = 62f;

        /// <summary>Отступ листалки от нижнего края.</summary>
        public const float PagerBottomInset = 18f;

        /// <summary>Ширина кнопки «Назад».</summary>
        public const float BackWidth = 160f;

        /// <summary>Высота кнопки «Назад»: кадр кнопки держит пропорцию 2:1.</summary>
        public const float BackHeight = 80f;

        /// <summary>Отступ кнопки «Назад» от правого края.</summary>
        public const float BackInset = 24f;

        /// <summary>Отступ кнопки «Назад» от нижнего края — на уровне листалки.</summary>
        public const float BackBottomInset = 18f;

        /// <summary>Высота строки состояния.</summary>
        public const float StatusHeight = 30f;

        /// <summary>Отступ строки состояния над листалкой.</summary>
        public const float StatusBottomInset = 82f;
    }
}
