#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Часть класса <see cref="HubScreenPrefabBuilder"/>: большой хаб по прототипу — карта,
    /// дверь и доска на стене, внизу значки меню, торговцев, отдыха и инвентаря.
    /// </summary>
    public static partial class HubScreenPrefabBuilder
    {
        private const string BigPrefabPath = "Assets/Prefabs/Screens/BigHubScreen.prefab";

        /// <summary>Картинка интерьера большого хаба.</summary>
        private const string InteriorPath = "Assets/Art/Backgrounds/bg_hub_house.png";

        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Большой хаб»")]
        private static void BuildBigFromMenu() => BuildBig();

        /// <summary>Точка входа batch-режима для большого хаба.</summary>
        public static void BuildBigFromBatch()
        {
            try
            {
                BuildBig();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает префаб большого хаба и назначает его в сцене Main.</summary>
        public static void BuildBig()
        {
            AnomalyPath.Editor.Art.SpriteCatalogBuilder.Rebuild();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var prefab = SavePrefab(BuildBigScreen(), BigPrefabPath);
            AssetDatabase.SaveAssets();
            AssignBigToScene(prefab);
            Debug.Log("AnomalyPath: префаб большого хаба собран");
        }

        private static GameObject BuildBigScreen()
        {
            var icons = LoadIcons();
            var root = new GameObject("BigHub", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;

            var backdrop = AddBackdrop(root.transform);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            // Сцена заполняет весь экран: полос по краям быть не должно.
            var stage = CreateRect(content, "Stage");
            Stretch(stage);

            var interior = CreateImage(stage, "Interior", Color.white);
            Stretch(interior.rectTransform);
            var interiorBackdrop = interior.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(interiorBackdrop, "image", interior);
            SetObject(interiorBackdrop, "sprite", LoadSprite(InteriorPath));

            var tooltip = BuildTooltip(stage);

            var map = BuildZone(
                stage,
                "Map",
                UiText.Code(UiTextKey.Hub_Map),
                0.14608f,
                0.38408f,
                0.38997f,
                0.76302f,
                LoadObjectArt("hub_map"),
                tooltip);
            var door = BuildZone(
                stage,
                "LeaveHub",
                UiText.Code(UiTextKey.Game_Leave_Hub),
                0.38154f,
                0.61156f,
                0.13867f,
                0.8125f,
                LoadObjectArt("hub_door"),
                tooltip);
            var board = BuildZone(
                stage,
                "QuestBoard",
                UiText.Code(UiTextKey.Game_Quest_Board),
                0.625f,
                0.86592f,
                0.38867f,
                0.77995f,
                LoadObjectArt("hub_board"),
                tooltip);

            var gear = BuildIconButton(
                stage,
                "Gear",
                icons.Gear,
                UiText.Code(UiTextKey.GameMenu_Title),
                tooltip,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(IconSide, IconBottom));
            var traders = BuildIconButton(
                stage,
                "Traders",
                icons.Bag,
                UiText.Code(UiTextKey.Trader_Traders),
                tooltip,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(-72f, IconBottom));
            var rest = BuildIconButton(
                stage,
                "Rest",
                icons.Rest,
                UiText.Code(UiTextKey.Game_Rest),
                tooltip,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(72f, IconBottom));
            var inventory = BuildIconButton(
                stage,
                "Backpack",
                icons.Backpack,
                UiText.Code(UiTextKey.Inventory_Open),
                tooltip,
                new Vector2(1f, 0f),
                new Vector2(1f, 0f),
                new Vector2(-IconSide, IconBottom));

            var npc = BuildNpcFigure(stage);
            BuildClock(stage, icons, out var clockHand, out var clockTime);

            var xpBar = CreateImage(stage, "PlayerXpBar", PlayPalette.XpBar);
            var xpBarRect = xpBar.rectTransform;
            xpBarRect.anchorMin = Vector2.zero;
            xpBarRect.anchorMax = new Vector2(1f, 0f);
            xpBarRect.pivot = new Vector2(0.5f, 0f);
            xpBarRect.offsetMin = Vector2.zero;
            xpBarRect.offsetMax = new Vector2(0f, 18f);

            var xpFill = CreateImage(xpBar.transform, "PlayerXpFill", PlayPalette.XpFill);
            var xpFillRect = xpFill.rectTransform;
            xpFillRect.anchorMin = new Vector2(0f, 0f);
            xpFillRect.anchorMax = new Vector2(0f, 1f);
            xpFillRect.pivot = new Vector2(0f, 0.5f);
            xpFillRect.anchoredPosition = Vector2.zero;
            xpFillRect.sizeDelta = Vector2.zero;

            for (var index = 1; index < 10; index++)
            {
                var segment = CreateImage(
                    xpBar.transform,
                    "Segment_" + index,
                    new Color(0f, 0f, 0f, 0.55f));
                var segmentRect = segment.rectTransform;
                segmentRect.anchorMin = new Vector2(index / 10f, 0f);
                segmentRect.anchorMax = new Vector2(index / 10f, 1f);
                segmentRect.pivot = new Vector2(0.5f, 0.5f);
                segmentRect.anchoredPosition = Vector2.zero;
                segmentRect.sizeDelta = new Vector2(1f, 0f);
            }

            var toast = CreateText(stage, "Toast", string.Empty, 20, TextAnchor.MiddleCenter, UiFactory.ToastColor);
            Place(
                toast.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 176f),
                new Vector2(900f, 56f));

            var screen = root.AddComponent<BigHubScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            SetObject(screen, "background", interiorBackdrop);
            SetObject(screen, "clockHand", clockHand);
            SetObject(screen, "clockTime", clockTime);
            SetObject(screen, "map", map);
            SetObject(screen, "door", door);
            SetObject(screen, "board", board);
            SetObject(screen, "gear", gear);
            SetObject(screen, "traders", traders);
            SetObject(screen, "rest", rest);
            SetObject(screen, "inventory", inventory);
            SetObject(screen, "npcFigure", npc.gameObject);
            SetObject(screen, "xpBar", xpBar);
            SetObject(screen, "xpFill", xpFill);
            SetObject(screen, "tooltip", tooltip);
            SetObject(screen, "toast", toast);
            return root;
        }

        /// <summary>
        /// Фигура квестового NPC на большом хабе: эллипс с портретом, именем и подсказкой
        /// действия. Пустая и скрытая, пока на локации нет NPC с доступным диалогом.
        /// </summary>
        /// <param name="stage">Сцена хаба.</param>
        /// <returns>Корень фигуры.</returns>
        private static Image BuildNpcFigure(RectTransform stage)
        {
            var figure = CreateImage(stage, "HubNpc", PlayPalette.Object);
            figure.raycastTarget = true;
            PlaceFraction(figure.rectTransform, 0.032f, 0.22f, 0.152f, 0.44f);
            var button = figure.gameObject.AddComponent<Button>();
            button.targetGraphic = figure;
            button.transition = Selectable.Transition.None;

            var icon = CreateImage(figure.transform, "Icon", Color.white);
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.22f, 0.18f);
            iconRect.anchorMax = new Vector2(0.78f, 0.82f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            icon.preserveAspect = true;

            var title = CreateText(
                figure.transform,
                "Title",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                Color.white);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(8f, 0f);
            titleRect.offsetMax = new Vector2(-8f, -6f);

            var hint = CreateText(
                figure.transform,
                "Hint",
                string.Empty,
                14,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = Vector2.zero;
            hintRect.anchorMax = new Vector2(1f, 0.5f);
            hintRect.offsetMin = new Vector2(8f, 6f);
            hintRect.offsetMax = new Vector2(-8f, 0f);

            figure.gameObject.SetActive(false);
            return figure;
        }

        private static void AssignBigToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "bigHubScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }
    }
}
