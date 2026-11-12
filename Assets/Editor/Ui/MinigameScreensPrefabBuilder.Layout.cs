#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Minigames;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="MinigameScreensPrefabBuilder"/>: общие элементы разметки и сохранение префабов.</summary>
    public static partial class MinigameScreensPrefabBuilder
    {
        private static GameObject BuildRoot<T>(string name, string spriteId)
            where T : MonoBehaviour, IScreen
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(T));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            ScreenBackdropAssets.Apply(backdrop, spriteId);
            var content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);
            var screen = root.GetComponent<T>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            return root;
        }

        private static GameObject CreateSlot(Transform parent, string name, float x)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = MinigamePalette.CellIdle;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            PlaceTopLeft((RectTransform)go.transform, new Vector2(x, 0f), new Vector2(SlotWidth, SlotHeight));
            var label = CreateText(go.transform, "Label", "·", 36, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 4f);
            return go;
        }

        private static GameObject CreateSymbol(Transform parent, string name, float x)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = UiFactory.ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            PlaceTopLeft((RectTransform)go.transform, new Vector2(x, 0f), new Vector2(SymbolSize, SymbolSize));
            var label = CreateText(go.transform, "Label", string.Empty, 26, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 4f);
            return go;
        }

        private static void CreateHistoryHeader(Transform parent)
        {
            var code = CreateText(parent, "HistoryCodeHeader", "Код", 14, TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(code.rectTransform, new Vector2(16f, -40f), new Vector2(150f, 20f));
            var exact = CreateText(parent, "HistoryExactHeader", "На месте", 14, TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(exact.rectTransform, new Vector2(186f, -40f), new Vector2(140f, 20f));
            var partial = CreateText(parent, "HistoryPartialHeader", "Не там", 14, TextAnchor.MiddleLeft,
                UiFactory.MutedTextColor);
            PlaceTopLeft(partial.rectTransform, new Vector2(336f, -40f), new Vector2(140f, 20f));
        }

        private static GameObject CreateHistoryRow(Transform parent, string name, float y)
        {
            var row = CreateRect(parent, name);
            PlaceTopLeft(row, new Vector2(0f, -y), new Vector2(488f, 36f));
            var code = CreateText(row, "Code", string.Empty, 16, TextAnchor.MiddleLeft, UiFactory.TextColor);
            PlaceTopLeft(code.rectTransform, new Vector2(0f, 0f), new Vector2(170f, 36f));
            var exact = CreateText(row, "Exact", string.Empty, 16, TextAnchor.MiddleLeft, MinigamePalette.Success);
            PlaceTopLeft(exact.rectTransform, new Vector2(186f, 0f), new Vector2(130f, 36f));
            var partial = CreateText(row, "Partial", string.Empty, 16, TextAnchor.MiddleLeft,
                UiFactory.ToastColor);
            PlaceTopLeft(partial.rectTransform, new Vector2(336f, 0f), new Vector2(130f, 36f));
            return row.gameObject;
        }

        private static GameObject CreateCell(Transform parent, string name, int index)
        {
            var row = index / RadarColumns;
            var column = index % RadarColumns;
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = MinigamePalette.CellIdle;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            PlaceTopLeft(
                (RectTransform)go.transform,
                new Vector2(column * (CellSize + CellGap), -row * (CellSize + CellGap)),
                new Vector2(CellSize, CellSize));
            var label = CreateText(go.transform, "Label", "?", 26, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 4f);
            return go;
        }

        private static Image CreateImage(Transform? parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }

            var image = go.GetComponent<Image>();
            image.color = color;
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

        private static Button CreateButton(Transform parent, string name, string text, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = UiFactory.ButtonColor;
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.ColorTint;
            var label = CreateText(go.transform, "Label", text, size, TextAnchor.MiddleCenter, UiFactory.TextColor);
            Stretch(label.rectTransform, 4f);
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
            return backdrop;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
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

        private static void AssignToScene(GameObject lockPrefab, GameObject radarPrefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "lockMinigameScreenPrefab", lockPrefab);
            SetObject(screen, "radarMinigameScreenPrefab", radarPrefab);
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
