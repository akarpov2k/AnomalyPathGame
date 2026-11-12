#nullable enable
using System;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>Часть класса <see cref="InventoryScreen"/>: заполнение слотов, фигуры и сетки.</summary>
    public sealed partial class InventoryScreen
    {
        private void FillHeader(PlayerState player)
        {
            nickname.text = player.Name;
            level.text = UiText.Format(Host.Texts, UiTextKey.Common_Level_Cap, player.Level);
            money.text = SaveSlotText.Money(Host.Texts, player.Money);
            if (weight == null)
            {
                return;
            }

            var total = Host.Inventory.TotalWeight(player.Inventory);
            var limit = player.Inventory.WeightLimit;
            weight.text = UiText.Format(Host.Texts, UiTextKey.Character_Weight, total, limit);
            weight.color = total > limit + 1e-9 ? CharacterPalette.Error : UiFactory.TextColor;
        }

        private void FillSlots(PlayerState player)
        {
            var limit = Math.Min(slotZones.Length, slotBodies.Length);
            for (var index = 0; index < limit; index++)
            {
                var slot = (EquipmentSlot)index;
                FillSlot(slotBodies[index], slot, player.Equipment.Get(slot));
            }
        }

        private void FillSlot(RectTransform body, EquipmentSlot slot, ItemInstance? item)
        {
            Clear(body);
            var prefix = "Slot" + slot;
            var plate = Host.Ui.CreateImage(prefix + "Plate", body, Color.clear);
            plate.raycastTarget = true;
            Stretch(plate.rectTransform);

            if (item == null)
            {
                var dash = MakeText(body, prefix + "Value", UiText.Get(Host.Texts, UiTextKey.Common_Dash), 18,
                    TextAnchor.MiddleCenter, UiFactory.MutedTextColor);
                Stretch(dash.rectTransform);
                return;
            }

            var template = CharacterWidgets.Template(WidgetHost, item);
            if (template != null && Host.Sprites.TryGet(template.Icon, out var sprite) && sprite != null)
            {
                var icon = Host.Ui.CreateImage(prefix + "Icon", body, Color.white);
                icon.sprite = sprite;
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                StretchPart(icon.rectTransform, 6f, 30f, 6f, 6f);
            }

            var value = MakeText(body, prefix + "Value", CharacterWidgets.Describe(WidgetHost, item), 16,
                TextAnchor.LowerCenter, UiFactory.TextColor);
            value.verticalOverflow = VerticalWrapMode.Truncate;
            StretchPart(value.rectTransform, 4f, 2f, 4f, 22f);
            CharacterWidgets.AttachItem(WidgetHost, this, plate, item, slot, -1, DragOrigin.Equipment, _tooltip);
            RarityFrame.Draw(Host.Ui, body, ItemRarity.Resolve(item, template));
        }

        private void FillArtifacts(PlayerState player)
        {
            Clear(artifactRow, artifactTemplate.gameObject);
            var capacity = Math.Max(1, Host.Equipment.TotalArtifactSlots(player.Equipment));
            for (var index = 0; index < capacity; index++)
            {
                var artifact = index < player.Equipment.Artifacts.Count ? player.Equipment.Artifacts[index] : null;
                var slot = Instantiate(artifactTemplate, artifactRow);
                slot.gameObject.name = "ArtifactSlot_" + index;
                slot.gameObject.SetActive(true);
                var prefix = "ArtifactSlot_" + index;
                var zone = slot.GetComponent<ItemDropZone>();
                if (zone != null)
                {
                    zone.Configure(_drag, _ => DropOnArtifact(_));
                }

                var body = (RectTransform)slot.transform;
                if (artifact == null)
                {
                    var dash = MakeText(body, prefix + "Value", UiText.Get(Host.Texts, UiTextKey.Common_Dash), 14,
                        TextAnchor.MiddleCenter, UiFactory.MutedTextColor);
                    Stretch(dash.rectTransform);
                    continue;
                }

                var template = CharacterWidgets.Template(WidgetHost, artifact);
                if (template != null && Host.Sprites.TryGet(template.Icon, out var sprite) && sprite != null)
                {
                    var icon = Host.Ui.CreateImage(prefix + "Icon", body, Color.white);
                    icon.sprite = sprite;
                    icon.preserveAspect = true;
                    icon.raycastTarget = false;
                    StretchPart(icon.rectTransform, 3f, 16f, 3f, 3f);
                }

                var value = MakeText(body, prefix + "Value", CharacterWidgets.Describe(WidgetHost, artifact), 12,
                    TextAnchor.LowerCenter, UiFactory.TextColor);
                value.verticalOverflow = VerticalWrapMode.Truncate;
                StretchPart(value.rectTransform, 2f, 1f, 2f, 14f);
                CharacterWidgets.AttachItem(WidgetHost, this, slot, artifact, null, index, DragOrigin.Equipment, _tooltip);
                RarityFrame.Draw(Host.Ui, body, ItemRarity.Resolve(artifact, template));
            }
        }

        private void FillGrid(PlayerState player)
        {
            var missing = _inventory.Build(player, gridArea, FilterPredicate);
            overflow.text = missing.Length == 0
                ? string.Empty
                : UiText.Format(Host.Texts, UiTextKey.Character_Overflow, missing);
            overflow.gameObject.SetActive(missing.Length > 0);
        }

        private void FillEmpty()
        {
            _tooltip.Hide();
            var limit = Math.Min(slotZones.Length, slotBodies.Length);
            for (var index = 0; index < limit; index++)
            {
                Clear(slotBodies[index]);
            }

            Clear(artifactRow, artifactTemplate.gameObject);
            Clear(quickRow, quickTemplate.gameObject);
            foreach (var value in statValues)
            {
                value.text = UiText.Get(Host.Texts, UiTextKey.Common_Dash);
            }

            _statsPanel.Clear();

            nickname.text = string.Empty;
            level.text = string.Empty;
            money.text = string.Empty;
            if (weight != null)
            {
                weight.text = string.Empty;
            }

            overflow.text = string.Empty;
            overflow.gameObject.SetActive(false);
            CharacterFigure.Apply(WidgetHost, figure, null, CharacterPalette.FigureArea);
            Clear(gridArea.Content);
            _drag.End();
        }

        private Text MakeText(Transform parent, string name, string value, int size, TextAnchor anchor, Color color)
        {
            var label = Host.Ui.CreateText(parent, value, size, anchor, color);
            label.gameObject.name = name;
            label.raycastTarget = false;
            var shadow = label.gameObject.AddComponent<Outline>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
            shadow.effectDistance = new Vector2(1f, -1f);
            shadow.useGraphicAlpha = true;
            return label;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void StretchPart(RectTransform rect, float left, float bottom, float right, float top)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        private static void Clear(Transform parent, GameObject? keep = null)
        {
            foreach (Transform child in parent)
            {
                if (keep != null && child.gameObject == keep)
                {
                    continue;
                }

                child.gameObject.SetActive(false);
                UnityEngine.Object.Destroy(child.gameObject);
            }
        }
    }
}
