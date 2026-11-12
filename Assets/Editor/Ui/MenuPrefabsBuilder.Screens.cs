#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsBuilder"/>: экраны загрузки и сохранения.</summary>
    public static partial class MenuPrefabsBuilder
    {
        private static GameObject BuildLoadScreen(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("LoadGame");
            var backdrop = AddBackdrop(root.transform, ScreenBackdropIds.Base);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            var rows = BuildRows(ui, content, MenuScreenMetrics.ListBottomInset);
            var template = BuildRowTemplate(ui, texts, rows.transform, forSaveScreen: false);

            var empty = CreateLabel(
                content,
                "Empty",
                UiText.Get(texts, UiTextKey.Load_Empty),
                24,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            Localize(empty, UiTextKey.Load_Empty);
            empty.gameObject.SetActive(false);
            Place(
                (RectTransform)empty.transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, 40f),
                new Vector2(900f, 60f));

            var status = CreateLabel(
                content,
                "Status",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            StretchBottomBand(
                (RectTransform)status.transform,
                MenuScreenMetrics.ListPadding,
                MenuScreenMetrics.StatusBottomInset,
                MenuScreenMetrics.ListPadding,
                MenuScreenMetrics.StatusHeight);

            var pager = BuildPager(ui, content);
            var back = CreateBackButton(ui, texts, content);

            var screen = root.AddComponent<LoadGameScreen>();
            SetObject(screen, "rows", rows);
            SetObject(screen, "rowTemplate", template);
            SetObject(screen, "empty", empty);
            SetObject(screen, "status", status);
            SetObject(screen, "pager", pager);
            SetObject(screen, "back", back);
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static GameObject BuildSaveScreen(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("SaveGame");
            var backdrop = AddBackdrop(root.transform, ScreenBackdropIds.Base);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            var rows = BuildRows(ui, content, MenuScreenMetrics.ListBottomInset);
            var template = BuildRowTemplate(ui, texts, rows.transform, forSaveScreen: true);

            var status = CreateLabel(
                content,
                "Status",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            StretchBottomBand(
                (RectTransform)status.transform,
                MenuScreenMetrics.ListPadding,
                MenuScreenMetrics.StatusBottomInset,
                MenuScreenMetrics.ListPadding,
                MenuScreenMetrics.StatusHeight);

            var pager = BuildPager(ui, content);
            var back = CreateBackButton(ui, texts, content);

            var screen = root.AddComponent<SaveGameScreen>();
            SetObject(screen, "rows", rows);
            SetObject(screen, "rowTemplate", template);
            SetObject(screen, "status", status);
            SetObject(screen, "pager", pager);
            SetObject(screen, "back", back);
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static ScreenBackdrop AddBackdrop(Transform parent, string? spriteId = null)
        {
            var image = CreateImage(parent, "ScreenBackdrop", Color.white);
            Stretch((RectTransform)image.transform);
            var backdrop = image.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", image);
            if (!string.IsNullOrEmpty(spriteId))
            {
                ScreenBackdropAssets.Apply(backdrop, spriteId!);
            }

            return backdrop;
        }

        private static Button CreateBackButton(UiFactory ui, IGameTexts texts, Transform parent)
        {
            var button = CreateLocalizedButton(ui, texts, parent, UiTextKey.Common_Back, UiFactory.ButtonColor, action: true);
            Place(
                (RectTransform)button.transform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-MenuScreenMetrics.BackInset, MenuScreenMetrics.BackBottomInset),
                new Vector2(MenuScreenMetrics.BackWidth, MenuScreenMetrics.BackHeight));
            return button;
        }

        private static void StyleInputField(TextInputField input)
        {
            var image = input.GetComponent<Image>();
            if (image != null)
            {
                image.color = MenuPalette.Field;
            }

            var labels = input.GetComponentsInChildren<Text>(true);
            if (labels.Length > 0)
            {
                labels[0].color = MenuPalette.FieldText;
            }

            if (labels.Length > 1)
            {
                labels[1].color = MenuPalette.FieldHint;
            }
        }
    }
}
