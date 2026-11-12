#nullable enable
namespace AnomalyPath.Runtime.Ui.Items
{
    /// <summary>Размеры подсказки предмета: ширина, отступы, кегли и зазор до курсора.</summary>
    public static class ItemTooltipMetrics
    {
        /// <summary>Ширина плашки подсказки.</summary>
        public const float Width = 340f;

        /// <summary>Кегль названия предмета.</summary>
        public const int TitleFontSize = 20;

        /// <summary>Кегль описания и характеристик.</summary>
        public const int BodyFontSize = 16;

        /// <summary>Отступ текста от краёв плашки.</summary>
        public const int Padding = 14;

        /// <summary>Зазор между строками подсказки.</summary>
        public const float Spacing = 4f;

        /// <summary>Отступ подсказки от края экрана.</summary>
        public const float Margin = 8f;

        /// <summary>Сдвиг подсказки от курсора.</summary>
        public const float Offset = 18f;
    }
}
