#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб экрана наград по прототипу `.prototypes/награды.penpot`:
    /// слева добыча противника, содержимое тайника или предметы награды за задание,
    /// справа инвентарь игрока с деньгами на плашке окна, внизу «Взять всё»,
    /// в углу крестик с подтверждением, если лут остался. Префаб назначается на
    /// <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static class RewardScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/RewardScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        private const float WindowWidth = 1010f;
        private const float WindowHeight = 900f;
        private const float Edge = 25f;
        private const float TitleTop = 14f;
        private const float TitleHeight = 52f;
        private const float CloseWidth = 84f;
        private const float CloseHeight = 58f;
        private const float PanelsTop = 76f;
        private const float PanelHeight = 620f;
        private const float PlayerPanelHeight = 640f;
        private const float PanelWidth = 450f;
        private const float PanelGap = 60f;
        private const float ColumnRight = Edge + PanelWidth + PanelGap;
        private const float PanelHeaderTop = 56f;
        private const float PanelHeaderHeight = 28f;
        private const float MoneyTop = 92f;
        private const float MoneyHeight = 28f;
        private const float RewardScrollTop = 132f;
        private const float PlayerScrollTop = 96f;
        private const float TakeAllWidth = 340f;
        private const float TakeAllHeight = 170f;
        private const float TakeAllTop = PanelsTop + PanelHeight + 14f;

        /// <summary>Пункт меню: собрать префаб экрана наград и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Награды»")]
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
                RewardScreenScreenshot.CaptureAll();
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
            Debug.Log("AnomalyPath: префаб экрана наград собран");
        }

        private static GameObject BuildScreen()
        {
            var root = BuildRoot(out var content);
            var screen = root.GetComponent<RewardScreen>();
            var window = CreateImage(content, "Window", RewardPalette.Window);
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
            AddLocalized(title, UiTextKey.Rewards_Title);

            var close = CreateButton(window.transform, "RewardClose", string.Empty, 26);
            PlaceTopRight((RectTransform)close.transform, new Vector2(-Edge, -TitleTop), new Vector2(CloseWidth, CloseHeight));
            AddLocalized(close.GetComponentInChildren<Text>(), UiTextKey.Common_Close);

            var rewardGrid = BuildRewardPanel(window.transform, out var sourceLabel, out var moneyLabel);
            var playerGrid = BuildPlayerPanel(window.transform, out var playerMoney, out var playerWeight);

            var takeAll = CreateActionButton(window.transform, "RewardTakeAll", out var takeAllLabel);
            PlaceTopLeft(
                (RectTransform)takeAll.transform,
                new Vector2(Edge + ((PanelWidth - TakeAllWidth) / 2f), -TakeAllTop),
                new Vector2(TakeAllWidth, TakeAllHeight));
            AddLocalized(takeAllLabel, UiTextKey.Rewards_Take_All);

            var toast = CreateText(
                window.transform,
                "RewardToast",
                string.Empty,
                16,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            PlaceTopLeft(
                toast.rectTransform,
                new Vector2(ColumnRight, -(TakeAllTop + 40f)),
                new Vector2(PanelWidth, 60f));

            SetObject(screen, "sourceLabel", sourceLabel);
            SetObject(screen, "moneyLabel", moneyLabel);
            SetObject(screen, "playerMoney", playerMoney);
            SetObject(screen, "playerWeight", playerWeight);
            SetObject(screen, "rewardGrid", rewardGrid);
            SetObject(screen, "playerGrid", playerGrid);
            SetObject(screen, "takeAll", takeAll);
            SetObject(screen, "close", close);
            SetObject(screen, "toast", toast);
            return root;
        }

        private static ScrollArea BuildRewardPanel(Transform parent, out Text header, out Text money)
        {
            var panel = CreateImage(parent, "RewardPanel", RewardPalette.Panel);
            panel.raycastTarget = true;
            PlaceTopLeft(panel.rectTransform, new Vector2(Edge, -PanelsTop), new Vector2(PanelWidth, PanelHeight));
            ApplyPanelTexture(panel, InventoryPanelArt.TraderTexturePath);

            header = CreateText(
                panel.transform,
                "RewardSource",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor,
                bold: true);
            PlaceTopLeft(
                header.rectTransform,
                new Vector2(16f, -PanelHeaderTop),
                new Vector2(PanelWidth - 32f, PanelHeaderHeight));

            money = CreateText(
                panel.transform,
                "RewardMoney",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor);
            PlaceTopLeft(
                money.rectTransform,
                new Vector2(16f, -MoneyTop),
                new Vector2(PanelWidth - 32f, MoneyHeight));

            return CreateScroll(
                panel.transform,
                new Vector2(16f, -RewardScrollTop),
                new Vector2(PanelWidth - 32f, PanelHeight - RewardScrollTop - 16f));
        }

        private static ScrollArea BuildPlayerPanel(Transform parent, out Text money, out Text weight)
        {
            var panel = CreateImage(parent, "PlayerPanel", RewardPalette.Panel);
            panel.raycastTarget = true;
            PlaceTopLeft(panel.rectTransform, new Vector2(ColumnRight, -PanelsTop), new Vector2(PanelWidth, PlayerPanelHeight));
            ApplyPanelTexture(panel, InventoryPanelArt.TexturePath);

            weight = CreateText(
                panel.transform,
                "PlayerWeight",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(weight.rectTransform, new Vector2(16f, -14f), new Vector2(PanelWidth - 32f, 28f));

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
                new Vector2(16f, -PlayerScrollTop),
                new Vector2(PanelWidth - 32f, PlayerPanelHeight - PlayerScrollTop - 16f));
        }

        private static void ApplyPanelTexture(Image panel, string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                return;
            }

            panel.sprite = sprite;
            panel.color = Color.white;
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
            var root = new GameObject("RewardScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);
            var screen = root.AddComponent<RewardScreen>();
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

            label = CreateText(go.transform, "Label", string.Empty, 24, TextAnchor.MiddleCenter, UiFactory.TextColor);
            ActionButtonMetrics.PlaceLabel(label, 24);
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
            ScreenBackdropAssets.Apply(backdrop, ScreenBackdropIds.Reward);
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

            SetObject(screen, "rewardScreenPrefab", prefab);
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
