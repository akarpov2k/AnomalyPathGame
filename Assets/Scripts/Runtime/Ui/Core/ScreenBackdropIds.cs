#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Id готовых фонов экранов в каталоге спрайтов. Файлы лежат в <c>Assets/Art/Backdrops</c>,
    /// id собирает сборщик каталога: <c>backdrops/</c> плюс имя файла без расширения.
    /// </summary>
    public static class ScreenBackdropIds
    {
        /// <summary>Общий фон: настройки, клавиши, сохранение, загрузка, новая игра, авторы.</summary>
        public const string Base = "backdrops/ui_bg_base";

        /// <summary>Фон экранов заданий: доска объявлений и журнал.</summary>
        public const string Quests = "backdrops/ui_bg_quests";

        /// <summary>Фон окна торговли.</summary>
        public const string Trade = "backdrops/ui_bg_trade";

        /// <summary>Фон экрана наград.</summary>
        public const string Reward = "backdrops/ui_bg_reward";

        /// <summary>Фон экрана талантов.</summary>
        public const string Talents = "backdrops/ui_bg_talents";

        /// <summary>Фон мини-игры «Взлом тайника».</summary>
        public const string Lock = "backdrops/ui_bg_lock";

        /// <summary>Фон мини-игры «Поиск артефакта».</summary>
        public const string Radar = "backdrops/ui_bg_radar";

        /// <summary>Фон окна «Инвентарь»: открытый сундук со снаряжением, а не локация.</summary>
        public const string Inventory = "backdrops/ui_bg_inventory";
    }
}
