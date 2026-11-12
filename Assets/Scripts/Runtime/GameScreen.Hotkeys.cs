#nullable enable
using System;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine.InputSystem;

namespace AnomalyPath.Runtime
{
    /// <summary>Часть класса <see cref="GameScreen"/>: горячие клавиши и быстрые сохранение и загрузка.</summary>
    public sealed partial class GameScreen
    {
        private static readonly HotkeyAction[] HotkeyOrder =
        {
            HotkeyAction.QuickSave,
            HotkeyAction.QuickLoad,
            HotkeyAction.Inventory,
            HotkeyAction.Character,
            HotkeyAction.Map,
            HotkeyAction.Quests,
            HotkeyAction.Talents
        };

        /// <summary>
        /// Выполняет действие горячей клавиши, если сейчас показано игровое поле.
        /// В бою, меню и при вводе текста действия недоступны.
        /// </summary>
        /// <param name="action">Действие.</param>
        /// <returns><c>true</c>, если действие выполнено.</returns>
        public bool TryRunHotkey(HotkeyAction action)
        {
            if (!IsGamePanelCurrent() || Session == null || Session.InCombat ||
                (_npcDialog != null && _npcDialog.IsShown))
            {
                return false;
            }

            switch (action)
            {
                case HotkeyAction.QuickSave:
                    return QuickSave();
                case HotkeyAction.QuickLoad:
                    return QuickLoad();
                case HotkeyAction.Inventory:
                    ShowInventory();
                    return true;
                case HotkeyAction.Character:
                    ShowInventory();
                    return true;
                case HotkeyAction.Map:
                    ShowMap();
                    return true;
                case HotkeyAction.Quests:
                    ShowQuests(null);
                    return true;
                case HotkeyAction.Talents:
                    ShowTalents();
                    return true;
                default:
                    return false;
            }
        }

        private void UpdateHotkeys()
        {
            if (TextInputField.HasFocus || !IsGamePanelCurrent() || Session == null || Session.InCombat ||
                (_npcDialog != null && _npcDialog.IsShown))
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            foreach (var action in HotkeyOrder)
            {
                var key = Settings.GetSavedBinding(action);
                if (key == Key.None)
                {
                    continue;
                }

                var control = keyboard[key];
                if (control == null || !control.wasPressedThisFrame)
                {
                    continue;
                }

                if (TryRunHotkey(action))
                {
                    return;
                }
            }
        }

        /// <summary>
        /// Показано ли игровое поле: общий экран локации или экран хаба.
        /// Горячие клавиши действуют на обоих, но не в меню, бою и диалогах.
        /// </summary>
        /// <returns><c>true</c>, если текущая панель — игровое поле.</returns>
        private bool IsGamePanelCurrent()
        {
            var current = Router.Current;
            if (current == null)
            {
                return false;
            }

            return current == _play.Panel ||
                   (_bigHub != null && current == _bigHub.Panel) ||
                   (_hub != null && current == _hub.Panel);
        }

        private bool QuickSave()
        {
            var session = Session;
            if (session == null)
            {
                Toast(UiText.Get(Texts, UiTextKey.Common_No_Game));
                return false;
            }

            try
            {
                Sessions.Save(session, SaveSlots.Quick, title: UiText.Get(Texts, UiTextKey.Quick_Save_Title));
                Toast(UiText.Get(Texts, UiTextKey.Quick_Save_Done));
                return true;
            }
            catch (Exception error)
            {
                Toast(UiText.Format(Texts, UiTextKey.Quick_Save_Error, error.Message));
                return false;
            }
        }

        private bool QuickLoad()
        {
            if (!Saves.Exists(SaveSlots.Quick))
            {
                Toast(UiText.Get(Texts, UiTextKey.Quick_Load_None));
                return false;
            }

            try
            {
                Session = Sessions.Load(SaveSlots.Quick);
                ShowGame(announce: false);
                Toast(UiText.Get(Texts, UiTextKey.Quick_Load_Done));
                return true;
            }
            catch (Exception error)
            {
                Toast(UiText.Format(Texts, UiTextKey.Quick_Load_Error, error.Message));
                return false;
            }
        }
    }
}
