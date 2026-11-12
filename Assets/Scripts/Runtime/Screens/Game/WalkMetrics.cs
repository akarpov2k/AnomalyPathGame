#nullable enable
namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Размеры режима похода в единицах канваса 1920×1080.</summary>
    public static class WalkMetrics
    {
        /// <summary>Запас фона за краями экрана для медленного сдвига.</summary>
        public const float BackdropOverscan = 160f;

        /// <summary>Доля движения камеры, на которую сдвигается фон.</summary>
        public const float BackdropParallax = 0.02f;

        /// <summary>Сколько слотов объектов и врагов на дороге.</summary>
        public const int FigureCount = 9;

        /// <summary>Запас дороги за последним объектом.</summary>
        public const float ObjectEndMargin = 700f;

        /// <summary>Сколько копий фона стоит в мире.</summary>
        public const int BackdropCount = 1;

        /// <summary>Сколько переставляемых частей у фона похода.</summary>
        public const int BackdropPartCount = 3;

        /// <summary>Запас мира за концом дороги.</summary>
        public const float WorldTail = 1920f;

        /// <summary>Ширина фигуры на дороге.</summary>
        public const float FigureWidth = 280f;

        /// <summary>Высота плашки подписи фигуры.</summary>
        public const float FigureLabelHeight = 46f;

        /// <summary>Ширина игрока.</summary>
        public const float PlayerWidth = 200f;

        /// <summary>Высота игрока.</summary>
        public const float PlayerHeight = 380f;

        /// <summary>Сколько кадров ходьбы игрока проигрывается в секунду.</summary>
        public const float WalkFramesPerSecond = 13f;

        /// <summary>Сколько кадров бега игрока проигрывается в секунду.</summary>
        public const float RunFramesPerSecond = 17f;

        /// <summary>Доля высоты экрана, занятая полосой неба сверху.</summary>
        public const float SkyBandHeight = 0.2f;

        /// <summary>
        /// Нижняя кромка полосы неба: она заходит на кадр локации, чтобы небо
        /// растворялось в нём без резкой линии.
        /// </summary>
        public const float SkyBandBottom = 0.74f;

        /// <summary>Доля ширины экрана, где светило восходит.</summary>
        public const float SkySunLeft = 0.1f;

        /// <summary>Доля ширины экрана, где светило садится.</summary>
        public const float SkySunRight = 0.9f;

        /// <summary>Нижняя граница дуги светила внутри полосы неба, доля полосы.</summary>
        public const float SkySunLow = 0.42f;

        /// <summary>Верхняя граница дуги светила внутри полосы неба, доля полосы.</summary>
        public const float SkySunHigh = 0.94f;

        /// <summary>Во сколько раз оттенок слабее внутри помещения.</summary>
        public const float IndoorTintFactor = 0.35f;

        /// <summary>Размер диска солнца в единицах канваса.</summary>
        public const float SkySunSize = 150f;

        /// <summary>Размер диска луны в единицах канваса.</summary>
        public const float SkyMoonSize = 110f;

        /// <summary>Высота полоски земли под фигурами.</summary>
        public const float GroundExtra = 90f;

        /// <summary>Ширина полоски пути.</summary>
        public const float ProgressWidth = 1000f;

        /// <summary>Высота полоски пути.</summary>
        public const float ProgressHeight = 16f;

        /// <summary>Высота полосы дороги/пола внизу.</summary>
        public const float RoadHeight = 260f;

        /// <summary>Высота тёмной полосы земли под фигурами.</summary>
        public const float GroundHeight = 160f;

        /// <summary>Нижний отступ ряда кнопок.</summary>
        public const float ButtonBottom = 40f;

        /// <summary>Нижний отступ кнопки взаимодействия.</summary>
        public const float InteractBottom = 180f;

        /// <summary>Ширина кнопки «Идти» и «В хаб».</summary>
        public const float ButtonWidth = 240f;

        /// <summary>Высота кнопок.</summary>
        public const float ButtonHeight = 80f;

        /// <summary>Ширина кнопки взаимодействия.</summary>
        public const float InteractWidth = 520f;

        /// <summary>Доля дороги, где стоит первое ответвление.</summary>
        public const float BranchFirstFraction = 0.5f;

        /// <summary>Расстояние между ответвлениями.</summary>
        public const float BranchGap = 700f;

        /// <summary>Дальность, с которой доступна кнопка «Вверх».</summary>
        public const float BranchRange = 220f;

        /// <summary>Высота картинки перехода над линией земли: она стоит на вертикальной дороге.</summary>
        public const float BranchRoadOffset = 160f;

        /// <summary>Высота полосы пола в помещении: по ней бежит персонаж, она едет с миром.</summary>
        public const float FloorStripHeight = 260f;

        /// <summary>Сколько ответвлений рисует префаб.</summary>
        public const int BranchCount = 3;

        /// <summary>Длительность перехода между дорогами и зонами.</summary>
        public const float TransitionSeconds = 0.35f;
    }
}
