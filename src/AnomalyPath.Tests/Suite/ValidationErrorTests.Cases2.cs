#nullable enable
using System.Collections.Generic;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: вторая половина случаев ошибок.</summary>
    public sealed partial class ValidationErrorTests
    {
        private List<(string Name, Dictionary<string, string> Files, string Fragment)> BuildErrorCasesB()
        {
            return new List<(string Name, Dictionary<string, string> Files, string Fragment)>
            {
                (
                    "ошибка: дефолтный скилл оружия не в его списке",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog(
                            "items/a.json", "skills/col_skills.json", "combat.json", "equipment.json", "topology.json"),
                        ["items/a.json"] = "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"wpn_a\",\"nameKey\":\"n\"," +
                                            "\"category\":\"weapon\",\"footprint\":[2,3],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"," +
                                            "\"weapon\":{\"classId\":\"wclass_rifle\",\"damageTypeId\":\"dmg_bullet\"," +
                                            "\"damage\":1,\"accuracy\":0.5,\"magazine\":10,\"ammoCaliber\":\"5.45x39\"," +
                                            "\"reloadTime\":2,\"armorPenetration\":0,\"durabilityMax\":10," +
                                            "\"durabilityPerShot\":0.1,\"skills\":[\"skill_shot\"]," +
                                            "\"defaultSkillId\":\"skill_missing\"}}",
                        ["skills/col_skills.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"collection\":true," +
                                                      "\"id\":\"col_skills\",\"entries\":[\"skills/skill_shot.json\"]}",
                        ["skills/skill_shot.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"id\":\"skill_shot\"," +
                                                      "\"nameKey\":\"n\",\"descriptionKey\":\"d\",\"type\":\"attack\"," +
                                                      "\"apCost\":1,\"ammoCost\":1,\"effects\":[{\"type\":\"damage\"}]}",
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "Дефолтный скилл"),
                (
                    "ошибка: у мутанта нет скиллов",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"mutant_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"mutant\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_rupture\"}} }",
                    },
                    "нет скиллов"),
                (
                    "ошибка: у мутанта оружие",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"mutant_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"mutant\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                                             "\"attack\":{\"weaponId\":\"wpn_x\"}} }",
                    },
                    "оружие запрещено"),
                (
                    "ошибка: у врага-человека нет оружия",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"human\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_rupture\"}} }",
                    },
                    "нет оружия в блоке атаки"),
                (
                    "ошибка: скиллы у не-мутанта",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"human\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"skills\":[\"skill_bite\"]," +
                                             "\"attack\":{\"weaponId\":\"wpn_x\"}} }",
                    },
                    "только у мутанта"),
                (
                    "ошибка: дефолтный скилл не в списке скиллов мутанта",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"mutant_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"mutant\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_shot\"," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_rupture\"}} }",
                    },
                    "Дефолтный скилл"),
                (
                    "ошибка: скилл мутанта тратит патроны",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json", "skills/col_skills.json", "combat.json", "equipment.json", "topology.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"mutant_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"mutant\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_rupture\"}} }",
                        ["skills/col_skills.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"collection\":true," +
                                                      "\"id\":\"col_skills\",\"entries\":[\"skills/skill_bite.json\"]}",
                        ["skills/skill_bite.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"id\":\"skill_bite\"," +
                                                      "\"nameKey\":\"n\",\"descriptionKey\":\"d\",\"type\":\"attack\"," +
                                                      "\"apCost\":1,\"ammoCost\":1,\"effects\":[{\"type\":\"damage\"}]}",
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "тратит патроны"),
                (
                    "ошибка: дефолтный скилл не атакующий",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json", "skills/col_skills.json", "combat.json", "equipment.json", "topology.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"mutant_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"mutant\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"skills\":[\"skill_cry\"],\"defaultSkillId\":\"skill_cry\"," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_rupture\"}} }",
                        ["skills/col_skills.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"collection\":true," +
                                                      "\"id\":\"col_skills\",\"entries\":[\"skills/skill_cry.json\"]}",
                        ["skills/skill_cry.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"id\":\"skill_cry\"," +
                                                     "\"nameKey\":\"n\",\"descriptionKey\":\"d\",\"type\":\"utility\"," +
                                                     "\"apCost\":1,\"effects\":[{\"type\":\"damage\"}]}",
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "должен быть атакующим"),
                (
                    "ошибка: неизвестный тип эффекта скилла",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("skills/col_skills.json", "combat.json", "equipment.json", "topology.json"),
                        ["skills/col_skills.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"collection\":true," +
                                                      "\"id\":\"col_skills\",\"entries\":[\"skills/skill_bad.json\"]}",
                        ["skills/skill_bad.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"id\":\"skill_bad\"," +
                                                     "\"nameKey\":\"n\",\"descriptionKey\":\"d\",\"type\":\"attack\"," +
                                                     "\"apCost\":1,\"effects\":[{\"type\":\"nuke\"}]}",
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "Неизвестный тип эффекта"),
                (
                    "ошибка: редкостей не шесть",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/col_rarities.json"),
                        ["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                         "\"id\":\"col_rarities\",\"entries\":[\"rarities/a.json\",\"rarities/b.json\"]}",
                        ["rarities/a.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_common\"," +
                                              "\"nameKey\":\"n\",\"order\":0}",
                        ["rarities/b.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_rare\"," +
                                              "\"nameKey\":\"n\",\"order\":1}",
                    },
                    "Редкостей должно быть"),
                (
                    "ошибка: порядок редкости повторяется",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/col_rarities.json"),
                        ["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                         "\"id\":\"col_rarities\",\"entries\":[\"rarities/r0.json\",\"rarities/r1.json\"," +
                                                         "\"rarities/r2.json\",\"rarities/r3.json\",\"rarities/r4.json\",\"rarities/r5.json\"]}",
                        ["rarities/r0.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0}",
                        ["rarities/r1.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_rare\",\"nameKey\":\"n\",\"order\":1}",
                        ["rarities/r2.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_special\",\"nameKey\":\"n\",\"order\":2}",
                        ["rarities/r3.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}",
                        ["rarities/r4.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_legendary\",\"nameKey\":\"n\",\"order\":4}",
                        ["rarities/r5.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_unique\",\"nameKey\":\"n\",\"order\":4}",
                    },
                    "Порядок редкости 4 повторяется"),
                (
                    "ошибка: нет записи редкости",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/col_rarities.json"),
                        ["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                         "\"id\":\"col_rarities\",\"entries\":[\"rarities/r0.json\",\"rarities/r1.json\"," +
                                                         "\"rarities/r2.json\",\"rarities/r3.json\",\"rarities/r4.json\"]}",
                        ["rarities/r0.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0}",
                        ["rarities/r1.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_rare\",\"nameKey\":\"n\",\"order\":1}",
                        ["rarities/r2.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_special\",\"nameKey\":\"n\",\"order\":2}",
                        ["rarities/r3.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}",
                        ["rarities/r4.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_legendary\",\"nameKey\":\"n\",\"order\":4}",
                    },
                    "Нет редкости: 'rar_unique'"),
                (
                    "ошибка: порядок редкости вне диапазона",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/col_rarities.json"),
                        ["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                         "\"id\":\"col_rarities\",\"entries\":[\"rarities/r0.json\",\"rarities/r1.json\"," +
                                                         "\"rarities/r2.json\",\"rarities/r3.json\",\"rarities/r4.json\",\"rarities/r5.json\"]}",
                        ["rarities/r0.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0}",
                        ["rarities/r1.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_rare\",\"nameKey\":\"n\",\"order\":1}",
                        ["rarities/r2.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_special\",\"nameKey\":\"n\",\"order\":2}",
                        ["rarities/r3.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}",
                        ["rarities/r4.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_legendary\",\"nameKey\":\"n\",\"order\":4}",
                        ["rarities/r5.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_unique\",\"nameKey\":\"n\",\"order\":6}",
                    },
                    "вне диапазона 0..5"),
                (
                    "ошибка: отрицательный бонус меткости редкости",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/col_rarities.json"),
                        ["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                         "\"id\":\"col_rarities\",\"entries\":[\"rarities/r0.json\",\"rarities/r1.json\"," +
                                                         "\"rarities/r2.json\",\"rarities/r3.json\",\"rarities/r4.json\",\"rarities/r5.json\"]}",
                        ["rarities/r0.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0}",
                        ["rarities/r1.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_rare\",\"nameKey\":\"n\",\"order\":1}",
                        ["rarities/r2.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_special\",\"nameKey\":\"n\",\"order\":2}",
                        ["rarities/r3.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}",
                        ["rarities/r4.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_legendary\",\"nameKey\":\"n\",\"order\":4}",
                        ["rarities/r5.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_unique\",\"nameKey\":\"n\",\"order\":5,\"accuracyBonus\":-0.1}",
                    },
                    "не может быть отрицательным"),
                (
                    "ошибка: порог аптечки ИИ вне диапазона",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson().Replace(
                            "\"medkitHpThreshold\":0.3",
                            "\"medkitHpThreshold\":1.5"),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "Порог здоровья для аптечки ИИ"),
                (
                    "ошибка: формула использует необъявленное имя",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "formulas/formula_x.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["formulas/formula_x.json"] = "{\"schemaVersion\":1,\"kind\":\"formulas\",\"id\":\"formula_x\"," +
                                                       "\"expression\":\"damage * k\",\"variables\":[\"damage\"]," +
                                                       "\"result\":\"int\"}",
                    },
                    "Формула использует неизвестное имя"),
                (
                    "ошибка: формула боя не найдена",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "Формула не найдена"),
                (
                    "ошибка: две записи настроек боя",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "combat2.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson(),
                        ["combat2.json"] = CombatJson().Replace("\"id\":\"combat\"", "\"id\":\"combat_alt\""),
                    },
                    "ровно одно"),
            };
        }
    }
}
