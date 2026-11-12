#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Talents;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб экрана талантов по прототипу: окно с заголовком по центру, панелью
    /// уровня и свободных очков, деревом карточек по row/column и кнопками «Сохранить» и «Назад».
    /// Префаб назначается на <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static class TalentScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/TalentScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        /// <summary>Пункт меню: собрать префаб талантов и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Таланты»")]
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

        /// <summary>Точка входа batch-режима: префаб, привязка к сцене и кадр экрана.</summary>
        public static void BuildAndCaptureFromBatch()
        {
            try
            {
                Build();
                TalentScreenScreenshot.CaptureAll();
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
            Debug.Log("AnomalyPath: префаб экрана талантов собран");
        }

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "talentScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildScreen()
        {
            var root = BuildRoot(out var content);
            var screen = root.GetComponent<TalentScreen>();
            var window = CreateImage(content, "Window", TalentPalette.Window);
            window.raycastTarget = true;
            PanelArtAssets.ApplyPanel(window);
            PlaceCenter(window.rectTransform, new Vector2(TalentMetrics.WindowWidth, TalentMetrics.WindowHeight));

            var title = CreateText(
                window.transform,
                "Title",
                string.Empty,
                26,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor,
                bold: true);
            PlaceTopCenter(title.rectTransform, new Vector2(0f, -TalentMetrics.TitleTop), new Vector2(TalentMetrics.TitleWidth, TalentMetrics.TitleHeight));
            AddLocalized(title, UiTextKey.Talent_Title);

            var panel = CreateImage(window.transform, "PointsPanel", TalentPalette.Panel);
            PanelArtAssets.ApplyPanel(panel);
            PlaceTopRight(
                panel.rectTransform,
                new Vector2(-TalentMetrics.PanelMargin, -TalentMetrics.PanelMargin),
                new Vector2(TalentMetrics.PanelWidth, TalentMetrics.PanelHeight));

            var level = CreateText(
                panel.transform,
                "TalentLevel",
                string.Empty,
                18,
                TextAnchor.MiddleLeft,
                UiFactory.TextColor,
                bold: true);
            PlaceTopLeft(
                level.rectTransform,
                new Vector2(TalentMetrics.PanelTextInset, -TalentMetrics.PanelTextInset),
                new Vector2(TalentMetrics.PanelTextWidth, TalentMetrics.LevelHeight));

            var points = CreateText(
                panel.transform,
                "TalentPoints",
                string.Empty,
                16,
                TextAnchor.UpperLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(
                points.rectTransform,
                new Vector2(TalentMetrics.PanelTextInset, -TalentMetrics.PointsTop),
                new Vector2(TalentMetrics.PanelTextWidth, TalentMetrics.PointsHeight));

            var tree = CreateScroll(
                window.transform,
                "Tree",
                new Vector2(TalentMetrics.TreeLeft, -TalentMetrics.TreeTop),
                new Vector2(TalentMetrics.TreeWidth, TalentMetrics.TreeHeight),
                TalentMetrics.ScrollBarWidth);
            var node = BuildNodeTemplate(tree.Content);

            var toast = CreateText(
                window.transform,
                "Toast",
                string.Empty,
                18,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            PlaceBottom(
                toast.rectTransform,
                new Vector2(0f, TalentMetrics.ToastBottom),
                new Vector2(TalentMetrics.ToastWidth, TalentMetrics.ToastHeight));

            var save = CreateActionButton(window.transform, "TalentSave", out var saveLabel);
            PlaceBottomLeft(
                (RectTransform)save.transform,
                new Vector2(TalentMetrics.ButtonSide, TalentMetrics.ButtonBottom),
                new Vector2(TalentMetrics.ButtonWidth, TalentMetrics.ButtonHeight));
            AddLocalized(saveLabel, UiTextKey.SaveGame_Save);

            var back = CreateActionButton(window.transform, "TalentBack", out var backLabel);
            PlaceBottomRight(
                (RectTransform)back.transform,
                new Vector2(-TalentMetrics.ButtonSide, TalentMetrics.ButtonBottom),
                new Vector2(TalentMetrics.ButtonWidth, TalentMetrics.ButtonHeight));
            AddLocalized(backLabel, UiTextKey.Common_Back);

            SetObject(screen, "title", title);
            SetObject(screen, "level", level);
            SetObject(screen, "points", points);
            SetObject(screen, "tree", tree);
            SetObject(screen, "nodeTemplate", node);
            SetObject(screen, "save", save);
            SetObject(screen, "back", back);
            SetObject(screen, "toast", toast);
            return root;
        }

        private static TalentNodeView BuildNodeTemplate(RectTransform content)
        {
            var go = new GameObject(
                "NodeTemplate",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(TalentNodeView),
                typeof(TalentHover));
            go.transform.SetParent(content, false);
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(TalentMetrics.NodeWidth, TalentMetrics.NodeHeight);

            var background = go.GetComponent<Image>();
            background.color = TalentPalette.Node;
            background.raycastTarget = true;
            var button = go.GetComponent<Button>();
            button.targetGraphic = background;

            var icon = CreateImage(go.transform, "Icon", TalentPalette.NodeIcon);
            Stretch(icon.rectTransform, TalentMetrics.IconInset);
            icon.preserveAspect = true;

            var plate = CreateImage(go.transform, "CounterPlate", TalentPalette.RankBadge);
            PlaceBottomRight(
                plate.rectTransform,
                new Vector2(-TalentMetrics.CounterInset, TalentMetrics.CounterInset),
                new Vector2(TalentMetrics.CounterWidth, TalentMetrics.CounterHeight));
            var counter = CreateText(
                plate.transform,
                "Counter",
                string.Empty,
                14,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            Stretch(counter.rectTransform, 0f);

            var view = go.GetComponent<TalentNodeView>();
            SetObject(view, "background", background);
            SetObject(view, "icon", icon);
            SetObject(view, "counter", counter);
            SetObject(view, "button", button);
            SetObject(view, "hover", go.GetComponent<TalentHover>());
            go.SetActive(false);
            return view;
        }

        private static GameObject BuildRoot(out RectTransform content)
        {
            var root = new GameObject("TalentScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);
            var screen = root.AddComponent<TalentScreen>();
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

            label = CreateText(go.transform, "Label", name, 18, TextAnchor.MiddleCenter, UiFactory.TextColor);
            ActionButtonMetrics.PlaceLabel(label, 18);
            return button;
        }

        private static LocalizedText AddLocalized(Text label, UiTextKey key)
        {
            var localized = label.gameObject.AddComponent<LocalizedText>();
            localized.Key = UiText.Code(key);
            return localized;
        }

        private static ScrollArea CreateScroll(
            Transform parent,
            string name,
            Vector2 topLeft,
            Vector2 size,
            float barWidth)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(ScrollArea));
            go.transform.SetParent(parent, false);
            PlaceTopLeft((RectTransform)go.transform, topLeft, size);
            var scroll = go.GetComponent<ScrollArea>();
            scroll.Configure(barWidth);
            return scroll;
        }

        private static ScreenBackdrop AddBackdrop(Transform parent)
        {
            var image = CreateImage(parent, "ScreenBackdrop", Color.white);
            Stretch(image.rectTransform, 0f);
            var backdrop = image.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", image);
            ScreenBackdropAssets.Apply(backdrop, ScreenBackdropIds.Talents);
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

        private static void PlaceBottom(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceBottomLeft(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceBottomRight(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
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
            root.SetActive(false);
            ButtonArtAssets.ApplyPlatesToFlatButtons(root);
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
