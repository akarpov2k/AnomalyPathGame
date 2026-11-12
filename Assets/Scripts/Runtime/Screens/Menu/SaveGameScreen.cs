#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Экран сохранения по прототипу: карточки существующих сохранений и свободная карточка
    /// «Новое сохранение» с полем названия внутри неё. Пустое имя даёт «Новое сохранение N»,
    /// совпадение имени с другим сохранением спрашивает подтверждение перезаписи того слота.
    /// Разметка, карточки, поле и кнопки приходят из префаба.
    /// </summary>
    public sealed class SaveGameScreen : PrefabScreenBase<IItemScreenHost>
    {
        [Tooltip("Прокручиваемый список карточек сохранений")]
        [SerializeField]
        private ScrollArea rows = null!;

        [Tooltip("Неактивный образец карточки сохранения")]
        [SerializeField]
        private SaveSlotRow rowTemplate = null!;

        [Tooltip("Строка состояния: ошибки сохранения")]
        [SerializeField]
        private Text status = null!;

        [Tooltip("Листалка страниц")]
        [SerializeField]
        private SaveSlotPager pager = null!;

        [Tooltip("Кнопка «Назад»")]
        [SerializeField]
        private Button back = null!;

        private readonly List<KeyValuePair<string, SaveSlotInfo?>> _entries =
            new List<KeyValuePair<string, SaveSlotInfo?>>();

        private readonly Dictionary<string, SaveSlotInfo> _slots =
            new Dictionary<string, SaveSlotInfo>(StringComparer.Ordinal);

        private ConfirmDialog _confirm = null!;
        private int _pageSize;
        private string _newSlot = string.Empty;
        private int _page;
        private int _pages = 1;

        /// <inheritdoc />
        protected override void OnBound()
        {
            _pageSize = Mathf.Max(1, Host.Rules.Require().SavePageSize);
            _confirm = Host.CreateConfirmDialog(Panel.transform);
            back.onClick.AddListener(Back);
            pager.Bind(Host.Texts);
            AddPageButton(pager.FirstPage, () => GoTo(0));
            AddPageButton(pager.PreviousPage, () => GoTo(_page - 1));
            AddPageButton(pager.NextPage, () => GoTo(_page + 1));
            AddPageButton(pager.LastPage, () => GoTo(_pages - 1));
            Panel.AddComponent<RectResizeWatcher>().Resized += OnResized;
        }

        /// <summary>Показывает сохранения: сначала существующие, затем свободную карточку.</summary>
        public void Show()
        {
            BuildEntries();
            _page = 0;
            status.text = string.Empty;
            Host.Router.Show(Panel);
            Rebuild();
            Canvas.ForceUpdateCanvases();
            SaveRowLayout.Apply(rows, _pageSize);
        }

        /// <summary>Esc закрывает подтверждение перезаписи, иначе возвращает в игру.</summary>
        /// <returns><c>true</c>, если Esc обработан.</returns>
        public override bool HandleEscape()
        {
            if (_confirm.HandleEscape())
            {
                return true;
            }

            Back();
            return true;
        }

        private void OnResized()
        {
            if (Panel.activeInHierarchy)
            {
                SaveRowLayout.Apply(rows, _pageSize);
            }
        }

        private static void AddPageButton(Button? button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private static int SlotNumber(string slot)
        {
            if (!slot.StartsWith(SaveSlots.Prefix, StringComparison.Ordinal))
            {
                return 0;
            }

            return int.TryParse(slot.Substring(SaveSlots.Prefix.Length), out var number) && number > 0
                ? number
                : 0;
        }

        private void BuildEntries()
        {
            _slots.Clear();
            foreach (var slot in Host.Saves.ListSlots())
            {
                _slots[slot.Slot] = slot;
            }

            var numeric = new List<SaveSlotInfo>();
            var extra = new List<string>();
            foreach (var pair in _slots)
            {
                if (SlotNumber(pair.Key) > 0)
                {
                    numeric.Add(pair.Value);
                }
                else
                {
                    extra.Add(pair.Key);
                }
            }

            numeric.Sort((left, right) => SlotNumber(left.Slot).CompareTo(SlotNumber(right.Slot)));
            extra.Sort(StringComparer.Ordinal);

            _entries.Clear();
            foreach (var slot in numeric)
            {
                _entries.Add(new KeyValuePair<string, SaveSlotInfo?>(slot.Slot, slot));
            }

            foreach (var slot in extra)
            {
                _entries.Add(new KeyValuePair<string, SaveSlotInfo?>(slot, _slots[slot]));
            }

            var next = 1;
            while (_slots.ContainsKey(SaveSlots.Slot(next)))
            {
                next++;
            }

            _newSlot = SaveSlots.Slot(next);
            _entries.Add(new KeyValuePair<string, SaveSlotInfo?>(_newSlot, null));
            _pages = Mathf.Max(1, Mathf.CeilToInt(_entries.Count / (float)_pageSize));
        }

        private void GoTo(int page)
        {
            var next = Mathf.Clamp(page, 0, _pages - 1);
            if (next == _page)
            {
                return;
            }

            _page = next;
            Rebuild();
        }

        private void Rebuild()
        {
            ClearRows();
            status.text = string.Empty;
            var start = _page * _pageSize;
            var count = Mathf.Clamp(_entries.Count - start, 0, _pageSize);
            for (var index = 0; index < count; index++)
            {
                AddRow(_entries[start + index]);
            }

            pager.Refresh(_page, _pages);
            SaveRowLayout.Apply(rows, _pageSize);
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
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }

        private void AddRow(KeyValuePair<string, SaveSlotInfo?> entry)
        {
            var slot = entry.Key;
            var info = entry.Value;
            var isNew = slot == _newSlot;
            var row = Instantiate(rowTemplate, rows.Content);
            row.gameObject.name = "SaveSlot_" + slot;
            row.gameObject.SetActive(true);
            row.Bind(
                Host.Texts,
                slot,
                SlotNumber(slot),
                info,
                isNew,
                isNew ? NextDefaultName() : string.Empty);
            var saveButton = row.Save;
            if (saveButton != null)
            {
                saveButton.onClick.AddListener(() => RequestSave(row));
            }
        }

        private void RequestSave(SaveSlotRow row)
        {
            if (Host.Session == null)
            {
                status.text = UiText.Get(Host.Texts, UiTextKey.SaveGame_Status_No_Game);
                return;
            }

            var slot = row.Slot;
            if (slot == _newSlot)
            {
                SaveNewSlot(row);
                return;
            }

            Write(slot, ExistingTitle(slot));
        }

        private void SaveNewSlot(SaveSlotRow row)
        {
            var typed = row.NameField != null ? row.NameField.Text.Trim() : string.Empty;
            var name = typed.Length > 0 ? typed : NextDefaultName();
            var existing = FindByTitle(name);
            if (existing != null)
            {
                _confirm.Show(
                    UiText.Get(Host.Texts, UiTextKey.SaveGame_Overwrite_Title),
                    UiText.Format(Host.Texts, UiTextKey.SaveGame_Overwrite_Question, name),
                    UiText.Get(Host.Texts, UiTextKey.SaveGame_Save),
                    () => Write(existing.Slot, name));
                return;
            }

            Write(_newSlot, name);
        }

        private string ExistingTitle(string slot)
        {
            if (_slots.TryGetValue(slot, out var info) && !string.IsNullOrWhiteSpace(info.Title))
            {
                return info.Title;
            }

            var number = SlotNumber(slot);
            return number > 0
                ? UiText.Format(Host.Texts, UiTextKey.SaveSlot_Cell, number)
                : slot;
        }

        private SaveSlotInfo? FindByTitle(string name)
        {
            foreach (var pair in _slots)
            {
                if (pair.Value.IsReadable &&
                    string.Equals(pair.Value.Title, name, StringComparison.OrdinalIgnoreCase))
                {
                    return pair.Value;
                }
            }

            return null;
        }

        private string NextDefaultName()
        {
            var count = 0;
            var prefix = UiText.Get(Host.Texts, UiTextKey.SaveSlot_New);
            foreach (var info in _slots.Values)
            {
                if (info.Title.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return UiText.Format(Host.Texts, UiTextKey.SaveSlot_New_Name, count + 1);
        }

        private void Write(string slot, string name)
        {
            var session = Host.Session;
            if (session == null)
            {
                status.text = UiText.Get(Host.Texts, UiTextKey.SaveGame_Status_No_Game);
                return;
            }

            try
            {
                Host.Sessions.Save(session, slot, title: name);
                Host.ShowGame(announce: false);
                Host.Toast(UiText.Format(Host.Texts, UiTextKey.SaveGame_Toast_Saved, name));
            }
            catch (Exception error)
            {
                status.text = UiText.Format(Host.Texts, UiTextKey.SaveGame_Status_Error, error.Message);
            }
        }

        private void Back()
        {
            if (Host.Session != null)
            {
                Host.ShowGame(announce: false);
            }
            else
            {
                Host.ShowMainMenu();
            }
        }
    }
}
