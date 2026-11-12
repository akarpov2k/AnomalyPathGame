#nullable enable
using System.Collections.Generic;
using AnomalyPath.Runtime.Settings;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Экран настройки клавиш по прототипу: раздел «Игра», строки «функция → клавиша»,
    /// отметка несохранённого изменения, «Сохранить» и «Назад» с подтверждением сброса.
    /// Строки строятся из неактивного образца префаба; горячие клавиши начинают работать
    /// только после «Сохранить».
    /// </summary>
    public sealed class KeyBindingsScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Прокручиваемый список привязок")]
        [SerializeField]
        private ScrollArea rows = null!;

        [Tooltip("Неактивный образец строки привязки")]
        [SerializeField]
        private KeyBindRow rowTemplate = null!;

        [Tooltip("Кнопка «Сохранить»")]
        [SerializeField]
        private Button save = null!;

        [Tooltip("Кнопка «Назад»")]
        [SerializeField]
        private Button back = null!;

        private readonly Dictionary<HotkeyAction, KeyBindRow> _rowByAction =
            new Dictionary<HotkeyAction, KeyBindRow>();

        private ConfirmDialog _confirm = null!;
        private HotkeyAction? _capture;

        /// <inheritdoc />
        protected override void OnBound()
        {
            _confirm = Host.CreateConfirmDialog(Panel.transform);
            save.onClick.AddListener(SaveBindings);
            back.onClick.AddListener(Back);
        }

        /// <summary>Показывает сохранённые привязки и строит строки списка.</summary>
        public void Show()
        {
            _capture = null;
            Host.Settings.RevertBindings();
            Host.Router.Show(Panel);
            Rebuild();
        }

        /// <summary>Esc закрывает подтверждение, отменяет ожидание клавиши или возвращает назад.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_confirm.HandleEscape())
            {
                return true;
            }

            if (_capture != null)
            {
                _capture = null;
                Rebuild();
                return true;
            }

            Back();
            return true;
        }

        private void Update()
        {
            if (_capture == null)
            {
                return;
            }

            var keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            foreach (var control in keyboard.allKeys)
            {
                if (control == null || !control.wasPressedThisFrame || control.keyCode == Key.Escape)
                {
                    continue;
                }

                var action = _capture.Value;
                Host.Settings.SetBinding(action, control.keyCode);
                _capture = null;
                Rebuild();
                return;
            }
        }

        private void Rebuild()
        {
            ClearRows();
            _rowByAction.Clear();
            foreach (var action in HotkeyDefaults.All)
            {
                var row = Instantiate(rowTemplate, rows.Content);
                row.gameObject.name = "KeyRow_" + action;
                row.gameObject.SetActive(true);
                var key = Host.Settings.GetBinding(action);
                var changed = key != Host.Settings.GetSavedBinding(action);
                row.Bind(Host.Texts, action, key, changed, _capture == action);
                var captured = action;
                row.KeyButton.onClick.AddListener(() => BeginCapture(captured));
                _rowByAction[action] = row;
            }
        }

        private void BeginCapture(HotkeyAction action)
        {
            _capture = action;
            foreach (var pair in _rowByAction)
            {
                var key = Host.Settings.GetBinding(pair.Key);
                var changed = key != Host.Settings.GetSavedBinding(pair.Key);
                pair.Value.Bind(Host.Texts, pair.Key, key, changed, pair.Key == action);
            }
        }

        private void SaveBindings()
        {
            _capture = null;
            Host.Settings.SaveBindings();
            Rebuild();
            Host.Toast(UiText.Get(Host.Texts, UiTextKey.Keys_Saved));
        }

        private void Back()
        {
            if (!Host.Settings.HasUnsavedBindings)
            {
                Host.ShowSettings();
                return;
            }

            _confirm.Show(
                UiText.Get(Host.Texts, UiTextKey.Keys_Unsaved_Title),
                UiText.Get(Host.Texts, UiTextKey.Keys_Unsaved_Question),
                UiText.Get(Host.Texts, UiTextKey.Keys_Unsaved_Discard),
                DiscardAndBack);
        }

        private void DiscardAndBack()
        {
            _capture = null;
            Host.Settings.RevertBindings();
            Host.ShowSettings();
        }

        private void ClearRows()
        {
            foreach (Transform child in rows.Content)
            {
                if (child.gameObject == rowTemplate.gameObject)
                {
                    continue;
                }

                child.SetParent(null, false);
                Destroy(child.gameObject);
            }
        }
    }
}
