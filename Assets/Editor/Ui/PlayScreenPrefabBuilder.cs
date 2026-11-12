#nullable enable
using System;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб основного игрового экрана: дорога, небо, часы, места объектов
    /// и противника, нижний ряд и полоса опыта. Вёрстка живёт в префабе, код только
    /// заполняет данные и подписывает кнопки.
    /// </summary>
    public static class PlayScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/PlayScreen.prefab";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string IconsFolder = "Assets/Art/Icons";
        private const string LocationsFolder = "Assets/Art/Locations";
        private const string WalkBackdropPath = "Assets/Art/Prototypes/walk_backdrop.png";
        private const string WalkRoadPath = "Assets/Art/Backdrops/walk_road.png";
        private const string WalkLanePath = "Assets/Prefabs/Screens/WalkLane.prefab";

        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Основной экран»")]
        private static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима.</summary>
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

        /// <summary>Собирает префаб и назначает его основному экрану в сцене Main.</summary>
        public static void Build()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Screens");
            var root = BuildScreen();
            root.SetActive(false);
            ButtonArtAssets.ApplyPlatesToFlatButtons(root);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException("Не удалось сохранить префаб " + PrefabPath);
            }

            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            Debug.Log("AnomalyPath: префаб основного экрана собран");
        }

        private static GameObject BuildScreen()
        {
            var root = new GameObject("PlayScreen", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;
            var screen = root.AddComponent<PlayScreen>();

            var backdropImage = CreateImage(root.transform, "ScreenBackdrop", Color.white);
            Stretch(backdropImage.rectTransform);
            var backdrop = backdropImage.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(screen, "backdrop", backdrop);

            var content = CreateRect(root.transform, "Content");
            Stretch(content);
            SetObject(screen, "content", content);

            var sceneRoot = CreateRect(content, "LocationScene");
            Stretch(sceneRoot);
            var scene = sceneRoot.gameObject.AddComponent<PlayScene>();
            SetObject(screen, "scene", scene);

            var skyBackdrop = CreateImage(sceneRoot, "SceneSkyBackdrop", Color.white);
            Stretch(skyBackdrop.rectTransform);
            SetSprite(skyBackdrop, LocationsFolder + "/loc_sky_day.png");
            var sky = CreateImage(sceneRoot, "SceneSky", Color.white);
            var skyRect = sky.rectTransform;
            skyRect.anchorMin = new Vector2(0f, 1f - PlayMetrics.SkyHeight);
            skyRect.anchorMax = new Vector2(1f, 1f);
            skyRect.offsetMin = Vector2.zero;
            skyRect.offsetMax = Vector2.zero;
            SetSprite(sky, LocationsFolder + "/loc_sky_day.png");

            var road = CreateImage(sceneRoot, "SceneRoad", Color.white);
            Stretch(road.rectTransform);
            SetSprite(road, LocationsFolder + "/loc_road_plain.png");
            var fitter = road.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.enabled = false;

            SetObject(scene, "skyBackdrop", skyBackdrop);
            SetObject(scene, "sky", sky);
            SetObject(scene, "road", road);
            SetObject(scene, "roadFitter", fitter);

            var left = BuildTransition(sceneRoot, "Transition_Left", PlayMetrics.TransitionLeftX1, PlayMetrics.TransitionLeftBottom, PlayMetrics.TransitionLeftX2, PlayMetrics.TransitionLeftTop);
            var center = BuildTransition(sceneRoot, "Transition_Center", PlayMetrics.TransitionCenterX1, PlayMetrics.TransitionCenterBottom, PlayMetrics.TransitionCenterX2, PlayMetrics.TransitionCenterTop);
            var right = BuildTransition(sceneRoot, "Transition_Right", PlayMetrics.TransitionRightX1, PlayMetrics.TransitionRightBottom, PlayMetrics.TransitionRightX2, PlayMetrics.TransitionRightTop);
            SetObject(scene, "leftRoot", left);
            SetObject(scene, "centerRoot", center);
            SetObject(scene, "rightRoot", right);

            BuildClock(content, screen);
            BuildFigure(content, screen, "ObjectLeft", "objectLeftRoot", PlayMetrics.ObjectLeftX1, PlayMetrics.ObjectLeftBottom, PlayMetrics.ObjectLeftX2, PlayMetrics.ObjectLeftTop);
            BuildFigure(content, screen, "ObjectRight", "objectRightRoot", PlayMetrics.ObjectRightX1, PlayMetrics.ObjectRightBottom, PlayMetrics.ObjectRightX2, PlayMetrics.ObjectRightTop);
            BuildFigure(content, screen, "Enemy", "enemyRoot", PlayMetrics.EnemyX1, PlayMetrics.EnemyBottom, PlayMetrics.EnemyX2, PlayMetrics.EnemyTop);

            var gear = BuildIcon(content, "Gear", "ui_hub_gear", PlayMetrics.IconSide, true);
            var rest = BuildIcon(content, "Rest", "ui_hub_rest", PlayMetrics.IconSide + PlayMetrics.IconSize + PlayMetrics.IconGap, true);
            var backpack = BuildIcon(content, "Backpack", "ui_hub_backpack", PlayMetrics.IconSide, false);
            var returnButton = BuildReturnButton(content);
            SetObject(screen, "gear", gear);
            SetObject(screen, "restButton", rest);
            SetObject(screen, "backpack", backpack);
            SetObject(screen, "returnButton", returnButton);

            BuildXpBar(content, screen);
            var walkBackdrop = AssetDatabase.LoadAssetAtPath<Sprite>(WalkBackdropPath);
            if (walkBackdrop != null)
            {
                SetObject(screen, "walkBackdrop", walkBackdrop);
            }

            var walkRoad = AssetDatabase.LoadAssetAtPath<Sprite>(WalkRoadPath);
            if (walkRoad != null)
            {
                SetObject(screen, "walkRoad", walkRoad);
            }

            var lanePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WalkLanePath);
            if (lanePrefab != null)
            {
                var lane = (GameObject)PrefabUtility.InstantiatePrefab(lanePrefab, content);
                lane.name = "WalkLane";
                lane.transform.SetSiblingIndex(sceneRoot.GetSiblingIndex() + 1);
                lane.SetActive(false);
                SetObject(screen, "walkLane", lane.GetComponent<WalkLaneView>());
            }
            else
            {
                Debug.LogWarning("AnomalyPath: нет префаба " + WalkLanePath);
            }

            return root;
        }

        private static void BuildClock(RectTransform parent, PlayScreen screen)
        {
            var clock = CreateRect(parent, "Clock");
            var rect = clock;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(PlayMetrics.SidePadding, -PlayMetrics.ClockTop);
            rect.sizeDelta = new Vector2(
                PlayMetrics.ClockSize + PlayMetrics.ClockTimeGap + PlayMetrics.ClockTimeWidth,
                PlayMetrics.ClockSize);
            SetObject(screen, "clockRoot", clock.gameObject);

            var face = CreateImage(clock, "ClockFace", Color.white);
            var faceRect = face.rectTransform;
            faceRect.anchorMin = new Vector2(0f, 1f);
            faceRect.anchorMax = new Vector2(0f, 1f);
            faceRect.pivot = new Vector2(0f, 1f);
            faceRect.anchoredPosition = Vector2.zero;
            faceRect.sizeDelta = new Vector2(PlayMetrics.ClockSize, PlayMetrics.ClockSize);
            face.preserveAspect = true;
            SetSprite(face, IconsFolder + "/ui_clock_face.png");

            var hand = CreateImage(clock, "ClockHand", Color.white);
            var handRect = hand.rectTransform;
            handRect.anchorMin = new Vector2(0f, 1f);
            handRect.anchorMax = new Vector2(0f, 1f);
            handRect.pivot = new Vector2(0.5f, 0.5f);
            handRect.anchoredPosition = new Vector2(
                PlayMetrics.ClockSize * 0.5f,
                -PlayMetrics.ClockSize * 0.5f);
            handRect.sizeDelta = new Vector2(PlayMetrics.ClockSize, PlayMetrics.ClockSize);
            hand.preserveAspect = true;
            SetSprite(hand, IconsFolder + "/ui_clock_hand.png");

            var time = CreateText(clock, "ClockTime", string.Empty, PlayMetrics.ClockTimeFontSize, TextAnchor.MiddleLeft);
            var timeRect = time.rectTransform;
            timeRect.anchorMin = new Vector2(0f, 1f);
            timeRect.anchorMax = new Vector2(0f, 1f);
            timeRect.pivot = new Vector2(0f, 1f);
            timeRect.anchoredPosition = new Vector2(PlayMetrics.ClockSize + PlayMetrics.ClockTimeGap, 0f);
            timeRect.sizeDelta = new Vector2(PlayMetrics.ClockTimeWidth, PlayMetrics.ClockTimeHeight);
            time.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.75f);
        }

        private static void BuildFigure(
            RectTransform parent,
            PlayScreen screen,
            string name,
            string field,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            // Фигура — прозрачная зона нажатия: подложка-плашка рисовала вокруг объекта
            // тёмный прямоугольник поверх сцены, а он тут не нужен.
            var figure = CreateImage(parent, name, new Color(1f, 1f, 1f, 0f));
            var rect = figure.rectTransform;
            rect.anchorMin = new Vector2(x1, y1);
            rect.anchorMax = new Vector2(x2, y2);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var button = figure.gameObject.AddComponent<Button>();
            button.targetGraphic = figure;
            button.transition = Selectable.Transition.None;
            figure.gameObject.AddComponent<HoverHighlight>();

            var shadow = CreateImage(figure.transform, "Shadow", PlayPalette.SceneShadow);
            var shadowRect = shadow.rectTransform;
            shadowRect.anchorMin = new Vector2(0.06f, 0.02f);
            shadowRect.anchorMax = new Vector2(0.94f, 0.18f);
            shadowRect.offsetMin = Vector2.zero;
            shadowRect.offsetMax = Vector2.zero;
            shadow.raycastTarget = false;
            shadow.gameObject.SetActive(false);

            var glow = CreateImage(figure.transform, "Glow", PlayPalette.SceneGlow);
            var glowRect = glow.rectTransform;
            glowRect.anchorMin = new Vector2(-0.1f, -0.1f);
            glowRect.anchorMax = new Vector2(1.1f, 1.1f);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            glow.raycastTarget = false;
            glow.gameObject.SetActive(false);

            var icon = CreateImage(figure.transform, "Icon", Color.white);
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.22f, 0.18f);
            iconRect.anchorMax = new Vector2(0.78f, 0.82f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            icon.preserveAspect = true;

            var title = CreateText(figure.transform, "Title", string.Empty, PlayMetrics.ObjectTitleFontSize, TextAnchor.MiddleCenter);
            title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(PlayMetrics.ObjectTextPadding, 0f);
            titleRect.offsetMax = new Vector2(-PlayMetrics.ObjectTextPadding, -PlayMetrics.ObjectTitleTop);

            var hint = CreateText(
                figure.transform,
                "Hint",
                string.Empty,
                PlayMetrics.ObjectHintFontSize,
                TextAnchor.MiddleCenter,
                UiFactory.MutedTextColor);
            hint.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
            var hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0.5f);
            hintRect.offsetMin = new Vector2(PlayMetrics.ObjectTextPadding, PlayMetrics.ObjectHintBottom);
            hintRect.offsetMax = new Vector2(-PlayMetrics.ObjectTextPadding, -PlayMetrics.ObjectHintTop);

            SetObject(screen, field, figure.gameObject);
        }

        private static RectTransform BuildTransition(
            RectTransform parent,
            string name,
            float x1,
            float y1,
            float x2,
            float y2)
        {
            var root = CreateRect(parent, name);
            root.anchorMin = new Vector2(x1, y1);
            root.anchorMax = new Vector2(x2, y2);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var glow = CreateImage(root, "TransitionGlow", PlayPalette.TransitionGlow);
            var glowRect = glow.rectTransform;
            glowRect.anchorMin = new Vector2(-PlayMetrics.TransitionGlowPadding, -PlayMetrics.TransitionGlowPadding);
            glowRect.anchorMax = new Vector2(1f + PlayMetrics.TransitionGlowPadding, 1f + PlayMetrics.TransitionGlowPadding);
            glowRect.offsetMin = Vector2.zero;
            glowRect.offsetMax = Vector2.zero;
            glow.preserveAspect = true;
            glow.raycastTarget = false;

            var shadow = CreateImage(root, "TransitionShadow", new Color(0f, 0f, 0f, 0.45f));
            var shadowRect = shadow.rectTransform;
            shadowRect.anchorMin = new Vector2(0.04f, 0.01f);
            shadowRect.anchorMax = new Vector2(0.96f, PlayMetrics.TransitionShadowHeight);
            shadowRect.offsetMin = Vector2.zero;
            shadowRect.offsetMax = Vector2.zero;
            shadow.raycastTarget = false;

            var art = CreateImage(root, "TransitionArt", PlayPalette.TransitionNormal);
            Stretch(art.rectTransform);
            art.preserveAspect = true;
            art.gameObject.AddComponent<Button>();
            art.gameObject.AddComponent<HoverHighlight>();
            return root;
        }

        private static Button BuildIcon(RectTransform parent, string name, string icon, float left, bool leftSide)
        {
            var button = CreateImage(parent, name, Color.white);
            var rect = button.rectTransform;
            rect.anchorMin = leftSide ? new Vector2(0f, 0f) : new Vector2(1f, 0f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = rect.anchorMin;
            rect.anchoredPosition = new Vector2(leftSide ? left : -left, PlayMetrics.IconBottom);
            rect.sizeDelta = new Vector2(PlayMetrics.IconSize, PlayMetrics.IconSize);
            button.preserveAspect = true;
            SetSprite(button, IconsFolder + "/" + icon + ".png");
            var component = button.gameObject.AddComponent<Button>();
            component.targetGraphic = button;
            ButtonArtAssets.ApplyPlateBehind(component);
            return component;
        }

        private static Button BuildReturnButton(RectTransform parent)
        {
            var image = CreateImage(parent, "ReturnToHub", UiFactory.ButtonColor);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, PlayMetrics.IconBottom);
            rect.sizeDelta = new Vector2(160f, 64f);
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            var label = CreateText(image.transform, "Text", string.Empty, 22, TextAnchor.MiddleCenter);
            Stretch(label.rectTransform);
            var localized = label.gameObject.AddComponent<LocalizedText>();
            localized.Key = UiText.Code(UiTextKey.Game_Return_Hub);
            return button;
        }

        private static void BuildXpBar(RectTransform parent, PlayScreen screen)
        {
            var bar = CreateImage(parent, "PlayerXpBar", PlayPalette.XpBar);
            var barRect = bar.rectTransform;
            barRect.anchorMin = Vector2.zero;
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.offsetMin = Vector2.zero;
            barRect.offsetMax = new Vector2(0f, PlayMetrics.XpBarHeight);
            SetObject(screen, "bar", bar);

            var fill = CreateImage(bar.transform, "PlayerXpFill", PlayPalette.XpFill);
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.anchoredPosition = Vector2.zero;
            fillRect.sizeDelta = Vector2.zero;
            SetObject(screen, "fill", fill);

            for (var index = 1; index < PlayMetrics.XpSegments; index++)
            {
                var segment = CreateImage(bar.transform, "Segment_" + index, new Color(0f, 0f, 0f, 0.55f));
                var segmentRect = segment.rectTransform;
                segmentRect.anchorMin = new Vector2(index / (float)PlayMetrics.XpSegments, 0f);
                segmentRect.anchorMax = new Vector2(index / (float)PlayMetrics.XpSegments, 1f);
                segmentRect.pivot = new Vector2(0.5f, 0.5f);
                segmentRect.anchoredPosition = Vector2.zero;
                segmentRect.sizeDelta = new Vector2(1f, 0f);
            }

            var tooltip = CreateText(parent, "XpTooltip", string.Empty, PlayMetrics.XpTooltipFontSize, TextAnchor.MiddleCenter, UiFactory.ToastColor);
            var tooltipRect = tooltip.rectTransform;
            tooltipRect.anchorMin = new Vector2(0.5f, 0f);
            tooltipRect.anchorMax = new Vector2(0.5f, 0f);
            tooltipRect.pivot = new Vector2(0.5f, 0f);
            tooltipRect.anchoredPosition = new Vector2(0f, PlayMetrics.XpTooltipBottom);
            tooltipRect.sizeDelta = new Vector2(PlayMetrics.XpTooltipWidth, PlayMetrics.XpTooltipHeight);
            tooltip.gameObject.SetActive(false);
            SetObject(screen, "tooltip", tooltip);

            var toast = CreateText(parent, "Toast", string.Empty, PlayMetrics.ToastFontSize, TextAnchor.MiddleCenter, UiFactory.ToastColor);
            var toastRect = toast.rectTransform;
            toastRect.anchorMin = new Vector2(0.5f, 0f);
            toastRect.anchorMax = new Vector2(0.5f, 0f);
            toastRect.pivot = new Vector2(0.5f, 0f);
            toastRect.anchoredPosition = new Vector2(0f, PlayMetrics.ToastBottom);
            toastRect.sizeDelta = new Vector2(PlayMetrics.ToastWidth, PlayMetrics.ToastHeight);
            SetObject(screen, "toastText", toast);
        }

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException("В сцене " + ScenePath + " нет GameScreen");
            }

            SetObject(screen, "playScreenPrefab", prefab);
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
                throw new InvalidOperationException("Нет поля " + field + " у " + target.name);
            }

            property.objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            return image;
        }

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor)
            => CreateText(parent, name, value, size, anchor, UiFactory.TextColor);

        private static Text CreateText(Transform parent, string name, string value, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = UiFactory.FindFont();
            text.fontSize = size;
            text.text = value;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static void SetSprite(Image image, string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var name = System.IO.Path.GetFileName(path);
            if (parent != null)
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
