#nullable enable
namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>Размеры выпадашки торговцев и подсказки с их именами в единицах канваса.</summary>
    public static class TraderMetrics
    {
        /// <summary>Отступ нижнего ряда значков от краёв экрана.</summary>
        public const float BottomMargin = 40f;

        /// <summary>Размер значка нижнего ряда в единицах канваса.</summary>
        public const float IconButtonSize = 112f;

        /// <summary>Ширина карточки торговца в выпадашке.</summary>
        public const float CardWidth = 184f;

        /// <summary>Высота портрета в карточке торговца.</summary>
        public const float CardPortraitHeight = 184f;

        /// <summary>Высота карточки торговца целиком: портрет без рамки и подписи.</summary>
        public const float CardHeight = CardPortraitHeight;

        /// <summary>Зазор между карточками торговцев.</summary>
        public const float CardGap = 12f;

        /// <summary>Внутренний отступ выпадашки торговцев.</summary>
        public const float MenuPadding = 12f;

        /// <summary>Высота заголовка выпадашки торговцев.</summary>
        public const float MenuHeaderHeight = 40f;

        /// <summary>Отступ выпадашки над значком торговцев.</summary>
        public const float MenuGap = 12f;

        /// <summary>Низ выпадашки: над значком торговцев.</summary>
        public const float MenuBottom = BottomMargin + IconButtonSize + MenuGap;

        /// <summary>Высота выпадашки торговцев.</summary>
        public const float MenuHeight = (MenuPadding * 2f) + MenuHeaderHeight + CardHeight;

        /// <summary>Ширина подсказки с именем торговца.</summary>
        public const float TooltipWidth = 260f;

        /// <summary>Высота подсказки с именем торговца.</summary>
        public const float TooltipHeight = 48f;

        /// <summary>Смещение подсказки от курсора: вправо и вниз.</summary>
        public const float TooltipOffset = 18f;

        /// <summary>Отступ подсказки от краёв экрана.</summary>
        public const float TooltipMargin = 8f;

        /// <summary>Размер шрифта заголовка выпадашки.</summary>
        public const int MenuHeaderSize = 24;

        /// <summary>Размер шрифта подсказки с именем торговца.</summary>
        public const int TooltipFontSize = 20;
    }
}
