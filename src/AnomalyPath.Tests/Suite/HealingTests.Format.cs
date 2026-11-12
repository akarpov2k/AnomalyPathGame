#nullable enable
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="HealingTests"/>: разбор данных и правила валидации.</summary>
    public sealed partial class HealingTests
    {
        private void ParserTests()
        {
            Check("разбор: блок расходника читается", () =>
            {
                var item = Parse(ConsumableJson(
                    "{\"useTime\":2,\"charges\":3,\"usableInCombat\":true," +
                    "\"effects\":[{\"type\":\"heal\",\"amount\":25},{\"type\":\"restoreAp\",\"amount\":1}]}"));

                Equal(2, item.Consumable!.UseTime, "цена");
                Equal(3, item.Consumable.Charges, "заряды");
                True(item.Consumable.UsableInCombat, "боевой");
                Equal(25, item.Consumable.TotalHeal(), "лечение");
                Equal(1, item.Consumable.TotalApRestore(), "ОД");
            });

            Check("разбор: правила блока расходника", () =>
            {
                Throws<DataException>(
                    () => Parse(
                        "{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"itm_x\",\"nameKey\":\"n\"," +
                        "\"category\":\"consumable\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"}"),
                    "нет блока");
                Throws<DataException>(
                    () => Parse(Weapon("wpn_x", extra: ",\"consumable\":{\"useTime\":1,\"effects\":[{\"type\":\"heal\",\"amount\":1}]}")),
                    "блок у не-расходника");
                Throws<DataException>(
                    () => Parse(ConsumableJson("{\"useTime\":1}")),
                    "нет эффектов");
                Throws<DataException>(
                    () => Parse(ConsumableJson("{\"useTime\":1,\"effects\":[{\"type\":\"heal\",\"amount\":0}]}")),
                    "лечение 0");
                Throws<DataException>(
                    () => Parse(ConsumableJson("{\"useTime\":1,\"effects\":[{\"type\":\"damage\",\"amount\":1}]}")),
                    "неизвестный эффект");
                Throws<DataException>(
                    () => Parse(ConsumableJson("{\"useTime\":1,\"charges\":0,\"effects\":[{\"type\":\"heal\",\"amount\":1}]}")),
                    "заряды 0");
            });

            Check("разбор: правила модификаторов медицины", () =>
            {
                Throws<DataException>(
                    () => Parse(ConsumableJson(
                        "{\"useTime\":1,\"effects\":[{\"type\":\"modifier\",\"stat\":\"luck\",\"value\":1}]}")),
                    "неизвестная характеристика");
                Throws<DataException>(
                    () => Parse(ConsumableJson(
                        "{\"useTime\":1,\"effects\":[{\"type\":\"modifier\",\"stat\":\"accuracy\",\"value\":0}]}")),
                    "нулевая величина");
                Throws<DataException>(
                    () => Parse(ConsumableJson(
                        "{\"useTime\":1,\"effects\":[{\"type\":\"modifier\",\"stat\":\"protection\",\"value\":1}]}")),
                    "защита без типа урона");
                Throws<DataException>(
                    () => Parse(ConsumableJson(
                        "{\"useTime\":1,\"effects\":[{\"type\":\"modifier\",\"stat\":\"actionApCost\",\"value\":0.5}]}")),
                    "дробная цена действия");
            });

            Check("разбор: травмы и быстрый слот медицины", () =>
            {
                var item = Parse(ConsumableJson(
                    "{\"useTime\":1,\"usableInCombat\":true,\"effects\":[{\"type\":\"heal\",\"amount\":1}]," +
                    "\"healsInjuries\":[\"injury_broken_leg\"]}"));
                Equal(1, item.Consumable!.HealsInjuries.Count, "травма");

                Throws<DataException>(
                    () => Parse(ConsumableJson(
                        "{\"useTime\":1,\"effects\":[{\"type\":\"heal\",\"amount\":1}]," +
                        "\"healsInjuries\":[\"injury_broken_leg\",\"injury_broken_leg\"]}")),
                    "дубль травмы");
                Throws<DataException>(
                    () => Parse(ConsumableJson(
                        "{\"useTime\":1,\"usableInCombat\":false,\"effects\":[{\"type\":\"heal\",\"amount\":1}]}")),
                    "небоевая в быстром слоте");
            });

            Check("разбор: аптечка врага требует применений и врага", () =>
            {
                Throws<DataException>(
                    () => ActorTemplate.FromJson(
                        JsonParser.Parse(
                            "{\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                            "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":1,\"accuracy\":0.5," +
                            "\"attack\":{\"weaponId\":\"wpn_x\"},\"medkitItemId\":\"itm_medkit_normal\"}}",
                            "actors"),
                        "actors"),
                    "без применений");
                Throws<DataException>(
                    () => ActorTemplate.FromJson(
                        JsonParser.Parse(
                            "{\"id\":\"npc_x\",\"type\":\"npc\",\"nameKey\":\"n\"," +
                            "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":0,\"accuracy\":0.5," +
                            "\"medkitItemId\":\"itm_medkit_normal\",\"medkitCharges\":1}}",
                            "actors"),
                        "actors"),
                    "не врагу");
            });
        }

        private void ValidationTests()
        {
            Check("ошибка данных: медицина ссылается на несуществующую травму", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/a.json"),
                        ["items/a.json"] = ConsumableJson(
                            "{\"useTime\":1,\"usableInCombat\":true," +
                            "\"effects\":[{\"type\":\"heal\",\"amount\":1}]," +
                            "\"healsInjuries\":[\"injury_missing\"]}")
                    },
                    root =>
                    {
                        var report = Preflight.Validate(root);
                        True(HasAnyError(report, "Травма не найдена"), "нет ошибки ссылки: " + Describe(report));
                    });
            });

            Check("ошибка данных: аптечка врага ссылается на несуществующий предмет", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_x.json", "actors/a.json"),
                        ["items/wpn_x.json"] = Weapon("wpn_x"),
                        ["actors/a.json"] =
                            "{\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                            "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":1,\"accuracy\":0.5," +
                            "\"attack\":{\"weaponId\":\"wpn_x\"},\"medkitItemId\":\"itm_missing\",\"medkitCharges\":1}}"
                    },
                    root =>
                    {
                        var report = Preflight.Validate(root);
                        True(HasAnyError(report, "Предмет не найден"), "нет ошибки ссылки: " + Describe(report));
                    });
            });

            Check("ошибка данных: аптечка врага — не медицина", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("items/wpn_x.json", "actors/a.json"),
                        ["items/wpn_x.json"] = Weapon("wpn_x"),
                        ["actors/a.json"] =
                            "{\"kind\":\"actors\",\"id\":\"enemy_x\",\"type\":\"enemy\",\"nameKey\":\"n\"," +
                            "\"combat\":{\"tier\":1,\"level\":1,\"health\":10,\"ap\":5,\"xp\":1,\"accuracy\":0.5," +
                            "\"attack\":{\"weaponId\":\"wpn_x\"},\"medkitItemId\":\"wpn_x\",\"medkitCharges\":1}}"
                    },
                    root =>
                    {
                        var report = Preflight.Validate(root);
                        True(HasAnyError(report, "боевой медициной"), "нет ошибки предмета: " + Describe(report));
                    });
            });
        }

        private CombatSettingsTemplate CombatSettings() =>
            CombatSettingsTemplate.FromJson(
                JsonParser.Parse(File.ReadAllText(Path.Combine(_dataRoot, "combat.json")), "combat"),
                "combat");

    }
}
