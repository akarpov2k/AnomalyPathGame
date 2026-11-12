#nullable enable
namespace AnomalyPath.Core.Localization
{
    /// <summary>Часть класса <see cref="CoreErrors"/>: сцена локации и постановка фигур.</summary>
    public static partial class CoreErrors
    {
        /// <summary>Неизвестный слот сцены.</summary>
        public const string StageUnknownSlot = "error.stage_unknown_slot";

        /// <summary>Значение блока сцены вне допустимого диапазона.</summary>
        public const string StageRange = "error.stage_range";

        /// <summary>У сцены нет ни одной точки земли.</summary>
        public const string StageAnchorsEmpty = "error.stage_anchors_empty";

        /// <summary>Цвет сцены не в формате #rrggbb.</summary>
        public const string StageColor = "error.stage_color";

        /// <summary>Слот сцены размечен дважды.</summary>
        public const string StageSlotDuplicate = "error.stage_slot_duplicate";

        /// <summary>Высота фигуры не положительна.</summary>
        public const string StageHeight = "error.stage_height";

        /// <summary>У картинки фигуры сцены нет id.</summary>
        public const string StageArtIcon = "error.stage_art_icon";
    }
}
