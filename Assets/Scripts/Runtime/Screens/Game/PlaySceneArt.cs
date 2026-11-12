#nullable enable
using AnomalyPath.Core.World;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Id графики игровой сцены: небо по фазе суток.
    /// Файлы лежат в <c>Assets/Art/Locations</c>, id совпадает с именем файла.
    /// </summary>
    public static class PlaySceneArt
    {
        /// <summary>Небо днём.</summary>
        public const string SkyDay = "loc_sky_day";

        /// <summary>Небо ночью.</summary>
        public const string SkyNight = "loc_sky_night";

        /// <summary>Дорога, если у локации нет собственной: разбитая трасса.</summary>
        public const string DefaultRoad = "loc_road_plain";

        /// <summary>Картинка тайника: у тайника нет записи спавна, иконка общая для всех локаций.</summary>
        public const string StashIcon = "icons/obj_stash";

        /// <summary>Небо для фазы суток: день — светлое, ночь — тёмное.</summary>
        /// <param name="phase">Фаза суток из часов мира.</param>
        /// <returns>Id неба в каталоге спрайтов.</returns>
        public static string Sky(TimePhase phase) => phase == TimePhase.Night ? SkyNight : SkyDay;
    }
}
