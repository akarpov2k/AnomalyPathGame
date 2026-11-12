#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Категория «сюжетные квесты»: разбор шаблонов, сервис, сейв и границы.</summary>
    public sealed partial class StoryQuestTests : DataTestBase
    {
        /// <summary>Создаёт категорию тестов сюжетных квестов.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public StoryQuestTests(string dataRoot)
            : base(dataRoot)
        {
        }

        private const string NpcActor =
            "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"npc_x\",\"type\":\"npc\",\"nameKey\":\"n\"}";

        private const string EnemyActor =
            "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
            "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
            "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
            "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
            "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}";

        public override void Run()
        {
            var services = BuildServices();
            ServiceTests(services);
            Check("сюжетный квест: цели, варианты и область прогресса", () =>
            {
                var quest = Parse(StoryWithHard());
                Equal("qst_s1", quest.Id, "id");
                Equal("main", quest.StoryLine, "линия");
                Equal("npc_giver", quest.GiverActorId, "выдающий");
                Equal("npc_giver", quest.TurnInActor, "сдача по умолчанию");
                Equal(QuestProgressScope.Life, quest.ProgressScope, "область прогресса");
                Equal(3, quest.Objectives.Count, "число целей");
                Equal(QuestObjectiveType.ClearLocations, quest.Objectives[0].Type, "тип 0");
                Equal(2, quest.Objectives[0].Count, "счётчик 0");
                Equal(1, quest.Objectives[0].ExcludeSubtypes.Count, "исключения 0");
                Equal(LocationSubtype.Path, quest.Objectives[0].ExcludeSubtypes[0], "подтип 0");
                Equal(3, quest.Objectives[1].Count, "счётчик 1");
                Equal(1, quest.Objectives[1].ActorPool.Count, "пул 1");
                Equal("npc_target", quest.Objectives[2].ActorId, "актёр 2");
                Equal(100, quest.Rewards.Money, "награда standard");

                True(quest.TryGetVariant("hard", out var hard), "вариант hard есть");
                Equal(4, hard.Objectives[0].Count, "hard: счётчик");
                Equal(70, hard.Rewards.Money, "hard: награда");
                False(quest.TryGetVariant("missing", out _), "неизвестного варианта нет");
            });

            Check("сюжетный квест: цели цепочки, локации и осмотра", () =>
            {
                const string quest =
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_s4\",\"questKind\":\"story\"," +
                    "\"storyLine\":\"main\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                    "\"giverActorId\":\"npc_giver\",\"progressScope\":\"cumulative\"," +
                    "\"objectives\":[" +
                    "{\"type\":\"clearChain\",\"chainId\":\"chain_atp\",\"completion\":\"talkToActor\"}," +
                    "{\"type\":\"reachLocation\",\"locationId\":\"loc_atp_gate\"}," +
                    "{\"type\":\"inspectObjects\",\"objectKind\":\"loot\",\"count\":2}]}";

                var parsed = Parse(quest);
                Equal(ChainCompletion.TalkToActor, parsed.Objectives[0].Completion, "завершение");
                Equal("chain_atp", parsed.Objectives[0].ChainId, "цепочка");
                Equal("loc_atp_gate", parsed.Objectives[1].LocationId, "локация");
                Equal(LocationObjectKind.Loot, parsed.Objectives[2].ObjectKind, "вид объекта");
                Equal(QuestProgressScope.Cumulative, parsed.ProgressScope, "область прогресса");
            });

            Check("сюжетный квест: неизвестный тип цели — код ошибки", () =>
            {
                var error = Throws<DataException>(
                    () => Parse(Quest("\"objectives\":[{\"type\":\"hunt\",\"count\":1}]")),
                    "неизвестная цель");
                Equal(CoreErrors.StoryObjectiveType, error.Code, "код");
            });

            Check("сюжетный квест: нулевой счётчик цели — код ошибки", () =>
            {
                var error = Throws<DataException>(
                    () => Parse(Quest("\"objectives\":[{\"type\":\"openStashes\",\"count\":0}]")),
                    "нулевой счётчик");
                Equal(CoreErrors.StoryObjectiveCount, error.Code, "код");
            });

            Check("сюжетный квест: пустые цели — код ошибки", () =>
            {
                var error = Throws<DataException>(
                    () => Parse(Quest("\"objectives\":[]")),
                    "пустые цели");
                Equal(CoreErrors.StoryObjectivesEmpty, error.Code, "код");
            });

            Check("сюжетный квест: неизвестная область прогресса", () =>
            {
                var error = Throws<DataException>(
                    () => Parse(StoryWithHard().Replace("\"progressScope\":\"life\"", "\"progressScope\":\"week\"")),
                    "область прогресса");
                Equal(CoreErrors.StoryProgressScope, error.Code, "код");
            });

            Check("сюжетный квест: повторяемый квест не читается как сюжетный", () =>
            {
                const string repeatable =
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_s1\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                    "\"difficulty\":\"easy\",\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}}";

                var error = Throws<DataException>(() => Parse(repeatable), "вид квеста");
                Equal(CoreErrors.StoryQuestKind, error.Code, "код");
            });

            Check("сюжетный квест: вариант standard в данных запрещён", () =>
            {
                const string quest =
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_s1\",\"questKind\":\"story\"," +
                    "\"storyLine\":\"main\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                    "\"giverActorId\":\"npc_giver\",\"progressScope\":\"life\"," +
                    "\"objectives\":[{\"type\":\"openStashes\",\"count\":1}]," +
                    "\"variants\":[{\"id\":\"standard\",\"objectives\":[{\"type\":\"openStashes\",\"count\":2}]}]}";

                var error = Throws<DataException>(() => Parse(quest), "standard в данных");
                Equal(CoreErrors.StoryVariantStandard, error.Code, "код");
            });

            Check("сюжетный квест: одинаковые варианты — код ошибки", () =>
            {
                var error = Throws<DataException>(
                    () => Parse(StoryWithHard().Replace(
                        "\"variants\":[",
                        "\"variants\":[{\"id\":\"hard\",\"objectives\":[{\"type\":\"openStashes\",\"count\":9}]},")),
                    "дубль варианта");
                Equal(CoreErrors.StoryVariantDuplicate, error.Code, "код");
            });

            Check("сюжетный квест: диапазон тиров цели", () =>
            {
                var error = Throws<DataException>(
                    () => Parse(Quest("\"objectives\":[{\"type\":\"killActors\",\"count\":1,\"minTier\":3,\"maxTier\":1}]")),
                    "диапазон тиров");
                Equal(CoreErrors.QuestTierRange, error.Code, "код");
            });

            Check("репозиторий: сюжетные квесты отделены от повторяемых", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_story.json", "quests/qst_rep.json"),
                        ["quests/qst_story.json"] = StoryWithHard(),
                        ["quests/qst_rep.json"] =
                            "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_rep\"," +
                            "\"questKind\":\"repeatable\",\"questType\":\"killMutants\",\"titleKey\":\"t\"," +
                            "\"descriptionKey\":\"d\",\"difficulty\":\"easy\",\"target\":{\"count\":1," +
                            "\"poolSize\":1,\"minTier\":0,\"maxTier\":2},\"rewards\":{\"money\":1}}"
                    },
                    root =>
                    {
                        var data = Load(root);
                        var story = StoryQuestRepository.FromLoadedData(data);
                        var repeatable = QuestRepository.FromLoadedData(data);

                        Equal(1, story.All.Count, "сюжетных");
                        Equal(1, repeatable.All.Count, "повторяемых");
                        True(story.Contains("qst_s1"), "сюжетный найден");
                        False(story.Contains("qst_rep"), "повторяемый не попал в сюжетные");
                        True(repeatable.Contains("qst_rep"), "повторяемый найден");
                        False(repeatable.Contains("qst_s1"), "сюжетный не попал в повторяемые");
                    });
            });

            Check("актёр: диалог у NPC читается, у врага запрещён", () =>
            {
                var npc = ActorTemplate.FromJson(
                    JsonParser.Parse(
                        "{\"kind\":\"actors\",\"id\":\"npc_x\",\"type\":\"npc\",\"nameKey\":\"n\"," +
                        "\"dialogId\":\"dlg_x\"}",
                        "test"),
                    "test");
                Equal("dlg_x", npc.DialogId, "диалог NPC");

                var enemy = EnemyActor.Replace("\"nameKey\":\"n\"", "\"nameKey\":\"n\",\"dialogId\":\"dlg_x\"");
                var error = Throws<DataException>(
                    () => ActorTemplate.FromJson(JsonParser.Parse(enemy, "test"), "test"),
                    "диалог врага");
                Equal(CoreErrors.DialogOnEnemy, error.Code, "код");
            });

            Check("валидация: выдача сюжетного квеста врагом — ошибка", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_s1.json", "actors/enemy.json"),
                        ["quests/qst_s1.json"] = StoryWithHard().Replace("\"npc_giver\"", "\"enemy_x\""),
                        ["actors/enemy.json"] = EnemyActor
                    },
                    root =>
                    {
                        var report = Preflight.Run(root).Report;
                        True(
                            HasAnyError(report, "является врагом"),
                            "нет ошибки о враге: " + Describe(report));
                    });
            });

            Check("валидация: следующий квест не найден — ошибка", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_s1.json", "actors/npc.json"),
                        ["quests/qst_s1.json"] = StoryWithHard().Replace(
                            "\"progressScope\":\"life\"",
                            "\"progressScope\":\"life\",\"nextQuestId\":\"qst_missing\""),
                        ["actors/npc.json"] = NpcActor
                    },
                    root =>
                    {
                        var report = Preflight.Run(root).Report;
                        True(
                            HasAnyError(report, "следующий квест"),
                            "нет ошибки о следующем квесте: " + Describe(report));
                    });
            });

            Check("валидация: цели hard меньше стандартных — ошибка", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_s1.json", "actors/npc.json"),
                        ["quests/qst_s1.json"] = StoryWithHard().Replace(
                            "\"count\":4,\"excludeSubtypes\":[\"path\"]",
                            "\"count\":1,\"excludeSubtypes\":[\"path\"]"),
                        ["actors/npc.json"] = NpcActor
                    },
                    root =>
                    {
                        var report = Preflight.Run(root).Report;
                        True(
                            HasAnyError(report, "цели варианта hard"),
                            "нет ошибки о целях hard: " + Describe(report));
                    });
            });

            Check("валидация: награда hard больше стандартной — ошибка", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_s1.json", "actors/npc.json"),
                        ["quests/qst_s1.json"] = StoryWithHard().Replace(
                            "\"money\":70",
                            "\"money\":150"),
                        ["actors/npc.json"] = NpcActor
                    },
                    root =>
                    {
                        var report = Preflight.Run(root).Report;
                        True(
                            HasAnyError(report, "награда варианта hard"),
                            "нет ошибки о награде hard: " + Describe(report));
                    });
            });

            Check("валидация: неизвестная локация цели — ошибка", () =>
            {
                const string quest =
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_s1\",\"questKind\":\"story\"," +
                    "\"storyLine\":\"main\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                    "\"giverActorId\":\"npc_x\",\"progressScope\":\"life\"," +
                    "\"objectives\":[{\"type\":\"reachLocation\",\"locationId\":\"loc_missing\"}]}";

                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("quests/qst_s1.json", "actors/npc.json"),
                        ["quests/qst_s1.json"] = quest,
                        ["actors/npc.json"] = NpcActor
                    },
                    root =>
                    {
                        var report = Preflight.Run(root).Report;
                        True(
                            HasAnyError(report, "Локация цели не найдена"),
                            "нет ошибки о локации: " + Describe(report));
                    });
            });
        }

        private static string Quest(string body)
        {
            return "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_s1\",\"questKind\":\"story\"," +
                   "\"storyLine\":\"main\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                   "\"giverActorId\":\"npc_giver\",\"progressScope\":\"life\"," +
                   "\"rewards\":{\"money\":100}," + body + "}";
        }

        private static string StoryWithHard()
        {
            return Quest("\"objectives\":[" +
                         "{\"type\":\"clearLocations\",\"count\":2,\"excludeSubtypes\":[\"path\"]}," +
                         "{\"type\":\"killActors\",\"count\":3,\"actorPool\":[\"enemy_x\"],\"minTier\":0,\"maxTier\":2}," +
                         "{\"type\":\"talkToActor\",\"actorId\":\"npc_target\"}]," +
                         "\"variants\":[{\"id\":\"hard\",\"textKey\":\"q.hard\"," +
                         "\"objectives\":[" +
                         "{\"type\":\"clearLocations\",\"count\":4,\"excludeSubtypes\":[\"path\"]}," +
                         "{\"type\":\"killActors\",\"count\":6,\"actorPool\":[\"enemy_x\"]}," +
                         "{\"type\":\"talkToActor\",\"actorId\":\"npc_target\"}]," +
                         "\"rewards\":{\"money\":70}}]");
        }

        private static StoryQuestTemplate Parse(string json) =>
            StoryQuestTemplate.FromJson(JsonParser.Parse(json, "test"), "test");
    }
}
