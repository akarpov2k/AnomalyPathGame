#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.Stage;
using AnomalyPath.Core.World;
using AnomalyPath.Editor.Art;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEditor;
using UnityEngine;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="LocationSceneScreenshot"/>: кадры сцены для размеченных локаций.</summary>
    public static partial class LocationSceneScreenshot
    {
        /// <summary>Пункт меню: кадры сцены на stage — по одному на каждую размеченную локацию.</summary>
        [MenuItem("Tools/AnomalyPath/UI/Скриншоты сцены на stage")]
        public static void CaptureStageMenu() => CaptureStageAll();

        /// <summary>Точка входа batch-режима: кадры всех локаций с блоком stage.</summary>
        public static void CaptureStageFromBatch()
        {
            try
            {
                SpriteCatalogBuilder.Rebuild();
                CaptureStageAll();
                EditorApplication.Exit(0);
            }
            catch (Exception error)
            {
                Debug.LogException(error);
                EditorApplication.Exit(1);
            }
        }

        /// <summary>Снимает кадры всех размеченных локаций без выхода из редактора.</summary>
        public static void CaptureStages() => CaptureStageAll();

        /// <summary>Снимает кадр каждой локации, у которой размечена сцена.</summary>
        private static void CaptureStageAll()
        {
            Directory.CreateDirectory(OutputFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<SpriteCatalog>(SpriteCatalogBuilder.CatalogPath);
            if (catalog == null)
            {
                throw new InvalidOperationException($"Нет каталога спрайтов {SpriteCatalogBuilder.CatalogPath}");
            }

            var result = Preflight.Run("Assets/Data");
            if (!result.CanStart || result.Services == null)
            {
                throw new InvalidOperationException(result.Report.Summary);
            }

            var services = result.Services;
            var locations = services.Resolve<ILocationRepository>();
            var spawns = services.Resolve<ISpawnRepository>();
            var actors = services.Resolve<IActorRepository>();
            var ui = new UiFactory(UiFactory.FindFont());

            foreach (var template in locations.All)
            {
                if (template.Stage == null)
                {
                    continue;
                }

                Capture(
                    ui,
                    catalog,
                    "location-stage-" + template.Id,
                    PlaySceneArt.SkyDay,
                    template.RoadArt ?? PlaySceneArt.DefaultRoad,
                    "bg_site_house",
                    Array.Empty<PlayTransition>(),
                    Array.Empty<Figure>(),
                    -1,
                    (instance, scene) => ShowStage(instance, scene, catalog, template, spawns, actors));
            }

            Debug.Log("AnomalyPath: кадры сцены на stage сняты");
        }

        /// <summary>Ставит фигуры на сцену так же, как игровой экран: по точкам stage и высотам.</summary>
        private static void ShowStage(
            GameObject instance,
            PlayScene scene,
            ISpriteCatalog catalog,
            LocationTemplate template,
            ISpawnRepository spawns,
            IActorRepository actors)
        {
            var stage = template.Stage;
            if (stage == null)
            {
                return;
            }

            // Плашки префаба по умолчанию видны и без картинки: в кадре остаются белые
            // прямоугольники. Скрываем все три места и показываем только занятые.
            foreach (var name in new[] { "ObjectLeft", "ObjectRight", "Enemy" })
            {
                var empty = FindNamed(instance.transform, name);
                if (empty != null)
                {
                    empty.gameObject.SetActive(false);
                }
            }

            var slots = new List<(string Name, StageSlot Slot, string IconId, double Height)>();
            foreach (var spawnId in template.SpawnPool)
            {
                if (slots.Count >= 2)
                {
                    break;
                }

                if (!spawns.TryGet(spawnId, out var entry) || entry == null ||
                    entry.Icon == null || entry.Height == null ||
                    entry.ObjectKind == LocationObjectKind.Enemy ||
                    entry.ObjectKind == LocationObjectKind.Npc)
                {
                    continue;
                }

                var name = slots.Count == 0 ? "ObjectLeft" : "ObjectRight";
                var slot = slots.Count == 0 ? StageSlot.Left : StageSlot.Right;
                slots.Add((name, slot, entry.Icon, entry.Height.Value));
            }

            foreach (var spawnId in template.SpawnPool)
            {
                if (!spawns.TryGet(spawnId, out var entry) || entry == null ||
                    entry.ObjectKind != LocationObjectKind.Enemy ||
                    entry.ActorId == null || entry.Height == null)
                {
                    continue;
                }

                var actor = actors.Get(entry.ActorId);
                if (actor.Portrait != null && catalog.TryGet(actor.Portrait, out var portrait) && portrait != null)
                {
                    slots.Add(("Enemy", StageSlot.Center, actor.Portrait, entry.Height.Value));
                    break;
                }
            }

            foreach (var item in slots)
            {
                var root = FindNamed(instance.transform, item.Name);
                var anchor = stage.Anchor(item.Slot);
                if (root == null || anchor == null)
                {
                    continue;
                }

                var sprite = StageSprite(catalog, item.IconId, item.Slot);
                if (sprite == null)
                {
                    continue;
                }

                var view = new PlayObject(root.gameObject, item.Name);
                var ground = scene.ToStagePoint(anchor.X, anchor.Y);
                var height = scene.StageHeight(anchor, item.Height);
                var tint = PlayPalette.SceneTint(stage.Tint, stage.Brightness);
                view.ShowScene(sprite, ground, height, tint, () => { }, true, item.Name);
            }
        }

        /// <summary>
        /// Картинка фигуры по слоту: правый слот берёт зеркальную копию, как игровой экран;
        /// копии нет — остаётся исходная вырезка.
        /// </summary>
        private static Sprite? StageSprite(ISpriteCatalog catalog, string iconId, StageSlot slot)
        {
            if (catalog.TryGet(StageArt.FigureId(iconId, slot), out var mirrored) && mirrored != null)
            {
                return mirrored;
            }

            return catalog.TryGet(iconId, out var art) ? art : null;
        }
    }
}
