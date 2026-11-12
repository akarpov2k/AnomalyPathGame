#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using AnomalyPath.Core;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Rules;

namespace AnomalyPath.Tests
{
    /// <summary>Правила прохождения: загрузка, репозиторий, значения и ошибки разбора.</summary>
    public sealed class RulesTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов правил.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public RulesTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            RepositoryTests();
            ParsingTests();
            ValidatorTests();
        }

        private void RepositoryTests()
        {
            Check("правила: загружаются из данных", () =>
            {
                var services = BuildServices();
                var repository = services.Resolve<IGameRulesRepository>();
                var rules = repository.Require();

                Equal("rules", rules.Id, "id");
                Equal(3, rules.BaseRows, "baseRows");
                Equal(5d, rules.OverloadKg, "overloadKg");
                Equal(4, rules.TierCount, "tierCount");
                Equal(8, rules.GridWidth, "gridWidth");
                Equal(20d, rules.BaseCarryWeight, "baseCarryWeight");
                Equal(100, rules.NeedsMax, "needsMax");
                Equal("actor_player", rules.PlayerActorId, "playerActorId");
                True(Math.Abs(rules.InsuranceChance - 0.15) < 1e-9, "insuranceChance");
                True(Math.Abs(rules.InsuranceStep - 0.05) < 1e-9, "insuranceStep");
                True(Math.Abs(rules.TradeDifficulty - 1) < 1e-9, "tradeDifficulty");
                True(Math.Abs(rules.TradeDifficultyMax - 3) < 1e-9, "tradeDifficultyMax");
                True(Math.Abs(rules.TradeDifficultyStep - 0.1) < 1e-9, "tradeDifficultyStep");
                True(Math.Abs(rules.RespawnPercent - 0.3) < 1e-9, "respawnPercent");
                True(Math.Abs(rules.XpLossChance - 0.4) < 1e-9, "xpLossChance");
                True(Math.Abs(rules.XpLossStep - 0.1) < 1e-9, "xpLossStep");
                True(Math.Abs(rules.XpLossMax - 1) < 1e-9, "xpLossMax");
                True(Math.Abs(rules.NightAccuracyPenalty - 0.2) < 1e-9, "nightAccuracyPenalty");
                True(Math.Abs(rules.NightAccuracyPenaltyMax - 0.5) < 1e-9, "nightAccuracyPenaltyMax");
                True(Math.Abs(rules.NightAccuracyPenaltyStep - 0.05) < 1e-9, "nightAccuracyPenaltyStep");
                True(Math.Abs(rules.Needs.FoodPerHour - 2) < 1e-9, "needs.foodPerHour");
                True(Math.Abs(rules.Needs.WaterPerHour - 3) < 1e-9, "needs.waterPerHour");
                True(Math.Abs(rules.Needs.HungryStaminaCostMultiplier - 1.2) < 1e-9, "needs.hungryStaminaCostMultiplier");
                Equal(1, rules.Needs.HungryApCostBonus, "needs.hungryApCostBonus");
                Equal(3, rules.Needs.ThirstDamage, "needs.thirstDamage");
                Equal(24, rules.NicknameMaxLength, "nicknameMaxLength");
                Equal(32, rules.PreviewMaxRows, "previewMaxRows");
                Equal(32, rules.MaxRows, "maxRows");
                Equal(6, rules.SavePageSize, "savePageSize");
                Equal("main", rules.MainStoryLine, "mainStoryLine");
                True(rules.Audio != null, "блок audio");
                Equal("mus-menu-theme", rules.Audio!.MenuMusicId, "audio.menuMusic");
                Equal("mus-combat-light", rules.Audio.CombatMusicId, "audio.combatMusic");
                True(Math.Abs(rules.Audio.AmbienceVolume - 0.55) < 1e-9, "audio.ambienceVolume");
                True(Math.Abs(rules.Audio.MusicVolume - 0.45) < 1e-9, "audio.musicVolume");
                True(Math.Abs(rules.Audio.FadeSeconds - 1.5) < 1e-9, "audio.fadeSeconds");
                True(repository.Rules != null, "Rules не null");
            });

            Check("правила: без файла репозиторий пуст, Require бросает", () =>
            {
                var repository = GameRulesRepository.FromLoadedData(
                    new LoadedData(new Dictionary<string, Dictionary<string, JsonValue>>()));

                True(repository.Rules == null, "правил нет");
                var error = Throws<DataException>(() => repository.Require(), "Require");
                Equal(CoreErrors.RulesMissing, error.Code, "код");
            });

            Check("правила: две записи — ошибка репозитория", () =>
            {
                var records = new Dictionary<string, JsonValue>
                {
                    ["rules"] = JsonParser.Parse(Rules(), "rules"),
                    ["rules2"] = JsonParser.Parse(Rules(), "rules2")
                };
                var data = new LoadedData(new Dictionary<string, Dictionary<string, JsonValue>>
                {
                    ["rules"] = records
                });

                var error = Throws<DataException>(
                    () => GameRulesRepository.FromLoadedData(data),
                    "две записи");
                Equal(CoreErrors.RulesSingleRecord, error.Code, "код");
            });
        }

        private void ParsingTests()
        {
            Check("правила: значения из данных", () =>
            {
                var rules = GameRulesTemplate.FromJson(JsonParser.Parse(Rules(), "rules"), "rules");
                Equal(4, rules.BaseRows, "baseRows");
                Equal(4, rules.TierCount, "tierCount");
                Equal(8, rules.GridWidth, "gridWidth");
                Equal(20d, rules.BaseCarryWeight, "baseCarryWeight");
                Equal(100, rules.NeedsMax, "needsMax");
                Equal("actor_player", rules.PlayerActorId, "playerActorId");
                True(Math.Abs(rules.InsuranceChance - 0.15) < 1e-9, "insuranceChance");
                True(Math.Abs(rules.InsuranceStep - 0.05) < 1e-9, "insuranceStep");
                True(Math.Abs(rules.RespawnPercent - 0.3) < 1e-9, "respawnPercent");
                True(Math.Abs(rules.XpLossChance - 0.4) < 1e-9, "xpLossChance");
                True(Math.Abs(rules.XpLossStep - 0.1) < 1e-9, "xpLossStep");
                True(Math.Abs(rules.XpLossMax - 1) < 1e-9, "xpLossMax");
                True(Math.Abs(rules.NightAccuracyPenalty - 0.2) < 1e-9, "nightAccuracyPenalty");
                True(Math.Abs(rules.NightAccuracyPenaltyMax - 0.5) < 1e-9, "nightAccuracyPenaltyMax");
                True(Math.Abs(rules.NightAccuracyPenaltyStep - 0.05) < 1e-9, "nightAccuracyPenaltyStep");
                True(Math.Abs(rules.Needs.FoodPerHour - 2) < 1e-9, "needs.foodPerHour");
                True(Math.Abs(rules.Needs.WaterPerHour - 3) < 1e-9, "needs.waterPerHour");
                True(Math.Abs(rules.Needs.HungryStaminaCostMultiplier - 1.2) < 1e-9, "needs.hungryStaminaCostMultiplier");
                Equal(1, rules.Needs.HungryApCostBonus, "needs.hungryApCostBonus");
                Equal(3, rules.Needs.ThirstDamage, "needs.thirstDamage");
                Equal(32, rules.MaxRows, "maxRows");
                Equal("rules", rules.Id, "id");
                Equal(1000, rules.StartingMoney, "startingMoney по умолчанию");
                Equal("mus-menu-theme", rules.Audio!.MenuMusicId, "audio.menuMusic");
                Equal("mus-combat-light", rules.Audio.CombatMusicId, "audio.combatMusic");
                True(Math.Abs(rules.Audio.AmbienceVolume - 0.55) < 1e-9, "audio.ambienceVolume");
                True(Math.Abs(rules.Audio.MusicVolume - 0.45) < 1e-9, "audio.musicVolume");
                True(Math.Abs(rules.Audio.FadeSeconds - 1.5) < 1e-9, "audio.fadeSeconds");
            });

            Check("правила: без блока audio музыка не настроена", () =>
            {
                var rules = GameRulesTemplate.FromJson(
                    JsonParser.Parse(WithoutAudio(Rules()), "rules"),
                    "rules");
                True(rules.Audio == null, "блока audio нет");
            });

            Check("правила: основная линия сюжетных квестов из данных", () =>
            {
                var rules = GameRulesTemplate.FromJson(
                    JsonParser.Parse(Rules(mainStoryLine: "line_main"), "rules"),
                    "rules");
                Equal("line_main", rules.MainStoryLine, "mainStoryLine");
            });

            Check("правила: стартовые деньги из данных", () =>
            {
                var rules = GameRulesTemplate.FromJson(
                    JsonParser.Parse(Rules(startingMoney: 2500), "rules"),
                    "rules");
                Equal(2500, rules.StartingMoney, "startingMoney");
            });

            var invalid = new (string Name, string Json, string Code)[]
            {
                ("чужой id", Rules(id: "other"), CoreErrors.RulesId),
                ("baseRows 0", Rules(baseRows: 0), CoreErrors.RulesRange),
                ("maxRows 0", Rules(maxRows: 0), CoreErrors.RulesRange),
                ("maxRows меньше baseRows", Rules(maxRows: 3), CoreErrors.RangeOrder),
                ("tierCount 0", Rules(tierCount: 0), CoreErrors.RulesRange),
                ("gridWidth 0", Rules(gridWidth: 0), CoreErrors.RulesRange),
                ("baseCarryWeight 0", Rules(baseCarryWeight: 0), CoreErrors.PositiveValue),
                ("needsMax 0", Rules(needsMax: 0), CoreErrors.RulesRange),
                ("startingMoney -1", Rules(startingMoney: -1), CoreErrors.RulesRange),
                ("пустой playerActorId", Rules(playerActorId: ""), CoreErrors.MissingStringField),
                ("insuranceChance 1.5", Rules(insuranceChance: 1.5), CoreErrors.RulesChance),
                ("insuranceChance -0.1", Rules(insuranceChance: -0.1), CoreErrors.RulesChance),
                ("insuranceStep 0", Rules(insuranceStep: 0), CoreErrors.PositiveValue),
                ("respawnPercent 0", Rules(respawnPercent: 0), CoreErrors.RulesChance),
                ("respawnPercent 1.5", Rules(respawnPercent: 1.5), CoreErrors.RulesChance),
                ("respawnPercent -0.1", Rules(respawnPercent: -0.1), CoreErrors.RulesChance),
                ("xpLossChance 1.5", Rules(xpLossChance: 1.5), CoreErrors.RulesChance),
                ("xpLossChance -0.1", Rules(xpLossChance: -0.1), CoreErrors.RulesChance),
                ("xpLossStep 0", Rules(xpLossStep: 0), CoreErrors.PositiveValue),
                ("xpLossMax 0", Rules(xpLossMax: 0), CoreErrors.RulesChance),
                ("xpLossMax 1.5", Rules(xpLossMax: 1.5), CoreErrors.RulesChance),
                ("xpLossChance выше максимума", Rules(xpLossChance: 0.8, xpLossMax: 0.5), CoreErrors.RangeOrder),
                ("nightAccuracyPenalty -0.1", Rules(nightAccuracyPenalty: -0.1), CoreErrors.RulesChance),
                ("nightAccuracyPenalty 1.5", Rules(nightAccuracyPenalty: 1.5), CoreErrors.RulesChance),
                ("nightAccuracyPenaltyMax 0", Rules(nightAccuracyPenaltyMax: 0), CoreErrors.RulesChance),
                ("nightAccuracyPenaltyMax 1.5", Rules(nightAccuracyPenaltyMax: 1.5), CoreErrors.RulesChance),
                ("nightAccuracyPenalty выше максимума", Rules(nightAccuracyPenalty: 0.4, nightAccuracyPenaltyMax: 0.3), CoreErrors.RangeOrder),
                ("nightAccuracyPenaltyStep 0", Rules(nightAccuracyPenaltyStep: 0), CoreErrors.PositiveValue),
                ("пустой mainStoryLine", Rules(mainStoryLine: ""), CoreErrors.MissingStringField),
                ("needs.foodPerHour -1", Rules(foodPerHour: -1), CoreErrors.RulesRange),
                ("needs.waterPerHour -1", Rules(waterPerHour: -1), CoreErrors.RulesRange),
                ("needs.hungryStaminaCostMultiplier 0.9", Rules(hungryStaminaCostMultiplier: 0.9), CoreErrors.RulesRange),
                ("needs.hungryApCostBonus -1", Rules(hungryApCostBonus: -1), CoreErrors.RulesRange),
                ("needs.thirstDamage -1", Rules(thirstDamage: -1), CoreErrors.RulesRange),
                ("без блока needs", Rules().Replace(",\"needs\":{\"foodPerHour\":2,\"waterPerHour\":3,\"hungryStaminaCostMultiplier\":1.2,\"hungryApCostBonus\":1,\"thirstDamage\":3}", string.Empty), CoreErrors.MissingArrayField),
                ("audio.ambienceVolume 1.5", Rules(ambienceVolume: 1.5), CoreErrors.RulesChance),
                ("audio.musicVolume -0.1", Rules(musicVolume: -0.1), CoreErrors.RulesChance),
                ("audio.fadeSeconds -1", Rules(fadeSeconds: -1), CoreErrors.NonNegative),
                ("пустой audio.menuMusic", Rules(menuMusic: ""), CoreErrors.MissingStringField),
                ("пустой audio.combatMusic", Rules(combatMusic: ""), CoreErrors.MissingStringField)
            };

            foreach (var item in invalid)
            {
                Check($"правила: {item.Name}", () =>
                {
                    var error = Throws<DataException>(
                        () => GameRulesTemplate.FromJson(JsonParser.Parse(item.Json, "rules"), "rules"),
                        item.Name);
                    Equal(item.Code, error.Code, "код");
                });
            }
        }

        private void ValidatorTests()
        {
            Check("правила: валидатор ловит нулевой шаг страховки", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rules.json"),
                        ["rules.json"] = Rules(insuranceStep: 0)
                    },
                    root =>
                    {
                        var report = Preflight.Validate(root);
                        True(HasIssue(report, "rules.json", "должно быть больше нуля"), "нет ошибки");
                    });
            });
        }

        private static string Rules(
            string id = "rules",
            double insuranceChance = 0.15,
            double insuranceStep = 0.05,
            int baseRows = 4,
            int tierCount = 4,
            string playerActorId = "actor_player",
            int gridWidth = 8,
            int maxRows = 32,
            double baseCarryWeight = 20,
            int needsMax = 100,
            int startingMoney = 1000,
            double respawnPercent = 0.3,
            double xpLossChance = 0.4,
            double xpLossStep = 0.1,
            double xpLossMax = 1,
            double nightAccuracyPenalty = 0.2,
            double nightAccuracyPenaltyMax = 0.5,
            double nightAccuracyPenaltyStep = 0.05,
            string mainStoryLine = "main",
            double foodPerHour = 2,
            double waterPerHour = 3,
            double hungryStaminaCostMultiplier = 1.2,
            int hungryApCostBonus = 1,
            int thirstDamage = 3,
            double ambienceVolume = 0.55,
            double musicVolume = 0.45,
            double fadeSeconds = 1.5,
            string menuMusic = "mus-menu-theme",
            string combatMusic = "mus-combat-light")
        {
            var insurance = insuranceChance.ToString(CultureInfo.InvariantCulture);
            var step = insuranceStep.ToString(CultureInfo.InvariantCulture);
            var respawn = respawnPercent.ToString(CultureInfo.InvariantCulture);
            var xpLoss = xpLossChance.ToString(CultureInfo.InvariantCulture);
            var xpStep = xpLossStep.ToString(CultureInfo.InvariantCulture);
            var xpMax = xpLossMax.ToString(CultureInfo.InvariantCulture);
            var night = nightAccuracyPenalty.ToString(CultureInfo.InvariantCulture);
            var nightMax = nightAccuracyPenaltyMax.ToString(CultureInfo.InvariantCulture);
            var nightStep = nightAccuracyPenaltyStep.ToString(CultureInfo.InvariantCulture);
            var baseWeight = baseCarryWeight.ToString(CultureInfo.InvariantCulture);
            var food = foodPerHour.ToString(CultureInfo.InvariantCulture);
            var water = waterPerHour.ToString(CultureInfo.InvariantCulture);
            var hungry = hungryStaminaCostMultiplier.ToString(CultureInfo.InvariantCulture);
            var ambience = ambienceVolume.ToString(CultureInfo.InvariantCulture);
            var music = musicVolume.ToString(CultureInfo.InvariantCulture);
            var fade = fadeSeconds.ToString(CultureInfo.InvariantCulture);
            return $"{{\"schemaVersion\":1,\"kind\":\"rules\",\"id\":\"{id}\",\"insuranceChance\":{insurance},\"insuranceStep\":{step},\"xpLossChance\":{xpLoss},\"xpLossStep\":{xpStep},\"xpLossMax\":{xpMax},\"respawnPercent\":{respawn},\"baseRows\":{baseRows},\"tierCount\":{tierCount},\"playerActorId\":\"{playerActorId}\",\"gridWidth\":{gridWidth},\"baseCarryWeight\":{baseWeight},\"needsMax\":{needsMax},\"startingMoney\":{startingMoney},\"tradeDifficulty\":1,\"tradeDifficultyMax\":3,\"tradeDifficultyStep\":0.1,\"nicknameMaxLength\":24,\"previewMaxRows\":32,\"maxRows\":{maxRows},\"savePageSize\":6,\"nightAccuracyPenalty\":{night},\"nightAccuracyPenaltyMax\":{nightMax},\"nightAccuracyPenaltyStep\":{nightStep},\"mainStoryLine\":\"{mainStoryLine}\",\"needs\":{{\"foodPerHour\":{food},\"waterPerHour\":{water},\"hungryStaminaCostMultiplier\":{hungry},\"hungryApCostBonus\":{hungryApCostBonus},\"thirstDamage\":{thirstDamage}}},\"audio\":{{\"menuMusic\":\"{menuMusic}\",\"combatMusic\":\"{combatMusic}\",\"ambienceVolume\":{ambience},\"musicVolume\":{music},\"fadeSeconds\":{fade}}}}}";
        }

        private static string WithoutAudio(string json) =>
            json.Replace(
                ",\"audio\":{\"menuMusic\":\"mus-menu-theme\",\"combatMusic\":\"mus-combat-light\",\"ambienceVolume\":0.55,\"musicVolume\":0.45,\"fadeSeconds\":1.5}",
                string.Empty,
                StringComparison.Ordinal);
    }
}
