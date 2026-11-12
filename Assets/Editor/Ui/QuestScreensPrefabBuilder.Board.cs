#nullable enable
using AnomalyPath.Runtime.Screens.Quests;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="QuestScreensPrefabBuilder"/>: префаб доски объявлений.</summary>
    public static partial class QuestScreensPrefabBuilder
    {
        /// <summary>Ширина карточки задания: текстовая колонка шире картинки и читается.</summary>
        private const float CardWidth = 340f;

        /// <summary>Высота карточки задания.</summary>
        private const float CardHeight = 238f;

        /// <summary>Зазор между карточками по горизонтали.</summary>
        private const float CardGapX = 20f;

        /// <summary>Зазор между карточками по вертикали.</summary>
        private const float CardGapY = 55f;

        private static GameObject BuildBoard()
        {
            var root = BuildRoot<QuestBoardScreen>("QuestBoard", ScreenBackdropIds.Quests, out var content);
            var screen = root.GetComponent<QuestBoardScreen>();
            var window = BuildBoardWindow(content, out var title, out var empty, out var close, out var toast);
            var scroll = CreateScroll(
                window.transform,
                "Slots",
                new Vector2(24f, -76f),
                new Vector2(BoardWidth - 48f, BoardHeight - 96f),
                12f);
            var grid = scroll.Content.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(CardWidth, CardHeight);
            grid.spacing = new Vector2(CardGapX, CardGapY);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 3;
            grid.childAlignment = TextAnchor.UpperCenter;
            var fitter = scroll.Content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var card = BuildCard(scroll.Content);

            SetObject(screen, "title", title);
            SetObject(screen, "empty", empty);
            SetObject(screen, "slots", scroll);
            SetObject(screen, "cardTemplate", card);
            SetObject(screen, "toast", toast);
            SetObject(screen, "close", close);
            return root;
        }

        private static Image BuildBoardWindow(
            Transform content,
            out Text title,
            out Text empty,
            out Button close,
            out Text toast)
        {
            var window = CreateImage(content, "Window", QuestPalette.Window);
            window.raycastTarget = true;
            PanelArtAssets.ApplyPanel(window);
            PlaceCenter(window.rectTransform, new Vector2(BoardWidth, BoardHeight));

            title = CreateText(
                window.transform,
                "Title",
                string.Empty,
                28,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor,
                bold: true);
            PlaceTopLeft(title.rectTransform, new Vector2(28f, -18f), new Vector2(760f, 44f));

            close = CreateIconButton(window.transform, "BoardClose", CloseIconPath);
            PlaceTopRight((RectTransform)close.transform, new Vector2(-18f, -16f), new Vector2(48f, 48f));
            ButtonArtAssets.ApplyPlateBehind(close);

            empty = CreateText(
                window.transform,
                "Empty",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            PlaceCenter(empty.rectTransform, new Vector2(800f, 40f));
            empty.gameObject.SetActive(false);

            toast = CreateText(
                window.transform,
                "Toast",
                string.Empty,
                20,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            PlaceBottom(toast.rectTransform, new Vector2(0f, 14f), new Vector2(900f, 32f));
            return window;
        }

        private static QuestBoardCard BuildCard(Transform parent)
        {
            var root = new GameObject("CardTemplate", typeof(RectTransform));
            root.transform.SetParent(parent, false);
            ((RectTransform)root.transform).sizeDelta = new Vector2(CardWidth, CardHeight);

            var background = CreateImage(root.transform, "Background", QuestPalette.Row);
            Stretch(background.rectTransform, 0f);
            background.raycastTarget = true;
            PanelArtAssets.ApplyPanel(background);

            var art = CreateImage(root.transform, "Art", QuestPalette.ArtPlaceholder);
            art.preserveAspect = true;
            PlaceTopLeft(art.rectTransform, new Vector2(8f, -8f), new Vector2(112f, 150f));

            var timer = CreateText(
                root.transform,
                "Timer",
                string.Empty,
                16,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            PlaceTopLeft(timer.rectTransform, new Vector2(8f, -8f), new Vector2(277f, 170f));

            // Текстовая колонка шире картинки: при прежней ширине описание переносилось
            // на 7–9 строк и вылезало за карточку поверх цели и соседей. Каждой подписи
            // отведена своя полоса, лишние строки обрезаются, а не наезжают друг на друга.
            var difficulty = CreateText(
                root.transform,
                "Difficulty",
                string.Empty,
                12,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(difficulty.rectTransform, new Vector2(128f, -8f), new Vector2(204f, 16f));

            var title = CreateText(
                root.transform,
                "Title",
                string.Empty,
                15,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            title.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceTopLeft(title.rectTransform, new Vector2(128f, -24f), new Vector2(204f, 38f));

            var description = CreateText(
                root.transform,
                "Description",
                string.Empty,
                13,
                TextAnchor.UpperLeft,
                UiFactory.TextColor);
            description.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceTopLeft(description.rectTransform, new Vector2(128f, -64f), new Vector2(204f, 64f));

            var targets = CreateText(
                root.transform,
                "Targets",
                string.Empty,
                12,
                TextAnchor.UpperLeft,
                UiFactory.MutedTextColor);
            targets.verticalOverflow = VerticalWrapMode.Truncate;
            PlaceTopLeft(targets.rectTransform, new Vector2(128f, -130f), new Vector2(204f, 48f));

            var take = CreatePlateButton(root.transform, "Take", out var takeLabel);
            var takeRect = (RectTransform)take.transform;
            takeRect.anchorMin = new Vector2(0f, 0f);
            takeRect.anchorMax = new Vector2(0f, 0f);
            takeRect.pivot = new Vector2(0f, 0f);
            takeRect.anchoredPosition = new Vector2(8f, 8f);
            takeRect.sizeDelta = new Vector2(130f, 34f);
            AddLocalized(takeLabel, UiTextKey.Quest_Take);

            var decline = CreatePlateButton(root.transform, "Decline", out var declineLabel);
            var declineRect = (RectTransform)decline.transform;
            declineRect.anchorMin = new Vector2(1f, 0f);
            declineRect.anchorMax = new Vector2(1f, 0f);
            declineRect.pivot = new Vector2(1f, 0f);
            declineRect.anchoredPosition = new Vector2(-8f, 8f);
            declineRect.sizeDelta = new Vector2(130f, 34f);
            AddLocalized(declineLabel, UiTextKey.Quest_Decline);

            var card = root.AddComponent<QuestBoardCard>();
            SetObject(card, "background", background);
            SetObject(card, "art", art);
            SetObject(card, "difficulty", difficulty);
            SetObject(card, "title", title);
            SetObject(card, "description", description);
            SetObject(card, "targets", targets);
            SetObject(card, "timer", timer);
            SetObject(card, "take", take);
            SetObject(card, "decline", decline);
            root.SetActive(false);
            return card;
        }
    }
}
