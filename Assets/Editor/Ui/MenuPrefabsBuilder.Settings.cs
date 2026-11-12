#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsBuilder"/>: экраны настроек и настройки клавиш.</summary>
    public static partial class MenuPrefabsBuilder
    {
        private static GameObject BuildSettingsScreen(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("Settings");
            var backdrop = AddBackdrop(root.transform, ScreenBackdropIds.Base);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);
            var column = CreateCenterColumn(content, SettingsMetrics.ColumnWidth);

            var rows = CreateRect(column, "Rows");
            rows.anchorMin = Vector2.zero;
            rows.anchorMax = Vector2.one;
            rows.pivot = new Vector2(0.5f, 0.5f);
            rows.offsetMin = new Vector2(SettingsMetrics.ListInset, SettingsMetrics.ListBottom);
            rows.offsetMax = new Vector2(-SettingsMetrics.ListInset, -SettingsMetrics.ListTop);
            var area = rows.gameObject.AddComponent<ScrollArea>();
            area.Configure(SettingsMetrics.ScrollbarWidth);
            var list = area.Content;
            var layout = list.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = SettingsMetrics.RowGap;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = list.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (area.Bar != null)
            {
                area.Bar.gameObject.SetActive(false);
            }

            var resolutionRow = BuildSettingRow(list, "ResolutionRow");
            var resolutionLabel = CreateLabel(
                resolutionRow,
                "ResolutionLabel",
                string.Empty,
                SettingsMetrics.LabelSize,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            Localize(resolutionLabel, UiTextKey.Settings_Resolution);
            resolutionLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var dropdownRoot = CreateRect(resolutionRow, "Resolution");
            var dropdownElement = dropdownRoot.gameObject.AddComponent<LayoutElement>();
            dropdownElement.preferredWidth = SettingsMetrics.ControlWidth;
            dropdownElement.minWidth = SettingsMetrics.ControlWidth;
            dropdownElement.flexibleWidth = 0f;
            dropdownElement.preferredHeight = SettingsMetrics.DropdownHeight;
            dropdownElement.minHeight = SettingsMetrics.DropdownHeight;
            var dropdown = BuildResolutionDropdown(ui, column, dropdownRoot);

            var volumeRow = BuildSettingRow(list, "VolumeRow");
            AddSettingLabel(volumeRow, UiTextKey.Settings_Volume);
            var volume = BuildSlider(ui, volumeRow, "Volume");
            var volumeValue = AddSettingValue(volumeRow, "VolumeValue");
            volume.value = 100f;

            var keysRow = BuildSettingRow(list, "KeysRow");
            var keys = CreateLocalizedButton(ui, texts, keysRow, UiTextKey.Settings_Keys, UiFactory.ButtonColor, action: true);
            var keysElement = keys.GetComponent<LayoutElement>();
            keysElement.preferredWidth = SettingsMetrics.KeysButtonWidth;
            keysElement.minWidth = SettingsMetrics.KeysButtonWidth;
            keysElement.flexibleWidth = 0f;
            keysElement.preferredHeight = SettingsMetrics.KeysButtonHeight;
            keysElement.minHeight = SettingsMetrics.KeysButtonHeight;
            var keysLabel = keys.GetComponentInChildren<Text>();
            if (keysLabel != null)
            {
                keysLabel.fontSize = SettingsMetrics.KeysButtonFontSize;
            }

            var sensitivityRow = BuildSettingRow(list, "SensitivityRow");
            AddSettingLabel(sensitivityRow, UiTextKey.Settings_Mouse_Sensitivity);
            var sensitivity = BuildSlider(ui, sensitivityRow, "Sensitivity");
            var sensitivityValue = AddSettingValue(sensitivityRow, "SensitivityValue");
            sensitivity.value = 50f;

            var back = CreateBackButton(ui, texts, column);

            var screen = root.AddComponent<SettingsScreen>();
            SetObject(screen, "resolution", dropdown);
            SetObject(screen, "volume", volume);
            SetObject(screen, "volumeValue", volumeValue);
            SetObject(screen, "sensitivity", sensitivity);
            SetObject(screen, "sensitivityValue", sensitivityValue);
            SetObject(screen, "keys", keys);
            SetObject(screen, "back", back);
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", column);
            return root;
        }

        private static GameObject BuildKeyBindingsScreen(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("KeyBindings");
            var backdrop = AddBackdrop(root.transform, ScreenBackdropIds.Base);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);
            var column = CreateCenterColumn(content, SettingsMetrics.ColumnWidth);

            var title = CreateLabel(
                column,
                "SectionTitle",
                string.Empty,
                SettingsMetrics.KeysTitleSize,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            Localize(title, UiTextKey.Keys_Section_Game);
            Place(
                (RectTransform)title.transform,
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(0f, 1f),
                new Vector2(SettingsMetrics.ListInset, -SettingsMetrics.KeysTitleTop),
                new Vector2(600f, SettingsMetrics.KeysTitleHeight));

            var rows = CreateRect(column, "Rows");
            rows.anchorMin = Vector2.zero;
            rows.anchorMax = Vector2.one;
            rows.pivot = new Vector2(0.5f, 0.5f);
            rows.offsetMin = new Vector2(SettingsMetrics.ListInset, SettingsMetrics.KeysListBottom);
            rows.offsetMax = new Vector2(-SettingsMetrics.ListInset, -SettingsMetrics.KeysListTop);
            var area = rows.gameObject.AddComponent<ScrollArea>();
            area.Configure(SettingsMetrics.ScrollbarWidth);
            var layout = area.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = SettingsMetrics.KeyRowGap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            var fitter = area.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (area.Bar != null)
            {
                area.Bar.gameObject.SetActive(false);
            }

            var template = BuildKeyRow(ui, area.Content);

            var save = CreateLocalizedButton(ui, texts, column, UiTextKey.Keys_Save, UiFactory.ButtonColor, action: true);
            Place(
                (RectTransform)save.transform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(
                    -(MenuScreenMetrics.BackInset + MenuScreenMetrics.BackWidth + SettingsMetrics.BottomGap),
                    SettingsMetrics.BottomInset),
                new Vector2(SettingsMetrics.SaveWidth, SettingsMetrics.SaveHeight));

            var back = CreateBackButton(ui, texts, column);

            var screen = root.AddComponent<KeyBindingsScreen>();
            SetObject(screen, "rows", area);
            SetObject(screen, "rowTemplate", template);
            SetObject(screen, "save", save);
            SetObject(screen, "back", back);
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", column);
            return root;
        }

        private static RectTransform CreateCenterColumn(Transform parent, float width)
        {
            var column = CreateRect(parent, "ContentColumn");
            column.anchorMin = new Vector2(0.5f, 0f);
            column.anchorMax = new Vector2(0.5f, 1f);
            column.pivot = new Vector2(0.5f, 0.5f);
            column.sizeDelta = new Vector2(width, 0f);
            column.anchoredPosition = Vector2.zero;
            return column;
        }

        private static ResolutionDropdown BuildResolutionDropdown(
            UiFactory ui,
            Transform content,
            RectTransform root)
        {
            var toggle = ui.CreateButton(root, "1920×1080");
            toggle.gameObject.name = "ResolutionToggle";
            var toggleRect = (RectTransform)toggle.transform;
            toggleRect.anchorMin = Vector2.zero;
            toggleRect.anchorMax = Vector2.one;
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;
            var label = toggle.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.alignment = TextAnchor.MiddleCenter;
            }

            var options = CreateRect(content, "ResolutionOptions");
            options.anchorMin = new Vector2(0f, 1f);
            options.anchorMax = new Vector2(0f, 1f);
            options.pivot = new Vector2(0f, 1f);
            options.sizeDelta = new Vector2(SettingsMetrics.ControlWidth, 0f);
            options.anchoredPosition = Vector2.zero;
            options.gameObject.SetActive(false);
            var background = options.gameObject.AddComponent<Image>();
            PanelArtAssets.ApplyPanel(background);
            var layout = options.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                (int)SettingsMetrics.DropdownInset,
                (int)SettingsMetrics.DropdownInset,
                (int)SettingsMetrics.DropdownInset,
                (int)SettingsMetrics.DropdownInset);
            layout.spacing = SettingsMetrics.DropdownRowGap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            options.gameObject.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            var option = ui.CreateButton(options, "1920×1080");
            option.gameObject.name = "ResolutionOption";
            var element = option.GetComponent<LayoutElement>();
            element.preferredHeight = SettingsMetrics.DropdownRowHeight;
            element.minHeight = SettingsMetrics.DropdownRowHeight;
            option.gameObject.SetActive(false);

            var dropdown = root.gameObject.AddComponent<ResolutionDropdown>();
            SetObject(dropdown, "toggle", toggle);
            SetObject(dropdown, "value", label);
            SetObject(dropdown, "options", options);
            SetObject(dropdown, "optionTemplate", option);
            return dropdown;
        }

        private static RectTransform BuildSettingRow(Transform parent, string name)
        {
            var row = CreateRect(parent, name);
            var background = row.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            PanelArtAssets.ApplyPanel(background);
            var element = row.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = SettingsMetrics.RowHeight;
            element.minHeight = SettingsMetrics.RowHeight;
            element.flexibleHeight = 0f;
            var layout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            return row;
        }

        private static void AddSettingLabel(Transform row, UiTextKey key)
        {
            var label = CreateLabel(
                row,
                "Label",
                string.Empty,
                SettingsMetrics.LabelSize,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            Localize(label, key);
            label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
        }

        private static Text AddSettingValue(Transform row, string name)
        {
            var value = CreateLabel(
                row,
                name,
                "0",
                SettingsMetrics.ValueSize,
                TextAnchor.MiddleRight,
                UiFactory.MutedTextColor);
            var element = value.gameObject.AddComponent<LayoutElement>();
            element.preferredWidth = SettingsMetrics.ValueWidth;
            element.minWidth = SettingsMetrics.ValueWidth;
            element.flexibleWidth = 0f;
            return value;
        }

        private static Slider BuildSlider(UiFactory ui, Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Slider));
            go.transform.SetParent(parent, false);
            var hitArea = go.GetComponent<Image>();
            hitArea.color = new Color(0f, 0f, 0f, 0f);
            hitArea.raycastTarget = true;
            var element = go.AddComponent<LayoutElement>();
            element.preferredWidth = SettingsMetrics.ControlWidth;
            element.minWidth = SettingsMetrics.ControlWidth;
            element.flexibleWidth = 0f;
            element.preferredHeight = SettingsMetrics.SliderHeight;
            element.minHeight = SettingsMetrics.SliderHeight;

            var background = ui.CreateImage("Background", go.transform, MenuPalette.Track);
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -SettingsMetrics.SliderTrackHeight / 2f);
            backgroundRect.offsetMax = new Vector2(0f, SettingsMetrics.SliderTrackHeight / 2f);

            var fillArea = CreateRect(go.transform, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(SettingsMetrics.SliderHandleWidth / 2f, -SettingsMetrics.SliderTrackHeight / 2f);
            fillArea.offsetMax = new Vector2(-SettingsMetrics.SliderHandleWidth / 2f, SettingsMetrics.SliderTrackHeight / 2f);
            var fill = ui.CreateImage("Fill", fillArea, MenuPalette.SliderFill);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = CreateRect(go.transform, "Handle Slide Area");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(SettingsMetrics.SliderHandleWidth / 2f, 0f);
            handleArea.offsetMax = new Vector2(-SettingsMetrics.SliderHandleWidth / 2f, 0f);
            var handle = ui.CreateImage("Handle", handleArea, MenuPalette.SliderHandle);
            handle.rectTransform.sizeDelta = new Vector2(SettingsMetrics.SliderHandleWidth, -8f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            slider.minValue = 0f;
            slider.maxValue = 100f;
            return slider;
        }

        private static KeyBindRow BuildKeyRow(UiFactory ui, Transform parent)
        {
            var root = CreateRect(parent, "RowTemplate");
            root.gameObject.SetActive(false);
            var background = root.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            PanelArtAssets.ApplyPanel(background);
            var element = root.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = SettingsMetrics.KeyRowHeight;
            element.minHeight = SettingsMetrics.KeyRowHeight;
            element.flexibleHeight = 0f;
            var layout = root.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 0, 0);
            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var actionLabel = CreateLabel(
                root,
                "ActionLabel",
                string.Empty,
                SettingsMetrics.KeyRowLabelSize,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            var nameElement = actionLabel.gameObject.AddComponent<LayoutElement>();
            nameElement.preferredWidth = SettingsMetrics.KeyNameWidth;
            nameElement.minWidth = SettingsMetrics.KeyNameWidth;
            nameElement.flexibleWidth = 0f;

            var star = CreateLabel(
                root,
                "ChangedStar",
                "*",
                SettingsMetrics.StarSize,
                TextAnchor.MiddleCenter,
                MenuPalette.Changed);
            var starElement = star.gameObject.AddComponent<LayoutElement>();
            starElement.preferredWidth = SettingsMetrics.StarWidth;
            starElement.minWidth = SettingsMetrics.StarWidth;
            starElement.flexibleWidth = 0f;
            star.gameObject.SetActive(false);

            // Разделитель забирает свободное место: появление звёздочки не двигает кнопку клавиши.
            var spacer = CreateRect(root, "Spacer");
            spacer.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var key = CreateSymbolButton(ui, root, "KeyButton", "F5");
            var keyElement = key.GetComponent<LayoutElement>();
            keyElement.preferredWidth = SettingsMetrics.KeyButtonWidth;
            keyElement.minWidth = SettingsMetrics.KeyButtonWidth;
            keyElement.flexibleWidth = 0f;
            keyElement.preferredHeight = SettingsMetrics.KeyButtonHeight;
            keyElement.minHeight = SettingsMetrics.KeyButtonHeight;
            var keyLabel = key.GetComponentInChildren<Text>();
            if (keyLabel != null)
            {
                keyLabel.fontSize = SettingsMetrics.KeyRowKeySize;
            }

            var row = root.gameObject.AddComponent<KeyBindRow>();
            SetObject(row, "actionLabel", actionLabel);
            SetObject(row, "keyButton", key);
            SetObject(row, "keyLabel", keyLabel);
            SetObject(row, "star", star.gameObject);
            return row;
        }
    }
}
