#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsScreenshot"/>: кадр окна «инвентарь».</summary>
    public static partial class MenuPrefabsScreenshot
    {
        private static void FillInventory(GameObject panel, IGameTexts texts, int pageSize)
        {
            Canvas.ForceUpdateCanvases();
            SetLabel(panel, "Nickname", "Сталкер");
            SetLabel(panel, "Level", UiText.Format(texts, UiTextKey.Common_Level_Cap, 3));
            SetLabel(panel, "InventoryMoney", UiText.Format(texts, UiTextKey.Common_Money, 1000));
            SetLabel(panel, "InventoryWeight", UiText.Format(texts, UiTextKey.Character_Weight, 12.4, 30));

            FillStat(panel, "Health", "100/100");
            FillStat(panel, "Food", "62/100");
            FillStat(panel, "Water", "48/100");
            FillStat(panel, "Stamina", "70/100");
            FillStats(panel, texts);

            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>("Assets/Art/SpriteCatalog.asset");
            var figure = FindDeep(panel.transform, "Figure")?.GetComponent<Image>();
            if (figure != null && catalog != null && catalog.TryGet("costumes/itm_armor_jacket", out var costume) && costume != null)
            {
                figure.sprite = costume;
                figure.color = Color.white;
            }

            FillSlot(panel, texts, "SlotMain", "wpn_ak74", "АК-74 · 30/30 · 100/100", 18);
            FillSlot(panel, texts, "SlotSecondary", "wpn_pm", "Пистолет Макарова · 8/8", 18);
            FillSlot(panel, texts, "SlotHelmet", "itm_helmet_light", "ССШ-68 · 25/25", 16);
            FillSlot(panel, texts, "SlotArmor", "itm_armor_plate", "Бронеплитник · 90/90", 16);
            FillSlot(panel, texts, "SlotBackpack", "itm_backpack_small", "Рюкзак · +10 кг", 16);
            FillSlot(panel, texts, "SlotBelt", "itm_belt_artifact", "Пояс · 5 слотов", 16);

            FillArtifacts(panel, texts);
            FillQuickSlots(panel, texts, catalog);
            FillGrid(panel, catalog);
        }

        private static void FillQuickSlots(GameObject panel, IGameTexts texts, SpriteCatalog? catalog)
        {
            var row = FindDeep(panel.transform, "QuickSlots");
            var template = row != null ? row.transform.Find("QuickSlotTemplate") as RectTransform : null;
            if (row == null || template == null)
            {
                return;
            }

            var dash = UiText.Get(texts, UiTextKey.Common_Dash);
            var samples = new[]
            {
                ("itm_medkit_army", "Армейская аптечка"),
                ("wpn_ak74", "АК-74 · 30/30 · 100/100"),
                (string.Empty, dash),
                (string.Empty, dash)
            };

            for (var index = 0; index < samples.Length; index++)
            {
                var sample = samples[index];
                var slot = UnityEngine.Object.Instantiate(template, row.transform);
                slot.name = "QuickSlot_" + index;
                slot.gameObject.SetActive(true);

                var label = slot.Find("Label")?.GetComponent<Text>();
                var icon = slot.Find("Icon")?.GetComponent<Image>();
                if (label != null)
                {
                    label.text = sample.Item2;
                    if (sample.Item1.Length == 0)
                    {
                        label.color = UiFactory.MutedTextColor;
                        label.alignment = TextAnchor.MiddleCenter;
                        label.rectTransform.offsetMin = new Vector2(4f, 4f);
                        label.rectTransform.offsetMax = new Vector2(-4f, -4f);
                    }
                    else
                    {
                        label.color = UiFactory.TextColor;
                    }
                }

                if (icon == null)
                {
                    continue;
                }

                if (sample.Item1.Length == 0 ||
                    catalog == null ||
                    !catalog.TryGet("icons/" + sample.Item1, out var sprite) ||
                    sprite == null)
                {
                    icon.enabled = false;
                    continue;
                }

                icon.sprite = sprite;
                icon.color = Color.white;
            }
        }

        private static void SetLabel(GameObject panel, string name, string value)
        {
            var label = FindDeep(panel.transform, name)?.GetComponent<Text>();
            if (label != null)
            {
                label.text = value;
            }
        }

        private static void FillStat(GameObject panel, string key, string value)
        {
            SetLabel(panel, "Stat" + key + "Value", value);
        }

        private static void FillStats(GameObject panel, IGameTexts texts)
        {
            var template = FindDeep(panel.transform, "StatsRowTemplate");
            var defense = FindDeep(panel.transform, "StatsDefenseRows");
            if (template == null || defense == null)
            {
                return;
            }

            AddStatRow(defense, template, "dmg_bullet", texts.Get("damage.dmg_bullet.name"), "11");
            AddStatRow(defense, template, "dmg_electric", texts.Get("damage.dmg_electric.name"), "7");
            AddStatRow(defense, template, "dmg_chemical", texts.Get("damage.dmg_chemical.name"), "7");
            AddStatRow(defense, template, "dmg_fire", texts.Get("damage.dmg_fire.name"), "6");
            AddStatRow(defense, template, "dmg_rupture", texts.Get("damage.dmg_rupture.name"), "6");
        }

        private static void AddStatRow(Transform rows, Transform template, string code, string label, string value)
        {
            var row = UnityEngine.Object.Instantiate(template, rows);
            row.name = "StatRow_" + code;
            row.gameObject.SetActive(true);

            var labelText = row.Find("Label")?.GetComponent<Text>();
            var valueText = row.Find("Value")?.GetComponent<Text>();
            if (labelText != null)
            {
                labelText.name = "StatLabel_" + code;
                labelText.text = label;
            }

            if (valueText != null)
            {
                valueText.name = "StatValue_" + code;
                valueText.text = value;
            }
        }

        private static void FillSlot(GameObject panel, IGameTexts texts, string slotName, string iconId, string value, int size)
        {
            var slot = FindDeep(panel.transform, slotName);
            var body = slot != null ? slot.transform.Find("Body") as RectTransform : null;
            if (body == null)
            {
                return;
            }

            var label = NewText(body, slotName + "Value", value, size, TextAnchor.LowerCenter, UiFactory.TextColor);
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.rectTransform.pivot = new Vector2(0.5f, 0f);
            label.rectTransform.offsetMin = new Vector2(4f, 2f);
            label.rectTransform.offsetMax = new Vector2(-4f, 26f);

            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>("Assets/Art/SpriteCatalog.asset");
            if (catalog == null || !catalog.TryGet("icons/" + iconId, out var sprite) || sprite == null)
            {
                return;
            }

            var icon = new GameObject(slotName + "Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(body, false);
            var image = icon.GetComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(6f, 30f);
            rect.offsetMax = new Vector2(-6f, -6f);
        }

        private static void FillArtifacts(GameObject panel, IGameTexts texts)
        {
            var row = FindDeep(panel.transform, "ArtifactSlots");
            var template = row != null ? row.transform.Find("ArtifactSlotTemplate") as RectTransform : null;
            if (row == null || template == null)
            {
                return;
            }

            var dash = UiText.Get(texts, UiTextKey.Common_Dash);
            for (var index = 0; index < 5; index++)
            {
                var slot = UnityEngine.Object.Instantiate(template, row.transform);
                slot.name = "ArtifactSlot_" + index;
                slot.gameObject.SetActive(true);
                var value = NewText(slot, "ArtifactSlot_" + index + "Value", dash, 12, TextAnchor.MiddleCenter, UiFactory.MutedTextColor);
                value.rectTransform.anchorMin = Vector2.zero;
                value.rectTransform.anchorMax = Vector2.one;
                value.rectTransform.offsetMin = Vector2.zero;
                value.rectTransform.offsetMax = Vector2.zero;
            }
        }

        private static void FillGrid(GameObject panel, SpriteCatalog? catalog)
        {
            var scrollObject = FindDeep(panel.transform, "GridArea");
            if (scrollObject == null)
            {
                return;
            }

            var scroll = scrollObject.GetComponent<ScrollArea>();
            var areaRect = (RectTransform)scrollObject.transform;
            var width = scroll != null && scroll.ViewportWidth > 1f
                ? scroll.ViewportWidth
                : areaRect.rect.width - 26f;
            if (width < 1f)
            {
                return;
            }

            const int columns = 8;
            const float gap = CharacterMetrics.GridGap;
            var height = scroll != null && scroll.ViewportHeight > 1f ? scroll.ViewportHeight : areaRect.rect.height;
            var side = Mathf.Max(CharacterMetrics.MinCellSize, InventoryGridArt.CellSide(width, columns, gap));
            var cell = new Vector2(side, side);
            var rows = InventoryGridArt.FitRows(height, side, gap);
            var size = InventoryGridArt.GridSize(columns, rows, cell, gap);
            var area = scroll != null && scroll.Content != null ? scroll.Content : areaRect;

            var root = new GameObject("InventoryGrid", typeof(RectTransform), typeof(Image));
            root.transform.SetParent(area, false);
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = false;
            var rootRect = (RectTransform)root.transform;
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);
            rootRect.sizeDelta = size;
            rootRect.anchoredPosition = Vector2.zero;
            scroll?.SetContentHeight(size.y);

            // В batch-режиме ScrollRect не обновляет AutoHide, поэтому полосу показываем вручную:
            // она нужна только когда сетка выше области.
            var viewportHeight = scroll != null && scroll.ViewportHeight > 1f ? scroll.ViewportHeight : height;
            var bar = FindDeep(scrollObject.transform, "Scrollbar");
            if (bar != null)
            {
                bar.gameObject.SetActive(size.y > viewportHeight + 0.5f);
            }

            for (var column = 1; column < columns; column++)
            {
                var line = NewImage(root.transform, "GridV" + column, InventoryGridArt.LineColor);
                var x = (column * (cell.x + gap)) - (gap / 2f) - (InventoryGridArt.LineThickness / 2f);
                PlaceLine(line.rectTransform, x, 0f, InventoryGridArt.LineThickness, size.y);
            }

            for (var row = 1; row < rows; row++)
            {
                var line = NewImage(root.transform, "GridH" + row, InventoryGridArt.LineColor);
                var y = (row * (cell.y + gap)) - (gap / 2f) - (InventoryGridArt.LineThickness / 2f);
                PlaceLine(line.rectTransform, 0f, y, size.x, InventoryGridArt.LineThickness);
            }

            SampleItem(root.transform, catalog, "wpn_ak74", 0, 0, 4, 2, cell, gap);
            SampleItem(root.transform, catalog, "itm_armor_plate", 0, 2, 2, 3, cell, gap);
            SampleItem(root.transform, catalog, "itm_helmet_light", 2, 2, 2, 1, cell, gap);
            SampleItem(root.transform, catalog, "itm_medkit_army", 5, 0, 1, 1, cell, gap);
            SampleItem(root.transform, catalog, "ammo_545", 6, 0, 1, 1, cell, gap);
            SampleItem(root.transform, catalog, "itm_food_stew", 4, 2, 1, 1, cell, gap);
        }

        private static void SampleItem(
            Transform parent,
            SpriteCatalog? catalog,
            string iconId,
            int column,
            int row,
            int width,
            int height,
            Vector2 cell,
            float gap)
        {
            var offset = InventoryGridArt.Offset(column, row, cell, gap);
            var size = InventoryGridArt.ItemSize(width, height, cell, gap);
            var plate = NewImage(parent, "Item_" + iconId + "_" + column + "_" + row, new Color(0f, 0f, 0f, 0f));
            PlaceLine(plate.rectTransform, offset.x, offset.y, size.x, size.y);

            if (catalog == null || !catalog.TryGet("icons/" + iconId, out var sprite) || sprite == null)
            {
                return;
            }

            var icon = NewImage(plate.transform, "ItemIcon_" + iconId, Color.white);
            icon.sprite = sprite;
            icon.preserveAspect = true;
            var rect = icon.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
        }

        private static Image NewImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text NewText(Transform parent, string name, string value, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = UiFactory.FindFont();
            label.fontSize = size;
            label.text = value;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static void PlaceLine(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
