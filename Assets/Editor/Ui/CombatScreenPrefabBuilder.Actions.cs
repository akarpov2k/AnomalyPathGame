#nullable enable
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Часть класса <see cref="CombatScreenPrefabBuilder"/>: форма действий — кнопки
    /// «Атака», «Скиллы», «Конец хода» и «Побег», быстрые слоты, выпадашка скиллов
    /// и плашка, закрывающая её кликом мимо.
    /// </summary>
    public static partial class CombatScreenPrefabBuilder
    {
        private const float FormButtonCenter = 0.5f;
        private const float QuickFrameLeft = 0.488f;
        private const float QuickFrameRight = 0.704f;
        private const float QuickFrameTop = 0.348f;
        private const float QuickFrameBottom = 0.900f;
        private const float QuickCellWidth = 150f;
        private const float QuickCellHeight = 62f;
        private const float MenuEntryHeight = 36f;
        private const float MenuInset = 18f;

        /// <summary>Прозрачная плашка, закрывающая выпадашку кликом мимо кнопки.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildMenuCatcher(RectTransform content, CombatScreen screen)
        {
            var catcher = CreateButton(content, "CombatMenuCatcher", 16);
            Stretch((RectTransform)catcher.transform, 0f);
            catcher.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
            catcher.gameObject.SetActive(false);
            SetObject(screen, "menuCatcher", catcher.gameObject);
        }

        /// <summary>Собирает форму действий, быстрые слоты и выпадашку скиллов.</summary>
        /// <param name="content">Корень содержимого.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildForm(RectTransform content, CombatScreen screen)
        {
            var form = CreateImage(content, "CombatForm", CombatPalette.ActionForm);
            var rect = form.rectTransform;
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(-(2f * Edge), FormHeight);
            rect.anchoredPosition = new Vector2(0f, FormBottom);

            var attack = CreateActionButton(rect, "CombatAttack", out var attackLabel);
            PlaceFormButton((RectTransform)attack.transform, 0.198f, 0f);
            AddLocalized(attackLabel, UiTextKey.Combat_Attack);
            SetObject(screen, "attack", attack);

            var skills = CreateActionButton(rect, "CombatSkills", out var skillsLabel);
            PlaceFormButton((RectTransform)skills.transform, 0.361f, 0f);
            AddLocalized(skillsLabel, UiTextKey.Combat_Skills);
            SetObject(screen, "skills", skills);

            var endTurn = CreateActionButton(rect, "CombatEndTurn", out var endLabel);
            PlaceFormButton((RectTransform)endTurn.transform, 0.809f, 1f);
            AddLocalized(endLabel, UiTextKey.Combat_End_Turn);
            SetObject(screen, "endTurn", endTurn);

            var flee = CreateActionButton(rect, "CombatFlee", out var fleeLabel);
            PlaceFormButton((RectTransform)flee.transform, 0.955f, 1f);
            AddLocalized(fleeLabel, UiTextKey.Combat_Flee);
            SetObject(screen, "flee", flee);

            BuildQuickSlots(rect, screen);
            var skillMenu = BuildMenu(
                rect,
                "CombatSkillMenu",
                new Vector2(0.443f, FormButtonCenter + 0.15f),
                new Vector2(340f, 340f),
                screen,
                "skillEntryTemplate",
                "skillEntries");
            skillMenu.SetActive(false);
            SetObject(screen, "skillMenu", skillMenu);
        }

        /// <summary>Ставит кнопку формы по доле ширины, по центру высоты формы.</summary>
        /// <param name="rect">Прямоугольник кнопки.</param>
        /// <param name="x">Доля ширины формы.</param>
        /// <param name="pivotX">Точка привязки по горизонтали: 0 — левый край, 1 — правый.</param>
        private static void PlaceFormButton(RectTransform rect, float x, float pivotX)
        {
            rect.anchorMin = new Vector2(x, FormButtonCenter);
            rect.anchorMax = new Vector2(x, FormButtonCenter);
            rect.pivot = new Vector2(pivotX, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 0f);
            rect.sizeDelta = new Vector2(ActionWidth, ActionHeight);
        }

        /// <summary>Быстрые слоты: рамка с сеткой 2×2 из неактивного образца.</summary>
        /// <param name="form">Прямоугольник формы.</param>
        /// <param name="screen">Собираемый экран.</param>
        private static void BuildQuickSlots(RectTransform form, CombatScreen screen)
        {
            var frame = CreateImage(form, "CombatQuickFrame", CombatPalette.WeaponPlate);
            PlaceFraction(
                frame.rectTransform,
                new Vector2(QuickFrameLeft, QuickFrameTop),
                new Vector2(QuickFrameRight, QuickFrameBottom));
            var grid = frame.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(6, 6, 6, 6);
            grid.spacing = new Vector2(8f, 8f);
            grid.cellSize = new Vector2(QuickCellWidth, QuickCellHeight);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;
            grid.childAlignment = TextAnchor.UpperLeft;

            var slot = CreateButton(frame.transform, "CombatQuickTemplate", 12);
            ButtonArtAssets.ApplyPlate(slot);
            var slotRect = (RectTransform)slot.transform;
            slotRect.anchorMin = new Vector2(0f, 1f);
            slotRect.anchorMax = new Vector2(0f, 1f);
            slotRect.pivot = new Vector2(0f, 1f);
            slotRect.sizeDelta = new Vector2(QuickCellWidth, QuickCellHeight);
            var slotLabel = slot.GetComponentInChildren<Text>();
            slotLabel.alignment = TextAnchor.LowerLeft;
            slotLabel.fontSize = 12;
            var labelRect = slotLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.offsetMin = new Vector2(54f, 8f);
            labelRect.offsetMax = new Vector2(-8f, 24f);
            var icon = CreateImage(slot.transform, "Icon", Color.white);
            icon.enabled = false;
            icon.preserveAspect = true;
            PlaceTopLeft(icon.rectTransform, new Vector2(10f, -10f), new Vector2(38f, 38f));
            slot.gameObject.SetActive(false);
            SetObject(screen, "quickSlots", frame.transform);
            SetObject(screen, "quickSlotTemplate", slot);
        }

        /// <summary>Выпадашка со строками действий и её неактивный образец строки.</summary>
        /// <param name="form">Прямоугольник формы.</param>
        /// <param name="name">Имя панели.</param>
        /// <param name="anchor">Точка появления рядом с кнопкой.</param>
        /// <param name="size">Размер панели.</param>
        /// <param name="screen">Собираемый экран.</param>
        /// <param name="templateField">Имя поля образца строки.</param>
        /// <param name="entriesField">Имя поля контейнера строк.</param>
        /// <returns>Панель выпадашки.</returns>
        private static GameObject BuildMenu(
            RectTransform form,
            string name,
            Vector2 anchor,
            Vector2 size,
            CombatScreen screen,
            string templateField,
            string entriesField)
        {
            var panel = CreateImage(form, name, CombatPalette.Menu);
            PanelArtAssets.Apply(panel);
            panel.raycastTarget = true;
            PlaceAt(panel.rectTransform, anchor, new Vector2(0f, 0f), Vector2.zero, size);
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(
                (int)MenuInset,
                (int)MenuInset,
                (int)MenuInset,
                (int)MenuInset);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            var fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var entry = CreateButton(panel.transform, name + "EntryTemplate", 16);
            ButtonArtAssets.ApplyPlate(entry);
            ((RectTransform)entry.transform).sizeDelta = new Vector2(size.x - (2f * MenuInset), MenuEntryHeight);
            var label = entry.GetComponentInChildren<Text>();
            label.alignment = TextAnchor.MiddleLeft;
            label.rectTransform.offsetMin = new Vector2(16f, 0f);
            label.rectTransform.offsetMax = new Vector2(-10f, 0f);
            entry.gameObject.SetActive(false);

            SetObject(screen, templateField, entry);
            SetObject(screen, entriesField, panel.transform);
            return panel.gameObject;
        }
    }
}


