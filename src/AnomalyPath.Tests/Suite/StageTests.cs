#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.Stage;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>
    /// Сцена локации: точки земли, масштаб в пикселях на метр, тон, высоты фигур и разбор
    /// блока `stage` с записями спавна.
    /// </summary>
    public sealed class StageTests : TestBase
    {
        /// <inheritdoc />
        public override void Run()
        {
            Check("сцена: высота фигуры в долях картинки считается от ppm", () =>
            {
                var anchor = new StageAnchor(StageSlot.Left, 0.2, 0.15, 160);
                Equal(0.125, anchor.HeightFraction(0.6, 768), "0.6 м при 160 ppm и картинке 768");
            });

            Check("сцена: высота фигуры растёт вместе с ppm", () =>
            {
                var small = new StageAnchor(StageSlot.Left, 0.2, 0.15, 100);
                var large = new StageAnchor(StageSlot.Left, 0.2, 0.15, 200);
                True(
                    large.HeightFraction(1, 800) > small.HeightFraction(1, 800),
                    "при большем ppm фигура выше");
            });

            Check("сцена: нулевая высота фигуры не считается", () =>
            {
                var anchor = new StageAnchor(StageSlot.Left, 0.2, 0.15, 160);
                Throws<ArgumentOutOfRangeException>(
                    () => anchor.HeightFraction(0, 768),
                    "нулевая высота фигуры");
            });

            Check("сцена: нулевая высота картинки не считается", () =>
            {
                var anchor = new StageAnchor(StageSlot.Left, 0.2, 0.15, 160);
                Throws<ArgumentOutOfRangeException>(
                    () => anchor.HeightFraction(1, 0),
                    "нулевая высота картинки");
            });

            Check("сцена: координаты вне 0..1 не принимаются", () =>
            {
                Throws<ArgumentOutOfRangeException>(
                    () => new StageAnchor(StageSlot.Left, 1.2, 0.5, 160),
                    "x больше единицы");
                Throws<ArgumentOutOfRangeException>(
                    () => new StageAnchor(StageSlot.Left, 0.5, -0.1, 160),
                    "y меньше нуля");
            });

            Check("сцена: нулевой ppm не принимается", () =>
            {
                Throws<ArgumentOutOfRangeException>(
                    () => new StageAnchor(StageSlot.Left, 0.5, 0.5, 0),
                    "ppm = 0");
            });

            Check("сцена: неизвестный слот не принимается", () =>
            {
                Throws<ArgumentException>(
                    () => new StageAnchor((StageSlot)99, 0.5, 0.5, 160),
                    "слот вне перечисления");
            });

            Check("сцена: повтор слота не принимается", () =>
            {
                var anchors = new[]
                {
                    new StageAnchor(StageSlot.Left, 0.2, 0.1, 160),
                    new StageAnchor(StageSlot.Left, 0.6, 0.1, 160)
                };
                Throws<ArgumentException>(() => new LocationStage(anchors), "два левых слота");
            });

            Check("сцена: без точек земли не бывает", () =>
            {
                Throws<ArgumentException>(
                    () => new LocationStage(Array.Empty<StageAnchor>()),
                    "пустой список точек");
            });

            Check("сцена: цвет принимается только в формате #rrggbb", () =>
            {
                var anchors = new[] { new StageAnchor(StageSlot.Center, 0.5, 0.2, 150) };
                Throws<ArgumentException>(
                    () => new LocationStage(anchors, "тёплый"),
                    "цвет словом");
                var stage = new LocationStage(anchors, "#F0E6D2");
                Equal("#F0E6D2", stage.Tint, "цвет из данных");
            });

            Check("сцена: яркость и высота тайника должны быть положительными", () =>
            {
                var anchors = new[] { new StageAnchor(StageSlot.Center, 0.5, 0.2, 150) };
                Throws<ArgumentOutOfRangeException>(
                    () => new LocationStage(anchors, null, 0),
                    "нулевая яркость");
                Throws<ArgumentOutOfRangeException>(
                    () => new LocationStage(anchors, null, 1, 0),
                    "нулевая высота тайника");
            });

            Check("сцена: точка ищется по слоту, размеченного слота нет — null", () =>
            {
                var stage = new LocationStage(new[]
                {
                    new StageAnchor(StageSlot.Left, 0.2, 0.1, 160),
                    new StageAnchor(StageSlot.Center, 0.5, 0.2, 150)
                });
                var left = stage.Anchor(StageSlot.Left);
                True(left != null, "левая точка найдена");
                Equal(0.2, left!.X, "x левой точки");
                True(stage.Anchor(StageSlot.Right) == null, "правого слота нет");
            });

            Check("сцена: блок stage читается из данных локации", () =>
            {
                var json = LocationJson(
                    "\"stage\":{\"tint\":\"#f0e6d2\",\"brightness\":0.9,\"stashHeight\":0.7," +
                    "\"anchors\":[{\"slot\":\"left\",\"x\":0.218,\"y\":0.147,\"ppm\":160}," +
                    "{\"slot\":\"right\",\"x\":0.6,\"y\":0.105,\"ppm\":175}," +
                    "{\"slot\":\"center\",\"x\":0.501,\"y\":0.219,\"ppm\":150}]}");
                var template = LocationTemplate.FromJson(JsonParser.Parse(json, "test"), "test");
                True(template.Stage != null, "сцена разобрана");
                Equal(3, template.Stage!.Anchors.Count, "точек земли");
                Equal(0.9, template.Stage.Brightness, "яркость");
                Equal(0.7, template.Stage.StashHeight ?? 0, "высота тайника");
                Equal(175, template.Stage.Anchor(StageSlot.Right)!.PixelsPerMeter, "ppm правой точки");
            });

            Check("сцена: локация без блока stage рисует плашку", () =>
            {
                var template = LocationTemplate.FromJson(JsonParser.Parse(LocationJson(string.Empty), "test"), "test");
                True(template.Stage == null, "сцены нет");
            });

            Check("сцена: пустой массив точек — ошибка данных", () =>
            {
                var json = LocationJson("\"stage\":{\"anchors\":[]}");
                Throws<DataException>(
                    () => LocationTemplate.FromJson(JsonParser.Parse(json, "test"), "test"),
                    "anchors пуст");
            });

            Check("сцена: точка земли без слота — ошибка данных", () =>
            {
                var json = LocationJson("\"stage\":{\"anchors\":[{\"x\":0.5,\"y\":0.5,\"ppm\":160}]}");
                Throws<DataException>(
                    () => LocationTemplate.FromJson(JsonParser.Parse(json, "test"), "test"),
                    "нет slot");
            });

            Check("сцена: повтор слота в данных — ошибка", () =>
            {
                var json = LocationJson(
                    "\"stage\":{\"anchors\":[" +
                    "{\"slot\":\"left\",\"x\":0.2,\"y\":0.1,\"ppm\":160}," +
                    "{\"slot\":\"left\",\"x\":0.4,\"y\":0.1,\"ppm\":160}]}");
                Throws<DataException>(
                    () => LocationTemplate.FromJson(JsonParser.Parse(json, "test"), "test"),
                    "два левых слота");
            });

            Check("сцена: координата точки вне 0..1 — ошибка", () =>
            {
                var json = LocationJson(
                    "\"stage\":{\"anchors\":[{\"slot\":\"left\",\"x\":1.4,\"y\":0.1,\"ppm\":160}]}");
                Throws<DataException>(
                    () => LocationTemplate.FromJson(JsonParser.Parse(json, "test"), "test"),
                    "x больше единицы");
            });

            Check("сцена: цвет локации без решётки — ошибка", () =>
            {
                var json = LocationJson(
                    "\"stage\":{\"tint\":\"f0e6d2\",\"anchors\":[{\"slot\":\"center\",\"x\":0.5,\"y\":0.2,\"ppm\":150}]}");
                Throws<DataException>(
                    () => LocationTemplate.FromJson(JsonParser.Parse(json, "test"), "test"),
                    "цвет без решётки");
            });

            Check("фигура: высота спавна читается из данных", () =>
            {
                var entry = SpawnEntry.FromJson(JsonParser.Parse(SpawnJson("\"height\":0.6"), "test"), "test");
                Equal(0.6, entry.Height ?? 0, "высота фигуры");
            });

            Check("фигура: без height запись рисуется плашкой", () =>
            {
                var entry = SpawnEntry.FromJson(JsonParser.Parse(SpawnJson(string.Empty), "test"), "test");
                True(entry.Height == null, "высоты нет");
            });

            Check("фигура: правый слот берёт зеркальную копию картинки", () =>
            {
                Equal(
                    "icons/obj_road_crate_mirror",
                    StageArt.FigureId("icons/obj_road_crate", StageSlot.Right),
                    "id правого слота");
            });

            Check("фигура: левый и центральный слоты берут исходную вырезку", () =>
            {
                Equal(
                    "icons/obj_road_crate",
                    StageArt.FigureId("icons/obj_road_crate", StageSlot.Left),
                    "id левого слота");
                Equal(
                    "icons/obj_road_crate",
                    StageArt.FigureId("icons/obj_road_crate", StageSlot.Center),
                    "id центрального слота");
            });

            Check("фигура: пустой id картинки не принимается", () =>
            {
                Throws<ArgumentException>(
                    () => StageArt.FigureId(" ", StageSlot.Right),
                    "id из пробелов");
            });
        }

        private static string LocationJson(string extra)
        {
            var suffix = extra.Length == 0 ? string.Empty : "," + extra;
            return "{" +
                   "\"id\":\"loc_x\",\"titleKey\":\"location.x.title\",\"kind\":\"locations\"," +
                   "\"locationKind\":\"wild\",\"subtype\":\"building\",\"tags\":[\"outdoor\"]," +
                   "\"tier\":1,\"backgrounds\":[\"bg_x\"],\"objectCount\":[1,2]," +
                   "\"spawnPool\":[\"spawn_x\"],\"roadArt\":\"loc_road_x\"" +
                   suffix +
                   "}";
        }

        private static string SpawnJson(string extra)
        {
            var suffix = extra.Length == 0 ? string.Empty : "," + extra;
            return "{" +
                   "\"id\":\"spawn_x\",\"titleKey\":\"spawn.x.title\",\"kind\":\"spawns\"," +
                   "\"objectKind\":\"loot\",\"weight\":1,\"lootTable\":\"loot_human\"" +
                   suffix +
                   "}";
        }
    }
}
