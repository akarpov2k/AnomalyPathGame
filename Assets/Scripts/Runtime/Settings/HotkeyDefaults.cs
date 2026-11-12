#nullable enable
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace AnomalyPath.Runtime.Settings
{
    /// <summary>
    /// Привязки горячих клавиш по умолчанию и порядок строк на экране настройки клавиш.
    /// Esc в список не входит: это служебная клавиша возврата, она не переназначается.
    /// </summary>
    public static class HotkeyDefaults
    {
        private static readonly HotkeyAction[] Actions =
        {
            HotkeyAction.QuickSave,
            HotkeyAction.QuickLoad,
            HotkeyAction.Inventory,
            HotkeyAction.Character,
            HotkeyAction.Map,
            HotkeyAction.Quests,
            HotkeyAction.Talents
        };

        /// <summary>Действия в порядке показа на экране настройки клавиш.</summary>
        public static IReadOnlyList<HotkeyAction> All => Actions;

        /// <summary>Клавиша действия по умолчанию.</summary>
        /// <param name="action">Действие.</param>
        /// <returns>Клавиша по умолчанию.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Действие неизвестно.</exception>
        public static Key For(HotkeyAction action) => action switch
        {
            HotkeyAction.QuickSave => Key.F5,
            HotkeyAction.QuickLoad => Key.F9,
            HotkeyAction.Inventory => Key.I,
            HotkeyAction.Character => Key.C,
            HotkeyAction.Map => Key.M,
            HotkeyAction.Quests => Key.J,
            HotkeyAction.Talents => Key.T,
            _ => throw new System.ArgumentOutOfRangeException(nameof(action))
        };
    }
}
