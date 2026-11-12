#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    public sealed class DataLoadingTests : DataTestBase
    {
        public DataLoadingTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            LoadingTests(); InheritanceTests();
        }

        private void LoadingTests()
        {
            var data = Load(_dataRoot);
            Check("загрузка: тринадцать сущностей в коллекциях", () =>
            {
                Equal(39, data.All("items").Count, "items");
            });

            Check("загрузка: бронебойные патроны с пробитием", () =>
            {
                True(data.TryGet("items", "ammo_545_ap", out var ap), "ammo_545_ap не найден");
                Equal(0.05, ap.Get("penetration").Number, "пробитие 5.45 AP");
                True(data.TryGet("items", "ammo_762x54_ap", out var sniper), "ammo_762x54_ap не найден");
                Equal(0.1, sniper.Get("penetration").Number, "пробитие 7.62×54 AP");
            });

            Check("загрузка: цены переходов заданы в минутах", () =>
            {
                True(data.TryGet("topology", "topology", out var topology), "topology не найдена");
                var costs = topology.Get("costs");
                Equal(25.0, costs.Get("road").Get("minutes").Number, "дорога");
                Equal(40.0, costs.Get("path").Get("minutes").Number, "тропа");
                Equal(10.0, costs.Get("gate").Get("minutes").Number, "ворота");
                Equal(45.0, costs.Get("tunnel").Get("minutes").Number, "тоннель");
                Equal(5.0, costs.Get("corridor").Get("minutes").Number, "коридор");
                Equal(20.0, topology.Get("route").Get("hubReturn").Get("minutes").Number, "возврат в хаб");
                Equal(240.0, topology.Get("travel").Get("onFoot").Get("minutesPerStep").Number, "между хабами");
            });

            Check("загрузка: отдых восстанавливает фиксированные 2 выносливости в час", () =>
            {
                True(data.TryGet("topology", "topology", out var topology), "topology не найдена");
                Equal(2.0, topology.Get("rest").Get("staminaPerHour").Number, "ставка отдыха");
            });

            Check("загрузка: абстрактные шаблоны не зарегистрированы", () =>
            {
                var items = data.All("items");
                foreach (var item in items)
                {
                    var isAbstract = item.TryGet("abstract", out var value) && value.Kind == JsonKind.Bool && value.Bool;
                    False(isAbstract, "абстрактный файл попал в реестр");
                }
            });

            Check("загрузка: id и вид на месте", () =>
            {
                True(data.TryGet("items", "wpn_ak74", out _), "wpn_ak74 не найден");
                True(data.TryGet("items", "wpn_akm", out _), "wpn_akm не найден");
                False(data.TryGet("items", "wpn_unknown", out _), "лишний id");
            });

            Check("загрузка: правила прохождения", () =>
            {
                True(data.TryGet("rules", "rules", out var rules), "rules не найдены");
                Equal(3.0, rules.Get("baseRows").Number, "baseRows");
                Equal(32.0, rules.Get("maxRows").Number, "maxRows");
                Equal(4.0, rules.Get("tierCount").Number, "tierCount");
                Equal(0.15, rules.Get("insuranceChance").Number, "insuranceChance");
                Equal(0.05, rules.Get("insuranceStep").Number, "insuranceStep");
                Equal(0.4, rules.Get("xpLossChance").Number, "xpLossChance");
                Equal(0.1, rules.Get("xpLossStep").Number, "xpLossStep");
                Equal(1.0, rules.Get("xpLossMax").Number, "xpLossMax");
                Equal(0.3, rules.Get("respawnPercent").Number, "respawnPercent");
            });

            Check("загрузка: предмет по умолчанию утрачиваемый", () =>
            {
                var items = BuildServices().Resolve<IItemRepository>();
                False(items.Get("wpn_ak74").Unlosable, "оружие утрачиваемое");
            });

            Check("загрузка: опыт врагов из данных", () =>
            {
                var actors = BuildServices().Resolve<IActorRepository>();
                Equal(40, actors.Get("enemy_bandit").Combat!.Xp, "опыт бандита");
                Equal(100, actors.Get("mutant_bear").Combat!.Xp, "опыт медведя");
            });

            Check("загрузка: экономика и торговцы из данных", () =>
            {
                var container = BuildServices();
                var economy = container.Resolve<IEconomySettings>();
                Equal(1, economy.Rounding, "шаг округления");
                Equal(1, economy.MinPrice, "минимальная цена");
                var hub = container.Resolve<ILocationRepository>().Get("loc_hub_settlement");
                Equal(2, hub.Traders.Count, "торговцы хаба");
                var smugger = container.Resolve<IActorRepository>().Get("trd_hub_smuggler");
                True(smugger.Trader != null, "блок торговли");
                Equal(12, smugger.Trader!.RestockHours, "период пополнения");
            });

            Check("загрузка: квесты и настройки квестов из данных", () =>
            {
                var container = BuildServices();
                var quests = container.Resolve<IQuestRepository>();
                Equal(6, quests.All.Count, "квестов");
                True(quests.Contains("qst_clear_house"), "квест зачистки");
                var rules = container.Resolve<IGameRulesRepository>().Require();
                Equal(3, rules.MaxActiveQuests, "предел активных");
                Equal(48, rules.QuestCooldownHours, "откат по умолчанию");
                var hub = container.Resolve<ILocationRepository>().Get("loc_hub_settlement");
                True(hub.QuestBoard != null, "доска объявлений хаба");
                Equal(6, hub.QuestBoard!.Slots.Count, "слотов доски");
                Equal(2, CountDifficulty(hub.QuestBoard, QuestDifficulty.Easy), "простых слотов");
                Equal(2, CountDifficulty(hub.QuestBoard, QuestDifficulty.Medium), "средних слотов");
                Equal(2, CountDifficulty(hub.QuestBoard, QuestDifficulty.Hard), "сложных слотов");
                Equal(24, RefreshHours(hub.QuestBoard, QuestDifficulty.Easy), "таймер простого слота");
                Equal(48, RefreshHours(hub.QuestBoard, QuestDifficulty.Medium), "таймер среднего слота");
                Equal(72, RefreshHours(hub.QuestBoard, QuestDifficulty.Hard), "таймер сложного слота");
            });

            Check("загрузка: дерево талантов из данных", () =>
            {
                var container = BuildServices();
                var talents = container.Resolve<ITalentRepository>();
                Equal("talents", talents.Tree.Id, "id дерева");
                Equal(8, talents.Tree.Nodes.Count, "талантов");
                True(talents.Tree.TryGetNode("tal_steady_hand", out var first), "первый талант");
                Equal(5, first!.MaxRank, "рангов у первого");
                Equal(0.01, first.Effects[0].PerRank, "меткость за ранг");
                Equal("icons/tal_steady_hand", first.Icon, "картинка первого");
                True(talents.Tree.TryGetNode("tal_toughness", out var toughness), "талант правой ветки");
                Equal(1, toughness!.Row, "строка правой ветки");
                Equal(2, toughness.Column, "колонка правой ветки");
            });

            Check("загрузка: признак «не утрачиваемый» читается из данных", () =>
            {
                var item = Template(
                    "{\"kind\":\"items\",\"id\":\"quest_test\",\"nameKey\":\"n\",\"category\":\"quest\"," +
                    "\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",\"unlosable\":true}");
                True(item.Unlosable, "unlosable");
                False(Template(TestData.CrateJson).Unlosable, "по умолчанию false");
            });

            Check("загрузка: вес предмета обязателен и положителен", () =>
            {
                var zero = Throws<DataException>(() => Template(
                    "{\"kind\":\"items\",\"id\":\"bad_weight\",\"nameKey\":\"n\",\"category\":\"material\"," +
                    "\"footprint\":[1,1],\"maxStack\":1,\"weight\":0,\"tier\":1,\"icon\":\"i\"}"), "вес 0");
                Equal(CoreErrors.PositiveValue, zero.Code, "код ошибки");

                var missing = Throws<DataException>(() => Template(
                    "{\"kind\":\"items\",\"id\":\"no_weight\",\"nameKey\":\"n\",\"category\":\"material\"," +
                    "\"footprint\":[1,1],\"maxStack\":1,\"tier\":1,\"icon\":\"i\"}"), "нет веса");
                Equal(CoreErrors.MissingNumberField, missing.Code, "код ошибки");
            });

            Check("загрузка: прибавка к весу у рюкзака и брони", () =>
            {
                var backpack = Template(
                    "{\"kind\":\"items\",\"id\":\"bp_test\",\"nameKey\":\"n\",\"category\":\"backpack\"," +
                    "\"footprint\":[2,2],\"maxStack\":1,\"weight\":1.5,\"extraWeight\":10,\"tier\":1,\"icon\":\"i\"}");
                Equal(10d, backpack.ExtraWeight, "extraWeight рюкзака");

                var armor = Template(
                    "{\"kind\":\"items\",\"id\":\"exo_test\",\"nameKey\":\"n\",\"category\":\"armor\"," +
                    "\"footprint\":[2,3],\"maxStack\":1,\"weight\":22,\"extraWeight\":40,\"tier\":4,\"icon\":\"i\"," +
                    "\"material\":\"medium\",\"durabilityFactor\":1.0,\"durabilityMax\":50,\"quality\":1}");
                Equal(40d, armor.ExtraWeight, "extraWeight брони");

                var wrong = Throws<DataException>(() => Template(
                    "{\"kind\":\"items\",\"id\":\"wrong_weight\",\"nameKey\":\"n\",\"category\":\"material\"," +
                    "\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"extraWeight\":5,\"tier\":1,\"icon\":\"i\"}"),
                    "вес у материала");
                Equal(CoreErrors.ExtraWeightOnCategory, wrong.Code, "код ошибки");

                var negative = Throws<DataException>(() => Template(
                    "{\"kind\":\"items\",\"id\":\"negative_weight\",\"nameKey\":\"n\",\"category\":\"backpack\"," +
                    "\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"extraWeight\":-1,\"tier\":1,\"icon\":\"i\"}"),
                    "отрицательная прибавка");
                Equal(CoreErrors.NonNegative, negative.Code, "код ошибки");
            });
        }

        private void InheritanceTests()
        {
            var container = BuildServices();
            var items = container.Resolve<IItemRepository>();

            Check("наследование: поля базового шаблона", () =>
            {
                var ak74 = items.Get("wpn_ak74");
                Equal(2.0, ak74.Weapon!.ReloadTime, "reloadTime из _base");
                Equal("dmg_bullet", ak74.Weapon!.DamageTypeId, "damageTypeId из _base");
                Equal(100, ak74.Weapon!.DurabilityMax, "durabilityMax из _base");
                Equal(30, ak74.Weapon!.Magazine, "вместимость магазина из своего блока");
                Equal("5.45x39", ak74.Weapon!.AmmoCaliber, "калибр из своего блока");
                Equal(1, ak74.MaxStack, "maxStack из _base");
                True(ak74.Rotatable, "rotatable из _base");
            });

            Check("наследование: свои числа перекрывают базу", () =>
            {
                var ak74 = items.Get("wpn_ak74");
                var akm = items.Get("wpn_akm");
                Equal(8.0, ak74.Weapon!.Damage, "ak74 damage");
                Equal(14.0, akm.Weapon!.Damage, "akm damage");
                Equal(1.0, ak74.Weapon!.AccuracyCoefficient, "ak74 accuracy");
                Equal(1.0, akm.Weapon!.AccuracyCoefficient, "akm accuracy");
                Equal(1.5, items.Get("wpn_sv98").Weapon!.AccuracyCoefficient, "sv98 accuracy");
                Equal(2, ak74.Tier, "ak74 tier");
                Equal(3, akm.Tier, "akm tier");
            });

            Check("include: миксин цены и свои перекрытия", () =>
            {
                var ak74 = items.Get("wpn_ak74");
                Equal(PriceMode.Reputation, ak74.Price!.Mode, "ak74 price.mode");
                Equal(1200.0, ak74.Price!.Base, "ak74 price.base");
                Equal("fct_caravan", ak74.Price!.FactionId, "ak74 price.factionId");

                var akm = items.Get("wpn_akm");
                Equal(PriceMode.Fixed, akm.Price!.Mode, "akm price.mode");
                Equal(900.0, akm.Price!.Buy, "akm price.buy");
            });

            Check("include: sellable и droppable из миксина", () =>
            {
                var data = Load(_dataRoot);
                JsonValue akm = default!;
                True(data.TryGet("items", "wpn_akm", out akm), "akm не найден");
                True(akm.Get("sellable").Bool, "sellable");
                True(akm.Get("droppable").Bool, "droppable");
            });
        }

        private static int CountDifficulty(QuestBoardTemplate board, QuestDifficulty difficulty)
        {
            var count = 0;
            foreach (var slot in board.Slots)
            {
                if (slot.Difficulty == difficulty)
                {
                    count++;
                }
            }

            return count;
        }

        private static int RefreshHours(QuestBoardTemplate board, QuestDifficulty difficulty)
        {
            foreach (var slot in board.Slots)
            {
                if (slot.Difficulty == difficulty)
                {
                    return slot.RefreshHours;
                }
            }

            return 0;
        }
    }
}
