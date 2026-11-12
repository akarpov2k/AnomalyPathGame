#nullable enable
using System;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Stage
{
    /// <summary>
    /// Правило картинки фигуры на сцене по стороне экрана: левый слот берёт вырезку как
    /// она нарисована, правый — её зеркальную копию, чтобы направление объекта (перед
    /// машины, открытая дверца, свисающая сеть) смотрело внутрь кадра, а не за край.
    /// Зеркальной копии нет — остаётся основная вырезка, решает каталог спрайтов.
    /// </summary>
    public static class StageArt
    {
        /// <summary>Хвост имени файла и id зеркальной копии: <c>obj_crate_mirror</c>.</summary>
        public const string MirrorSuffix = "_mirror";

        /// <summary>
        /// Id картинки фигуры для слота: правый слот получает зеркальную копию,
        /// левый и центральный — исходную вырезку.
        /// </summary>
        /// <param name="iconId">Id вырезки в каталоге спрайтов, как он записан в данных.</param>
        /// <param name="slot">Слот сцены, в который ставится фигура.</param>
        /// <returns>Id, по которому экран ищет спрайт.</returns>
        /// <exception cref="ArgumentNullException">Id не задан.</exception>
        /// <exception cref="ArgumentException">Id пустой.</exception>
        public static string FigureId(string iconId, StageSlot slot)
        {
            if (iconId == null)
            {
                throw new ArgumentNullException(nameof(iconId));
            }

            if (string.IsNullOrWhiteSpace(iconId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.StageArtIcon, iconId),
                    nameof(iconId));
            }

            return slot == StageSlot.Right ? iconId + MirrorSuffix : iconId;
        }
    }
}
