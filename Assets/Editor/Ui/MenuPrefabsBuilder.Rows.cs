#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsBuilder"/>: строки сохранений и листалка страниц.</summary>
    public static partial class MenuPrefabsBuilder
    {
        private static ScrollArea BuildRows(UiFactory ui, Transform parent, float bottomInset)
        {
            var rows = CreateRect(parent, "Rows");
            rows.anchorMin = Vector2.zero;
            rows.anchorMax = Vector2.one;
            rows.pivot = new Vector2(0.5f, 0.5f);
            rows.offsetMin = new Vector2(MenuScreenMetrics.ListPadding, bottomInset);
            rows.offsetMax = new Vector2(-MenuScreenMetrics.ListPadding, -MenuScreenMetrics.ListTopInset);

            var area = rows.gameObject.AddComponent<ScrollArea>();
            area.Configure(MenuScreenMetrics.ScrollbarWidth);
            var layout = area.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = MenuScreenMetrics.RowGap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = area.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (area.Bar != null)
            {
                // AutoHide включает полосу сам, когда контент выше области.
                area.Bar.gameObject.SetActive(false);
            }

            return area;
        }

        private static SaveSlotRow BuildRowTemplate(
            UiFactory ui,
            IGameTexts texts,
            Transform parent,
            bool forSaveScreen)
        {
            var root = CreateRect(parent, "RowTemplate");
            root.gameObject.SetActive(false);
            var background = root.gameObject.AddComponent<Image>();
            background.color = MenuPalette.Panel;
            background.raycastTarget = true;
            var element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = MenuScreenMetrics.RowHeight;
            element.minHeight = MenuScreenMetrics.RowHeight;
            element.flexibleHeight = 0f;

            var buttonsWidth = forSaveScreen
                ? MenuScreenMetrics.RowSaveButtonWidth
                : MenuScreenMetrics.RowButtonWidth;
            var textRight = MenuScreenMetrics.RowButtonInset + buttonsWidth + 16f;

            var thumbnail = CreateImage(root, "Thumbnail", MenuPalette.Cell);
            Place(
                (RectTransform)thumbnail.transform,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(MenuScreenMetrics.RowThumbnailInset, 0f),
                new Vector2(MenuScreenMetrics.RowThumbnailWidth, MenuScreenMetrics.RowThumbnailHeight));

            var title = CreateLabel(
                root,
                "Title",
                string.Empty,
                MenuScreenMetrics.RowTitleSize,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            StretchTopBand(
                (RectTransform)title.transform,
                MenuScreenMetrics.RowTextLeft,
                MenuScreenMetrics.RowTextTop,
                textRight,
                MenuScreenMetrics.RowTitleHeight);

            var lineTop = MenuScreenMetrics.RowTextTop + MenuScreenMetrics.RowTitleHeight;
            var details = CreateLabel(
                root,
                "Details",
                string.Empty,
                MenuScreenMetrics.RowLineSize,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            StretchTopBand(
                (RectTransform)details.transform,
                MenuScreenMetrics.RowTextLeft,
                lineTop,
                textRight,
                MenuScreenMetrics.RowLineHeight);

            var note = CreateLabel(
                root,
                "Note",
                string.Empty,
                MenuScreenMetrics.RowLineSize,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            StretchTopBand(
                (RectTransform)note.transform,
                MenuScreenMetrics.RowTextLeft,
                lineTop,
                textRight,
                MenuScreenMetrics.RowLineHeight);

            var day = CreateLabel(
                root,
                "Day",
                string.Empty,
                MenuScreenMetrics.RowLineSize,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            StretchTopBand(
                (RectTransform)day.transform,
                MenuScreenMetrics.RowTextLeft,
                lineTop + MenuScreenMetrics.RowLineHeight,
                textRight,
                MenuScreenMetrics.RowLineHeight);

            var row = root.gameObject.AddComponent<SaveSlotRow>();
            SetObject(row, "title", title);
            SetObject(row, "details", details);
            SetObject(row, "day", day);
            SetObject(row, "note", note);
            SetObject(row, "thumbnail", thumbnail);

            if (forSaveScreen)
            {
                // Поле названия живёт в свободной карточке: у остальных карточек оно скрыто.
                var nameField = ui.CreateInputField(
                    root,
                    UiText.Get(texts, UiTextKey.SaveGame_Name_Placeholder),
                    string.Empty,
                    32,
                    "SaveNameInput");
                StyleInputField(nameField);
                StretchCenterBand(
                    (RectTransform)nameField.transform,
                    MenuScreenMetrics.RowTextLeft,
                    textRight,
                    MenuScreenMetrics.RowNameHeight);
                var hints = nameField.GetComponentsInChildren<Text>(true);
                if (hints.Length > 1)
                {
                    Localize(hints[1], UiTextKey.SaveGame_Name_Placeholder);
                }

                SetObject(row, "nameField", nameField);

                var save = CreateLocalizedButton(ui, texts, root, UiTextKey.SaveGame_Save, ActionColor);
                Place(
                    (RectTransform)save.transform,
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(1f, 0.5f),
                    new Vector2(-MenuScreenMetrics.RowButtonInset, 0f),
                    new Vector2(
                        MenuScreenMetrics.RowSaveButtonWidth,
                        MenuScreenMetrics.RowSaveButtonHeight));
                SetObject(row, "saveButton", save);
                return row;
            }

            var halfStep = (MenuScreenMetrics.RowButtonGap / 2f) + (MenuScreenMetrics.RowButtonHeight / 2f);
            var load = CreateLocalizedButton(ui, texts, root, UiTextKey.Load_Load, ActionColor);
            Place(
                (RectTransform)load.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-MenuScreenMetrics.RowButtonInset, halfStep),
                new Vector2(MenuScreenMetrics.RowButtonWidth, MenuScreenMetrics.RowButtonHeight));
            var delete = CreateLocalizedButton(ui, texts, root, UiTextKey.Common_Delete, DangerColor);
            Place(
                (RectTransform)delete.transform,
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f),
                new Vector2(-MenuScreenMetrics.RowButtonInset, -halfStep),
                new Vector2(MenuScreenMetrics.RowButtonWidth, MenuScreenMetrics.RowButtonHeight));
            SetObject(row, "loadButton", load);
            SetObject(row, "deleteButton", delete);
            return row;
        }

        private static SaveSlotPager BuildPager(UiFactory ui, Transform parent)
        {
            var root = CreateRect(parent, "SlotPager");
            Place(
                root,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, MenuScreenMetrics.PagerBottomInset),
                new Vector2(MenuScreenMetrics.PagerWidth, MenuScreenMetrics.PagerHeight));

            var first = CreatePagerArrow(ui, root, "FirstPage", "<<", 0f, anchorLeft: true);
            var previous = CreatePagerArrow(
                ui,
                root,
                "PrevPage",
                "<",
                MenuScreenMetrics.PagerArrowInset,
                anchorLeft: true);
            var next = CreatePagerArrow(
                ui,
                root,
                "NextPage",
                ">",
                MenuScreenMetrics.PagerArrowInset,
                anchorLeft: false);
            var last = CreatePagerArrow(ui, root, "LastPage", ">>", 0f, anchorLeft: false);

            var label = CreateLabel(root, "PageLabel", string.Empty, 22, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Place(
                (RectTransform)label.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                new Vector2(MenuScreenMetrics.PagerLabelWidth, MenuScreenMetrics.PagerArrowHeight));

            var pager = root.gameObject.AddComponent<SaveSlotPager>();
            SetObject(pager, "firstPage", first);
            SetObject(pager, "previousPage", previous);
            SetObject(pager, "pageLabel", label);
            SetObject(pager, "nextPage", next);
            SetObject(pager, "lastPage", last);
            return pager;
        }

        private static Button CreatePagerArrow(
            UiFactory ui,
            RectTransform parent,
            string name,
            string caption,
            float offset,
            bool anchorLeft)
        {
            var button = CreateSymbolButton(ui, parent, name, caption);
            var anchorX = anchorLeft ? 0f : 1f;
            Place(
                (RectTransform)button.transform,
                new Vector2(anchorX, 0.5f),
                new Vector2(anchorX, 0.5f),
                new Vector2(anchorX, 0.5f),
                new Vector2(anchorLeft ? offset : -offset, 0f),
                new Vector2(MenuScreenMetrics.PagerArrowWidth, MenuScreenMetrics.PagerArrowHeight));
            return button;
        }
    }
}
