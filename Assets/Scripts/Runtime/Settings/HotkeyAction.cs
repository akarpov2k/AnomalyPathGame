#nullable enable
namespace AnomalyPath.Runtime.Settings
{
    /// <summary>Действие, которое можно назначить на горячую клавишу.</summary>
    public enum HotkeyAction
    {
        /// <summary>Быстрое сохранение игры.</summary>
        QuickSave,

        /// <summary>Быстрая загрузка последнего быстрого сохранения.</summary>
        QuickLoad,

        /// <summary>Открыть отдельный экран инвентаря.</summary>
        Inventory,

        /// <summary>Открыть экран персонажа.</summary>
        Character,

        /// <summary>Открыть карту мира.</summary>
        Map,

        /// <summary>Открыть журнал заданий.</summary>
        Quests,

        /// <summary>Открыть дерево талантов.</summary>
        Talents
    }
}
