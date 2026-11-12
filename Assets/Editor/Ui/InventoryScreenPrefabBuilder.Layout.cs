#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="InventoryScreenPrefabBuilder"/>: разметка окна по прототипу.</summary>
    public static partial class InventoryScreenPrefabBuilder
    {
        /// <summary>Собранные части панели персонажа.</summary>
        private sealed class CharacterParts
        {
            public Image Figure = null!;
            public RectTransform ArtifactRow = null!;
            public Image ArtifactTemplate = null!;
            public Text QuickTitle = null!;
            public Text QuickHint = null!;
            public RectTransform QuickRow = null!;
            public Image QuickTemplate = null!;
            public RectTransform Injuries = null!;
            public Text Nickname = null!;
            public Text Level = null!;
            public Button Close = null!;
            public Button Talents = null!;
            public Image[] SlotZones = Array.Empty<Image>();
            public RectTransform[] SlotBodies = Array.Empty<RectTransform>();
            public Text[] StatValues = Array.Empty<Text>();
            public Image StatsPanel = null!;
            public RectTransform StatsDefenseRows = null!;
            public RectTransform StatsRowTemplate = null!;
            public Button StatsEye = null!;
            public Image StatsEyeIcon = null!;
        }

        /// <summary>Собранные части окна инвентаря.</summary>
        private sealed class InventoryParts
        {
            public Image Frame = null!;
            public RectTransform GridArea = null!;
            public ScrollArea GridScroll = null!;
            public Text Money = null!;
            public Text Weight = null!;
            public Text Overflow = null!;
            public Button[] Filters = Array.Empty<Button>();
        }

        private static CharacterParts BuildCharacterPanel(UiFactory ui, IGameTexts texts, Transform parent)
        {
            var parts = new CharacterParts();
            var panel = CreateImage(parent, "CharacterPanel", CharacterPalette.Frame);
            panel.raycastTarget = true;
            PanelArtAssets.Apply(panel, CharacterPanelAlpha);
            PlaceBand((RectTransform)panel.transform, 0f, ReferenceWidth / ReferenceTotal, 16f, 16f);

            var composition = CreateRect(panel.transform, "Composition");
            Stretch(composition);
            var fitter = composition.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = LayoutWidth / ReferenceHeight;

            parts.Close = CreateLocalizedButton(ui, texts, composition, UiTextKey.Common_Close, UiFactory.ButtonColor);
            parts.Close.gameObject.name = "InventoryClose";
            PlaceFraction((RectTransform)parts.Close.transform, 78f, 18f, 84f, 58f);

            parts.Talents = CreateLocalizedButton(ui, texts, composition, UiTextKey.Talent_Open, UiFactory.ButtonColor);
            parts.Talents.gameObject.name = "InventoryTalents";
            PlaceFraction((RectTransform)parts.Talents.transform, TalentsLeft, TalentsTop, TalentsWidth, TalentsHeight);

            parts.Nickname = CreateLabel(composition, "Nickname", string.Empty, 18, TextAnchor.MiddleRight, UiFactory.TextColor);
            AddShadow(parts.Nickname);
            PlaceFraction(parts.Nickname.rectTransform, 605f + SlotsShift, 20f, 267f, 34f);

            parts.Level = CreateLabel(composition, "Level", string.Empty, 14, TextAnchor.MiddleRight, UiFactory.MutedTextColor);
            AddShadow(parts.Level);
            PlaceFraction(parts.Level.rectTransform, 605f + SlotsShift, 54f, 267f, 26f);

            parts.SlotZones = new Image[6];
            parts.SlotBodies = new RectTransform[6];

            Slot(parts, 0, ui, texts, composition, "SlotMain", UiTextKey.Character_Slot_Main, 78f + SlotsShift, 662f, 485f, 198f);
            Slot(parts, 1, ui, texts, composition, "SlotSecondary", UiTextKey.Character_Slot_Secondary, 604f + SlotsShift, 662f, 268f, 198f);
            Slot(parts, 2, ui, texts, composition, "SlotHelmet", UiTextKey.Character_Slot_Helmet, 78f + SlotsShift, 116f, 264f, 198f);
            Slot(parts, 3, ui, texts, composition, "SlotArmor", UiTextKey.Character_Slot_Armor, 78f + SlotsShift, 332f, 264f, 214f);
            Slot(parts, 4, ui, texts, composition, "SlotBackpack", UiTextKey.Character_Slot_Backpack, 606f + SlotsShift, 119f, 264f, 198f);
            Slot(parts, 5, ui, texts, composition, "SlotBelt", UiTextKey.Character_Slot_Belt, 606f + SlotsShift, 346f, 264f, 198f);

            parts.Figure = CreateImage(composition, "Figure", CharacterPalette.FigureArea);
            parts.Figure.preserveAspect = true;
            PlaceFraction((RectTransform)parts.Figure.transform, 383f + SlotsShift, 119f, 180f, 427f);

            parts.ArtifactRow = CreateRect(composition, "ArtifactSlots");
            PlaceFraction(parts.ArtifactRow, 83f + SlotsShift, 571f, 602f, 72f);
            var artifacts = parts.ArtifactRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            artifacts.spacing = 12f;
            artifacts.childAlignment = TextAnchor.MiddleLeft;
            artifacts.childControlWidth = true;
            artifacts.childControlHeight = true;
            artifacts.childForceExpandWidth = false;
            artifacts.childForceExpandHeight = false;

            parts.ArtifactTemplate = CreateImage(parts.ArtifactRow, "ArtifactSlotTemplate", CharacterPalette.Slot);
            parts.ArtifactTemplate.raycastTarget = true;
            parts.ArtifactTemplate.gameObject.AddComponent<ItemDropZone>();
            ApplySlotFrame(parts.ArtifactTemplate, SlotFrameSmall);
            var templateElement = parts.ArtifactTemplate.gameObject.AddComponent<LayoutElement>();
            templateElement.preferredWidth = 113f;
            templateElement.preferredHeight = 72f;
            templateElement.flexibleWidth = 0f;
            parts.ArtifactTemplate.gameObject.SetActive(false);

            BuildQuickSlots(composition, parts);

            BuildStats(ui, texts, composition, parts);
            BuildStatsPanel(ui, composition, parts);

            parts.Injuries = CreateRect(composition, "StatInjuries");
            PlaceFraction(parts.Injuries, 78f + SlotsShift, 1000f, 794f, 24f);
            return parts;
        }

        private static void Slot(
            CharacterParts parts,
            int index,
            UiFactory ui,
            IGameTexts texts,
            Transform parent,
            string name,
            UiTextKey captionKey,
            float x,
            float y,
            float width,
            float height)
        {
            var zone = CreateImage(parent, name, CharacterPalette.Slot);
            zone.raycastTarget = true;
            zone.gameObject.AddComponent<ItemDropZone>();
            ApplySlotFrame(zone, SlotFrameLarge);
            PlaceFraction((RectTransform)zone.transform, x, y, width, height);

            var caption = CreateLabel(zone.transform, "Caption", string.Empty, 12, TextAnchor.UpperLeft, UiFactory.MutedTextColor);
            Localize(caption, captionKey);
            AddShadow(caption);
            Place(caption.rectTransform,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -4f),
                new Vector2(-16f, 18f));

            var body = CreateRect(zone.transform, "Body");
            body.anchorMin = Vector2.zero;
            body.anchorMax = Vector2.one;
            body.offsetMin = new Vector2(6f, 6f);
            body.offsetMax = new Vector2(-6f, -22f);

            parts.SlotZones[index] = zone;
            parts.SlotBodies[index] = body;
        }

        /// <summary>Ставит слоту рамку из текстуры ржавой панели, как у диалогов.</summary>
        /// <param name="image">Плашка слота.</param>
        /// <param name="pixelsPerUnitMultiplier">Во сколько раз уменьшить кант рамки.</param>
        private static void ApplySlotFrame(Image image, float pixelsPerUnitMultiplier)
        {
            image.sprite = PanelArtAssets.Load();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = pixelsPerUnitMultiplier;
            image.color = Color.white;
        }

        private static void BuildQuickSlots(Transform parent, CharacterParts parts)        {
            parts.QuickTitle = CreateLabel(parent, "QuickTitle", string.Empty, 15, TextAnchor.MiddleLeft, UiFactory.TextColor);
            Localize(parts.QuickTitle, UiTextKey.Character_Quick_Title);
            AddShadow(parts.QuickTitle);
            PlaceFraction(parts.QuickTitle.rectTransform, QuickLeft, QuickTop, QuickWidth, 22f);

            parts.QuickHint = CreateLabel(parent, "QuickHint", string.Empty, 12, TextAnchor.MiddleLeft, UiFactory.MutedTextColor);
            Localize(parts.QuickHint, UiTextKey.Character_Quick_Hint);
            AddShadow(parts.QuickHint);
            PlaceFraction(parts.QuickHint.rectTransform, QuickLeft, QuickTop + 24f, QuickWidth, 18f);

            parts.QuickRow = CreateRect(parent, "QuickSlots");
            var rowRect = parts.QuickRow;
            rowRect.anchorMin = new Vector2(QuickLeft / LayoutWidth, 1f - ((QuickTop + 50f) / ReferenceHeight));
            rowRect.anchorMax = rowRect.anchorMin;
            rowRect.pivot = new Vector2(0f, 1f);
            rowRect.anchoredPosition = Vector2.zero;
            rowRect.sizeDelta = new Vector2(QuickWidth, QuickSlotHeight);
            var layout = parts.QuickRow.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = QuickGap;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = parts.QuickRow.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            parts.QuickTemplate = CreateImage(parts.QuickRow, "QuickSlotTemplate", CharacterPalette.Slot);
            parts.QuickTemplate.raycastTarget = true;
            parts.QuickTemplate.gameObject.AddComponent<ItemDropZone>();
            ApplySlotFrame(parts.QuickTemplate, SlotFrameSmall);
            var element = parts.QuickTemplate.gameObject.AddComponent<LayoutElement>();
            element.preferredHeight = QuickSlotHeight;

            var icon = CreateImage(parts.QuickTemplate.transform, "Icon", Color.white);
            icon.preserveAspect = true;
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(8f, 0f);
            iconRect.sizeDelta = new Vector2(52f, 52f);

            var label = CreateLabel(parts.QuickTemplate.transform, "Label", string.Empty, 14, TextAnchor.MiddleLeft, UiFactory.TextColor);
            AddShadow(label);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 9;
            label.resizeTextMaxSize = 14;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            var labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(68f, 4f);
            labelRect.offsetMax = new Vector2(-8f, -4f);

            parts.QuickTemplate.gameObject.SetActive(false);
        }

        private static void BuildStats(UiFactory ui, IGameTexts texts, Transform parent, CharacterParts parts)
        {
            var container = CreateRect(parent, "Stats");
            PlaceFraction(container, 78f + SlotsShift, 930f, 794f, 44f);

            var rows = new[]
            {
                ("Health", "ui_stat_health", CharacterPalette.Health),
                ("Food", "ui_stat_food", CharacterPalette.Hunger),
                ("Water", "ui_stat_water", CharacterPalette.Thirst),
                ("Stamina", "ui_stat_stamina", CharacterPalette.Stamina)
            };

            parts.StatValues = new Text[rows.Length];
            for (var index = 0; index < rows.Length; index++)
            {
                var row = rows[index];
                var line = CreateRect(container, "Stat" + row.Item1 + "Row");
                line.anchorMin = new Vector2(0f, 1f);
                line.anchorMax = new Vector2(0f, 1f);
                line.pivot = new Vector2(0f, 1f);
                line.anchoredPosition = new Vector2(index * 198f, 0f);
                line.sizeDelta = new Vector2(190f, 28f);

                var icon = CreateImage(line, "Stat" + row.Item1 + "Icon", row.Item3);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/Icons/{row.Item2}.png");
                if (sprite != null)
                {
                    icon.sprite = sprite;
                    icon.color = Color.white;
                    icon.preserveAspect = true;
                }

                Place((RectTransform)icon.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(24f, 24f));

                var value = CreateLabel(line, "Stat" + row.Item1 + "Value", string.Empty, 20, TextAnchor.MiddleLeft, UiFactory.TextColor);
                AddShadow(value);
                value.rectTransform.anchorMin = Vector2.zero;
                value.rectTransform.anchorMax = Vector2.one;
                value.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                value.rectTransform.offsetMin = new Vector2(30f, 0f);
                value.rectTransform.offsetMax = Vector2.zero;

                parts.StatValues[index] = value;
            }
        }

        private static void BuildStatsPanel(UiFactory ui, Transform parent, CharacterParts parts)
        {
            parts.StatsPanel = CreateImage(parent, "StatsPanel", CharacterPalette.StatsPanel);
            parts.StatsPanel.raycastTarget = false;
            PanelArtAssets.Apply(parts.StatsPanel, StatsPanelAlpha);
            parts.StatsPanel.pixelsPerUnitMultiplier = StatsFrameScale;
            var rect = (RectTransform)parts.StatsPanel.transform;
            rect.anchorMin = new Vector2(StatsLeft / LayoutWidth, 1f - (StatsTop / ReferenceHeight));
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(StatsWidth, 0f);

            var layout = parts.StatsPanel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 18, 16);
            layout.spacing = 3f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = parts.StatsPanel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = CreateLabel(parts.StatsPanel.transform, "StatsTitle", string.Empty, 15, TextAnchor.MiddleLeft, CharacterPalette.StatsTitle);
            Localize(title, UiTextKey.Character_Stats_Defense);
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            var line = CreateImage(parts.StatsPanel.transform, "StatsLine", CharacterPalette.StatsLine);
            line.gameObject.AddComponent<LayoutElement>().preferredHeight = 1f;

            parts.StatsDefenseRows = BuildStatsRows(parts.StatsPanel.transform, "StatsDefenseRows");
            parts.StatsRowTemplate = BuildStatsRow(parent);
            BuildStatsEye(ui, parent, parts);
        }

        private static RectTransform BuildStatsRows(Transform parent, string name)
        {
            var rows = CreateRect(parent, name);
            var rowsLayout = rows.gameObject.AddComponent<VerticalLayoutGroup>();
            rowsLayout.spacing = 2f;
            rowsLayout.childAlignment = TextAnchor.UpperLeft;
            rowsLayout.childControlWidth = true;
            rowsLayout.childControlHeight = true;
            rowsLayout.childForceExpandWidth = true;
            rowsLayout.childForceExpandHeight = false;
            return rows;
        }

        private static RectTransform BuildStatsRow(Transform parent)
        {
            var row = CreateRect(parent, "StatsRowTemplate");
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 22f;

            var label = CreateLabel(row, "Label", string.Empty, 14, TextAnchor.MiddleLeft, UiFactory.MutedTextColor);
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceAnchor(label.rectTransform, 0f, 0f, 0.58f, 1f);

            var value = CreateLabel(row, "Value", string.Empty, 14, TextAnchor.MiddleRight, CharacterPalette.StatsValue);
            value.horizontalOverflow = HorizontalWrapMode.Overflow;
            value.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceAnchor(value.rectTransform, 0.58f, 0f, 1f, 1f);

            row.gameObject.SetActive(false);
            return row;
        }

        private static void BuildStatsEye(UiFactory ui, Transform parent, CharacterParts parts)
        {
            parts.StatsEye = ui.CreateButton(parent, string.Empty);
            parts.StatsEye.gameObject.name = "StatsEye";
            var label = parts.StatsEye.GetComponentInChildren<Text>(true);
            if (label != null)
            {
                UnityEngine.Object.DestroyImmediate(label.gameObject);
            }

            var element = parts.StatsEye.GetComponent<LayoutElement>();
            if (element != null)
            {
                UnityEngine.Object.DestroyImmediate(element);
            }

            parts.StatsEye.GetComponent<Image>().color = UiFactory.ButtonColor;
            PlaceFraction((RectTransform)parts.StatsEye.transform, EyeLeft, EyeTop, EyeSize, EyeSize);

            parts.StatsEyeIcon = CreateImage(parts.StatsEye.transform, "StatsEyeIcon", Color.white);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Icons/ui_icon_eye.png");
            if (sprite != null)
            {
                parts.StatsEyeIcon.sprite = sprite;
                parts.StatsEyeIcon.preserveAspect = true;
            }
            else
            {
                parts.StatsEyeIcon.color = CharacterPalette.StatsEyeMuted;
            }

            var iconRect = parts.StatsEyeIcon.rectTransform;
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = new Vector2(10f, 10f);
            iconRect.offsetMax = new Vector2(-10f, -10f);
        }

        private static void PlaceAnchor(RectTransform rect, float xMin, float yMin, float xMax, float yMax)
        {
            rect.anchorMin = new Vector2(xMin, yMin);
            rect.anchorMax = new Vector2(xMax, yMax);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static InventoryParts BuildInventoryPanel(UiFactory ui, IGameTexts texts, Transform parent)
        {
            var parts = new InventoryParts();
            var frame = CreateImage(parent, "InventoryFrame", CharacterPalette.Frame);
            frame.raycastTarget = true;
            PlaceBand((RectTransform)frame.transform, (ReferenceWidth + ReferenceGap) / ReferenceTotal, 1f, 16f, 16f);
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryPanelArt.TexturePath);
            if (sprite != null)
            {
                frame.sprite = sprite;
                frame.color = Color.white;
            }

            parts.GridArea = CreateRect(frame.transform, "GridArea");
            parts.GridArea.anchorMin = Vector2.zero;
            parts.GridArea.anchorMax = Vector2.one;
            parts.GridArea.offsetMin = new Vector2(16f, 96f);
            parts.GridArea.offsetMax = new Vector2(-16f, -96f);
            parts.GridScroll = parts.GridArea.gameObject.AddComponent<ScrollArea>();
            parts.GridScroll.Configure(18f);

            parts.Money = CreateLabel(frame.transform, "InventoryMoney", string.Empty, 24, TextAnchor.MiddleCenter, UiFactory.TextColor);
            var plate = parts.Money.rectTransform;
            plate.anchorMin = new Vector2(InventoryPanelArt.CurrencyPlate.xMin, InventoryPanelArt.CurrencyPlate.yMin);
            plate.anchorMax = new Vector2(InventoryPanelArt.CurrencyPlate.xMax, InventoryPanelArt.CurrencyPlate.yMax);
            plate.offsetMin = Vector2.zero;
            plate.offsetMax = Vector2.zero;
            InventoryPanelArt.DecorateMoney(parts.Money);

            parts.Weight = CreateLabel(frame.transform, "InventoryWeight", string.Empty, 20, TextAnchor.MiddleLeft, UiFactory.TextColor);
            Place(parts.Weight.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(20f, -20f), new Vector2(300f, 40f));

            parts.Overflow = CreateLabel(frame.transform, "Overflow", string.Empty, 13, TextAnchor.LowerCenter, UiFactory.ToastColor);            Place(parts.Overflow.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 68f), new Vector2(-24f, 26f));
            parts.Overflow.gameObject.SetActive(false);

            var filters = CreateRect(frame.transform, "Filters");
            Place(filters, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 8f), new Vector2(-14f, 56f));
            var row = filters.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.spacing = 4f;
            row.childAlignment = TextAnchor.MiddleCenter;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = true;
            row.childForceExpandHeight = true;

            var entries = new[]
            {
                ("All", UiTextKey.Inventory_Filter_All),
                ("Weapons", UiTextKey.Inventory_Filter_Weapons),
                ("Armor", UiTextKey.Inventory_Filter_Armor),
                ("Consumables", UiTextKey.Inventory_Filter_Consumables),
                ("Other", UiTextKey.Inventory_Filter_Other)
            };
            parts.Filters = new Button[entries.Length];
            for (var index = 0; index < entries.Length; index++)
            {
                var entry = entries[index];
                var button = CreateLocalizedButton(ui, texts, filters, entry.Item2, UiFactory.ButtonColor);
                button.gameObject.name = "InventoryFilter" + entry.Item1;
                var label = button.GetComponentInChildren<Text>();
                label.fontSize = 14;
                label.resizeTextForBestFit = true;
                label.resizeTextMinSize = 9;
                label.resizeTextMaxSize = 14;
                button.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
                parts.Filters[index] = button;
            }

            parts.Frame = frame;
            return parts;
        }
    }
}
