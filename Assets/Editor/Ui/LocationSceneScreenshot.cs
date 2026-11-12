#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Editor.Art;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>
    /// Кадры сцены локации: небо, дорога, объекты-переходы, объекты, аномалия и противник
    /// без запуска игры. Собирает сцену кодом в world-space канвасе и рендерит камерой в PNG
    /// (в batch-mode нет Game View, поэтому нужен свой проход рендера).
    /// </summary>
    public static partial class LocationSceneScreenshot
    {
        /// <summary>Папка кадров; в git не попадает.</summary>
        private const string OutputFolder = "Logs/Screenshots";

        /// <summary>Префаб основного экрана: из него берётся сцена для кадра.</summary>
        private const string PrefabPath = "Assets/Prefabs/Screens/PlayScreen.prefab";

        private const int Width = 1920;
        private const int Height = 1080;

        /// <summary>Дорога заставы АТП: кадр показывает собственный фон локации.</summary>
        private const string AtpGateRoad = "loc_road_atp_gate";

        /// <summary>Дорога двора АТП.</summary>
        private const string AtpYardRoad = "loc_road_atp_yard";

        /// <summary>Пункт меню: снимает кадры сцены локации.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншот сцены локации")]
        public static void CaptureMenu() => CaptureAll();

        /// <summary>Точка входа batch-режима.</summary>
        public static void CaptureFromBatch()
        {
            try
            {
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>
        /// Точка входа batch-режима: сначала пересобирает каталог спрайтов,
        /// чтобы новые PNG попали в него, затем снимает кадры сцены.
        /// </summary>
        public static void CaptureAtpFromBatch()
        {
            try
            {
                SpriteCatalogBuilder.Rebuild();
                CaptureAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает день, ночь, три перехода, АТП и кадры новых локаций.</summary>
        public static void CaptureAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(SpriteCatalogBuilder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException($"Нет каталога спрайтов {SpriteCatalogBuilder.CatalogPath}");
            }

            var ui = new UiFactory(UiFactory.FindFont());

            Capture(
                ui,
                catalog,
                "location-scene-day",
                PlaySceneArt.SkyDay,
                PlaySceneArt.DefaultRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_house", "loc_wooden_house"),
                    Transition("edge_swamp", "loc_swamp")
                },
                Figures("icons/obj_car_wreck", "icons/obj_road_crate", "portraits/enemy_bandit"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-night",
                PlaySceneArt.SkyNight,
                PlaySceneArt.DefaultRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_house", "loc_wooden_house"),
                    Transition("edge_swamp", "loc_swamp")
                },
                Figures("icons/anomaly_glow", "icons/obj_field_debris", "portraits/mutant_wolf"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-three",
                PlaySceneArt.SkyDay,
                PlaySceneArt.DefaultRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_block", "loc_ruined_block"),
                    Transition("edge_swamp", "loc_swamp"),
                    Transition("edge_forest", null)
                },
                Figures("icons/obj_forest_deadfall", "icons/obj_stash", "portraits/mutant_boar"),
                2);

            Capture(
                ui,
                catalog,
                "location-scene-atp-gate",
                PlaySceneArt.SkyDay,
                AtpGateRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_atp_yard", "loc_atp_yard")
                },
                Figures("icons/obj_road_crate", null, "portraits/enemy_bandit"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-atp-yard",
                PlaySceneArt.SkyDay,
                AtpYardRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_atp_garage", "loc_atp_garage"),
                    Transition("edge_atp_barracks", "loc_atp_barracks"),
                    Transition("edge_atp_gate", "loc_atp_gate")
                },
                Figures("icons/obj_industrial_crate", "icons/obj_stash", "portraits/enemy_merc"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-atp-dispatch",
                PlaySceneArt.SkyDay,
                AtpYardRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_atp_dispatch", "loc_atp_dispatch")
                },
                Figures("icons/obj_house_table", null, "portraits/enemy_merc"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-two-clean",
                PlaySceneArt.SkyDay,
                PlaySceneArt.DefaultRoad,
                "bg_site_house",
                new[]
                {
                    Transition("edge_house", "loc_wooden_house"),
                    Transition("edge_swamp", "loc_swamp")
                },
                Figures(null, null, null),
                0);

            Capture(
                ui,
                catalog,
                "location-scene-forest-single",
                PlaySceneArt.SkyDay,
                "loc_road_forest",
                "bg_route_forest",
                new[]
                {
                    Transition("edge_route_forest", "loc_route_forest")
                },
                Figures(null, null, null),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-highway-single",
                PlaySceneArt.SkyDay,
                "loc_road_highway",
                "bg_route_highway",
                new[]
                {
                    Transition("edge_terrain_field", "loc_terrain_field")
                },
                Figures(null, null, null),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-house-single",
                PlaySceneArt.SkyDay,
                "loc_road_house",
                "bg_site_house",
                new[]
                {
                    Transition("edge_wooden_house", "loc_wooden_house")
                },
                Figures(null, null, null),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-industrial-single",
                PlaySceneArt.SkyDay,
                "loc_road_industrial",
                "bg_site_house",
                new[]
                {
                    Transition("edge_hangar", "loc_industrial_hangar")
                },
                Figures(null, null, null),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-swamp-single",
                PlaySceneArt.SkyDay,
                "loc_road_swamp",
                "bg_route_forest",
                new[]
                {
                    Transition("edge_swamp", "loc_swamp")
                },
                Figures(null, null, null),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-highway",
                PlaySceneArt.SkyDay,
                "loc_road_highway",
                "bg_route_highway",
                new[]
                {
                    Transition("edge_route_forest", "loc_route_forest"),
                    Transition("edge_terrain_field", "loc_terrain_field")
                },
                Figures("icons/anomaly_glow", "icons/obj_car_wreck", "portraits/enemy_bandit"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-industrial",
                PlaySceneArt.SkyDay,
                "loc_road_industrial",
                "bg_site_house",
                new[]
                {
                    Transition("edge_laboratory", "loc_building_laboratory"),
                    Transition("edge_block", "loc_ruined_block")
                },
                Figures("icons/obj_industrial_crate", "icons/anomaly_chem", "portraits/enemy_merc"),
                -1);

            Capture(
                ui,
                catalog,
                "location-scene-anomaly",
                PlaySceneArt.SkyDay,
                "loc_road_anomaly",
                "bg_site_anomaly",
                new[]
                {
                    Transition("edge_site_anomaly", "loc_site_anomaly")
                },
                Figures("icons/anomaly_crater", "icons/anomaly_glow", "portraits/mutant_boar"),
                -1);
        }

        private static PlayTransition Transition(string id, string? artId) =>
            new PlayTransition(id, artId, () => { });

        /// <summary>Что показать в местах объектов и противника: id картинки из каталога.</summary>
        private static IReadOnlyList<Figure> Figures(string? left, string? right, string? enemy) =>
            new[]
            {
                new Figure("ObjectLeft", left),
                new Figure("ObjectRight", right),
                new Figure("Enemy", enemy)
            };

        private static void Capture(
            UiFactory ui,
            ISpriteCatalog catalog,
            string fileName,
            string sky,
            string road,
            string background,
            IReadOnlyList<PlayTransition> transitions,
            IReadOnlyList<Figure> figures,
            int forwardIndex,
            Action<GameObject, PlayScene>? decorate = null)
        {
            var cameraObject = new GameObject("CaptureCamera", typeof(Camera));
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = Height / 2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.black;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;

            var canvasObject = new GameObject("CaptureCanvas", typeof(Canvas));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var canvasRect = (RectTransform)canvasObject.transform;
            canvasRect.sizeDelta = new Vector2(Width, Height);
            canvasRect.position = Vector3.zero;

            var sceneObject = new GameObject("Screen", typeof(RectTransform));
            var sceneRect = (RectTransform)sceneObject.transform;
            sceneRect.SetParent(canvasRect, false);
            sceneRect.anchorMin = Vector2.zero;
            sceneRect.anchorMax = Vector2.one;
            sceneRect.offsetMin = Vector2.zero;
            sceneRect.offsetMax = Vector2.zero;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Нет префаба " + PrefabPath);
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, sceneRect);
            var instanceRect = (RectTransform)instance.transform;
            instanceRect.anchorMin = Vector2.zero;
            instanceRect.anchorMax = Vector2.one;
            instanceRect.offsetMin = Vector2.zero;
            instanceRect.offsetMax = Vector2.zero;
            instance.SetActive(true);

            var scene = instance.GetComponentInChildren<PlayScene>(true);
            if (scene == null)
            {
                throw new InvalidOperationException("В префабе " + PrefabPath + " нет PlayScene");
            }

            scene.SetSprites(catalog);
            scene.SetRoad(road);
            scene.SetSky(sky);
            scene.SetTransitions(transitions, forwardIndex);
            if (decorate != null)
            {
                decorate(instance, scene);
            }
            else
            {
                ShowFigures(instance, catalog, figures);
            }
            Canvas.ForceUpdateCanvases();

            Render(camera, fileName);

            UnityEngine.Object.DestroyImmediate(instance);
            UnityEngine.Object.DestroyImmediate(sceneObject);
            UnityEngine.Object.DestroyImmediate(canvasObject);
            UnityEngine.Object.DestroyImmediate(cameraObject);
        }

        /// <summary>
        /// Показывает объекты, аномалию и противника так же, как это делает игровой экран:
        /// через <see cref="PlayObject.Show"/>, с картинкой из каталога спрайтов.
        /// </summary>
        /// <param name="instance">Экземпляр префаба основного экрана.</param>
        /// <param name="catalog">Каталог спрайтов.</param>
        /// <param name="figures">Что показать в каждом месте.</param>
        private static void ShowFigures(GameObject instance, ISpriteCatalog catalog,
            IReadOnlyList<Figure> figures)
        {
            foreach (var figure in figures)
            {
                var root = FindNamed(instance.transform, figure.Slot);
                if (root == null)
                {
                    continue;
                }

                Sprite? sprite = null;
                if (figure.ArtId != null)
                {
                    catalog.TryGet(figure.ArtId, out sprite);
                }

                var view = new PlayObject(root.gameObject, figure.Slot);
                view.Show(string.Empty, string.Empty, () => { }, true, figure.Slot, sprite);
            }
        }

        private static Transform? FindNamed(Transform root, string name)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private static void Render(Camera camera, string fileName)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            target.Create();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            var texture = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0f, 0f, Width, Height), 0, 0);
            texture.Apply();
            RenderTexture.active = null;
            camera.targetTexture = null;

            var path = Path.Combine(OutputFolder, fileName + ".png");
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            target.Release();
            UnityEngine.Object.DestroyImmediate(target);
            Debug.Log($"AnomalyPath: кадр сцены локации — {path}");
        }

        /// <summary>Одно место фигуры на сцене: имя слота префаба и id картинки.</summary>
        private readonly struct Figure
        {
            /// <summary>Создаёт описание фигуры.</summary>
            /// <param name="slot">Имя слота в префабе: <c>ObjectLeft</c>, <c>ObjectRight</c>, <c>Enemy</c>.</param>
            /// <param name="artId">Id картинки в каталоге спрайтов; <c>null</c> — только эллипс.</param>
            public Figure(string slot, string? artId)
            {
                Slot = slot;
                ArtId = artId;
            }

            /// <summary>Имя слота в префабе.</summary>
            public string Slot { get; }

            /// <summary>Id картинки в каталоге спрайтов.</summary>
            public string? ArtId { get; }
        }
    }
}
