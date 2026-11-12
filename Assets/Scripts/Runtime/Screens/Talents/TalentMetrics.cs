#nullable enable
namespace AnomalyPath.Runtime.Screens.Talents
{
    /// <summary>Размеры и тайминги экрана талантов в единицах канваса.</summary>
    public static class TalentMetrics
    {
        /// <summary>Ширина окна талантов.</summary>
        public const float WindowWidth = 725f;

        /// <summary>Высота окна талантов.</summary>
        public const float WindowHeight = 900f;

        /// <summary>Верх заголовка от верха окна.</summary>
        public const float TitleTop = 16f;

        /// <summary>Высота заголовка.</summary>
        public const float TitleHeight = 44f;

        /// <summary>Ширина заголовка: по центру окна, чтобы не заходить на панель очков.</summary>
        public const float TitleWidth = 260f;

        /// <summary>Отступ панели уровня и очков от верхнего правого угла окна.</summary>
        public const float PanelMargin = 16f;

        /// <summary>Ширина панели уровня и очков.</summary>
        public const float PanelWidth = 209f;

        /// <summary>Высота панели уровня и очков.</summary>
        public const float PanelHeight = 141f;

        /// <summary>Отступ строк панели от её левого верхнего угла.</summary>
        public const float PanelTextInset = 12f;

        /// <summary>Ширина строк панели.</summary>
        public const float PanelTextWidth = 185f;

        /// <summary>Высота строки уровня.</summary>
        public const float LevelHeight = 28f;

        /// <summary>Верх строки свободных очков.</summary>
        public const float PointsTop = 46f;

        /// <summary>Высота строки свободных очков.</summary>
        public const float PointsHeight = 78f;

        /// <summary>Верх области дерева от верха окна.</summary>
        public const float TreeTop = 80f;

        /// <summary>Ширина карточки таланта.</summary>
        public const float NodeWidth = 120f;

        /// <summary>Высота карточки таланта.</summary>
        public const float NodeHeight = 112f;

        /// <summary>Отступ картинки таланта от края карточки.</summary>
        public const float IconInset = 4f;

        /// <summary>Ширина плашки счётчика ранга.</summary>
        public const float CounterWidth = 52f;

        /// <summary>Высота плашки счётчика ранга.</summary>
        public const float CounterHeight = 20f;

        /// <summary>Отступ плашки счётчика от правого нижнего угла карточки.</summary>
        public const float CounterInset = 6f;

        /// <summary>Зазор между колонками дерева.</summary>
        public const float ColumnGap = 40f;

        /// <summary>Шаг колонки дерева: карточка и зазор.</summary>
        public const float ColumnStep = NodeWidth + ColumnGap;

        /// <summary>Зазор между строками дерева.</summary>
        public const float RowGap = 16f;

        /// <summary>Шаг строки дерева: карточка и зазор.</summary>
        public const float RowStep = NodeHeight + RowGap;

        /// <summary>Ширина полосы прокрутки дерева.</summary>
        public const float ScrollBarWidth = 8f;

        /// <summary>Запас ширины области прокрутки под полосу и её отступ.</summary>
        public const float ScrollReserve = ScrollBarWidth + 8f;

        /// <summary>Ширина видимой области дерева.</summary>
        public const float TreeViewportWidth = (3f * NodeWidth) + (2f * ColumnGap);

        /// <summary>Ширина области прокрутки вместе с запасом под полосу.</summary>
        public const float TreeWidth = TreeViewportWidth + ScrollReserve;

        /// <summary>Высота видимой области дерева: пять строк карточек.</summary>
        public const float TreeHeight = (5f * NodeHeight) + (4f * RowGap);

        /// <summary>Левая граница области дерева: видимая часть по центру окна, полоса справа.</summary>
        public const float TreeLeft = (WindowWidth - TreeViewportWidth) / 2f;

        /// <summary>Ширина кнопки внизу окна.</summary>
        public const float ButtonWidth = 260f;

        /// <summary>Высота кнопки внизу окна.</summary>
        public const float ButtonHeight = 130f;

        /// <summary>Отступ нижних кнопок от низа окна.</summary>
        public const float ButtonBottom = 20f;

        /// <summary>Боковой отступ нижних кнопок от краёв окна.</summary>
        public const float ButtonSide = 60f;

        /// <summary>Низ полосы сообщений над кнопками.</summary>
        public const float ToastBottom = 156f;

        /// <summary>Высота полосы сообщений.</summary>
        public const float ToastHeight = 34f;

        /// <summary>Ширина полосы сообщений.</summary>
        public const float ToastWidth = 640f;

        /// <summary>Сколько держать курсор на таланте до показа подсказки, секунды.</summary>
        public const float TooltipDelay = 2f;

        /// <summary>Ширина подсказки.</summary>
        public const float TooltipWidth = 360f;

        /// <summary>Смещение подсказки от курсора.</summary>
        public const float TooltipOffset = 18f;

        /// <summary>Отступ подсказки от края экрана.</summary>
        public const float TooltipMargin = 8f;

        /// <summary>Ширина связи между талантами.</summary>
        public const float LinkWidth = 4f;
    }
}
