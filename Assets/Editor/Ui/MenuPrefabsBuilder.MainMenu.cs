#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MenuPrefabsBuilder"/>: стартовое меню.</summary>
    public static partial class MenuPrefabsBuilder
    {
        /// <summary>Картинка стартового меню: сталкер с артефактом без фона, стоит ногами на дороге.</summary>
        private const string ArtworkSpritePath = "Assets/Art/Backdrops/menu_art.png";

        /// <summary>Задник стартового меню: вид на электростанцию за колючей проволокой.</summary>
        private const string BackdropSpritePath = "Assets/Art/Backdrops/menu_backdrop.png";

        private static GameObject BuildMainMenu(UiFactory ui, IGameTexts texts)
        {
            var root = CreatePanel("MainMenu");
            var backdropImage = CreateImage(root.transform, "ScreenBackdrop", Color.white);
            Stretch((RectTransform)backdropImage.transform);
            var backdrop = backdropImage.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", backdropImage);
            SetObject(backdrop, "sprite", LoadSprite(BackdropSpritePath));

            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            // Вырезка сталкера с артефактом лежит поверх задника: пропорции сохраняются,
            // поэтому фигура не растягивается под форму панели. Прямоугольник поставлен так,
            // что ступни фигуры приходятся на дорогу перед колючей проволокой.
            var artwork = CreateImage(content, "Artwork", Color.white);
            artwork.sprite = LoadSprite(ArtworkSpritePath);
            artwork.preserveAspect = true;
            var artworkRect = (RectTransform)artwork.transform;
            artworkRect.anchorMin = new Vector2(MenuScreenMetrics.ArtworkLeft, 0f);
            artworkRect.anchorMax = new Vector2(MenuScreenMetrics.ArtworkRight, 1f);
            artworkRect.pivot = new Vector2(0.5f, 0.5f);
            artworkRect.offsetMin = new Vector2(0f, MenuScreenMetrics.ArtworkInsetBottom);
            artworkRect.offsetMax = new Vector2(0f, -MenuScreenMetrics.ArtworkInsetTop);

            var column = BuildMenuColumn(content);
            var title = CreateLabel(
                column,
                "GameTitle",
                UiText.Get(texts, UiTextKey.MainMenu_Game_Title),
                MenuScreenMetrics.GameTitleSize,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            Localize(title, UiTextKey.MainMenu_Game_Title);
            title.font = UiFactory.FindBoldFont();
            var titleElement = title.gameObject.AddComponent<LayoutElement>();
            titleElement.preferredHeight = MenuScreenMetrics.GameTitleHeight;
            titleElement.minHeight = MenuScreenMetrics.GameTitleHeight;
            titleElement.flexibleHeight = 0f;
            titleElement.preferredWidth = MenuScreenMetrics.MenuColumnWidth;
            titleElement.minWidth = MenuScreenMetrics.MenuColumnWidth;
            titleElement.flexibleWidth = 0f;
            var newGame = CreateMenuButton(ui, texts, column, UiTextKey.MainMenu_New_Game);
            var load = CreateMenuButton(ui, texts, column, UiTextKey.MainMenu_Load);
            var settings = CreateMenuButton(ui, texts, column, UiTextKey.MainMenu_Settings);
            var authors = CreateMenuButton(ui, texts, column, UiTextKey.MainMenu_Authors);
            var quit = CreateMenuButton(ui, texts, column, UiTextKey.Common_Quit);

            var version = CreateLabel(
                content,
                "VersionLabel",
                string.Empty,
                18,
                TextAnchor.MiddleRight,
                UiFactory.MutedTextColor);
            Place(
                (RectTransform)version.transform,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-MenuScreenMetrics.VersionInsetRight, MenuScreenMetrics.VersionInsetBottom),
                new Vector2(280f, 26f));

            var screen = root.AddComponent<MainMenuScreen>();
            SetObject(screen, "newGame", newGame);
            SetObject(screen, "load", load);
            SetObject(screen, "settings", settings);
            SetObject(screen, "authors", authors);
            SetObject(screen, "quit", quit);
            SetObject(screen, "version", version);
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"Не найден спрайт меню {path}");
            }

            return sprite;
        }

        private static RectTransform BuildMenuColumn(Transform parent)
        {
            var column = CreateRect(parent, "Buttons");
            Place(
                column,
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f),
                new Vector2(MenuScreenMetrics.MenuButtonInset, 0f),
                new Vector2(MenuScreenMetrics.MenuColumnWidth, 0f));

            var layout = column.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = MenuScreenMetrics.MenuButtonSpacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;

            var fitter = column.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return column;
        }

        private static Button CreateMenuButton(
            UiFactory ui,
            IGameTexts texts,
            Transform parent,
            UiTextKey key)
        {
            var button = CreateLocalizedButton(ui, texts, parent, key, UiFactory.ButtonColor, action: true);
            var label = button.GetComponentInChildren<Text>();
            if (label == null)
            {
                throw new InvalidOperationException("У кнопки стартового меню нет подписи");
            }

            label.fontSize = MenuScreenMetrics.MenuButtonFontSize;
            PlaceMenuButtonLabel(label);

            var element = button.GetComponent<LayoutElement>();
            element.preferredHeight = MenuScreenMetrics.MenuButtonHeight;
            element.minHeight = MenuScreenMetrics.MenuButtonHeight;
            element.flexibleHeight = 0f;
            element.preferredWidth = MenuScreenMetrics.MenuButtonWidth;
            element.minWidth = MenuScreenMetrics.MenuButtonWidth;
            element.flexibleWidth = 0f;
            return button;
        }

        /// <summary>
        /// Ставит подпись по центру металлической таблички кадра, а не всего прямоугольника кнопки:
        /// на кадре табличка смещена вниз, над ней проходят трубы, а справа горит лампа. Доли кадра
        /// общие для всех кнопок-действий — <see cref="ActionButtonMetrics"/>.
        /// </summary>
        /// <param name="label">Подпись кнопки.</param>
        private static void PlaceMenuButtonLabel(Text label) =>
            ActionButtonMetrics.PlaceLabel(label, MenuScreenMetrics.MenuButtonFontSize);
    }
}
