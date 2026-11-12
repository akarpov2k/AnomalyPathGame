#nullable enable
using System;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Собирает префаб слоя похода: фон-копии, мир, игрок, фигуры, полоска пути,
    /// еда/вода и кнопки. Вёрстка живёт в префабе, код только заполняет данные.
    /// </summary>
    public static class WalkLanePrefabBuilder
    {
        /// <summary>Путь префаба слоя похода.</summary>
        public const string PrefabPath = "Assets/Prefabs/Screens/WalkLane.prefab";

        private const string ButtonsFolder = "Assets/Art/Buttons";
        private const string PlayerSpritePath = "Assets/Art/Costumes/combat_itm_armor_plate.png";
        private const string PlayerFramesFolder = "Assets/Art/Player";
        private const int PlayerFrameCount = 8;

        /// <summary>Собирает префаб из меню.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Собрать префаб «Поход»")]
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

        /// <summary>Собирает и сохраняет префаб.</summary>
        public static void Build()
        {
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Screens");
            var root = BuildLane();
            SetActive(root, false);
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            if (prefab == null)
            {
                throw new InvalidOperationException("Не удалось сохранить префаб " + PrefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("AnomalyPath: префаб слоя похода собран");
        }

        private static GameObject BuildLane()
        {
            var root = new GameObject("WalkLane", typeof(RectTransform), typeof(WalkLaneView));
            var rootRect = (RectTransform)root.transform;
            Stretch(rootRect);
            var view = root.GetComponent<WalkLaneView>();
            var group = root.AddComponent<CanvasGroup>();
            group.alpha = 1f;

            var font = UiFactory.FindFont();
            var normal = LoadSprite(ButtonsFolder + "/ui_btn_mp_ind_empty.png");
            var green = LoadSprite(ButtonsFolder + "/ui_btn_mp_ind_green.png");
            var ui = new UiFactory(font, normal, green);

            var fallback = CreateImage(rootRect, "Fallback", null, Color.black);
            Stretch(fallback.rectTransform);

            var backdrops = new Image[WalkMetrics.BackdropCount];
            var backdrop = CreateImage(rootRect, "Backdrop", null, Color.white);
            var backdropRect = backdrop.rectTransform;
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = new Vector2(1f, 1f - WalkMetrics.SkyBandHeight);
            backdropRect.offsetMin = new Vector2(-WalkMetrics.BackdropOverscan, 0f);
            backdropRect.offsetMax = new Vector2(WalkMetrics.BackdropOverscan, 0f);
            backdrops[0] = backdrop;

            var skyRoot = CreateRect(rootRect, "WalkSky");
            Stretch(skyRoot);
            var skyView = skyRoot.gameObject.AddComponent<WalkSkyView>();
            var bandRoot = CreateRect(skyRoot, "SkyBand");
            bandRoot.anchorMin = new Vector2(0f, WalkMetrics.SkyBandBottom);
            bandRoot.anchorMax = new Vector2(1f, 1f);
            bandRoot.offsetMin = Vector2.zero;
            bandRoot.offsetMax = Vector2.zero;
            var skyDay = CreateImage(bandRoot, "SkyDay", LoadFrame("Assets/Art/Sky/sky_band_day.png"), Color.white);
            Stretch(skyDay.rectTransform);
            var skyNight = CreateImage(bandRoot, "SkyNight", LoadFrame("Assets/Art/Sky/sky_band_night.png"), new Color(1f, 1f, 1f, 0f));
            Stretch(skyNight.rectTransform);
            var sun = CreateSkyImage(skyRoot, "Sun", "Assets/Art/Sky/sky_sun.png", WalkMetrics.SkySunSize);
            var moon = CreateSkyImage(skyRoot, "Moon", "Assets/Art/Sky/sky_moon.png", WalkMetrics.SkyMoonSize);

            var world = CreateRect(rootRect, "World");
            world.anchorMin = new Vector2(0f, 0f);
            world.anchorMax = new Vector2(0f, 1f);
            world.pivot = new Vector2(0f, 0.5f);
            world.sizeDelta = new Vector2(6720f, 0f);
            world.anchoredPosition = Vector2.zero;

            var backdropParts = new Image[WalkMetrics.BackdropPartCount];
            for (var index = 0; index < backdropParts.Length; index++)
            {
                var part = CreateImage(world, "BackdropPart" + index, null, Color.white);
                var partRect = part.rectTransform;
                partRect.anchorMin = new Vector2(0f, 0f);
                partRect.anchorMax = new Vector2(0f, 1f - WalkMetrics.SkyBandHeight);
                partRect.pivot = new Vector2(0f, 0f);
                partRect.offsetMin = new Vector2(index * 3040f, 0f);
                partRect.offsetMax = new Vector2((index + 1) * 3040f, 0f);
                part.preserveAspect = false;
                part.raycastTarget = false;
                part.gameObject.SetActive(false);
                backdropParts[index] = part;
            }

            var road = CreateImage(world, "Road", null, Color.white);
            var roadRect = road.rectTransform;
            roadRect.anchorMin = new Vector2(0f, 0f);
            roadRect.anchorMax = new Vector2(1f, 0f);
            roadRect.pivot = new Vector2(0.5f, 0f);
            roadRect.sizeDelta = new Vector2(0f, WalkMetrics.RoadHeight);
            roadRect.anchoredPosition = Vector2.zero;
            road.preserveAspect = false;
            road.gameObject.SetActive(false);

            var ground = CreateImage(world, "Ground", null, new Color(0.04f, 0.05f, 0.04f, 0.55f));
            var groundRect = ground.rectTransform;
            groundRect.anchorMin = new Vector2(0f, 0f);
            groundRect.anchorMax = new Vector2(1f, 0f);
            groundRect.pivot = new Vector2(0.5f, 0f);
            groundRect.sizeDelta = new Vector2(0f, WalkMetrics.GroundHeight);
            groundRect.anchoredPosition = Vector2.zero;

            var player = CreateRect(rootRect, "Player");
            player.anchorMin = new Vector2(0f, 0f);
            player.anchorMax = new Vector2(0f, 0f);
            player.pivot = new Vector2(0.5f, 0f);
            player.sizeDelta = new Vector2(WalkMetrics.PlayerWidth, WalkMetrics.PlayerHeight);
            player.anchoredPosition = new Vector2(420f, 180f);
            var playerImage = player.gameObject.AddComponent<Image>();
            playerImage.raycastTarget = false;
            playerImage.preserveAspect = true;
            playerImage.sprite = LoadSprite(PlayerSpritePath);
            playerImage.color = playerImage.sprite != null ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
            var playerView = player.gameObject.AddComponent<WalkPlayerView>();

            var tint = CreateImage(rootRect, "WalkTint", null, new Color(0f, 0f, 0f, 0f));
            Stretch(tint.rectTransform);

            // Небо рисуется поверх кадра (чтобы растворяться в нём), но под оттенком и кнопками.
            skyRoot.SetSiblingIndex(tint.transform.GetSiblingIndex());

            var progress = CreateImage(rootRect, "Progress", null, new Color(0f, 0f, 0f, 0.55f));
            progress.gameObject.name = "WalkProgress";
            var progressRect = progress.rectTransform;
            progressRect.anchorMin = new Vector2(0.5f, 1f);
            progressRect.anchorMax = new Vector2(0.5f, 1f);
            progressRect.pivot = new Vector2(0.5f, 1f);
            progressRect.anchoredPosition = new Vector2(0f, -84f);
            progressRect.sizeDelta = new Vector2(WalkMetrics.ProgressWidth, WalkMetrics.ProgressHeight);

            var fill = CreateImage(progressRect, "Fill", null, new Color(0.72f, 0.6f, 0.32f, 1f));
            var fillRect = fill.rectTransform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(0f, 1f);
            fillRect.pivot = new Vector2(0f, 0.5f);
            fillRect.sizeDelta = Vector2.zero;
            fillRect.anchoredPosition = Vector2.zero;

            var progressLabel = ui.CreateText(rootRect, string.Empty, 18, TextAnchor.MiddleRight);
            progressLabel.name = "WalkProgressLabel";
            Place(progressLabel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(430f, -104f), new Vector2(240f, 28f));
            progressLabel.raycastTarget = false;
            progressLabel.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);

            var needs = ui.CreateText(rootRect, string.Empty, 20, TextAnchor.MiddleLeft);
            needs.name = "WalkNeeds";
            Place(needs.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -150f), new Vector2(620f, 28f));
            needs.raycastTarget = false;
            needs.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);

            var forward = CreateButton(ui, rootRect, "WalkForward", new Vector2(-600f, WalkMetrics.ButtonBottom), new Vector2(WalkMetrics.ButtonWidth, WalkMetrics.ButtonHeight));
            var run = CreateButton(ui, rootRect, "WalkRun", new Vector2(-80f, WalkMetrics.ButtonBottom), new Vector2(WalkMetrics.ButtonWidth, WalkMetrics.ButtonHeight));
            var returnToHub = CreateButton(ui, rootRect, "WalkReturn", new Vector2(-340f, WalkMetrics.ButtonBottom), new Vector2(WalkMetrics.ButtonWidth, WalkMetrics.ButtonHeight));
            var interact = CreateButton(ui, rootRect, "WalkInteract", new Vector2(0f, WalkMetrics.InteractBottom), new Vector2(WalkMetrics.InteractWidth, WalkMetrics.ButtonHeight));
            interact.gameObject.SetActive(false);
            var interactLabel = interact.GetComponentInChildren<Text>();
            var up = CreateButton(ui, rootRect, "WalkUp", new Vector2(700f, WalkMetrics.ButtonBottom), new Vector2(WalkMetrics.ButtonWidth, WalkMetrics.ButtonHeight));
            up.gameObject.SetActive(false);
            var upLabel = up.GetComponentInChildren<Text>();

            var branches = new WalkBranchView[WalkMetrics.BranchCount];
            for (var index = 0; index < branches.Length; index++)
            {
                branches[index] = CreateBranch(world, index);
            }

            var figures = new WalkFigureView[WalkMetrics.FigureCount];
            for (var index = 0; index < figures.Length; index++)
            {
                figures[index] = CreateFigure(world, index);
            }

            var walkFrames = LoadFrames("player_walk_");
            var runFrames = LoadFrames("player_run_");
            var idleFrame = LoadFrame(PlayerFramesFolder + "/player_idle.png") ?? LoadSprite(PlayerSpritePath);
            playerImage.sprite = idleFrame;
            playerImage.color = idleFrame != null ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);

            var serialized = new SerializedObject(view);
            Set(serialized, "group", group);
            Set(serialized, "world", world);
            Set(serialized, "fallback", fallback);
            Set(serialized, "ground", groundRect);
            Set(serialized, "road", road);
            Set(serialized, "player", player);
            Set(serialized, "playerView", playerView);
            Set(serialized, "skyView", skyView);
            Set(serialized, "forward", forward);
            Set(serialized, "run", run);
            Set(serialized, "returnToHub", returnToHub);
            Set(serialized, "interact", interact);
            Set(serialized, "interactLabel", interactLabel);
            Set(serialized, "up", up);
            Set(serialized, "upLabel", upLabel);
            Set(serialized, "progressFill", fill);
            Set(serialized, "progressLabel", progressLabel);
            Set(serialized, "needsLabel", needs);
            SetArray(serialized, "backdrops", backdrops);
            SetArray(serialized, "backdropParts", backdropParts);
            SetArray(serialized, "figures", figures);
            SetArray(serialized, "branches", branches);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            var playerSerialized = new SerializedObject(playerView);
            SetObject(playerSerialized, "image", playerImage);
            SetObjects(playerSerialized, "walkFrames", walkFrames);
            SetObjects(playerSerialized, "runFrames", runFrames);
            SetObject(playerSerialized, "idleFrame", idleFrame);
            playerSerialized.ApplyModifiedPropertiesWithoutUndo();

            var skySerialized = new SerializedObject(skyView);
            SetObject(skySerialized, "skyDay", skyDay);
            SetObject(skySerialized, "skyNight", skyNight);
            SetObject(skySerialized, "sun", sun);
            SetObject(skySerialized, "moon", moon);
            SetObject(skySerialized, "tint", tint);
            skySerialized.ApplyModifiedPropertiesWithoutUndo();
            return root;
        }

        private static WalkBranchView CreateBranch(RectTransform world, int index)
        {
            var root = CreateRect(world, "WalkBranch" + index);
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(520f, 700f);
            var branch = root.gameObject.AddComponent<WalkBranchView>();

            var path = CreateImage(root, "Path", null, Color.white);
            var pathRect = path.rectTransform;
            pathRect.anchorMin = new Vector2(0.5f, 0f);
            pathRect.anchorMax = new Vector2(0.5f, 0f);
            pathRect.pivot = new Vector2(0.5f, 0f);
            pathRect.sizeDelta = new Vector2(520f, 600f);
            pathRect.anchoredPosition = Vector2.zero;
            path.preserveAspect = true;

            var objectImage = CreateImage(root, "TransitionObject", null, Color.white);
            var objectRect = objectImage.rectTransform;
            objectRect.anchorMin = new Vector2(0.5f, 0f);
            objectRect.anchorMax = new Vector2(0.5f, 0f);
            objectRect.pivot = new Vector2(0.5f, 0f);
            objectRect.sizeDelta = new Vector2(300f, 300f);
            objectRect.anchoredPosition = Vector2.zero;
            objectImage.preserveAspect = true;
            var objectShadow = objectImage.gameObject.AddComponent<Shadow>();
            objectShadow.effectColor = new Color(0f, 0f, 0f, 0.55f);
            objectShadow.effectDistance = new Vector2(0f, -6f);

            var arrow = CreateText(root, "Arrow", "↑", 44, TextAnchor.MiddleCenter, new Color(0.85f, 0.74f, 0.42f, 1f));
            Place(arrow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 250f), new Vector2(80f, 60f));
            arrow.raycastTarget = false;

            var label = CreateText(root, "Label", string.Empty, 18, TextAnchor.MiddleCenter, Color.white);
            Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 310f), new Vector2(520f, 44f));
            label.raycastTarget = false;
            label.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.9f);

            var serialized = new SerializedObject(branch);
            serialized.FindProperty("path").objectReferenceValue = path;
            serialized.FindProperty("objectImage").objectReferenceValue = objectImage;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.FindProperty("arrow").objectReferenceValue = arrow;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return branch;
        }

        private static WalkFigureView CreateFigure(RectTransform world, int index)
        {
            var root = CreateRect(world, "WalkFigure" + index);
            root.anchorMin = new Vector2(0f, 0f);
            root.anchorMax = new Vector2(0f, 0f);
            root.pivot = new Vector2(0.5f, 0f);
            root.sizeDelta = new Vector2(WalkMetrics.FigureWidth, 300f);
            var figure = root.gameObject.AddComponent<WalkFigureView>();

            var icon = CreateImage(root, "Icon", null, Color.white);
            var iconRect = icon.rectTransform;
            iconRect.anchorMin = new Vector2(0.5f, 0f);
            iconRect.anchorMax = new Vector2(0.5f, 0f);
            iconRect.pivot = new Vector2(0.5f, 0f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(WalkMetrics.FigureWidth, 300f);
            icon.preserveAspect = true;

            var plate = CreateImage(root, "LabelPlate", null, new Color(0f, 0f, 0f, 0.62f));
            var plateRect = plate.rectTransform;
            plateRect.anchorMin = new Vector2(0f, 0f);
            plateRect.anchorMax = new Vector2(1f, 0f);
            plateRect.pivot = new Vector2(0.5f, 0f);
            plateRect.sizeDelta = new Vector2(0f, WalkMetrics.FigureLabelHeight);
            plateRect.anchoredPosition = new Vector2(0f, -WalkMetrics.FigureLabelHeight);

            var label = CreateText(root, "Label", string.Empty, 20, TextAnchor.MiddleCenter, Color.white);
            Place(
                label.rectTransform,
                new Vector2(0f, 0f),
                new Vector2(0.5f, 0f),
                new Vector2(0f, 2f - WalkMetrics.FigureLabelHeight),
                new Vector2(-12f, 42f));
            label.rectTransform.anchorMin = new Vector2(0f, 0f);
            label.rectTransform.anchorMax = new Vector2(1f, 0f);
            label.raycastTarget = false;

            var serialized = new SerializedObject(figure);
            serialized.FindProperty("icon").objectReferenceValue = icon;
            serialized.FindProperty("label").objectReferenceValue = label;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            root.gameObject.SetActive(false);
            return figure;
        }

        private static Button CreateButton(
            UiFactory ui,
            RectTransform parent,
            string name,
            Vector2 position,
            Vector2 size)
        {
            var button = ui.CreateActionButton(parent, string.Empty, null);
            button.name = name;
            Place(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), position, size);
            return button;
        }

        private static Image CreateImage(RectTransform parent, string name, Sprite? sprite, Color color)
        {
            var rect = CreateRect(parent, name);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static Image CreateSkyImage(RectTransform parent, string name, string path, float size)
        {
            var image = CreateImage(parent, name, LoadFrame(path), Color.white);
            var rect = image.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;
            image.preserveAspect = true;
            image.gameObject.SetActive(false);
            return image;
        }

        private static Text CreateText(
            RectTransform parent,
            string name,
            string text,
            int size,
            TextAnchor anchor,
            Color color)
        {
            var rect = CreateRect(parent, name);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = UiFactory.FindFont();
            label.fontSize = size;
            label.text = text;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            return label;
        }

        private static RectTransform CreateRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static void Place(RectTransform rect, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        private static Sprite? LoadSprite(string path) =>
            AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static Sprite? LoadFrame(string path)
        {
            EnsureSpriteImporter(path);
            return LoadSprite(path);
        }

        private static Sprite?[] LoadFrames(string prefix)
        {
            var frames = new Sprite?[PlayerFrameCount];
            for (var index = 0; index < frames.Length; index++)
            {
                frames[index] = LoadFrame(PlayerFramesFolder + "/" + prefix + (index + 1).ToString("00") + ".png");
            }

            return frames;
        }

        private static void EnsureSpriteImporter(string path)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer))
            {
                return;
            }

            var changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (importer.maxTextureSize < 1024)
            {
                importer.maxTextureSize = 1024;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void SetObject(SerializedObject serialized, string field, UnityEngine.Object? value)
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("Нет поля " + field);
            }

            property.objectReferenceValue = value;
        }

        private static void SetObjects(SerializedObject serialized, string field, UnityEngine.Object?[] values)
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("Нет массива " + field);
            }

            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void Set(SerializedObject serialized, string field, UnityEngine.Object? value)
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("У WalkLaneView нет поля " + field);
            }

            property.objectReferenceValue = value;
        }

        private static void SetArray<T>(SerializedObject serialized, string field, T[] values)
            where T : UnityEngine.Object
        {
            var property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException("У WalkLaneView нет массива " + field);
            }

            property.arraySize = values.Length;
            for (var index = 0; index < values.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            }
        }

        private static void SetActive(GameObject root, bool active) => root.SetActive(active);

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent!);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", System.IO.Path.GetFileName(path));
        }
    }
}
