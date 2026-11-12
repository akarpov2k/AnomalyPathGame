#nullable enable
using System;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Часть класса <see cref="InventoryScreen"/>: столбец быстрых слотов «Быстрый доступ».
    /// Слоты строятся из неактивного образца префаба по числу слотов экипировки;
    /// предмет кладётся и убирается перетаскиванием, меню предмета в них не открывается.
    /// </summary>
    public sealed partial class InventoryScreen
    {
        private void FillQuickSlots(PlayerState player)
        {
            Clear(quickRow, quickTemplate.gameObject);
            var count = Math.Max(0, player.Equipment.QuickSlotCount);
            for (var index = 0; index < count; index++)
            {
                var slotIndex = index;
                var item = player.Equipment.GetQuick(index);
                var slot = Instantiate(quickTemplate, quickRow);
                slot.gameObject.name = "QuickSlot_" + index;
                slot.gameObject.SetActive(true);

                var zone = slot.GetComponent<ItemDropZone>();
                if (zone != null)
                {
                    zone.Configure(_drag, id => DropOnQuick(id, slotIndex));
                }

                var icon = slot.transform.Find("Icon")?.GetComponent<Image>();
                var label = slot.transform.Find("Label")?.GetComponent<Text>();
                if (item == null)
                {
                    if (icon != null)
                    {
                        icon.enabled = false;
                    }

                    if (label != null)
                    {
                        label.text = UiText.Get(Host.Texts, UiTextKey.Common_Dash);
                        label.color = UiFactory.MutedTextColor;
                        label.alignment = TextAnchor.MiddleCenter;
                        label.rectTransform.offsetMin = new Vector2(4f, 4f);
                        label.rectTransform.offsetMax = new Vector2(-4f, -4f);
                    }

                    continue;
                }

                var template = CharacterWidgets.Template(WidgetHost, item);
                if (icon != null)
                {
                    if (template != null && Host.Sprites.TryGet(template.Icon, out var sprite) && sprite != null)
                    {
                        icon.enabled = true;
                        icon.sprite = sprite;
                    }
                    else
                    {
                        icon.enabled = false;
                    }
                }

                if (label != null)
                {
                    label.text = CharacterWidgets.Describe(WidgetHost, item);
                    label.color = UiFactory.TextColor;
                }

                CharacterWidgets.AttachItem(
                    WidgetHost,
                    this,
                    slot,
                    item,
                    null,
                    -1,
                    DragOrigin.QuickSlot,
                    _tooltip,
                    menu: false);
                RarityFrame.Draw(Host.Ui, (RectTransform)slot.transform, ItemRarity.Resolve(item, template));
            }
        }
    }
}
