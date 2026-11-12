#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб экрана авторов: фон, заголовок игры сверху по центру, большое окно
    /// с идущими титрами по центру и кнопку выхода в стартовое меню снизу.
    /// Префаб назначается на <c>GameScreen</c> в сцене Main.
    /// </summary>
    public static class AuthorsScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/AuthorsScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string ActionFramePath = "Assets/Art/Buttons/ui_btn_mp_ind_empty.png";
        private const string ActionFrameGreenPath = "Assets/Art/Buttons/ui_btn_mp_ind_green.png";

        /// <summary>Пункт меню: собрать префаб авторов и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Авторы»")]
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

        /// <summary>Точка входа batch-режима: префаб, привязка к сцене и кадр экрана меню.</summary>
        public static void BuildAndCaptureFromBatch()
        {
            try
            {
                Build();
                MenuPrefabsScreenshot.CaptureAll();
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
            Debug.Log("AnomalyPath: префаб экрана авторов собран");
        }

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "authorsScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildScreen()
        {
            var root = new GameObject("AuthorsScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var backdrop = AddBackdrop(root.transform);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            var title = CreateText(
                content,
                "AuthorsTitle",
                string.Empty,
                AuthorsMetrics.GameTitleSize,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor,
                bold: true);
            PlaceTopCenter(
                title.rectTransform,
                new Vector2(0f, -AuthorsMetrics.GameTitleTop),
                new Vector2(AuthorsMetrics.GameTitleWidth, AuthorsMetrics.GameTitleHeight));
            AddLocalized(title, UiTextKey.MainMenu_Game_Title);

            var window = CreateImage(content, "Window", AuthorsPalette.Window);
            PlaceCenter(window.rectTransform, new Vector2(AuthorsMetrics.WindowWidth, AuthorsMetrics.WindowHeight));
            window.gameObject.AddComponent<RectMask2D>();

            var track = BuildCreditsTrack(window.transform, out var label);
            var scroller = window.gameObject.AddComponent<CreditsScroller>();
            SetObject(scroller, "viewport", window.rectTransform);
            SetObject(scroller, "track", track);
            SetObject(scroller, "label", label);
            SetFloat(scroller, "speed", AuthorsMetrics.CreditsSpeed);

            var back = CreateActionButton(content, "AuthorsBack", out var backLabel);
            PlaceBottomCenter(
                (RectTransform)back.transform,
                new Vector2(0f, AuthorsMetrics.ButtonBottom),
                new Vector2(AuthorsMetrics.ButtonWidth, AuthorsMetrics.ButtonHeight));
            AddLocalized(backLabel, UiTextKey.Authors_Back);

            var screen = root.AddComponent<AuthorsScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            SetObject(screen, "credits", scroller);
            SetObject(screen, "back", back);
            return root;
        }

        /// <summary>Полотно титров внутри окна: сюда двигает текст <see cref="CreditsScroller"/>.</summary>
        private static RectTransform BuildCreditsTrack(Transform parent, out Text label)
        {
            var track = CreateRect(parent, "CreditsTrack");
            track.anchorMin = new Vector2(0f, 0f);
            track.anchorMax = new Vector2(1f, 0f);
            track.pivot = new Vector2(0.5f, 0f);
            track.anchoredPosition = Vector2.zero;
            track.sizeDelta = new Vector2(-2f * AuthorsMetrics.CreditsSideInset, 0f);

            label = CreateText(
                track,
                "Credits",
                string.Empty,
                AuthorsMetrics.CreditsFontSize,
                TextAnchor.UpperCenter,
                AuthorsPalette.Credits);
            label.lineSpacing = AuthorsMetrics.CreditsLineSpacing;
            Stretch(label.rectTransform);
            AddLocalized(label, UiTextKey.Authors_Credits);
            return track;
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

            label = CreateText(go.transform, "Label", name, 22, TextAnchor.MiddleCenter, UiFactory.TextColor);
            ActionButtonMetrics.PlaceLabel(label, 22);
            return button;
        }

        private static void AddLocalized(Text label, UiTextKey key)
        {
            var localized = label.gameObject.AddComponent<LocalizedText>();
            localized.Key = UiText.Code(key);
        }

        private static ScreenBackdrop AddBackdrop(Transform parent)
        {
            var image = CreateImage(parent, "ScreenBackdrop", Color.white);
            Stretch(image.rectTransform);
            var backdrop = image.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", image);
            ScreenBackdropAssets.Apply(backdrop, ScreenBackdropIds.Base);
            return backdrop;
        }

        private static void Stretch(RectTransform rect, float inset = 0f)
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

        private static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"Нет поля {field} у {target.GetType().Name}");
            }

            property.floatValue = value;
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
