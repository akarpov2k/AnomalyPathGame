#nullable enable
using System;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="DialogPrefabsBuilder"/>: общие элементы префабов.</summary>
    public static partial class DialogPrefabsBuilder
    {
        private static ScrollArea CreateScroll(Transform parent)
        {
            var go = new GameObject("DialogPhrases", typeof(RectTransform), typeof(ScrollArea));
            go.transform.SetParent(parent, false);
            PlaceTopLeft((RectTransform)go.transform,
                DialogMetrics.WindowPadding,
                DialogMetrics.PhrasesTop,
                DialogMetrics.WindowWidth - (2f * DialogMetrics.WindowPadding),
                DialogMetrics.PhrasesHeight);
            var scroll = go.GetComponent<ScrollArea>();
            scroll.Configure(DialogMetrics.ScrollbarWidth);
            return scroll;
        }

        private static Button AddPhraseTemplate(ScrollArea scroll)
        {
            var button = CreateButton(scroll.Content, "DialogPhraseTemplate",
                UiFactory.ButtonColor, DialogMetrics.PhraseFontSize, "Фраза");
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(DialogMetrics.PhrasePadding, -DialogMetrics.PhraseHeight);
            rect.offsetMax = new Vector2(-DialogMetrics.PhrasePadding, 0f);
            button.gameObject.SetActive(false);
            return button;
        }

        private static Image CreateWindow(Transform parent, string name, float height)
        {
            var image = CreateImage(parent, name, Window);
            PanelArtAssets.Apply(image);
            image.raycastTarget = true;
            return image;
        }

        private static Button AddMenuButton(
            Transform parent,
            string name,
            string caption,
            int index,
            UiTextKey key)
        {
            var button = CreateButton(parent, name, UiFactory.ButtonColor, DialogMetrics.MenuFontSize, caption);
            var rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            var top = DialogMetrics.WindowPadding +
                      (index * (DialogMetrics.MenuButtonHeight + DialogMetrics.MenuButtonGap));
            rect.anchoredPosition = new Vector2(0f, -top);
            rect.sizeDelta = new Vector2(DialogMetrics.MenuButtonWidth, DialogMetrics.MenuButtonHeight);
            Localize(button.GetComponentInChildren<Text>(), key);
            return button;
        }

        private static GameObject CreateRoot(string name, Color color, out Button backdrop)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            var image = root.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;
            backdrop = root.AddComponent<Button>();
            backdrop.targetGraphic = image;
            return root;
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

        private static RectTransform CreateColumn(Transform parent, string name, float width)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(width, 0f);
            var layout = go.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            go.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var image = go.AddComponent<Image>();
            PanelArtAssets.Apply(image);
            image.raycastTarget = true;
            return rect;
        }

        private static Transform AddRow(Transform parent, string name, float height, float spacing)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            go.transform.SetParent(parent, false);
            var layout = go.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandHeight = false;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return go.transform;
        }

        private static Button AddButton(Transform parent, string name, Color color, int fontSize, UiTextKey key)
        {
            var button = CreateButton(parent, name, color, fontSize, string.Empty);
            Localize(button.GetComponentInChildren<Text>(), key);
            button.gameObject.name = UiText.Get(_texts, key);
            return button;
        }

        private static Button CreateButton(Transform parent, string name, Color color, int fontSize, string caption)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            go.AddComponent<LayoutElement>().preferredHeight = 54f;
            var label = AddText(go.transform, "Label", caption, fontSize, UiFactory.TextColor, 0f);
            Stretch(label.rectTransform);
            return button;
        }

        private static Text AddText(
            Transform parent,
            string name,
            string text,
            int size,
            Color color,
            float height)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = UiFactory.FindFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            if (height > 0f)
            {
                go.AddComponent<LayoutElement>().preferredHeight = height;
            }

            return label;
        }

        private static Slider AddSlider(Transform parent)
        {
            var go = new GameObject("RestHoursSlider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().preferredHeight = 40f;

            var background = CreateImage(go.transform, "Background", new Color(0.16f, 0.18f, 0.18f, 1f));
            background.raycastTarget = true;
            var backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.5f);
            backgroundRect.anchorMax = new Vector2(1f, 0.5f);
            backgroundRect.offsetMin = new Vector2(0f, -5f);
            backgroundRect.offsetMax = new Vector2(0f, 5f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            // Полоса заливки — тонкая: Slider берёт высоту из якорей области, поэтому
            // область задана полосой, а не точкой в центре.
            fillAreaRect.anchorMin = new Vector2(0f, 0.375f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.625f);
            fillAreaRect.offsetMin = new Vector2(8f, 0f);
            fillAreaRect.offsetMax = new Vector2(-8f, 0f);
            var fill = CreateImage(fillArea.transform, "Fill", Green);
            fill.rectTransform.anchorMin = Vector2.zero;
            fill.rectTransform.anchorMax = Vector2.one;
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);
            var handle = CreateImage(handleArea.transform, "Handle", new Color(0.85f, 0.9f, 0.85f, 1f));
            handle.raycastTarget = true;
            var handleRect = handle.rectTransform;
            // Slider сам двигает ползунок по дорожке: ему нужна точка якоря слева и
            // положительная высота, иначе ползунок вырождается и перетаскивание не ловит клик.
            handleRect.anchorMin = new Vector2(0f, 0.5f);
            handleRect.anchorMax = new Vector2(0f, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(22f, 40f);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.direction = Slider.Direction.LeftToRight;
            slider.wholeNumbers = true;
            return slider;
        }

        private static void Localize(Text? label, UiTextKey key)
        {
            if (label == null)
            {
                throw new InvalidOperationException("У подписи нет компонента Text");
            }

            var localized = label.gameObject.AddComponent<LocalizedText>();
            localized.Key = UiText.Code(key);
        }

        private static void PlaceBottom(RectTransform rect, float bottom, float width, float height)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, bottom);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void PlaceTopLeft(RectTransform rect, float left, float top, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(left, -top);
            rect.sizeDelta = new Vector2(width, height);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static GameObject Save(GameObject root, string path)
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

        private static void AssignToScene(GameObject[] prefabs)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "confirmDialogPrefab", prefabs[0]);
            SetObject(screen, "exitDialogPrefab", prefabs[1]);
            SetObject(screen, "restDialogPrefab", prefabs[2]);
            SetObject(screen, "dialogOverlayPrefab", prefabs[3]);
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

        private static IGameTexts LoadTexts()
        {
            var result = Preflight.Run("Assets/Data");
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            return result.Services.Resolve<IGameTexts>();
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
    }
}
