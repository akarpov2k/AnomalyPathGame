#nullable enable
using System;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Screens.Map;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб экрана карты мира: картинка зоны во весь экран, на ней прозрачный
    /// образец кликабельной вырезки хаба, плашка с двумя способами перехода и крестик справа
    /// сверху. Точки вырезок приходят из данных локаций (<c>worldMap</c>), числа разметки —
    /// здесь и в инспекторе префаба.
    /// </summary>
    public static partial class MapScreenPrefabBuilder
    {
        private const string PrefabPath = "Assets/Prefabs/Screens/MapScreen.prefab";
        private const string ScreensFolder = "Assets/Prefabs/Screens";
        private const string ScenePath = "Assets/Scenes/Main.unity";
        private const string DataRoot = "Assets/Data";
        private const string PicturePath = "Assets/Art/Backdrops/world_map.png";
        private const string CloseIconPath = "Assets/Art/Icons/ui_icon_close.png";

        /// <summary>Размер сцены в единицах канваса: картинка карты вписывается в него.</summary>
        private const float ReferenceWidth = 1920f;

        /// <summary>Высота сцены в единицах канваса.</summary>
        private const float ReferenceHeight = 1080f;

        /// <summary>Пропорция картинки карты: 2752×1536.</summary>
        private const float PictureAspect = 2752f / 1536f;

        private const float CloseInset = 26f;
        private const float CloseSize = 84f;
        private const float CloseIconInset = 14f;

        /// <summary>Ширина маленькой кнопки перехода.</summary>
        private const float TravelButtonWidth = 260f;

        /// <summary>Высота маленькой кнопки перехода: под табличкой ещё помещается строка цены.</summary>
        private const float TravelButtonHeight = 160f;

        /// <summary>Зазор между двумя кнопками пары.</summary>
        private const float TravelButtonSpacing = 10f;

        /// <summary>Поле от края металлической таблички до подписи и цены.</summary>
        private const float TravelLabelPadding = 5f;

        /// <summary>Кегль названия способа перехода на кнопке.</summary>
        private const int TravelModeFontSize = 18;

        /// <summary>Кегль цены перехода на кнопке.</summary>
        private const int TravelPriceFontSize = 10;

        /// <summary>Минимальный кегль цены перехода.</summary>
        private const int TravelPriceFontMin = 8;

        /// <summary>Высота строки цены под табличкой.</summary>
        private const float TravelPriceHeight = 16f;

        /// <summary>Отступ строки цены от нижнего края кнопки.</summary>
        private const float TravelTextLift = 1f;

        private const float ToastHeight = 56f;
        private const int ToastFontSize = 20;
        private const float ToastBottom = 20f;

        /// <summary>Пункт меню: собрать префаб карты мира и привязать его к сцене.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Карта мира»")]
        public static void BuildFromMenu() => Build();

        /// <summary>Точка входа batch-режима: каталог спрайтов и префаб за один запуск.</summary>
        public static void BuildFromBatch()
        {
            try
            {
                AnomalyPath.Editor.Art.SpriteCatalogBuilder.Rebuild();
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Собирает префаб экрана и назначает его в сцене Main.</summary>
        public static void Build()
        {
            var texts = LoadTexts();
            EnsureFolder("Assets/Prefabs");
            EnsureFolder(ScreensFolder);

            var ui = new UiFactory(UiFactory.FindFont(), ButtonArtAssets.Normal(), ButtonArtAssets.Highlighted());
            var prefab = SavePrefab(BuildScreen(ui, texts), PrefabPath);
            AssetDatabase.SaveAssets();
            AssignToScene(prefab);
            Debug.Log("AnomalyPath: префаб экрана карты мира собран");
        }

        private static IGameTexts LoadTexts()
        {
            var result = Preflight.Run(DataRoot);
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            return result.Services.Resolve<IGameTexts>();
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

        private static void AssignToScene(GameObject prefab)
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var screen = UnityEngine.Object.FindAnyObjectByType<AnomalyPath.Runtime.GameScreen>();
            if (screen == null)
            {
                throw new InvalidOperationException($"В сцене {ScenePath} нет GameScreen");
            }

            SetObject(screen, "mapScreenPrefab", prefab);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        private static GameObject BuildScreen(UiFactory ui, IGameTexts texts)
        {
            var root = new GameObject("Map", typeof(RectTransform), typeof(Image));
            Stretch((RectTransform)root.transform);
            root.GetComponent<Image>().color = UiFactory.BackgroundColor;

            var backdrop = AddBackdrop(root.transform);
            var content = CreateRect(root.transform, "Content");
            Stretch(content);

            var stage = CreateRect(content, "Stage");
            stage.anchorMin = new Vector2(0.5f, 0.5f);
            stage.anchorMax = new Vector2(0.5f, 0.5f);
            stage.pivot = new Vector2(0.5f, 0.5f);
            stage.anchoredPosition = Vector2.zero;
            stage.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
            var fitter = stage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = PictureAspect;

            var picture = CreateImage(stage, "Picture", Color.white);
            Stretch(picture.rectTransform);
            var pictureSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PicturePath);
            if (pictureSprite != null)
            {
                picture.sprite = pictureSprite;
            }
            else
            {
                Debug.LogWarning($"AnomalyPath: нет картинки карты {PicturePath}");
            }

            var spots = CreateRect(stage, "Spots");
            Stretch(spots);
            var spotTemplate = BuildSpotTemplate(spots);

            var panel = BuildTravelPanel(ui, texts, stage, out var walk, out var walkPrice, out var guide, out var guidePrice);
            panel.SetActive(false);

            var toast = CreateText(
                stage,
                "Toast",
                string.Empty,
                ToastFontSize,
                TextAnchor.MiddleCenter,
                UiFactory.ToastColor);
            Place(
                toast.rectTransform,
                new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, ToastBottom),
                new Vector2(1000f, ToastHeight));
            var shadow = toast.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);
            toast.gameObject.SetActive(false);

            var close = BuildClose(content);

            var screen = root.AddComponent<MapScreen>();
            SetObject(screen, "backdrop", backdrop);
            SetObject(screen, "content", content);
            SetObject(screen, "spots", spots);
            SetObject(screen, "spotTemplate", spotTemplate);
            SetObject(screen, "close", close);
            SetObject(screen, "travelPanel", panel);
            SetObject(screen, "walk", walk);
            SetObject(screen, "walkPrice", walkPrice);
            SetObject(screen, "guide", guide);
            SetObject(screen, "guidePrice", guidePrice);
            SetObject(screen, "toast", toast);
            return root;
        }

        /// <summary>Прозрачная вырезка хаба: кнопка на всей области и подсветка при наведении.</summary>
        private static Button BuildSpotTemplate(RectTransform spots)
        {
            var go = new GameObject("SpotTemplate", typeof(RectTransform), typeof(Image), typeof(Button), typeof(MapSpot));
            go.transform.SetParent(spots, false);

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            var glow = CreateImage((RectTransform)go.transform, "Glow", MapPalette.ZoneHighlight);
            Stretch(glow.rectTransform);
            glow.gameObject.SetActive(false);

            var spot = go.GetComponent<MapSpot>();
            SetObject(spot, "highlight", glow);
            go.SetActive(false);
            return button;
        }

        /// <summary>
        /// Пара маленьких кнопок перехода: прозрачный контейнер без фона, кнопки стоят одна
        /// над другой и в игре встают рядом с выбранной вырезкой.
        /// </summary>
        private static GameObject BuildTravelPanel(
            UiFactory ui,
            IGameTexts texts,
            RectTransform stage,
            out Button walk,
            out Text walkPrice,
            out Button guide,
            out Text guidePrice)
        {
            var panel = CreateRect(stage, "TravelPanel");
            Place(
                panel,
                Vector2.zero,
                Vector2.zero,
                Vector2.zero,
                new Vector2(TravelButtonWidth, (TravelButtonHeight * 2f) + TravelButtonSpacing));

            walk = BuildTravelButton(ui, texts, panel, UiTextKey.Map_Walk, "Walk", TravelButtonHeight + TravelButtonSpacing, out walkPrice);
            guide = BuildTravelButton(ui, texts, panel, UiTextKey.Map_Guide, "Guide", 0f, out guidePrice);
            return panel.gameObject;
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
            Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.font = UiFactory.FindFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private static ScreenBackdrop AddBackdrop(Transform parent)
        {
            var image = CreateImage(parent, "ScreenBackdrop", Color.white);
            Stretch(image.rectTransform);
            var backdrop = image.gameObject.AddComponent<ScreenBackdrop>();
            SetObject(backdrop, "image", image);
            return backdrop;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(
            RectTransform rect,
            Vector2 anchor,
            Vector2 pivot,
            Vector2 position,
            Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
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

        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException($"Нет поля {field} у {target.GetType().Name}");
            }

            property.stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
