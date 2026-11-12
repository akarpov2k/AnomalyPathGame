#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using System.Collections.Generic;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.World;


namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="CombatTests"/>: скиллы, медицина, результат боя и настройки.</summary>
    public sealed partial class CombatTests
    {
        private void MoreCombatTests(ServiceContainer services, IActorRepository actors, ICombatService combat, IEquipmentService equipment, IEquipmentSettings settings, ICombatSettings combatSettings)
        {
            Check("голод: смена оружия дороже на 1 ОД", () =>
            {
                var gear = new Equipment("player", settings.QuickSlots);
                gear.Set(EquipmentSlot.Main, Weapon("wpn_ak74"));
                gear.Set(EquipmentSlot.Secondary, Weapon("wpn_pm"));
                var state = combat.Start(
                    actors.Get("actor_player"),
                    gear,
                    actors.Get("enemy_bandit"),
                    playerApCostBonus: 1);

                True(combat.TrySwapWeapon(state), "смена");

                Equal(state.Player.MaxAp - 2, state.Player.Ap, "цена с голодом");
            });

            Check("смена оружия: пустой запасной слот и чужой ход", () =>
            {
                var gear = new Equipment("player", settings.QuickSlots);
                gear.Set(EquipmentSlot.Main, Weapon("wpn_ak74"));
                var state = combat.Start(
                    actors.Get("actor_player"), gear, actors.Get("enemy_bandit"));

                False(combat.TrySwapWeapon(state), "нечего менять");
                combat.EndTurn(state);
                False(combat.TrySwapWeapon(state), "не ход игрока");
                Equal(3, state.Player.Ap, "ОД не тронуты");
            });

            Check("быстрый слот: оружие меняется с активным за ОД", () =>
            {
                var gear = new Equipment("player", settings.QuickSlots);
                var main = Weapon("wpn_ak74");
                var quick = Weapon("wpn_pm");
                gear.Set(EquipmentSlot.Main, main);
                gear.SetQuick(1, quick);
                var state = combat.Start(
                    actors.Get("actor_player"), gear, actors.Get("enemy_bandit"));

                True(combat.TryUseQuickWeapon(state, 1), "оружие из быстрого слота");

                True(ReferenceEquals(quick, combat.PlayerWeapon(state)), "активное оружие из слота");
                True(ReferenceEquals(main, gear.GetQuick(1)), "прежнее оружие встало в быстрый слот");
                Equal(state.Player.MaxAp - combatSettings.WeaponSwapApCost, state.Player.Ap, "цена смены оружия");
            });

            Check("быстрый слот: пустой слот, расходник и чужой ход", () =>
            {
                var gear = new Equipment("player", settings.QuickSlots);
                gear.Set(EquipmentSlot.Main, Weapon("wpn_ak74"));
                var state = combat.Start(
                    actors.Get("actor_player"), gear, actors.Get("enemy_bandit"));

                False(combat.TryUseQuickWeapon(state, 0), "пустой слот");
                gear.SetQuick(0, new ItemInstance("quick_stim_combat", "itm_stim_adrenaline", 1, charges: 1));
                False(combat.TryUseQuickWeapon(state, 0), "расходник — не оружие");
                gear.SetQuick(0, Weapon("wpn_pm"));
                combat.EndTurn(state);
                False(combat.TryUseQuickWeapon(state, 0), "не ход игрока");
                Equal(3, state.Player.Ap, "ОД не тронуты");
            });

            Check("меткость: отношение уровней больше 5 даёт 98%", () =>
            {
                var boss = new ActorTemplate(
                    "boss",
                    ActorType.Enemy,
                    "n",
                    new CombatBlockTemplate(1, 6, 10, 5, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    boss);
                combat.EndTurn(state);
                Equal(0.98, combat.HitChance(state, Weapon("wpn_ak74")), "уровень 6 против 1");
            });

            Check("меткость: ночью преимущество в уровнях тоже теряет 20%", () =>
            {
                var boss = new ActorTemplate(
                    "boss",
                    ActorType.Enemy,
                    "n",
                    new CombatBlockTemplate(1, 6, 10, 5, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    boss,
                    phase: TimePhase.Night,
                    nightAccuracyPenalty: 0.2);
                combat.EndTurn(state);
                Equal(0.78, System.Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "0.98 − 0.2");
            });

            Check("меткость: отношение уровней ровно 5 не даёт 98%", () =>
            {
                var boss = new ActorTemplate(
                    "boss",
                    ActorType.Enemy,
                    "n",
                    new CombatBlockTemplate(1, 5, 10, 5, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    boss);
                combat.EndTurn(state);
                Equal(0.88, System.Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "порог не превышен");
            });

            Check("меткость: ночью штраф вычитается из базы у игрока", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    phase: TimePhase.Night,
                    nightAccuracyPenalty: 0.2);
                Equal(0.66, System.Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "(0.8 − 0.2) × 1.1");
                Equal(TimePhase.Night, state.Phase, "фаза боя");
                Equal(0.2, state.NightAccuracyPenalty, "штраф в состоянии боя");
            });

            Check("меткость: ночью штраф действует и на врага", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"), CombatScenario.Ambush,
                    phase: TimePhase.Night,
                    nightAccuracyPenalty: 0.2);
                var weapon = state.EnemyWeapon!;
                var template = services.Resolve<IItemRepository>().Get(weapon.ItemId);
                var multiplier = WeaponStats.Multiplier(
                    weapon,
                    template,
                    services.Resolve<IRarityRepository>());
                var expected = (0.7 - 0.2) * template.Weapon!.AccuracyCoefficient * multiplier;
                Equal(
                    System.Math.Round(expected, 6),
                    System.Math.Round(combat.HitChance(state, weapon), 6),
                    "(0.7 − 0.2) × коэффициент оружия");
            });

            Check("меткость: днём и при нулевом штрафе ночь не меняет шанс", () =>
            {
                var day = Start(combat, actors, equipment, settings);
                Equal(0.88, System.Math.Round(combat.HitChance(day, Weapon("wpn_ak74")), 6), "день");
                var nightZero = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    phase: TimePhase.Night,
                    nightAccuracyPenalty: 0);
                Equal(0.88, System.Math.Round(combat.HitChance(nightZero, Weapon("wpn_ak74")), 6), "ночь без штрафа");
            });

            Check("меткость: ночной штраф не опускает базу ниже нуля", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    phase: TimePhase.Night,
                    nightAccuracyPenalty: 1);
                Equal(0, System.Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "шанс 0");
            });

            Check("меткость: ночью «пожалей» считается от штрафованной базы", () =>
            {
                var service = Custom(services, new StubRandom(0.99));
                var state = service.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    phase: TimePhase.Night,
                    nightAccuracyPenalty: 0.2);
                var weapon = Weapon("wpn_ak74");
                service.TryAttack(state, weapon, out var first);
                False(first.Hit, "промах");
                Equal(0.6963, System.Math.Round(service.HitChance(state, weapon), 6), "0.66 + 0.033");
            });

            Check("меткость: промах повышает шанс на 5% базового", () =>
            {
                var service = Custom(services, new StubRandom(0.99));
                var state = service.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"));
                var weapon = Weapon("wpn_ak74");
                service.TryAttack(state, weapon, out var first);
                False(first.Hit, "промах");
                Equal(0.9284, System.Math.Round(service.HitChance(state, weapon), 6), "0.88 + 0.044");
            });

            Check("меткость: поправка редкости прибавляется", () =>
            {
                var common = new ActorTemplate(
                    "common_gunner",
                    ActorType.Enemy,
                    "n",
                    new CombatBlockTemplate(1, 1, 10, 5, new Dictionary<string, int>(), 0.8, 0));
                var rare = new ActorTemplate(
                    "rare_gunner",
                    ActorType.Enemy,
                    "n",
                    new CombatBlockTemplate(1, 1, 10, 5, new Dictionary<string, int>(), 0.8, 0),
                    rarity: Rarity.Rare);
                var commonState = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    common);
                var rareState = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    rare);
                combat.EndTurn(commonState);
                combat.EndTurn(rareState);

                var commonChance = combat.HitChance(commonState, Weapon("wpn_ak74"));
                var rareChance = combat.HitChance(rareState, Weapon("wpn_ak74"));
                True(System.Math.Abs(rareChance - commonChance - 0.022) < 1e-9, "редкий точнее на 0.02 × 1.1");
            });

            Check("DI: боевой сервис доступен", () =>
            {
                True(services.IsRegistered<ICombatService>(), "зарегистрирован");
            });

            Check("настройки боя: цена смены оружия", () =>
            {
                var defaults = CombatSettingsTemplate.FromJson(JsonParser.Parse(CombatJson(), "combat"), "combat");
                Equal(1, defaults.WeaponSwapApCost, "по умолчанию");
                var custom = CombatSettingsTemplate.FromJson(
                    JsonParser.Parse(
                        CombatJson().Replace("\"weaponSwapApCost\":1", "\"weaponSwapApCost\":2"),
                        "combat"),
                    "combat");
                Equal(2, custom.WeaponSwapApCost, "из данных");
                Throws<DataException>(
                    () => CombatSettingsTemplate.FromJson(
                        JsonParser.Parse(
                            CombatJson().Replace("\"weaponSwapApCost\":1", "\"weaponSwapApCost\":-1"),
                            "combat"),
                        "combat"),
                    "отрицательная цена");
            });

            Check("настройки боя: порог аптечки ИИ", () =>
            {
                var defaults = CombatSettingsTemplate.FromJson(JsonParser.Parse(CombatJson(), "combat"), "combat");
                Equal(0.3, defaults.MedkitHpThreshold, "по умолчанию 30%");

                var custom = CombatSettingsTemplate.FromJson(
                    JsonParser.Parse(
                        CombatJson().Replace("\"medkitHpThreshold\":0.3", "\"medkitHpThreshold\":0.5"),
                        "combat"),
                    "combat");
                Equal(0.5, custom.MedkitHpThreshold, "из данных");

                Throws<DataException>(
                    () => CombatSettingsTemplate.FromJson(
                        JsonParser.Parse(
                            CombatJson().Replace("\"medkitHpThreshold\":0.3", "\"medkitHpThreshold\":1.5"),
                            "combat"),
                        "combat"),
                    "больше единицы");
                Throws<DataException>(
                    () => CombatSettingsTemplate.FromJson(
                        JsonParser.Parse(
                            CombatJson().Replace("\"medkitHpThreshold\":0.3", "\"medkitHpThreshold\":-0.1"),
                            "combat"),
                        "combat"),
                    "отрицательный порог");
            });
        }
    }
}
