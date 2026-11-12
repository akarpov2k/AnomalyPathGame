#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Ориентация экрана и ширина канваса в единицах разметки: в портрете канвас 1080,
    /// в ландшафте 1920. Экраны считают по ней раскладку, а не по абсолютным пикселям.
    /// Канвас масштабируется режимом <c>Expand</c>: опорная область не меньше 1920×1080
    /// (1080×1920 в портрете), поэтому при любом разрешении окна разметка не обрезается,
    /// а свободное место остаётся по краям.
    /// </summary>
    public static class LayoutOrientation
    {
        /// <summary>Ширина канваса в портрете.</summary>
        public const float PortraitWidth = 1080f;

        /// <summary>Ширина канваса в ландшафте.</summary>
        public const float LandscapeWidth = 1920f;

        /// <summary><c>true</c>, если экран выше, чем шире.</summary>
        public static bool IsPortrait => Screen.height > Screen.width;
    }
}
