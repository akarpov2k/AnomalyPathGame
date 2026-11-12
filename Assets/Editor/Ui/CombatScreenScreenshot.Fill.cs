#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="CombatScreenScreenshot"/>: заполнение панелей боевого префаба.</summary>
    public static partial class CombatScreenScreenshot
    {
        private static void FillProgress(
            GameObject panel,
            IProgressionService progression,
            PlayerState player,
            IGameTexts texts)
        {
            var required = progression.RequiredXp(player.Level);
            var floor = progression.TotalXpToLevel(player.Level);
            var into = Math.Max(0, player.Xp - floor);
            SetFill(panel, "CombatXp/Fill", Fraction(into, required));
            SetText(panel, "CombatXp/Label", UiText.Format(texts, UiTextKey.Combat_Xp, player.Level, into, required));
        }

        private static void FillQuickSlots(
            GameObject panel,
            CombatState combat,
            IItemRepository items,
            SpriteCatalog? catalog,
            IGameTexts texts)
        {
            var frame = FindDeep(panel.transform, "CombatQuickFrame");
            var template = FindDeep(panel.transform, "CombatQuickTemplate");
            var equipment = combat.Player.Equipment;
            if (frame == null || template == null || equipment == null)
            {
                return;
            }

            template.gameObject.SetActive(false);
            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                var slot = UnityEngine.Object.Instantiate(template, frame);
                slot.gameObject.name = "Quick" + index;
                slot.gameObject.SetActive(true);
                var item = equipment.QuickSlots[index];
                var templateData = item == null ? null : items.Get(item.ItemId);
                var label = slot.Find("Label");
                if (label != null)
                {
                    label.GetComponent<Text>().text = templateData == null
                        ? UiText.Get(texts, UiTextKey.Combat_Quick_Empty)
                        : texts.Get(templateData.NameKey);
                }

                var icon = slot.Find("Icon");
                if (icon != null && catalog != null && templateData != null &&
                    catalog.TryGet(templateData.Icon, out var sprite) && sprite != null)
                {
                    var image = icon.GetComponent<Image>();
                    image.sprite = sprite;
                    image.enabled = true;
                }
            }
        }

        private static void FillMenu(
            GameObject panel,
            MenuContext menu,
            CombatState combat,
            IGameTexts texts,
            MenuKind kind)
        {
            if (kind == MenuKind.None)
            {
                return;
            }

            var menuTransform = FindDeep(panel.transform, "CombatSkillMenu");
            if (menuTransform == null)
            {
                return;
            }

            menuTransform.gameObject.SetActive(true);
            var template = menuTransform.GetChild(0);
            template.gameObject.SetActive(false);
            var actions = menu.Combat.GetActions(combat, menu.Combat.PlayerWeapon(combat));

            foreach (var action in actions)
            {
                if (action.Kind != CombatActionKind.Skill || action.SkillId == null)
                {
                    continue;
                }

                var name = SkillName(menu.Skills, texts, action.SkillId);
                AddEntry(
                    template,
                    menuTransform,
                    UiText.Format(texts, UiTextKey.Combat_Skill_Button, name, action.ApCost, action.AmmoCost),
                    name,
                    action.IsAvailable);
            }

            AddEntry(
                template,
                menuTransform,
                UiText.Format(
                    texts,
                    UiTextKey.Combat_Skill_Button_No_Ammo,
                    UiText.Get(texts, UiTextKey.Combat_Reload),
                    menu.Settings.ReloadApCost),
                "reload",
                true);
            AddEntry(
                template,
                menuTransform,
                UiText.Format(
                    texts,
                    UiTextKey.Combat_Skill_Button_No_Ammo,
                    UiText.Get(texts, UiTextKey.Combat_Swap),
                    menu.Settings.WeaponSwapApCost),
                "swap",
                combat.Player.Equipment?.Get(EquipmentSlot.Secondary) != null);
        }

        private static string SkillName(ISkillRepository skills, IGameTexts texts, string skillId) =>
            skills.TryGet(skillId, out var template) && template != null
                ? texts.Get(template.NameKey)
                : skillId;

        private static void AddEntry(
            Transform template,
            Transform menu,
            string label,
            string name,
            bool interactable)
        {
            var entry = UnityEngine.Object.Instantiate(template, menu);
            entry.gameObject.name = name;
            entry.gameObject.SetActive(true);
            var text = entry.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }

            var button = entry.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = interactable;
            }
        }

        private static void FillJournal(GameObject panel, IReadOnlyList<string> lines)
        {
            var journal = FindDeep(panel.transform, "CombatJournalPanel");
            var text = FindPath(panel.transform, "CombatJournalPanel/Text");
            if (journal == null)
            {
                return;
            }

            journal.gameObject.SetActive(lines.Count > 0);
            if (text != null)
            {
                text.GetComponent<Text>().text = string.Join("\n", lines);
            }
        }

    }
}
