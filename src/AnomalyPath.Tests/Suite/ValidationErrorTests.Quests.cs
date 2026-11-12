#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: ошибки квестов.</summary>
    public sealed partial class ValidationErrorTests
    {
        private void QuestErrorTests()
        {
            var cases = new (string Name, string Quest, string ExtraFile, string ExtraJson, string Fragment)[]
            {
                (
                    "ошибка: цель квеста — неизвестный предмет",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"findItems\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"target\":{\"itemPool\":[\"missing\"],\"itemKinds\":1,\"itemCount\":1," +
                    "\"npcPool\":[\"enemy_x\"],\"npcCount\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"human\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"attack\":{\"weaponId\":\"wpn_x\"}}}",
                    "не найден"),
                (
                    "ошибка: цель квеста — неизвестный актёр",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"actorPool\":[\"mutant_missing\"]," +
                    "\"minTier\":0,\"maxTier\":2},\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "Актёр не найден"),
                (
                    "ошибка: цель зачистки не точка интереса",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"clearLocation\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"target\":{\"locationPool\":[\"loc_route\"],\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1}}",
                    "locations/loc_route.json",
                    "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_route\",\"titleKey\":\"t\"," +
                    "\"locationKind\":\"wild\",\"subtype\":\"path\",\"tags\":[\"road\"],\"tier\":1," +
                    "\"backgrounds\":[\"bg\"],\"objectCount\":[0,0]}",
                    "дикой точкой интереса"),
                (
                    "ошибка: пул целей квеста пуст",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":9,\"maxTier\":9}," +
                    "\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "не дал ни одного кандидата"),
                (
                    "ошибка: предмет награды квеста не найден",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1,\"items\":[{\"itemId\":\"missing\",\"count\":1}]}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "Предмет награды квеста не найден"),
                (
                    "ошибка: неизвестная сложность квеста",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                    "\"difficulty\":\"impossible\"," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "Неизвестная сложность квеста"),
                (
                    "ошибка: провал по времени без срока",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"failCondition\":\"time\"," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "Срок квеста"),
                (
                    "ошибка: срок у провала по смерти",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"failCondition\":\"death\",\"timeLimitHours\":5," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "Срок квеста"),
                (
                    "ошибка: неизвестное условие провала квеста",
                    "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                    "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\",\"difficulty\":\"easy\"," +
                    "\"failCondition\":\"random\"," +
                    "\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                    "\"rewards\":{\"money\":1}}",
                    "actors/enemy.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\"," +
                    "\"nameKey\":\"n\",\"roles\":[\"mutant\"],\"combat\":{\"tier\":1,\"level\":1," +
                    "\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                    "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                    "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_bullet\"}}}",
                    "Неизвестное условие провала"),
            };

            foreach (var testCase in cases)
            {
                Check(testCase.Name, () =>
                {
                    WithFixture(
                        new Dictionary<string, string>
                        {
                            ["catalog.json"] = Catalog("quests/qst_x.json", testCase.ExtraFile),
                            ["quests/qst_x.json"] = testCase.Quest,
                            [testCase.ExtraFile] = testCase.ExtraJson,
                        },
                        root =>
                        {
                            var report = Preflight.Run(root).Report;
                            True(
                                HasIssue(report, "qst_x", testCase.Fragment),
                                $"нет ошибки: {testCase.Fragment}; {Describe(report)}");
                        });
                });
            }
        }

        private void QuestBoardErrorTests()
        {
            const string quest =
                "{\"schemaVersion\":1,\"kind\":\"quests\",\"id\":\"qst_x\",\"questKind\":\"repeatable\"," +
                "\"questType\":\"killMutants\",\"titleKey\":\"t\",\"descriptionKey\":\"d\"," +
                "\"difficulty\":\"easy\",\"target\":{\"count\":1,\"poolSize\":1,\"minTier\":0,\"maxTier\":2}," +
                "\"rewards\":{\"money\":1}}";
            var cases = new (string Name, string Board, string Fragment)[]
            {
                (
                    "ошибка: квест пула доски не найден",
                    "\"questBoard\":{\"slots\":[{\"difficulty\":\"easy\",\"refreshHours\":24}]," +
                    "\"questPool\":[\"qst_missing\"]}",
                    "Квест доски не найден"),
                (
                    "ошибка: в пуле доски нет квеста сложности слота",
                    "\"questBoard\":{\"slots\":[{\"difficulty\":\"medium\",\"refreshHours\":48}]," +
                    "\"questPool\":[\"qst_x\"]}",
                    "нет квеста сложности"),
                (
                    "ошибка: квест повторяется в пуле доски",
                    "\"questBoard\":{\"slots\":[{\"difficulty\":\"easy\",\"refreshHours\":24}]," +
                    "\"questPool\":[\"qst_x\",\"qst_x\"]}",
                    "повторяется в пуле доски"),
                (
                    "ошибка: пул доски пуст",
                    "\"questBoard\":{\"slots\":[{\"difficulty\":\"easy\",\"refreshHours\":24}]," +
                    "\"questPool\":[]}",
                    "нет слотов или пул квестов пуст"),
                (
                    "ошибка: неизвестная сложность слота доски",
                    "\"questBoard\":{\"slots\":[{\"difficulty\":\"impossible\",\"refreshHours\":24}]," +
                    "\"questPool\":[\"qst_x\"]}",
                    "Неизвестная сложность квеста"),
                (
                    "ошибка: квест локации-заглушки не найден",
                    "\"stubAfterQuest\":\"qst_missing\"",
                    "Квест локации-заглушки не найден"),
            };

            foreach (var testCase in cases)
            {
                Check(testCase.Name, () =>
                {
                    WithFixture(
                        new Dictionary<string, string>
                        {
                            ["catalog.json"] = Catalog("quests/qst_x.json", "locations/loc_hub.json"),
                            ["quests/qst_x.json"] = quest,
                            ["locations/loc_hub.json"] =
                                "{\"schemaVersion\":1,\"kind\":\"locations\",\"id\":\"loc_hub\"," +
                                "\"titleKey\":\"t\",\"locationKind\":\"hub\",\"subtype\":\"hub\"," +
                                "\"tags\":[\"hub\"],\"tier\":0,\"backgrounds\":[\"bg\"],\"objectCount\":[0,0]," +
                                testCase.Board + "}",
                        },
                        root =>
                        {
                            var report = Preflight.Run(root).Report;
                            True(
                                HasIssue(report, "loc_hub", testCase.Fragment),
                                $"нет ошибки: {testCase.Fragment}; {Describe(report)}");
                        });
                });
            }
        }
    }
}
