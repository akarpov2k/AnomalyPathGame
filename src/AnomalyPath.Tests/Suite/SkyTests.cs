#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Rules;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Кривая неба: ключевые кадры, положение светила по часам и флаг внутренности.</summary>
    public sealed class SkyTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов неба.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public SkyTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            DataTests();
            CurveTests();
            ToneTests();
            ParsingTests();
            InteriorTests();
        }

        private void DataTests()
        {
            Check("небо: кривая из данных", () =>
            {
                var rules = BuildServices().Resolve<IGameRulesRepository>().Require();
                var sky = rules.Sky;
                Equal(9, sky.Keyframes.Count, "ключевых кадров");
                True(Math.Abs(sky.Keyframes[0].Hour) < 1e-9, "первый час");
                True(Math.Abs(sky.Keyframes[8].Hour - 24) < 1e-9, "последний час");
                True(Math.Abs(sky.Keyframes[0].TintAlpha - 0.5) < 1e-5, "ночной оттенок");
                True(Math.Abs(sky.Keyframes[4].TintAlpha) < 1e-5, "полдень без оттенка");
                True(Math.Abs(sky.Keyframes[4].Light.R - 1) < 1e-5, "полуденный свет");
                True(Math.Abs(sky.Keyframes[0].Glow - 0.9) < 1e-5, "яркость луны");
            });

            Check("небо: значения по умолчанию при отсутствии блока", () =>
            {
                const string minimal =
                    "{\"id\":\"rules\",\"insuranceChance\":0.15,\"insuranceStep\":0.05,\"baseRows\":4," +
                    "\"tierCount\":4,\"playerActorId\":\"actor_player\",\"gridWidth\":8," +
                    "\"baseCarryWeight\":20,\"needsMax\":100,\"startingMoney\":1000," +
                    "\"tradeDifficulty\":1,\"tradeDifficultyMax\":3,\"tradeDifficultyStep\":0.1," +
                    "\"nicknameMaxLength\":24,\"previewMaxRows\":32,\"maxRows\":32,\"savePageSize\":6," +
                    "\"respawnPercent\":0.3,\"xpLossChance\":0.4,\"xpLossStep\":0.1,\"xpLossMax\":1," +
                    "\"nightAccuracyPenalty\":0.2,\"nightAccuracyPenaltyMax\":0.5," +
                    "\"nightAccuracyPenaltyStep\":0.05,\"mainStoryLine\":\"main\"," +
                    "\"needs\":{\"foodPerHour\":1,\"waterPerHour\":1,\"hungryStaminaCostMultiplier\":1," +
                    "\"hungryApCostBonus\":0,\"thirstDamage\":0}}";
                var rules = GameRulesTemplate.FromJson(JsonParser.Parse(minimal, "rules"), "rules");
                Equal(SkySettings.Default.Keyframes.Count, rules.Sky.Keyframes.Count, "кадров по умолчанию");
            });
        }

        private void CurveTests()
        {
            Check("небо: полдень — солнце высоко, оттенка нет", () =>
            {
                var state = SkyCurve.Evaluate(new WorldClock(1, 12, 6, 22), SkySettings.Default);
                True(!state.Night, "день");
                True(Math.Abs(state.Elevation - 0.92388f) < 1e-3, "высота");
                True(Math.Abs(state.Azimuth - 0.375) < 1e-5, "путь");
                True(Math.Abs(state.TintAlpha) < 1e-5, "оттенок");
            });

            Check("небо: зенит — середина дня", () =>
            {
                var state = SkyCurve.Evaluate(new WorldClock(1, 14, 6, 22), SkySettings.Default);
                True(Math.Abs(state.Elevation - 1) < 1e-5, "высота в зените");
                True(Math.Abs(state.Azimuth - 0.5) < 1e-5, "середина пути");
            });

            Check("небо: восход и закат — светило на горизонте", () =>
            {
                var rise = SkyCurve.Evaluate(new WorldClock(1, 6, 6, 22), SkySettings.Default);
                True(Math.Abs(rise.Elevation) < 1e-5, "высота на восходе");
                True(Math.Abs(rise.Azimuth) < 1e-5, "путь на восходе");
                True(!rise.Night, "восход ещё день");

                var set = SkyCurve.Evaluate(new WorldClock(1, 22, 6, 22), SkySettings.Default);
                True(set.Night, "закат уже ночь");
                True(Math.Abs(set.Elevation) < 1e-5, "высота на закате");
            });

            Check("небо: полночь — луна высоко", () =>
            {
                var state = SkyCurve.Evaluate(new WorldClock(1, 0, 6, 22), SkySettings.Default);
                True(state.Night, "ночь");
                True(Math.Abs(state.Elevation - 0.7071f) < 1e-3, "высота луны");
                True(Math.Abs(state.Azimuth - 0.25f) < 1e-5, "путь луны");
                True(state.TintAlpha >= 0.49f, "ночной оттенок");
            });

            Check("небо: смена дня и ночи без скачка высоты", () =>
            {
                var before = SkyCurve.Evaluate(new WorldClock(1, 21.999, 6, 22), SkySettings.Default);
                var after = SkyCurve.Evaluate(new WorldClock(1, 22.001, 6, 22), SkySettings.Default);
                True(before.Elevation < 0.01f, "перед ночью светило низко");
                True(after.Elevation < 0.01f, "после начала ночи светило низко");
                True(!before.Night && after.Night, "флаг ночи переключился");
            });
        }

        private void ToneTests()
        {
            Check("небо: ключевой час возвращается без смешивания", () =>
            {
                var tone = SkySettings.Default.ToneAt(6.5);
                True(Math.Abs(tone.TintAlpha) < 1e-5, "рассвет не красит сцену");
                True(Math.Abs(tone.Light.G - 0.65) < 1e-5, "зелёный рассвета");
            });

            Check("небо: между кадрами цвет смешивается", () =>
            {
                var tone = SkySettings.Default.ToneAt(23.25);
                True(Math.Abs(tone.TintAlpha - 0.45) < 1e-3, "сила оттенка между 21 и 24");
                True(Math.Abs(tone.Light.R - 0.69) < 1e-3, "красный между 21 и 24");
            });

            Check("небо: переход через полночь смешивает последний и первый кадр", () =>
            {
                var settings = new SkySettings(new[]
                {
                    new SkyKeyframe(0, new SkyColor(0, 0, 0), new SkyColor(0, 0, 0), 0, 0),
                    new SkyKeyframe(12, new SkyColor(1, 1, 1), new SkyColor(1, 1, 1), 1, 1)
                });
                var tone = settings.ToneAt(18);
                True(Math.Abs(tone.TintAlpha - 0.5) < 1e-5, "половина перехода");
                True(Math.Abs(tone.Light.R - 0.5) < 1e-5, "половина цвета");
            });

            Check("небо: цвет вне 0..1 и один кадр — ошибка", () =>
            {
                Throws<ArgumentOutOfRangeException>(
                    () => new SkyColor(1.5f, 0, 0),
                    "канал вне диапазона");
                Throws<ArgumentException>(
                    () => new SkySettings(new[] { new SkyKeyframe(0, new SkyColor(0, 0, 0), new SkyColor(0, 0, 0), 0, 0) }),
                    "один кадр");
                Throws<ArgumentException>(
                    () => new SkySettings(new[]
                    {
                        new SkyKeyframe(12, new SkyColor(0, 0, 0), new SkyColor(0, 0, 0), 0, 0),
                        new SkyKeyframe(6, new SkyColor(0, 0, 0), new SkyColor(0, 0, 0), 0, 0)
                    }),
                    "часы не возрастают");
            });
        }

        private void ParsingTests()
        {
            Check("небо: разбор двух ключевых кадров", () =>
            {
                var settings = SkySettings.FromJson(
                    JsonParser.Parse("{\"keyframes\":[{\"hour\":0,\"light\":[0,0,0],\"tint\":[0,0,0]}," +
                                    "{\"hour\":12,\"light\":[1,1,1],\"tint\":[1,1,1],\"tintAlpha\":0.5,\"glow\":0.25}]}", "rules"),
                    "rules");
                Equal(2, settings.Keyframes.Count, "кадров");
                True(Math.Abs(settings.Keyframes[1].TintAlpha - 0.5) < 1e-5, "tintAlpha");
                True(Math.Abs(settings.Keyframes[1].Glow - 0.25) < 1e-5, "glow");
            });

            Check("небо: битые ключевые кадры — ошибка данных", () =>
            {
                var cases = new[]
                {
                    "{\"keyframes\":[{\"hour\":0,\"light\":[0,0,0],\"tint\":[0,0,0]}]}",
                    "{\"keyframes\":[{\"hour\":12,\"light\":[0,0,0],\"tint\":[0,0,0]}," +
                    "{\"hour\":6,\"light\":[0,0,0],\"tint\":[0,0,0]}]}",
                    "{\"keyframes\":[{\"hour\":0,\"light\":[1.5,0,0],\"tint\":[0,0,0]}," +
                    "{\"hour\":12,\"light\":[0,0,0],\"tint\":[0,0,0]}]}",
                    "{\"keyframes\":[]}"
                };

                foreach (var json in cases)
                {
                    var error = Throws<DataException>(
                        () => SkySettings.FromJson(JsonParser.Parse(json, "rules"), "rules"),
                        json);
                    Equal(CoreErrors.RulesRange, error.Code, "код ошибки");
                }
            });
        }

        private void InteriorTests()
        {
            Check("локации: внутренность помечена флагом interior", () =>
            {
                var locations = BuildServices().Resolve<ILocationRepository>();
                True(locations.Get("loc_building_block_interior").Interior, "панельный дом внутри");
                True(locations.Get("loc_atp_dispatch_interior").Interior, "диспетчерская внутри");
                True(!locations.Get("loc_atp_yard").Interior, "двор АТП снаружи");
                True(!locations.Get("loc_terrain_forest").Interior, "лес снаружи");
            });
        }
    }
}
