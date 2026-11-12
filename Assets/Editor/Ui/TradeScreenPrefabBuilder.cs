#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Trader;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб окна торговли по прототипу `.prototypes/торговля.penpot`:
    /// слева товары торговца, справа инвентарь игрока с деньгами на плашке окна,
    /// между ними выбранный предмет, количество и кнопка «Купить»/«Продать» с итоговой ценой.
    /// Префаб назначается на <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static class TradeScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/TradeScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        private const float WindowWidth = 1400f;
        private const float WindowHeight = 900f;
        private const float Edge = 24f;
        private const float TitleTop = 10f;
        private const float TitleHeight = 56f;
        private const float PanelsTop = 76f;
        private const float PanelHeight = 800f;
        private const float PanelWidth = 430f;
        private const float Gap = 16f;
        private const float ColumnLeft = Edge + PanelWidth + Gap;
        private const float MiddleWidth = WindowWidth - (2f * Edge) - (2f * PanelWidth) - (2f * Gap);
        private const float ColumnRight = Edge + PanelWidth + Gap + MiddleWidth + Gap;
        private const float PanelHeaderTop = 62f;
        private const float PanelHeaderHeight = 28f;
        private const float ScrollTop = 96f;
        private const float CloseWidth = 84f;
        private const float CloseHeight = 58f;

        /// <summary>Пункт меню: собрать префаб окна торговли и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Торговля»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима: префаб и привязка к сцене.</summary>
        public static void BuildFromBatch()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Точка входа batch-режима: префаб, привязка к сцене и кадр окна.</summary>
        public static void BuildAndCaptureFromBatch()
        {
            try
            {
                Build();
                TradeScreenScreenshot.CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает префаб и назначает его на <c>GameScreen</c>.</summary>
        public static void Build()
        {
            AnomalyPath.Editor.Art.SpriteCatalogBuilder.Rebuild();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var prefab = SavePrefab(BuildScreen(), PrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            Debug.Log("AnomalyPath: префаб окна торговли собран");
        }

        private static GameObject BuildScreen()
        {
            var root = BuildRoot(out var content);
            var screen = root.GetComponent<TradeScreen>();
            var window = CreateImage(content, "Window", TradePalette.Window);
            window.raycastTarget = true;
            PlaceCenter(window.rectTransform, new Vector2(WindowWidth, WindowHeight));

            var title = CreateText(
                window.transform,
                "Title",
                string.Empty,
                26,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor,
                bold: true);
            PlaceTopCenter(title.rectTransform, new Vector2(0f, -TitleTop), new Vector2(WindowWidth - 300f, TitleHeight));

            var close = CreateButton(window.transform, "TradeClose", string.Empty, 26);
            PlaceTopRight((RectTransform)close.transform, new Vector2(-Edge, -TitleTop), new Vector2(CloseWidth, CloseHeight));
            AddLocalized(close.GetComponentInChildren<Text>(), UiTextKey.Common_Close);

            var traderGrid = BuildTraderPanel(window.transform, out _);
            var playerGrid = BuildPlayerPanel(window.transform, out var money);

            var preview = CreateImage(window.transform, "SelectedPreview", TradePalette.Preview);
            PlaceTopLeft(preview.rectTransform, new Vector2(ColumnLeft, -PanelsTop), new Vector2(MiddleWidth, 210f));
            var selectedIcon = CreateImage(preview.transform, "SelectedIcon", Color.white);
            selectedIcon.preserveAspect = true;
            selectedIcon.enabled = false;
            PlaceTopLeft(selectedIcon.rectTransform, new Vector2(10f, -10f), new Vector2(MiddleWidth - 20f, 190f));

            var selectedName = CreateText(
                window.transform,
                "SelectedName",
                string.Empty,
                22,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            PlaceTopLeft(selectedName.rectTransform, new Vector2(ColumnLeft, -(PanelsTop + 216f)), new Vector2(MiddleWidth, 42f));

            var selectedPrice = CreateText(
                window.transform,
                "SelectedPrice",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            PlaceTopLeft(selectedPrice.rectTransform, new Vector2(ColumnLeft, -(PanelsTop + 258f)), new Vector2(MiddleWidth, 30f));

            var quantityLabel = CreateText(
                window.transform,
                "QuantityLabel",
                string.Empty,
                15,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            PlaceTopLeft(quantityLabel.rectTransform, new Vector2(ColumnLeft, -(PanelsTop + 306f)), new Vector2(MiddleWidth, 26f));
            AddLocalized(quantityLabel, UiTextKey.Trade_Quantity);

            var stepLeft = ColumnLeft + ((MiddleWidth - 336f) / 2f);
            var minus = CreateButton(window.transform, "TradeMinus", "−", 26);
            PlaceTopLeft((RectTransform)minus.transform, new Vector2(stepLeft, -(PanelsTop + 338f)), new Vector2(56f, 56f));

            var quantity = CreateText(
                window.transform,
                "Quantity",
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            PlaceTopLeft(quantity.rectTransform, new Vector2(stepLeft + 64f, -(PanelsTop + 338f)), new Vector2(120f, 56f));

            var plus = CreateButton(window.transform, "TradePlus", "+", 26);
            PlaceTopLeft((RectTransform)plus.transform, new Vector2(stepLeft + 192f, -(PanelsTop + 338f)), new Vector2(56f, 56f));

            var max = CreateButton(window.transform, "TradeMax", string.Empty, 16);
            PlaceTopLeft((RectTransform)max.transform, new Vector2(stepLeft + 256f, -(PanelsTop + 338f)), new Vector2(80f, 56f));
            AddLocalized(max.GetComponentInChildren<Text>(), UiTextKey.Trade_Max);

            var action = CreateActionButton(window.transform, "TradeAction", out var actionLabel);
            PlaceTopLeft((RectTransform)action.transform, new Vector2(ColumnLeft + 60f, -(PanelsTop + 520f)), new Vector2(340f, 170f));

            var toast = CreateText(
                window.transform,
                "TradeToast",
                string.Empty,
                16,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            PlaceTopLeft(toast.rectTransform, new Vector2(ColumnLeft, -(PanelsTop + 706f)), new Vector2(MiddleWidth, 40f));

            SetObject(screen, "title", title);
            SetObject(screen, "close", close);
            SetObject(screen, "traderGrid", traderGrid);
            SetObject(screen, "playerGrid", playerGrid);
            SetObject(screen, "money", money);
            SetObject(screen, "selectedIcon", selectedIcon);
            SetObject(screen, "selectedName", selectedName);
            SetObject(screen, "selectedPrice", selectedPrice);
            SetObject(screen, "quantity", quantity);
            SetObject(screen, "minus", minus);
            SetObject(screen, "plus", plus);
            SetObject(screen, "max", max);
            SetObject(screen, "action", action);
            SetObject(screen, "actionLabel", actionLabel);
            SetObject(screen, "toast", toast);
            return root;
        }

        private static ScrollArea BuildTraderPanel(Transform parent, out Text header)
        {
            var panel = CreateImage(parent, "TraderPanel", TradePalette.Panel);
            panel.raycastTarget = true;
            PlaceTopLeft(panel.rectTransform, new Vector2(Edge, -PanelsTop), new Vector2(PanelWidth, PanelHeight));
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryPanelArt.TraderTexturePath);
            if (sprite != null)
            {
                panel.sprite = sprite;
                panel.color = Color.white;
            }

            header = CreateText(
                panel.transform,
                "TraderHeader",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor,
                bold: true);
            PlaceTopLeft(header.rectTransform, new Vector2(16f, -PanelHeaderTop), new Vector2(PanelWidth - 32f, PanelHeaderHeight));
            AddLocalized(header, UiTextKey.Trade_Trader_Items);

            return CreateScroll(
                panel.transform,
                new Vector2(16f, -ScrollTop),
                new Vector2(PanelWidth - 32f, PanelHeight - ScrollTop - 16f));
        }

        private static ScrollArea BuildPlayerPanel(Transform parent, out Text money)
        {
            var panel = CreateImage(parent, "PlayerPanel", TradePalette.Panel);
            panel.raycastTarget = true;
            PlaceTopLeft(panel.rectTransform, new Vector2(ColumnRight, -PanelsTop), new Vector2(PanelWidth, PanelHeight));
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(InventoryPanelArt.TexturePath);
            if (sprite != null)
            {
                panel.sprite = sprite;
                panel.color = Color.white;
            }

            money = CreateText(
                panel.transform,
                "PlayerMoney",
                string.Empty,
                24,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            var plate = money.rectTransform;
            plate.anchorMin = new Vector2(InventoryPanelArt.CurrencyPlate.xMin, InventoryPanelArt.CurrencyPlate.yMin);
            plate.anchorMax = new Vector2(InventoryPanelArt.CurrencyPlate.xMax, InventoryPanelArt.CurrencyPlate.yMax);
            plate.offsetMin = Vector2.zero;
            plate.offsetMax = Vector2.zero;
            InventoryPanelArt.DecorateMoney(money);

            return CreateScroll(
                panel.transform,
                new Vector2(16f, -96f),
                new Vector2(PanelWidth - 32f, PanelHeight - 112f));
        }

        private static ScrollArea CreateScroll(Transform parent, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject("GridArea", typeof(RectTransform), typeof(ScrollArea));
            go.transform.SetParent(parent, false);
            PlaceTopLeft((RectTransform)go.transform, topLeft, size);
            var scroll = go.GetComponent<ScrollArea>();
            scroll.Configure(12f);
            return scroll;
        }

        private static GameObject BuildRoot(out RectTransform content)
        {
            var root = new GameObject("TradeScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);
            var screen = root.AddComponent<TradeScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static Image CreateImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Text CreateText(
            Transform parent,
            string name,
            string text,
            int size,
            TextAnchor anchor,
            Color color,
            bool bold = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = bold ? UiFactory.FindBoldFont() : UiFactory.FindFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, int fontSize)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = UiFactory.ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;

            var label = CreateText(go.transform, "Label", text, fontSize, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 2f);
            return button;
        }

        private static Button CreateActionButton(Transform parent, string name, out Text label)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(ActionFramePath);
            var highlighted = AssetDatabase.LoadAssetAtPath<Sprite>(ActionFrameGreenPath);
            if (frame != null)
            {
                image.sprite = frame;
                image.color = Color.white;
            }
            else
            {
                image.color = UiFactory.ButtonColor;
            }

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            if (frame != null)
            {
                var art = go.AddComponent<ButtonArt>();
                art.Configure(image, frame, highlighted);
            }

            label = CreateText(go.transform, "Label", string.Empty, 20, TextAnchor.MiddleCenter, UiFactory.TextColor);
            ActionButtonMetrics.PlaceLabel(label, 20);
            return button;
        }

        private static LocalizedText AddLocalized(Text? label, UiTextKey key)
        {
            if (label == null)
            {
                throw new InvalidOperationException("У подписи нет компонента Text");
            }

            var localized = label.gameObject.AddComponent<LocalizedText>();
            localized.Key = UiText.Code(key);
            return localized;
        }

        private static ScreenBackdrop AddBackdrop(Transform parent)
        {
            var image = CreateImage(parent, "ScreenBackdrop", Color.white);
            Stretch(image.rectTransform, 0f);
            var backdrop = image.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", image);
            ScreenBackdropAssets.Apply(backdrop, ScreenBackdropIds.Trade);
            return backdrop;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceTopRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceCenter(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)!.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path)!;
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "tradeScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object? value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"Нет поля {field} у {target.GetType().Name}");
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            ButtonArtAssets.ApplyPlatesToFlatButtons(root);
            root.SetActive(false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException($"Не удалось сохранить префаб {path}");
            }

            return prefab;
        }
    }
}
