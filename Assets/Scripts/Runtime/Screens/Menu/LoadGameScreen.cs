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
    /// Экран загрузки по прототипу: строки сохранений с кнопками «Загрузить» и «Удалить»,
    /// листалка страниц и возврат. Разметка, строки и кнопки приходят из префаба;
    /// код собирает строки из данных и обрабатывает нажатия.
    /// </summary>
    public sealed class LoadGameScreen : PrefabScreenBase<IItemScreenHost>
    {
        [Tooltip("Прокручиваемый список строк сохранений")]
        [SerializeField]
        private ScrollArea rows = null!;

        [Tooltip("Неактивный образец строки сохранения")]
        [SerializeField]
        private SaveSlotRow rowTemplate = null!;

        [Tooltip("Подпись «Сохранений нет»")]
        [SerializeField]
        private Text empty = null!;

        [Tooltip("Строка состояния: ошибки загрузки и удаления")]
        [SerializeField]
        private Text status = null!;

        [Tooltip("Листалка страниц")]
        [SerializeField]
        private SaveSlotPager pager = null!;

        [Tooltip("Кнопка «Назад»")]
        [SerializeField]
        private Button back = null!;

        private readonly List<SaveSlotInfo> _slots = new List<SaveSlotInfo>();
        private ConfirmDialog _confirm = null!;
        private int _pageSize;
        private bool _fromGame;
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

        /// <summary>Показывает список сохранений.</summary>
        /// <param name="fromGame">Пришли из игры, а не из стартового меню.</param>
        public void Show(bool fromGame)
        {
            _fromGame = fromGame;
            _page = 0;
            _slots.Clear();
            _slots.AddRange(Host.Saves.ListSlots());
            SortSlots(_slots);
            _pages = PageCount();
            status.text = string.Empty;
            Host.Router.Show(Panel);
            Rebuild();
            Canvas.ForceUpdateCanvases();
            SaveRowLayout.Apply(rows, _pageSize);
        }

        /// <summary>Esc закрывает подтверждение удаления, иначе возвращает назад.</summary>
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

        private static void AddPageButton(Button? button, Action action)
        {
            if (button != null)
            {
                button.onClick.AddListener(() => action());
            }
        }

        private void OnResized()
        {
            if (Panel.activeInHierarchy)
            {
                SaveRowLayout.Apply(rows, _pageSize);
            }
        }

        private static void SortSlots(List<SaveSlotInfo> slots)
        {
            slots.Sort((left, right) =>
            {
                var leftNumber = SlotNumber(left.Slot);
                var rightNumber = SlotNumber(right.Slot);
                if (leftNumber != rightNumber)
                {
                    return leftNumber.CompareTo(rightNumber);
                }

                return string.CompareOrdinal(left.Slot, right.Slot);
            });
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

        private int PageCount() => Mathf.Max(1, Mathf.CeilToInt(_slots.Count / (float)_pageSize));

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
            var count = Mathf.Clamp(_slots.Count - start, 0, _pageSize);
            for (var index = 0; index < count; index++)
            {
                AddRow(_slots[start + index]);
            }

            empty.gameObject.SetActive(_slots.Count == 0);
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

        private void AddRow(SaveSlotInfo info)
        {
            var row = Instantiate(rowTemplate, rows.Content);
            row.gameObject.name = "LoadSlot_" + info.Slot;
            row.gameObject.SetActive(true);
            row.Bind(Host.Texts, info.Slot, SlotNumber(info.Slot), info, isNew: false);
            var slot = info.Slot;
            var loadButton = row.Load;
            if (loadButton != null)
            {
                loadButton.onClick.AddListener(() => LoadSlot(slot));
            }

            var deleteButton = row.Delete;
            if (deleteButton != null)
            {
                deleteButton.onClick.AddListener(() => ConfirmDelete(slot));
            }
        }

        private void ConfirmDelete(string slot)
        {
            var title = slot;
            foreach (var item in _slots)
            {
                if (item.Slot == slot)
                {
                    title = string.IsNullOrWhiteSpace(item.Title) ? slot : item.Title;
                    break;
                }
            }

            _confirm.Show(
                UiText.Get(Host.Texts, UiTextKey.Load_Delete_Title),
                UiText.Format(Host.Texts, UiTextKey.Load_Delete_Question, title),
                () => DeleteSave(slot));
        }

        private void DeleteSave(string slot)
        {
            try
            {
                Host.Saves.Delete(slot);
            }
            catch (Exception error)
            {
                status.text = UiText.Format(Host.Texts, UiTextKey.Load_Delete_Error, error.Message);
                return;
            }

            _slots.Clear();
            _slots.AddRange(Host.Saves.ListSlots());
            SortSlots(_slots);
            _pages = PageCount();
            if (_page >= _pages)
            {
                _page = _pages - 1;
            }

            Rebuild();
            status.text = UiText.Get(Host.Texts, UiTextKey.Load_Deleted);
        }

        private void LoadSlot(string slot)
        {
            try
            {
                Host.Session = Host.Sessions.Load(slot);
                Host.ShowGame();
            }
            catch (Exception error)
            {
                status.text = UiText.Format(Host.Texts, UiTextKey.Load_Load_Error, error.Message);
            }
        }

        private void Back()
        {
            if (_fromGame && Host.Session != null)
            {
                Host.ShowGame();
            }
            else
            {
                Host.ShowMainMenu();
            }
        }
    }
}
