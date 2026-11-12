#nullable enable
namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Размеры основного игрового экрана в единицах канваса 1920×1080 и доли сцены
    /// из прототипа: небо полосой сверху, переходы в верхней половине, объекты и противник
    /// ниже, внизу полоса опыта и значки меню и инвентаря.
    /// </summary>
    public static class PlayMetrics
    {
        /// <summary>Боковой отступ верхних подписей.</summary>
        public const float SidePadding = 48f;

        /// <summary>Высота полосы неба, доля высоты экрана.</summary>
        public const float SkyHeight = 0.234f;

        /// <summary>Левый край перехода-ветки 2, доля ширины сцены.</summary>
        public const float TransitionLeftX1 = 0.034f;

        /// <summary>Правый край перехода-ветки 2, доля ширины сцены.</summary>
        public const float TransitionLeftX2 = 0.255f;

        /// <summary>Нижний край перехода-ветки 2, доля высоты сцены.</summary>
        public const float TransitionLeftBottom = 0.548f;

        /// <summary>Верхний край перехода-ветки 2, доля высоты сцены.</summary>
        public const float TransitionLeftTop = 0.794f;

        /// <summary>Левый край перехода «дальше», доля ширины сцены.</summary>
        public const float TransitionCenterX1 = 0.3895f;

        /// <summary>Правый край перехода «дальше», доля ширины сцены.</summary>
        public const float TransitionCenterX2 = 0.6105f;

        /// <summary>Нижний край перехода «дальше», доля высоты сцены.</summary>
        public const float TransitionCenterBottom = 0.602f;

        /// <summary>Верхний край перехода «дальше», доля высоты сцены.</summary>
        public const float TransitionCenterTop = 0.848f;

        /// <summary>
        /// Точка схода дороги: доля высоты картинки дороги от её низа, где сходятся края.
        /// Одиночный переход ставится низом на эту линию, поэтому он стоит на дороге,
        /// а не висит над ней.
        /// </summary>
        public const float RoadHorizon = 0.44f;

        /// <summary>Ширина центрального перехода у горизонта, доля ширины сцены.</summary>
        public const float CenterTransitionWidth = 0.16f;

        /// <summary>Высота центрального перехода у горизонта, доля высоты сцены.</summary>
        public const float CenterTransitionHeight = 0.2f;

        /// <summary>Левый край перехода-ветки 1, доля ширины сцены.</summary>
        public const float TransitionRightX1 = 0.693f;

        /// <summary>Правый край перехода-ветки 1, доля ширины сцены.</summary>
        public const float TransitionRightX2 = 0.914f;

        /// <summary>Нижний край перехода-ветки 1, доля высоты сцены.</summary>
        public const float TransitionRightBottom = 0.495f;

        /// <summary>Верхний край перехода-ветки 1, доля высоты сцены.</summary>
        public const float TransitionRightTop = 0.741f;

        /// <summary>Насколько свечение больше места объекта, доля его размера.</summary>
        public const float TransitionGlowPadding = 0.18f;

        /// <summary>Множитель размера объекта при наведении.</summary>
        public const float TransitionHoverScale = 1.04f;

        /// <summary>Высота тени под объектом-переходом, доля его места.</summary>
        public const float TransitionShadowHeight = 0.18f;

        /// <summary>Левый край указателя внутри места объекта, доля ширины.</summary>
        public const float TransitionMarkerLeft = 0.3f;

        /// <summary>Правый край указателя внутри места объекта, доля ширины.</summary>
        public const float TransitionMarkerRight = 0.7f;

        /// <summary>Нижний край указателя внутри места объекта, доля высоты.</summary>
        public const float TransitionMarkerBottom = 0.22f;

        /// <summary>Верхний край указателя внутри места объекта, доля высоты.</summary>
        public const float TransitionMarkerTop = 0.78f;

        /// <summary>Левый край левого объекта сцены, доля ширины.</summary>
        public const float ObjectLeftX1 = 0.062f;

        /// <summary>Правый край левого объекта сцены, доля ширины.</summary>
        public const float ObjectLeftX2 = 0.263f;

        /// <summary>Нижний край левого объекта сцены, доля высоты.</summary>
        public const float ObjectLeftBottom = 0.233f;

        /// <summary>Верхний край левого объекта сцены, доля высоты.</summary>
        public const float ObjectLeftTop = 0.435f;

        /// <summary>Левый край правого объекта сцены, доля ширины.</summary>
        public const float ObjectRightX1 = 0.658f;

        /// <summary>Правый край правого объекта сцены, доля ширины.</summary>
        public const float ObjectRightX2 = 0.859f;

        /// <summary>Нижний край правого объекта сцены, доля высоты.</summary>
        public const float ObjectRightBottom = 0.147f;

        /// <summary>Верхний край правого объекта сцены, доля высоты.</summary>
        public const float ObjectRightTop = 0.349f;

        /// <summary>Левый край противника, доля ширины.</summary>
        public const float EnemyX1 = 0.339f;

        /// <summary>Правый край противника, доля ширины.</summary>
        public const float EnemyX2 = 0.575f;

        /// <summary>Нижний край противника, доля высоты.</summary>
        public const float EnemyBottom = 0.042f;

        /// <summary>Верхний край противника, доля высоты.</summary>
        public const float EnemyTop = 0.454f;

        /// <summary>Боковой отступ текста в области объекта.</summary>
        public const float ObjectTextPadding = 16f;

        /// <summary>Множитель ширины контактной тени относительно ширины фигуры на сцене.</summary>
        public const float SceneShadowWidth = 1.12f;

        /// <summary>Высота контактной тени относительно ширины фигуры на сцене.</summary>
        public const float SceneShadowHeight = 0.22f;

        /// <summary>Насколько контактная тень уходит ниже точки земли, доля ширины фигуры.</summary>
        public const float SceneShadowSink = 0.02f;

        /// <summary>Насколько свечение больше фигуры при наведении, доля её размера.</summary>
        public const float SceneGlowPadding = 0.22f;

        /// <summary>Множитель размера фигуры сцены при наведении.</summary>
        public const float SceneHoverScale = 1.03f;

        /// <summary>Отступ заголовка объекта сверху.</summary>
        public const float ObjectTitleTop = 16f;

        /// <summary>Отступ подсказки объекта снизу.</summary>
        public const float ObjectHintBottom = 16f;

        /// <summary>Отступ подсказки объекта сверху.</summary>
        public const float ObjectHintTop = 6f;

        /// <summary>Размер шрифта заголовка объекта.</summary>
        public const int ObjectTitleFontSize = 30;

        /// <summary>Размер шрифта подсказки объекта.</summary>
        public const int ObjectHintFontSize = 18;

        /// <summary>Отступ часов сверху: других подписей в верхней полосе нет.</summary>
        public const float ClockTop = 24f;

        /// <summary>Сторона круглого циферблата.</summary>
        public const float ClockSize = 118f;

        /// <summary>Зазор между циферблатом и численным временем.</summary>
        public const float ClockTimeGap = 16f;

        /// <summary>Ширина подписи времени.</summary>
        public const float ClockTimeWidth = 280f;

        /// <summary>Высота подписи времени.</summary>
        public const float ClockTimeHeight = 60f;

        /// <summary>Размер шрифта подписи времени.</summary>
        public const int ClockTimeFontSize = 24;

        /// <summary>Сторона значка нижнего ряда.</summary>
        public const float IconSize = 64f;

        /// <summary>Сторона плашки под значком нижнего ряда.</summary>
        public const float IconPlate = 112f;

        /// <summary>Боковой отступ нижних значков.</summary>
        public const float IconSide = 40f;

        /// <summary>Отступ нижних значков от полосы опыта.</summary>
        public const float IconBottom = 40f;

        /// <summary>Зазор между значками нижнего ряда.</summary>
        public const float IconGap = 16f;

        /// <summary>Высота полосы опыта внизу экрана.</summary>
        public const float XpBarHeight = 18f;

        /// <summary>Сколько блоков в полосе опыта: по 10 %.</summary>
        public const int XpSegments = 10;

        /// <summary>Отступ подсказки с уровнем и опытом от нижнего края.</summary>
        public const float XpTooltipBottom = 28f;

        /// <summary>Ширина подсказки с уровнем и опытом.</summary>
        public const float XpTooltipWidth = 900f;

        /// <summary>Высота подсказки с уровнем и опытом.</summary>
        public const float XpTooltipHeight = 30f;

        /// <summary>Размер шрифта подсказки с уровнем и опытом.</summary>
        public const int XpTooltipFontSize = 20;

        /// <summary>Отступ всплывающего сообщения снизу: над нижним рядом значков.</summary>
        public const float ToastBottom = 120f;

        /// <summary>Ширина всплывающего сообщения.</summary>
        public const float ToastWidth = 900f;

        /// <summary>Высота всплывающего сообщения.</summary>
        public const float ToastHeight = 56f;

        /// <summary>Размер шрифта всплывающего сообщения.</summary>
        public const int ToastFontSize = 20;
    }
}
