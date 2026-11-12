#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using System.Collections.Generic;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Services;
using AnomalyPath.Core.Talents;

namespace AnomalyPath.Tests
{
    /// <summary>Бой: актёры, меткость, урон, износ, ОД и ходы.</summary>
    public sealed partial class CombatTests : DataTestBase
    {
        public CombatTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            var services = BuildServices();
            var actors = services.Resolve<IActorRepository>();
            var items = services.Resolve<IItemRepository>();
            var combat = services.Resolve<ICombatService>();
            var equipment = services.Resolve<IEquipmentService>();
            var settings = services.Resolve<IEquipmentSettings>();
            var combatSettings = services.Resolve<ICombatSettings>();

            Check("бой: цены перезарядки и смены оружия читаются из данных", () =>
            {
                Equal(1, combatSettings.ReloadApCost, "цена перезарядки");
                Equal(1, combatSettings.WeaponSwapApCost, "цена смены оружия");
            });

            Check("бой: актёры загружены", () =>
            {
                Equal(10, actors.All.Count, "актёры");
                Equal(45, actors.Get("enemy_bandit").Combat!.Health, "здоровье бандита");
                Equal(3, actors.Get("actor_player").Combat!.Ap, "ОД игрока");
                Equal(3, actors.Get("enemy_bandit").Combat!.Ap, "ОД бандита");
                Equal(4, actors.Get("enemy_merc").Combat!.Ap, "ОД наёмника");
                Equal(0.8, actors.Get("actor_player").Combat!.Accuracy, "меткость игрока");
            });

            Check("бой: атака врагов читается из данных", () =>
            {
                var bandit = actors.Get("enemy_bandit").Combat!.Attack!;
                Equal(2, bandit.WeaponIds.Count, "два оружия бандита");
                Equal("wpn_pm", bandit.WeaponIds[0], "первое оружие бандита");
                Equal("wpn_obrez", bandit.WeaponIds[1], "второе оружие бандита");
                False(bandit.IsNatural, "бандит бьёт оружием");
                var wolf = actors.Get("mutant_wolf").Combat!;
                True(wolf.Attack!.IsNatural, "волк бьёт естественно");
                Equal(8.0, wolf.Attack.Damage, "урон волка");
                Equal("skill_bite", wolf.DefaultSkillId, "дефолтный скилл волка");
                Equal("skill_bite", wolf.Skills[0], "скилл волка в списке");
                True(actors.Get("enemy_bandit").Combat!.DefaultSkillId == null, "у бандита скиллы в оружии");
            });

            Check("бой: оружие противника выбирается из пула с полным магазином", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var template = items.Get(state.EnemyWeapon!.ItemId);
                True(template.Weapon != null, "у выбранного предмета есть блок weapon");
                Equal(template.Weapon!.Magazine, state.EnemyWeapon.MagazineRounds, "патроны");

                var repeat = Start(combat, actors, equipment, settings);
                Equal(state.EnemyWeapon.ItemId, repeat.EnemyWeapon!.ItemId, "один seed — одно оружие");
            });

            Check("бой: пул оружия даёт оба варианта", () =>
            {
                var seen = new HashSet<string>();
                for (var seed = 0; seed < 64; seed++)
                {
                    var state = combat.Start(
                        actors.Get("actor_player"),
                        new Equipment("player", settings.QuickSlots),
                        actors.Get("enemy_bandit"),
                        enemyWeaponSeed: seed);
                    seen.Add(state.EnemyWeapon!.ItemId);
                }

                Equal(2, seen.Count, "оба оружия встречаются");
                True(seen.Contains("wpn_pm"), "пистолет");
                True(seen.Contains("wpn_obrez"), "обрез");
            });

            Check("бой: игрок ходит первым", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                True(ReferenceEquals(state.Current, state.Player), "первый ход игрока");
                Equal(CombatScenarioKind.Normal, state.Scenario.Kind, "обычный сценарий");
                Equal(CombatStartSide.Player, state.Scenario.First, "первый ход игрока");
            });

            Check("бой: засада — противник ходит первым", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"), CombatScenario.Ambush);
                True(ReferenceEquals(state.Current, state.Enemy), "первый ход противника");
                Equal(CombatScenarioKind.Ambush, state.Scenario.Kind, "сценарий");
                Equal(CombatStartSide.Enemy, state.Scenario.First, "сторона первого хода");
            });

            Check("бой: сюжетный сценарий — первым ходит заданная сторона", () =>
            {
                var enemyFirst = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    CombatScenario.Story(CombatStartSide.Enemy));
                True(ReferenceEquals(enemyFirst.Current, enemyFirst.Enemy), "первым ходит противник");
                Equal(CombatScenarioKind.Story, enemyFirst.Scenario.Kind, "сценарий");

                var playerFirst = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    CombatScenario.Story(CombatStartSide.Player));
                True(ReferenceEquals(playerFirst.Current, playerFirst.Player), "первым ходит игрок");
                Equal(CombatStartSide.Player, playerFirst.Scenario.First, "сторона первого хода");
            });

            Check("бой: неизвестная сторона сюжетного сценария отклоняется", () =>
            {
                Throws<System.ArgumentOutOfRangeException>(
                    () => CombatScenario.Story((CombatStartSide)99), "сторона");
            });

            Check("меткость: автомат без модификаторов 80%", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                Equal(0.88, System.Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "0.8 × 1.0 × 1.1");
            });

            Check("меткость: коэффициент точности оружия учитывается", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                Equal(0.64, System.Math.Round(combat.HitChance(state, Weapon("wpn_pm")), 6), "0.8 × 0.8");
                Equal(1.0, System.Math.Round(combat.HitChance(state, Weapon("wpn_sv98")), 6), "0.8 × 1.5 обрезано до 1");
            });

            Check("бой: таланты добавляют меткость, ОД и здоровье", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"),
                    new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    playerModifiers: new TalentModifiers(accuracy: 0.1, apMax: 2, healthMax: 10));
                Equal(5, state.Player.MaxAp, "максимум ОД");
                Equal(5, state.Player.Ap, "текущие ОД");
                Equal(110, state.Player.MaxHealth, "максимум здоровья");
                Equal(0.99, System.Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "0.9 × 1.1");
            });

            Check("бой: частичный режим восстанавливает ОД по ставке данных", () =>
            {
                var partial = new CombatService(
                    services.Resolve<IItemRepository>(),
                    services.Resolve<IDamageTypeRepository>(),
                    services.Resolve<IEquipmentService>(),
                    services.Resolve<IArmorWearService>(),
                    services.Resolve<IFormulaRepository>(),
                    Settings(ApRestoreKind.Partial, 2),
                    BalanceTemplate.Empty,
                    new StubRandom(0.99),
                    services.Resolve<IInventoryService>(),
                    services.Resolve<ISkillRepository>(),
                    services.Resolve<IRarityRepository>(),
                    services.Resolve<IInjuryRepository>());
                var state = partial.Start(
                    actors.Get("actor_player"),
                    new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"));
                True(partial.TryAttack(state, Weapon("wpn_ak74"), out _), "атака");
                Equal(2, state.Player.Ap, "ОД после атаки");
                partial.EndTurn(state);
                partial.EndTurn(state);
                Equal(3, state.Player.Ap, "ставка восстановила ОД");
            });

            Check("бой: урон вычитает защиту цели", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                True(combat.TryAttack(state, Weapon("wpn_sv98"), out var result), "атака");
                True(result.Hit, "попадание гарантировано");
                Equal(37, result.Damage, "39 − 2");
                Equal(8.0, state.Enemy.Health, "здоровье бандита");
            });

            Check("бой: базовые сопротивления персонажа берутся из данных актёра", () =>
            {
                var player = new Combatant(actors.Get("actor_player"));
                Equal(0, player.OwnProtection("dmg_bullet"), "пули");
                Equal(1, player.OwnProtection("dmg_electric"), "электричество");
                Equal(1, player.OwnProtection("dmg_chemical"), "химия");
                Equal(1, player.OwnProtection("dmg_fire"), "огонь");
                Equal(1, player.OwnProtection("dmg_rupture"), "разрыв");
                Equal(0, player.OwnProtection("dmg_wood"), "неизвестный тип");
            });

            Check("бой: базовая защита персонажа входит в урон", () =>
            {
                var service = Custom(services, new StubRandom(0.0));
                var state = service.Start(
                    actors.Get("actor_player"),
                    new Equipment("player", settings.QuickSlots),
                    actors.Get("mutant_wolf"));
                service.EndTurn(state);
                True(service.TryEnemyAttack(state, out var result), "атака волка");
                True(result.Hit, "попадание");
                Equal(7, result.Damage, "8 − 1 базовая защита от разрыва");
            });

            Check("бой: атака тратит ОД и изнашивает оружие", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_ak74");
                combat.TryAttack(state, weapon, out _);
                Equal(2, state.Player.Ap, "ОД");
                Equal(99, weapon.Durability!.Value, "прочность оружия");
            });

            Check("бой: промах не наносит урон, но тратит выстрел", () =>
            {
                var service = Custom(services, new StubRandom(0.99));
                var state = service.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"));
                var weapon = Weapon("wpn_ak74");
                True(service.TryAttack(state, weapon, out var result), "атака");
                False(result.Hit, "промах");
                Equal(0, result.Damage, "урон");
                Equal(45.0, state.Enemy.Health, "здоровье не изменилось");
                Equal(99, weapon.Durability!.Value, "оружие всё равно износилось");
                Equal(2, state.Player.Ap, "ОД потрачены");
            });

            Check("бой: сломанное оружие не стреляет", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var broken = new ItemInstance("w", "wpn_ak74", 1, 0, 100);
                False(combat.TryAttack(state, broken, out _), "атака");
            });

            Check("бой: без ОД атака недоступна", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var pistol = Weapon("wpn_pm");
                for (var i = 0; i < state.Player.MaxAp; i++)
                {
                    combat.TryAttack(state, pistol, out _);
                }

                Equal(0, state.Player.Ap, "ОД кончились");
                False(combat.TryAttack(state, pistol, out _), "атака без ОД");
            });

            Check("бой: смена хода восстанавливает ОД и считает раунды", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                combat.TryAttack(state, Weapon("wpn_ak74"), out _);
                combat.EndTurn(state);
                True(ReferenceEquals(state.Current, state.Enemy), "ходит враг");
                Equal(1, state.Round, "раунд");
                combat.EndTurn(state);
                True(ReferenceEquals(state.Current, state.Player), "снова игрок");
                Equal(3, state.Player.Ap, "ОД восстановлены");
                Equal(2, state.Round, "новый раунд");
            });

            Check("бой: попадание изнашивает броню и шлем", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                equipment.TryEquip(state.Player.Equipment!, new ItemInstance("a", "itm_armor_jacket", 1, 40, 40), EquipmentSlot.Armor, out _);
                equipment.TryEquip(state.Player.Equipment!, new ItemInstance("h", "itm_helmet_light", 1, 25, 25), EquipmentSlot.Helmet, out _);
                combat.EndTurn(state);
                True(combat.TryAttack(state, Weapon("wpn_sv98"), out var result), "атака врага");
                True(result.Hit, "попадание");
                Equal(35, result.Damage, "39 − (3 + 1)");
                Equal(37, state.Player.Equipment!.Get(EquipmentSlot.Armor)!.Durability!.Value, "броня");
                Equal(21, state.Player.Equipment.Get(EquipmentSlot.Helmet)!.Durability!.Value, "шлем");
            });

            Check("бой: защита артефакта уменьшает урон по здоровью", () =>
            {
                var playerEquipment = new Equipment("player", settings.QuickSlots);
                equipment.TryEquip(playerEquipment, Instance("itm_belt_artifact"), EquipmentSlot.Belt, out _);
                True(equipment.TryEquipArtifact(playerEquipment, Instance("art_gravity_lens"), out _), "артефакт");

                var service = Custom(services, new StubRandom(0.0));
                var state = service.Start(
                    actors.Get("actor_player"),
                    playerEquipment,
                    actors.Get("mutant_wolf"));
                service.EndTurn(state);
                True(service.TryEnemyAttack(state, out var result), "атака волка");
                True(result.Hit, "попадание");
                Equal(4, result.Damage, "8 − (1 базовая + 3 артефакт)");
            });

            Check("бой: смерть цели завершает бой", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var killed = false;
                for (var i = 0; i < 5 && !killed; i++)
                {
                    combat.TryAttack(state, Weapon("wpn_sv98"), out var result);
                    killed = result.Killed;
                    if (!killed)
                    {
                        combat.EndTurn(state);
                    }
                }

                True(killed, "бандит убит");
                True(state.IsFinished, "бой завершён");
            });

            Check("бой: выстрел тратит патрон из магазина", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_ak74");
                combat.TryAttack(state, weapon, out _);
                Equal(29, weapon.Magazine!.Value, "патронов в магазине");
            });

            Check("перезарядка берёт патроны из неполного стака и тратит ОД", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var inventory = services.Resolve<IInventoryService>();
                var grid = new GridContainer("ammo", 4, 4);
                inventory.TryAdd(grid, "ammo_545", 3, out _);
                inventory.TryAdd(grid, "ammo_545", 120, out _);
                var weapon = Weapon("wpn_ak74");
                combat.TryAttack(state, weapon, out _);
                Equal(29, weapon.Magazine!.Value, "после выстрела");
                True(combat.TryReload(state, weapon, grid), "перезарядка");
                Equal(30, weapon.Magazine!.Value, "магазин полон");
                Equal(1, state.Player.Ap, "ОД за перезарядку");
                Equal(122, inventory.Count(grid, "ammo_545"), "патроны взяты из неполного стака");
            });

            Check("перезарядка дробовика — по одному патрону", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var grid = new GridContainer("ammo", 4, 4);
                services.Resolve<IInventoryService>().TryAdd(grid, "ammo_23x75", 4, out _);
                var weapon = new ItemInstance("w", "wpn_ks23", 1, 120, 120, null, 0);

                True(combat.TryReload(state, weapon, grid), "первый патрон");
                Equal(1, weapon.MagazineRounds, "один патрон");
                Equal(state.Player.MaxAp - 1, state.Player.Ap, "ОД за патрон");
                True(combat.TryReload(state, weapon, grid), "второй патрон");
                Equal(2, weapon.MagazineRounds, "два патрона");
                Equal(state.Player.MaxAp - 2, state.Player.Ap, "ОД за два патрона");
            });

            Check("голод: перезарядка дороже на 1 ОД", () =>
            {
                var state = Start(combat, actors, equipment, settings, playerApCostBonus: 1);
                var grid = new GridContainer("ammo", 4, 4);
                services.Resolve<IInventoryService>().TryAdd(grid, "ammo_545", 30, out _);
                var weapon = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 10);

                True(combat.TryReload(state, weapon, grid), "перезарядка");

                Equal(state.Player.MaxAp - 2, state.Player.Ap, "цена с голодом");
            });

            Check("перезарядка без патронов не выполняется", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var grid = new GridContainer("ammo", 4, 4);
                var weapon = Weapon("wpn_ak74");
                combat.TryAttack(state, weapon, out _);
                False(combat.TryReload(state, weapon, grid), "перезарядка");
                Equal(2, state.Player.Ap, "ОД не потрачены");
            });

            Check("перезарядка берёт патроны из самых маленьких стопок", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var inventory = services.Resolve<IInventoryService>();
                var grid = new GridContainer("ammo", 4, 4);
                var template = services.Resolve<IItemRepository>().Get("ammo_545");
                var large = new ItemInstance("large", "ammo_545", 5);
                grid.Place(large, template, 0, 0, false);
                var small = new ItemInstance("small", "ammo_545", 2);
                grid.Place(small, template, 1, 0, false);
                var weapon = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 25);

                True(combat.TryReload(state, weapon, grid), "перезарядка");

                Equal(30, weapon.MagazineRounds, "магазин полон");
                Equal(2, large.Stack, "из большей стопки взяли остаток");
                True(grid.Find("small") == null, "меньшая стопка израсходована");
                Equal(2, inventory.Count(grid, "ammo_545"), "патроны в инвентаре");
            });

            Check("перезарядка учитывает патроны, оставшиеся в магазине", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var grid = new GridContainer("ammo", 4, 4);
                services.Resolve<IInventoryService>().TryAdd(grid, "ammo_545", 30, out _);
                var weapon = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 10);

                True(combat.TryReload(state, weapon, grid), "перезарядка");

                Equal(30, weapon.MagazineRounds, "магазин полон");
                Equal(10, services.Resolve<IInventoryService>().Count(grid, "ammo_545"), "взято ровно недостающее");
                Equal(2, state.Player.Ap, "ОД за перезарядку");
            });

            Check("перезарядка врага: пустой магазин заряжается полностью за ОД", () =>
            {
                var shooterActor = new ActorTemplate(
                    "shooter", ActorType.Enemy, "n",
                    new CombatBlockTemplate(1, 1, 50, 12, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    actors.Get("actor_player"), new Equipment("player", settings.QuickSlots),
                    shooterActor);
                combat.EndTurn(state);
                var pistol = Weapon("wpn_pm");
                for (var i = 0; i < 8; i++)
                {
                    combat.TryAttack(state, pistol, out _);
                }

                Equal(0, pistol.Magazine!.Value, "магазин пуст");
                Equal(4, state.Enemy.Ap, "ОД после восьми выстрелов");
                True(combat.TryAttack(state, pistol, out _), "атака после автоперезарядки");
                Equal(7, pistol.Magazine!.Value, "полный магазин минус выстрел");
                Equal(2, state.Enemy.Ap, "перезарядка и атака стоили 2 ОД");
            });

            Check("перезарядка игрока: пустой магазин не заряжается сам", () =>
            {
                var strongActor = new ActorTemplate(
                    "strong", ActorType.Npc, "n",
                    new CombatBlockTemplate(1, 1, 100, 12, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    strongActor, new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"));
                var pistol = Weapon("wpn_pm");
                for (var i = 0; i < 8; i++)
                {
                    combat.TryAttack(state, pistol, out _);
                }

                False(combat.TryAttack(state, pistol, out _), "нужна ручная перезарядка");
                Equal(0, pistol.Magazine!.Value, "магазин пуст");
            });

            Check("смена оружия: игрок тратит ОД и меняет активный слот", () =>
            {
                var gear = new Equipment("player", settings.QuickSlots);
                gear.Set(EquipmentSlot.Main, Weapon("wpn_ak74"));
                gear.Set(EquipmentSlot.Secondary, Weapon("wpn_pm"));
                var state = combat.Start(
                    actors.Get("actor_player"), gear, actors.Get("enemy_bandit"));

                Equal("wpn_ak74", combat.PlayerWeapon(state)!.ItemId, "основное");
                True(combat.TrySwapWeapon(state), "смена");
                Equal("wpn_pm", combat.PlayerWeapon(state)!.ItemId, "запасное");
                Equal(2, state.Player.Ap, "цена смены");
                True(combat.TrySwapWeapon(state), "обратная смена");
                Equal("wpn_ak74", combat.PlayerWeapon(state)!.ItemId, "основное снова");
                Equal(1, state.Player.Ap, "две смены");
            });

            MoreCombatTests(services, actors, combat, equipment, settings, combatSettings);
        }
    }
}
