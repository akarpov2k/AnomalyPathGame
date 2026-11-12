#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine.InputSystem;

namespace AnomalyPath.Runtime.Settings
{
    /// <summary>Подписи настройки клавиш: название действия из каталога и читаемое имя клавиши.</summary>
    public static class HotkeyText
    {
        /// <summary>Ключ каталога строк с названием действия.</summary>
        /// <param name="action">Действие.</param>
        /// <returns>Ключ подписи.</returns>
        public static UiTextKey ActionKey(HotkeyAction action) => action switch
        {
            HotkeyAction.QuickSave => UiTextKey.Action_QuickSave,
            HotkeyAction.QuickLoad => UiTextKey.Action_QuickLoad,
            HotkeyAction.Inventory => UiTextKey.Action_Inventory,
            HotkeyAction.Character => UiTextKey.Action_Character,
            HotkeyAction.Map => UiTextKey.Action_Map,
            HotkeyAction.Quests => UiTextKey.Action_Quests,
            HotkeyAction.Talents => UiTextKey.Action_Talents,
            _ => UiTextKey.Common_Dash
        };

        /// <summary>Подпись клавиши для игрока; несвязанное действие показывает прочерк из каталога.</summary>
        /// <param name="key">Клавиша.</param>
        /// <param name="texts">Каталог строк.</param>
        /// <returns>Подпись клавиши.</returns>
        public static string KeyLabel(Key key, IGameTexts texts) =>
            key == Key.None ? UiText.Get(texts, UiTextKey.Keys_Unbound) : KeyName(key);

        /// <summary>Читаемое имя клавиши: F5, I, Space, Left Shift.</summary>
        /// <param name="key">Клавиша.</param>
        /// <returns>Имя без перевода.</returns>
        public static string KeyName(Key key)
        {
            if (key >= Key.A && key <= Key.Z)
            {
                return ((char)('A' + (key - Key.A))).ToString();
            }

            if (key >= Key.F1 && key <= Key.F12)
            {
                return "F" + (key - Key.F1 + 1);
            }

            return key switch
            {
                Key.Space => "Space",
                Key.Enter => "Enter",
                Key.Tab => "Tab",
                Key.Escape => "Esc",
                Key.Backspace => "Backspace",
                Key.Delete => "Delete",
                Key.Insert => "Insert",
                Key.Home => "Home",
                Key.End => "End",
                Key.PageUp => "Page Up",
                Key.PageDown => "Page Down",
                Key.UpArrow => "Up Arrow",
                Key.DownArrow => "Down Arrow",
                Key.LeftArrow => "Left Arrow",
                Key.RightArrow => "Right Arrow",
                Key.LeftShift => "Left Shift",
                Key.RightShift => "Right Shift",
                Key.LeftCtrl => "Left Ctrl",
                Key.RightCtrl => "Right Ctrl",
                Key.LeftAlt => "Left Alt",
                Key.RightAlt => "Right Alt",
                Key.CapsLock => "Caps Lock",
                Key.NumLock => "Num Lock",
                Key.PrintScreen => "Print Screen",
                Key.ScrollLock => "Scroll Lock",
                Key.Pause => "Pause",
                Key.Backquote => "`",
                Key.Quote => "'",
                Key.Semicolon => ";",
                Key.Comma => ",",
                Key.Period => ".",
                Key.Slash => "/",
                Key.Backslash => "\\",
                Key.LeftBracket => "[",
                Key.RightBracket => "]",
                Key.Minus => "-",
                Key.Equals => "=",
                Key.Digit0 => "0",
                Key.Digit1 => "1",
                Key.Digit2 => "2",
                Key.Digit3 => "3",
                Key.Digit4 => "4",
                Key.Digit5 => "5",
                Key.Digit6 => "6",
                Key.Digit7 => "7",
                Key.Digit8 => "8",
                Key.Digit9 => "9",
                _ => key.ToString()
            };
        }
    }
}
