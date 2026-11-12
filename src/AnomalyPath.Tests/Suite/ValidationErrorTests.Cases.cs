#nullable enable
using System.Collections.Generic;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: первая половина случаев ошибок.</summary>
    public sealed partial class ValidationErrorTests
    {
        private List<(string Name, Dictionary<string, string> Files, string Fragment)> BuildErrorCasesA()
        {
            return new List<(string Name, Dictionary<string, string> Files, string Fragment)>
            {
                (
                    "ошибка: дубль id в коллекции",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/col.json"),
                        ["items/col.json"] = Collection("items/a.json", "items/a.json"),
                        ["items/a.json"] = Weapon("wpn_ak74"),
                    },
                    "подключён дважды"),
                (
                    "ошибка: extends на несуществующий файл",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = Weapon("wpn_ak74", extends: "_base/missing.json"),
                    },
                    "Файл не найден"),
                (
                    "ошибка: extends на другой вид",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["_base/loc.json"] = "{\"schemaVersion\":1,\"kind\":\"locations\",\"abstract\":true}",
                        ["items/a.json"] = Weapon("wpn_ak74", extends: "_base/loc.json"),
                    },
                    "extends на другой вид"),
                (
                    "ошибка: битый json",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\": \"items\", ",
                    },
                    "Ожидалось имя поля"),
                (
                    "ошибка: неизвестный материал брони",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"arm_wood\",\"nameKey\":\"n\",\"category\":\"armor\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"durabilityMax\":10,\"material\":\"wood\",\"durabilityFactor\":1.0 }",
                    },
                    "Неизвестный материал"),
                (
                    "ошибка: неизвестный тип урона в защите",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"arm_wood\",\"nameKey\":\"n\",\"category\":\"armor\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"durabilityMax\":10,\"material\":\"light\",\"durabilityFactor\":1.0,\"quality\":1,\"protection\":{\"dmg_wood\":1} }",
                    },
                    "Неизвестный тип урона"),
                (
                    "ошибка: защита у предмета, который её не даёт",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"trp_p\",\"nameKey\":\"n\",\"category\":\"trophy\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"protection\":{\"dmg_bullet\":1} }",
                    },
                    "Защита допустима только у брони"),
                (
                    "ошибка: отрицательная защита",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"arm_neg\",\"nameKey\":\"n\",\"category\":\"armor\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"durabilityMax\":10,\"material\":\"light\",\"durabilityFactor\":1.0,\"quality\":1,\"protection\":{\"dmg_bullet\":-1} }",
                    },
                    "не может быть отрицательным"),
                (
                    "ошибка: путь выходит за корень",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("../secret.json"),
                    },
                    "выходит за корень"),
                (
                    "ошибка: weapon без блока weapon",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"wpn_x\",\"nameKey\":\"n\",\"category\":\"weapon\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\" }",
                    },
                    "без блока weapon"),
                (
                    "ошибка: у врага нет опыта",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"accuracy\":0.5} }",
                    },
                    "xp"),
                (
                    "ошибка: отрицательная защита актёра",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"npc_p\",\"type\":\"npc\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"accuracy\":0.8,\"protection\":{\"dmg_bullet\":-1}} }",
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "не может быть отрицательным"),
                (
                    "ошибка: магазин оружия меньше одного патрона",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"wpn_x\",\"nameKey\":\"n\",\"category\":\"weapon\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"weapon\":{\"classId\":\"w\",\"damageTypeId\":\"dmg_bullet\",\"damage\":1,\"accuracy\":0.5,\"magazine\":0,\"ammoCaliber\":\"c\",\"reloadTime\":1,\"armorPenetration\":0,\"durabilityMax\":10,\"durabilityPerShot\":0.1}}",
                    },
                    "вместимость магазина"),
                (
                    "ошибка: у врага нет блока attack",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5} }",
                    },
                    "блока attack"),
                (
                    "ошибка: блок атаки у не-врага",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"npc_x\",\"type\":\"npc\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_rupture\"}} }",
                    },
                    "только у врага"),
                (
                    "ошибка: в атаке смешаны оружие и естественная атака",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"weaponId\":\"wpn_x\",\"damage\":5}} }",
                    },
                    "смешаны"),
                (
                    "ошибка: в атаке смешаны weaponId и пул оружия",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json"),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"weaponId\":\"wpn_x\",\"weaponIds\":[\"wpn_y\"]}} }",
                    },
                    "смешаны"),
                (
                    "ошибка: тип урона атаки не найден",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"roles\":[\"mutant\"]," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"skills\":[\"skill_bite\"],\"defaultSkillId\":\"skill_bite\"," +
                                             "\"attack\":{\"damage\":1,\"damageTypeId\":\"dmg_missing\"}} }",
                    },
                    "Неизвестный тип урона"),
                (
                    "ошибка: оружие атаки не найдено",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("actors/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"weaponId\":\"wpn_missing\"}} }",
                    },
                    "Предмет не найден"),
                (
                    "ошибка: оружие атаки без блока weapon",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "actors/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                        ["items/a.json"] = "{ \"kind\":\"items\",\"id\":\"wpn_x\",\"nameKey\":\"n\",\"category\":\"trophy\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\" }",
                        ["actors/a.json"] = "{ \"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                                             "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":1,\"xp\":1,\"accuracy\":0.5," +
                                             "\"attack\":{\"weaponId\":\"wpn_x\"}} }",
                    },
                    "без блока weapon"),
                (
                    "ошибка: нет записи прогрессии",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "Настройки прогрессии не загружены"),
                (
                    "ошибка: отрицательный рост кривой прогрессии",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json", "progression.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                        ["progression.json"] = "{\"schemaVersion\":1,\"kind\":\"progression\",\"id\":\"progression\",\"levelBaseXp\":100," +
                                               "\"levelGrowthPercent\":-5,\"killXpPerRank\":10,\"rarityBonusPercent\":10," +
                                               "\"maxRank\":5,\"maxRarity\":5}",
                    },
                    "levelGrowthPercent"),
                (
                    "ошибка: чужой id записи прогрессии",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json", "progression.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                        ["progression.json"] = "{\"schemaVersion\":1,\"kind\":\"progression\",\"id\":\"progress\",\"levelBaseXp\":100," +
                                               "\"levelGrowthPercent\":20,\"killXpPerRank\":10,\"rarityBonusPercent\":10," +
                                               "\"maxRank\":5,\"maxRarity\":5}",
                    },
                    "id записи прогрессии должен быть"),
                (
                    "ошибка: две записи прогрессии",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json", "progression.json", "progression2.json"),
                        ["items/a.json"] = Weapon("wpn_a"),
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                        ["progression.json"] = "{\"schemaVersion\":1,\"kind\":\"progression\",\"id\":\"progression_alt\",\"levelBaseXp\":100," +
                                               "\"levelGrowthPercent\":20,\"killXpPerRank\":10,\"rarityBonusPercent\":10," +
                                               "\"maxRank\":5,\"maxRarity\":5}",
                        ["progression2.json"] = "{\"schemaVersion\":1,\"kind\":\"progression\",\"id\":\"progression\",\"levelBaseXp\":100," +
                                                "\"levelGrowthPercent\":20,\"killXpPerRank\":10,\"rarityBonusPercent\":10," +
                                                "\"maxRank\":5,\"maxRarity\":5}",
                    },
                    "Запись прогрессии должна быть ровно одна"),
                (
                    "ошибка: оружие ссылается на несуществующий скилл",
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog(
                            "items/a.json", "skills/col_skills.json", "combat.json", "equipment.json", "topology.json"),
                        ["items/a.json"] = "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"wpn_a\",\"nameKey\":\"n\"," +
                                            "\"category\":\"weapon\",\"footprint\":[2,3],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"," +
                                            "\"weapon\":{\"classId\":\"wclass_rifle\",\"damageTypeId\":\"dmg_bullet\"," +
                                            "\"damage\":1,\"accuracy\":0.5,\"magazine\":10,\"ammoCaliber\":\"5.45x39\"," +
                                            "\"reloadTime\":2,\"armorPenetration\":0,\"durabilityMax\":10," +
                                            "\"durabilityPerShot\":0.1,\"skills\":[\"skill_shot\",\"skill_missing\"]," +
                                            "\"defaultSkillId\":\"skill_shot\"}}",
                        ["skills/col_skills.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"collection\":true," +
                                                      "\"id\":\"col_skills\",\"entries\":[\"skills/skill_shot.json\"]}",
                        ["skills/skill_shot.json"] = "{\"schemaVersion\":1,\"kind\":\"skills\",\"id\":\"skill_shot\"," +
                                                      "\"nameKey\":\"n\",\"descriptionKey\":\"d\",\"type\":\"attack\"," +
                                                      "\"apCost\":1,\"ammoCost\":1,\"effects\":[{\"type\":\"damage\"}]}",
                        ["combat.json"] = CombatJson(),
                        ["equipment.json"] = EquipmentJson(),
                        ["topology.json"] = TopologyJson(),
                    },
                    "Скилл не найден"),
            };
        }
    }
}
