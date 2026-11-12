#nullable enable
using AnomalyPath.Runtime.Screens.Quests;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="QuestScreensPrefabBuilder"/>: префаб журнала заданий.</summary>
    public static partial class QuestScreensPrefabBuilder
    {
        /// <summary>Ширина панели списка журнала.</summary>
        private const float ListPanelWidth = 502f;

        /// <summary>Высота панели списка журнала.</summary>
        private const float ListPanelHeight = 718f;

        /// <summary>Ширина панели описания задания.</summary>
        private const float DetailsWidth = 302f;

        /// <summary>Высота панели описания задания.</summary>
        private const float DetailsHeight = 620f;

        /// <summary>Ширина кнопки действия журнала.</summary>
        private const float ActionWidth = 160f;

        /// <summary>Высота кнопки действия журнала.</summary>
        private const float ActionHeight = 80f;

        private static GameObject BuildJournal()
        {
            var root = BuildRoot<QuestJournalScreen>("QuestJournal", ScreenBackdropIds.Quests, out var content);
            var screen = root.GetComponent<QuestJournalScreen>();
            var window = CreateImage(content, "Window", QuestPalette.Window);
            window.raycastTarget = true;
            PanelArtAssets.ApplyPanel(window);
            PlaceCenter(window.rectTransform, new Vector2(JournalWidth, JournalHeight));

            var title = CreateText(
                window.transform,
                "Title",
                string.Empty,
                26,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor,
                bold: true);
            PlaceTopLeft(title.rectTransform, new Vector2(28f, -18f), new Vector2(500f, 44f));
            AddLocalized(title, UiTextKey.Quest_Journal_Title);

            var close = CreateIconButton(window.transform, "JournalClose", CloseIconPath);
            PlaceTopRight((RectTransform)close.transform, new Vector2(-18f, -16f), new Vector2(48f, 48f));
            ButtonArtAssets.ApplyPlateBehind(close);

            BuildListPanel(window.transform, out var storyTab, out var sideTab, out var repeatableTab, out var scroll, out var row, out var empty);
            BuildDetails(window.transform, out var detailsTitle, out var detailsKind, out var detailsDescription, out var detailsProgress, out var detailsConditions, out var conditionsHeader, out var detailsTargets, out var detailsReward, out var detailsHint);

            var action = CreateActionButton(
                window.transform,
                "JournalAction",
                ActionFramePath,
                ActionFrameGreenPath,
                out var actionLabel);
            PlaceTopLeft(
                (RectTransform)action.transform,
                new Vector2(556f + ((DetailsWidth - ActionWidth) * 0.5f), -92f - DetailsHeight - 16f),
                new Vector2(ActionWidth, ActionHeight));

            var toast = CreateText(
                window.transform,
                "Toast",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            PlaceBottom(toast.rectTransform, new Vector2(0f, 12f), new Vector2(860f, 30f));

            SetObject(screen, "title", title);
            SetObject(screen, "storyTab", storyTab);
            SetObject(screen, "sideTab", sideTab);
            SetObject(screen, "repeatableTab", repeatableTab);
            SetObject(screen, "list", scroll);
            SetObject(screen, "rowTemplate", row);
            SetObject(screen, "empty", empty);
            SetObject(screen, "detailsTitle", detailsTitle);
            SetObject(screen, "detailsKind", detailsKind);
            SetObject(screen, "detailsDescription", detailsDescription);
            SetObject(screen, "detailsProgress", detailsProgress);
            SetObject(screen, "detailsConditions", detailsConditions);
            SetObject(screen, "conditionsHeader", conditionsHeader);
            SetObject(screen, "detailsTargets", detailsTargets);
            SetObject(screen, "detailsReward", detailsReward);
            SetObject(screen, "detailsHint", detailsHint);
            SetObject(screen, "action", action);
            SetObject(screen, "actionLabel", actionLabel);
            SetObject(screen, "toast", toast);
            SetObject(screen, "close", close);
            return root;
        }

        private static Image BuildListPanel(
            Transform window,
            out Button storyTab,
            out Button sideTab,
            out Button repeatableTab,
            out ScrollArea scroll,
            out QuestJournalRow row,
            out Text empty)
        {
            var panel = CreateImage(window, "ListPanel", QuestPalette.Panel);
            PanelArtAssets.ApplyPanel(panel);
            PlaceTopLeft(panel.rectTransform, new Vector2(36f, -92f), new Vector2(ListPanelWidth, ListPanelHeight));

            var tabs = CreateRect(panel.transform, "Tabs");
            PlaceTopLeft(tabs, new Vector2(31f, -16f), new Vector2(440f, 40f));
            var layout = tabs.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            storyTab = BuildTab(tabs, "StoryTab", UiTextKey.Quest_Category_Story);
            sideTab = BuildTab(tabs, "SideTab", UiTextKey.Quest_Category_Side);
            repeatableTab = BuildTab(tabs, "RepeatableTab", UiTextKey.Quest_Category_Repeatable);

            scroll = CreateScroll(panel.transform, "List", new Vector2(31f, -64f), new Vector2(440f, 640f), 12f);
            var stack = scroll.Content.gameObject.AddComponent<VerticalLayoutGroup>();
            stack.spacing = 8f;
            stack.childAlignment = TextAnchor.UpperCenter;
            stack.childControlWidth = true;
            stack.childControlHeight = false;
            stack.childForceExpandWidth = true;
            stack.childForceExpandHeight = false;
            var fitter = scroll.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            row = BuildRow(scroll.Content);

            empty = CreateText(
                panel.transform,
                "Empty",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            PlaceTopLeft(empty.rectTransform, new Vector2(31f, -64f), new Vector2(440f, 640f));
            empty.gameObject.SetActive(false);
            return panel;
        }

        private static Button BuildTab(Transform parent, string name, UiTextKey key)
        {
            var button = CreatePlateButton(parent, name, out var label);
            var rect = (RectTransform)button.transform;
            rect.sizeDelta = new Vector2(141f, 40f);
            button.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
            AddLocalized(label, key);
            return button;
        }

        private static QuestJournalRow BuildRow(Transform content)
        {
            var root = new GameObject("RowTemplate", typeof(RectTransform));
            root.transform.SetParent(content, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(440f, 78f);
            root.AddComponent<LayoutElement>().preferredHeight = 78f;

            var background = CreateImage(root.transform, "Background", QuestPalette.Row);
            Stretch(background.rectTransform, 0f);
            background.raycastTarget = true;

            var select = root.AddComponent<Button>();
            select.targetGraphic = background;

            var title = CreateText(
                root.transform,
                "Title",
                string.Empty,
                16,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            PlaceTopLeft(title.rectTransform, new Vector2(16f, -6f), new Vector2(408f, 26f));

            var progress = CreateText(
                root.transform,
                "Progress",
                string.Empty,
                14,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            PlaceTopLeft(progress.rectTransform, new Vector2(16f, -32f), new Vector2(408f, 22f));

            var time = CreateText(
                root.transform,
                "Time",
                string.Empty,
                12,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(time.rectTransform, new Vector2(16f, -54f), new Vector2(408f, 18f));

            var row = root.AddComponent<QuestJournalRow>();
            SetObject(row, "background", background);
            SetObject(row, "title", title);
            SetObject(row, "progress", progress);
            SetObject(row, "time", time);
            SetObject(row, "select", select);
            root.SetActive(false);
            return row;
        }

        private static Image BuildDetails(
            Transform window,
            out Text detailsTitle,
            out Text detailsKind,
            out Text detailsDescription,
            out Text detailsProgress,
            out Text detailsConditions,
            out Text conditionsHeader,
            out Text detailsTargets,
            out Text detailsReward,
            out Text detailsHint)
        {
            var panel = CreateImage(window, "DetailsPanel", QuestPalette.Panel);
            PanelArtAssets.ApplyPanel(panel);
            PlaceTopLeft(panel.rectTransform, new Vector2(556f, -92f), new Vector2(DetailsWidth, DetailsHeight));

            detailsTitle = CreateText(
                panel.transform,
                "DetailsTitle",
                string.Empty,
                20,
                TextAnchor.UpperLeft,
                UiFactory.TextColor,
                bold: true);
            PlaceTopLeft(detailsTitle.rectTransform, new Vector2(16f, -14f), new Vector2(270f, 52f));

            detailsKind = CreateText(
                panel.transform,
                "DetailsKind",
                string.Empty,
                14,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(detailsKind.rectTransform, new Vector2(16f, -70f), new Vector2(270f, 22f));

            detailsDescription = CreateText(
                panel.transform,
                "DetailsDescription",
                string.Empty,
                15,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            PlaceTopLeft(detailsDescription.rectTransform, new Vector2(16f, -96f), new Vector2(270f, 150f));

            var header = CreateText(
                panel.transform,
                "ConditionsHeader",
                string.Empty,
                14,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(header.rectTransform, new Vector2(16f, -252f), new Vector2(270f, 20f));
            AddLocalized(header, UiTextKey.Quest_Details_Conditions);
            conditionsHeader = header;

            detailsConditions = CreateText(
                panel.transform,
                "DetailsConditions",
                string.Empty,
                14,
                TextAnchor.UpperLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(detailsConditions.rectTransform, new Vector2(16f, -274f), new Vector2(270f, 44f));

            detailsProgress = CreateText(
                panel.transform,
                "DetailsProgress",
                string.Empty,
                15,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            PlaceTopLeft(detailsProgress.rectTransform, new Vector2(16f, -320f), new Vector2(270f, 26f));

            detailsTargets = CreateText(
                panel.transform,
                "DetailsTargets",
                string.Empty,
                14,
                TextAnchor.UpperLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(detailsTargets.rectTransform, new Vector2(16f, -348f), new Vector2(270f, 116f));

            detailsReward = CreateText(
                panel.transform,
                "DetailsReward",
                string.Empty,
                15,
                TextAnchor.MiddleLeft,
                UiFactory.ToastColor);
            PlaceTopLeft(detailsReward.rectTransform, new Vector2(16f, -466f), new Vector2(270f, 26f));

            detailsHint = CreateText(
                panel.transform,
                "DetailsHint",
                string.Empty,
                13,
                TextAnchor.UpperLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(detailsHint.rectTransform, new Vector2(16f, -494f), new Vector2(270f, 100f));
            return panel;
        }
    }
}
