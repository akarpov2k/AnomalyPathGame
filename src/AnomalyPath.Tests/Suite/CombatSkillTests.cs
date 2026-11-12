#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Talents;

namespace AnomalyPath.Tests
{
    /// <summary>Действия боя: скиллы оружия и актёра, очередь, цена ОД и предпросмотр.</summary>
    public sealed class CombatSkillTests : DataTestBase
    {
        public CombatSkillTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            var services = BuildServices();
            var actors = services.Resolve<IActorRepository>();
            var combat = services.Resolve<ICombatService>();
            var equipment = services.Resolve<IEquipmentService>();
            var settings = services.Resolve<IEquipmentSettings>();
            var skills = services.Resolve<ISkillRepository>();

            Check("действия: без оружия и скиллов список пуст", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var actions = combat.GetActions(state, null);

                Equal(0, actions.Count, "действий");
            });

            Check("скилл: неизвестный id отклоняется", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_ak74");

                False(combat.TryUseSkill(state, weapon, "skill_unknown", out var result), "скилл");
                Equal(CombatActionAvailability.MissingSkill, result.Availability, "причина");
                Equal(3, state.Player.Ap, "ОД не тронуты");
                Equal(30, weapon.MagazineRounds, "патроны не тронуты");
            });

            Check("скилл: данные загружены", () =>
            {
                Equal(15, skills.All.Count, "скиллов");
                True(skills.Contains("skill_shot"), "базовый выстрел");
                True(skills.Contains("skill_stock"), "удар прикладом");
                True(skills.Contains("skill_bite"), "скилл мутанта");
            });

            Check("скилл мутанта: без оружия бьёт скиллом из данных", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"),
                    new Equipment("player", settings.QuickSlots),
                    actors.Get("mutant_wolf"),
                    CombatScenario.Ambush);
                var actions = combat.GetActions(state, null);

                Equal(1, actions.Count, "действий: укус");
                var bite = Action(actions, "skill_bite");
                Equal(0, bite.AmmoCost, "без патронов");
                True(bite.IsAvailable, "укус доступен");

                True(combat.TryUseSkill(state, null, "skill_bite", out var result), "укус выполнен");
                Equal(1, result.Hits.Count, "один удар");
                Equal(3, state.Enemy.Ap, "укус стоит одно ОД");
                True(
                    Math.Abs(state.Player.MaxHealth - state.Player.Health - result.Hits[0].Damage) < 1e-6,
                    "урон списан с игрока");
            });

            Check("действия: автомат предлагает одиночный и очередь", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var actions = combat.GetActions(state, Weapon("wpn_ak74"));

                Equal(6, actions.Count, "действий");
                var shot = Action(actions, "skill_shot");
                Equal(CombatActionKind.Skill, shot.Kind, "вид действия");
                Equal(1, shot.ApCost, "цена выстрела");
                Equal(1, shot.AmmoCost, "патроны выстрела");
                True(shot.IsAvailable, "выстрел доступен");
                var burst = Action(actions, "skill_burst");
                Equal(2, burst.ApCost, "цена очереди");
                Equal(3, burst.AmmoCost, "патроны очереди");
                True(burst.IsAvailable, "очередь доступна");
            });

            Check("голод: действия игрока дороже на 1 ОД", () =>
            {
                var state = Start(combat, actors, equipment, settings, playerApCostBonus: 1);
                var actions = combat.GetActions(state, Weapon("wpn_ak74"));

                Equal(2, Action(actions, "skill_shot").ApCost, "цена выстрела");
                Equal(3, Action(actions, "skill_burst").ApCost, "цена очереди");
            });

            Check("очередь: три отдельных выстрела, патроны и износ", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_ak74");
                True(combat.TryUseSkill(state, weapon, "skill_burst", out var result), "очередь выполнена");

                Equal(3, result.Hits.Count, "выстрелов");
                True(result.Hits[0].Hit && result.Hits[1].Hit && result.Hits[2].Hit, "все попали");
                Equal(21, result.TotalDamage, "три попадания по 7");
                Equal(24.0, state.Enemy.Health, "здоровье цели");
                Equal(27, weapon.MagazineRounds, "патроны");
                Equal(97, weapon.Durability!.Value, "износ за три выстрела");
                Equal(1, state.Player.Ap, "ОД");
            });

            Check("очередь без патронов недоступна", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 2);
                var burst = Action(combat.GetActions(state, weapon), "skill_burst");

                Equal(CombatActionAvailability.NoAmmo, burst.Availability, "причина");
                False(combat.TryUseSkill(state, weapon, "skill_burst", out var result), "очередь");
                Equal(CombatActionAvailability.NoAmmo, result.Availability, "результат");
                Equal(3, state.Player.Ap, "ОД не потрачены");
            });

            Check("приклад: КС-23 бьёт без патронов", () =>
            {
                var state = combat.Start(
                    actors.Get("actor_player"),
                    new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"),
                    playerModifiers: new TalentModifiers(accuracy: 0.5));
                var empty = new ItemInstance("w", "wpn_ks23", 1, 100, 100, null, 0);
                var stock = Action(combat.GetActions(state, empty), "skill_stock");

                Equal(0, stock.AmmoCost, "патроны");
                Equal(1, stock.ApCost, "ОД");
                Equal(CombatActionAvailability.Available, stock.Availability, "доступен");

                True(combat.TryUseSkill(state, empty, "skill_stock", out var result), "приклад выполнен");
                Equal(1, result.Hits.Count, "один удар");
                Equal(10, result.TotalDamage, "30 × 0.4 − защита 2");
                Equal(0, empty.MagazineRounds, "патроны не тронуты");
            });

            Check("снайперка: прицельный выстрел доступен и дороже", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_sv98");
                var actions = combat.GetActions(state, weapon);

                var aimed = Action(actions, "skill_aimed");
                True(aimed.IsAvailable, "снайперский выстрел");
                True(aimed.IsAvailable, "прицельный выстрел");
                Equal(2, aimed.ApCost, "цена прицельного");
                Equal(1, aimed.AmmoCost, "патроны прицельного");
            });

            Check("статус: модификаторы меняют точность и цену действия", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                state.Player.ApplyStatus(new CombatStatus(
                    "test_status",
                    new CombatModifiers(-0.2, -1)));

                Equal(0.66, Math.Round(combat.HitChance(state, Weapon("wpn_ak74")), 6), "точность");
                var shot = Action(combat.GetActions(state, Weapon("wpn_ak74")), "skill_shot");
                Equal(0, shot.ApCost, "цена действия уменьшена");
            });

            Check("скилл: не хватает ОД", () =>
            {
                var weak = new ActorTemplate(
                    "weak",
                    ActorType.Npc,
                    "n",
                    new CombatBlockTemplate(1, 1, 100, 1, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    weak,
                    new Equipment("player", settings.QuickSlots),
                    actors.Get("enemy_bandit"));
                var burst = Action(combat.GetActions(state, Weapon("wpn_ak74")), "skill_burst");

                Equal(CombatActionAvailability.NoAp, burst.Availability, "причина");
                False(combat.TryUseSkill(state, Weapon("wpn_ak74"), "skill_burst", out _), "очередь");
                Equal(1, state.Player.Ap, "ОД не тронуты");
            });

            Check("скилл: сломанное оружие недоступно", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var broken = new ItemInstance("w", "wpn_ak74", 1, 0, 100, null, 30);
                var action = Action(combat.GetActions(state, broken), "skill_shot");

                Equal(CombatActionAvailability.BrokenWeapon, action.Availability, "причина");
                False(combat.TryUseSkill(state, broken, "skill_shot", out _), "скилл");
            });

            Check("предпросмотр: одиночный выстрел — шанс, урон и отсутствие добивания", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_ak74");
                var preview = combat.PreviewSkill(state, weapon, "skill_shot");

                True(preview.Available, "доступен");
                Equal(1, preview.ApCost, "цена");
                Equal(1, preview.AmmoCost, "патроны");
                Equal(1, preview.Hits, "выстрелов");
                var chance = combat.HitChance(state, weapon);
                True(Math.Abs(preview.HitChance - chance) < 1e-9, "шанс");
                True(Math.Abs(preview.ExpectedDamage - chance * 7) < 1e-9, "урон 8.8 минус защита 2");
                True(Math.Abs(preview.KillChance) < 1e-9, "полного бандита не добивает");
            });

            Check("предпросмотр: очередь считает ожидание по «пожалей»", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var preview = combat.PreviewSkill(state, Weapon("wpn_ak74"), "skill_burst");

                True(preview.Available, "доступна");
                Equal(3, preview.Hits, "выстрелов");
                Equal(2, preview.ApCost, "цена");
                Equal(3, preview.AmmoCost, "патроны");
                True(preview.ExpectedDamage > 3 * preview.HitChance * 7, "промахи повышают следующие шансы");
                True(preview.ExpectedDamage <= 21 + 1e-9, "не больше полного урона");
            });

            Check("предпросмотр: приклад доступен с пустым магазином", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var preview = combat.PreviewSkill(state, new ItemInstance("w", "wpn_ks23", 1, 100, 100, null, 0), "skill_stock");

                True(preview.Available, "доступен");
                Equal(1, preview.ApCost, "цена");
                Equal(0, preview.AmmoCost, "патроны");
                True(preview.ExpectedDamage > 0, "урон ожидается");
            });

            Check("предпросмотр: малая цель — вероятность добить и обрезка урона", () =>
            {
                var weak = new ActorTemplate(
                    "weak_gunner",
                    ActorType.Enemy,
                    "n",
                    new CombatBlockTemplate(1, 1, 8, 5, new Dictionary<string, int>(), 0.8, 0));
                var state = combat.Start(
                    actors.Get("actor_player"),
                    new Equipment("player", settings.QuickSlots),
                    weak);
                var preview = combat.PreviewSkill(state, Weapon("wpn_ak74"), "skill_burst");

                True(preview.ExpectedDamage <= 8 + 1e-9, "ожидание обрезано здоровьем");
                True(preview.KillChance > 1 - Math.Pow(1 - preview.HitChance, 3) - 1e-9, "промахи повышают шанс добить");
                True(preview.KillChance <= 1 + 1e-9, "вероятность в пределах 0..1");
            });

            Check("предпросмотр: недоступный скилл отдаёт причину и цену", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var empty = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 2);
                var noAmmo = combat.PreviewSkill(state, empty, "skill_burst");
                False(noAmmo.Available, "нет патронов");
                Equal(CombatActionAvailability.NoAmmo, noAmmo.Availability, "причина");
                Equal(2, noAmmo.ApCost, "цена известна");

                var broken = new ItemInstance("w", "wpn_ak74", 1, 0, 100, null, 30);
                Equal(
                    CombatActionAvailability.BrokenWeapon,
                    combat.PreviewSkill(state, broken, "skill_shot").Availability,
                    "сломано");
                Equal(
                    CombatActionAvailability.MissingSkill,
                    combat.PreviewSkill(state, Weapon("wpn_ak74"), "skill_unknown").Availability,
                    "нет скилла");
            });
            Check("статус: очередь даёт отдачу, изготовка снимает её", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_ak74");
                var before = combat.HitChance(state, weapon);
                True(combat.TryUseSkill(state, weapon, "skill_burst", out _), "очередь");
                var afterBurst = combat.HitChance(state, weapon);
                True(afterBurst < before, "точность после отдачи ниже");
                True(state.Player.Modifiers.AccuracyBonus < 0, "отдача наложена");

                True(combat.TryUseSkill(state, weapon, "skill_brace", out _), "изготовка");
                True(state.Player.Modifiers.AccuracyBonus > -0.15, "отдача снята");
                True(combat.HitChance(state, weapon) > afterBurst, "точность выросла");

                combat.EndTurn(state);
                Equal(0, state.Player.Statuses.Count, "статусы истекли за ход");
            });

            Check("статус: подавление снижает меткость и повышает цену действий врага", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                True(
                    combat.TryUseSkill(state, Weapon("wpn_ak74"), "skill_suppression", out _),
                    "подавление");
                Equal(1, state.Enemy.Statuses.Count, "статус врага");
                True(Math.Abs(state.Enemy.Modifiers.AccuracyBonus + 0.15) < 1e-9, "меткость врага");
                Equal(1, state.Enemy.Modifiers.ActionApCostBonus, "цена действия врага");
            });

            Check("статус: прицеливание повышает урон следующего выстрела", () =>
            {
                var state = Start(combat, actors, equipment, settings);
                var weapon = Weapon("wpn_sv98");
                var before = combat.PreviewSkill(state, weapon, "skill_aimed");
                True(combat.TryUseSkill(state, weapon, "skill_sniper_prepare", out _), "прицеливание");
                var after = combat.PreviewSkill(state, weapon, "skill_aimed");
                True(after.ExpectedDamage > before.ExpectedDamage, "урон вырос");
                True(state.Player.Modifiers.DamageMultiplier > 0, "множитель урона");
            });
        }

        private static CombatAction Action(IReadOnlyList<CombatAction> actions, string skillId)
        {
            foreach (var action in actions)
            {
                if (string.Equals(action.SkillId, skillId, StringComparison.Ordinal))
                {
                    return action;
                }
            }

            throw new InvalidOperationException("Действие не найдено: " + skillId);
        }

        private static CombatState Start(
            ICombatService combat,
            IActorRepository actors,
            IEquipmentService equipment,
            IEquipmentSettings settings,
            int playerApCostBonus = 0)
        {
            return combat.Start(
                actors.Get("actor_player"),
                new Equipment("player", settings.QuickSlots),
                actors.Get("enemy_bandit"),
                playerApCostBonus: playerApCostBonus);
        }

        private static ItemInstance Weapon(string id) =>
            new ItemInstance("w", id, 1, 100, 100, null, MagazineSize(id));

        private static int MagazineSize(string id) => id switch
        {
            "wpn_pm" => 8,
            "wpn_sv98" => 10,
            "wpn_ks23" => 4,
            _ => 30,
        };
    }
}
