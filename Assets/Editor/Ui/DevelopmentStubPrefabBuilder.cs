#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб экрана-заглушки «Игра в стадии разработки»: затемнённый задник, окно
    /// с заголовком, текстом «Дальнейший контент будет позже» и кнопкой «Понятно». Префаб
    /// назначается на <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static class DevelopmentStubPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/DevelopmentStubScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        private const float WindowWidth = 900f;
        private const float WindowHeight = 520f;
        private const float CloseWidth = 240f;
        private const float CloseHeight = 140f;

        /// <summary>Пункт меню: собрать префаб заглушки и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Заглушка»")]
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

        /// <summary>Точка входа batch-режима: префаб, привязка к сцене и кадр заглушки.</summary>
        public static void BuildAndCaptureFromBatch()
        {
            try
            {
                Build();
                MenuPrefabsScreenshot.CaptureDevelopmentStub();
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
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var prefab = SavePrefab(BuildScreen(), PrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            Debug.Log("AnomalyPath: префаб экрана-заглушки собран");
        }

        private static GameObject BuildScreen()
        {
            var root = new GameObject("DevelopmentStubScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform, 0f);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            var content = CreateRect(root.transform, "Content");
            Stretch(content, 0f);

            var screen = root.AddComponent<DevelopmentStubScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);

            var window = CreateImage(content, "Window", new Color(0.055f, 0.075f, 0.08f, 0.97f));
            window.raycastTarget = true;
            PlaceCenter(window.rectTransform, new Vector2(WindowWidth, WindowHeight));

            var title = CreateText(
                window.transform,
                "StubTitle",
                string.Empty,
                40,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor,
                bold: true);
            PlaceTopCenter(title.rectTransform, new Vector2(0f, -48f), new Vector2(WindowWidth - 80f, 64f));
            AddLocalized(title, UiTextKey.Development_Stub_Title);

            var message = CreateText(
                window.transform,
                "StubMessage",
                string.Empty,
                26,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            PlaceTopCenter(message.rectTransform, new Vector2(0f, -140f), new Vector2(WindowWidth - 120f, 120f));
            AddLocalized(message, UiTextKey.Development_Stub_Message);

            var close = CreateActionButton(window.transform, "StubClose", out var closeLabel);
            PlaceBottomCenter((RectTransform)close.transform, new Vector2(0f, 52f), new Vector2(CloseWidth, CloseHeight));
            AddLocalized(closeLabel, UiTextKey.Development_Stub_Close);
            SetObject(screen, "close", close);
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

            label = CreateText(go.transform, "Label", string.Empty, 20, TextAnchor.MiddleCenter, UiFactory.TextColor);
            ActionButtonMetrics.PlaceLabel(label, 20);
            return button;
        }

        private static LocalizedText AddLocalized(Text label, UiTextKey key)
        {
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
            ScreenBackdropAssets.Apply(backdrop, ScreenBackdropIds.Base);
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

        private static void PlaceTopCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static void PlaceBottomCenter(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
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

            SetObject(screen, "developmentStubScreenPrefab", prefab);
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
