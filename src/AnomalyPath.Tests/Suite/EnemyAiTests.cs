#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Services;

namespace AnomalyPath.Tests
{
    /// <summary>ИИ противника: план на бюджет ОД, добивание, сближение, перезарядка и детерминизм.</summary>
    public sealed partial class EnemyAiTests : DataTestBase
    {
        public EnemyAiTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            var services = BuildServices();
            var settings = Settings();
            var items = services.Resolve<IItemRepository>();

            var finisher = NewCombat(services, settings, new StubRandom(0));
            var finisherAi = new CombatAi(finisher, items, settings);
            var finisherState = finisher.Start(
                Player(10),
                EmptyEquipment(services),
                Enemy("ai_gunner", "wpn_ak74", 5, 1.0),
                CombatScenario.Ambush);

            Check("ИИ: добивает противника и тратит минимум ОД", () =>
            {
                var plan = finisherAi.Plan(finisherState);
                True(plan.Count >= 1, "план не пуст");
                Equal(EnemyTurnActionKind.Skill, plan[0].Kind, "первое действие — скилл");
                Equal("skill_rifle_aimed", plan[0].SkillId, "решающий выстрел");

                var result = finisherAi.TakeTurn(finisherState);
                True(result.PlayerKilled, "игрок погиб");
                True(result.Actions.Count >= 1, "действие выполнено");
                True(result.ApSpent <= 5, "бюджет не превышен");
                True(finisherState.Player.IsDead, "бой завершён");
            });

            Check("ИИ: без ОД план пуст", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_idle", "wpn_ak74", 0, 1.0),
                    CombatScenario.Ambush);

                Equal(0, ai.Plan(state).Count, "действий нет");
                var result = ai.TakeTurn(state);
                Equal(0, result.Actions.Count, "ход пуст");
                Equal(0, result.Damage, "урона нет");
            });

            Check("ИИ: перезаряжается, когда магазин пуст", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_pistol", "wpn_pm", 12, 1.0),
                    CombatScenario.Ambush);
                for (var i = 0; i < 8; i++)
                {
                    combat.TryAttack(state, state.EnemyWeapon!, out _);
                }

                Equal(0, state.EnemyWeapon!.MagazineRounds, "магазин пуст");
                True(combat.CanEnemyReload(state), "перезарядка доступна");

                var plan = ai.Plan(state);
                Equal(EnemyTurnActionKind.Reload, plan[0].Kind, "сначала перезарядка");

                var result = ai.TakeTurn(state);
                Equal(EnemyTurnActionKind.Reload, result.Actions[0].Step.Kind, "перезарядка выполнена");
                True(state.EnemyWeapon.MagazineRounds > 0, "патроны заряжены");
            });

            Check("ИИ: план детерминирован и не меняет бой", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_gunner", "wpn_ak74", 5, 0.8),
                    CombatScenario.Ambush);

                var first = ai.Plan(state);
                var second = ai.Plan(state);

                Equal(first.Count, second.Count, "длина");
                for (var i = 0; i < first.Count; i++)
                {
                    Equal(first[i].Kind, second[i].Kind, "вид");
                    Equal(first[i].SkillId, second[i].SkillId, "скилл");
                    Equal(first[i].ApCost, second[i].ApCost, "цена");
                    True(Math.Abs(first[i].ExpectedDamage - second[i].ExpectedDamage) < 1e-9, "ожидание");
                }

                Equal(5, state.Enemy.Ap, "ОД не тронуты планом");
                Equal(100, state.Player.Health, "здоровье игрока не тронуто");
            });

            Check("ИИ: не тратит ОД на добивание сверх нужного", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(5),
                    EmptyEquipment(services),
                    Enemy("ai_gunner", "wpn_ak74", 5, 1.0),
                    CombatScenario.Ambush);

                var plan = ai.Plan(state);
                Equal(1, plan.Count, "одно действие");
                Equal(EnemyTurnActionKind.Skill, plan[0].Kind, "скилл");
                Equal("skill_shot", plan[0].SkillId, "дешёвый выстрел");
            });

            Check("ИИ: выбирает скилл с наибольшим ожидаемым уроном в бюджете ОД", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_damage", "wpn_ak74", 2, 1.0),
                    CombatScenario.Ambush);

                var plan = ai.Plan(state);

                True(plan.Count > 0, "план есть");
                Equal(EnemyTurnActionKind.Skill, plan[0].Kind, "первое действие — скилл");
                Equal("skill_burst", plan[0].SkillId, "очередь выгоднее двух одиночных");
                True(plan[0].ExpectedDamage > 16, "ожидание больше двух одиночных выстрелов");
                Equal(2, plan[0].ApCost, "цена в бюджете ОД");
            });

            Check("ИИ: действие выбирается заново и выполняется по одному", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_stepwise", "wpn_ak74", 5, 1.0),
                    CombatScenario.Ambush);

                var plan = ai.Plan(state);
                True(plan.Count > 0, "план есть");

                True(ai.TryExecuteNext(state, out var first), "первое действие");
                Equal(plan[0].Kind, first.Step.Kind, "вид первого действия");
                Equal(plan[0].SkillId, first.Step.SkillId, "скилл первого действия");
                Equal(5 - plan[0].ApCost, state.Enemy.Ap, "ОД уменьшились только на первое действие");

                True(ai.TryExecuteNext(state, out var second), "второе действие");
                Equal(5 - plan[0].ApCost - second.Step.ApCost, state.Enemy.Ap, "ОД уменьшились на два действия");
            });

            Check("ИИ: ход целиком повторяет пошаговые действия", () =>
            {
                var stepCombat = NewCombat(services, settings, new StubRandom(0));
                var stepAi = new CombatAi(stepCombat, items, settings);
                var stepState = stepCombat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_turn", "wpn_ak74", 5, 1.0),
                    CombatScenario.Ambush);

                var stepActions = new List<EnemyTurnAction>();
                while (stepAi.TryExecuteNext(stepState, out var action))
                {
                    stepActions.Add(action);
                }

                var turnCombat = NewCombat(services, settings, new StubRandom(0));
                var turnAi = new CombatAi(turnCombat, items, settings);
                var turnState = turnCombat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_turn", "wpn_ak74", 5, 1.0),
                    CombatScenario.Ambush);

                var turn = turnAi.TakeTurn(turnState);

                Equal(stepActions.Count, turn.Actions.Count, "столько же действий");
                for (var i = 0; i < stepActions.Count; i++)
                {
                    Equal(stepActions[i].Step.Kind, turn.Actions[i].Step.Kind, "вид");
                    Equal(stepActions[i].Damage, turn.Actions[i].Damage, "урон");
                    Equal(stepActions[i].Hit, turn.Actions[i].Hit, "попадание");
                }
            });

            Check("ИИ: промах виден в действии", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.99));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_miss", "wpn_ak74", 5, 0.8),
                    CombatScenario.Ambush);

                var result = ai.TakeTurn(state);

                True(result.Actions.Count > 0, "действия выполнены");
                False(result.Actions[0].Hit, "первый выстрел промахнулся");
                Equal(0, result.Actions[0].Damage, "урона нет");
            });

            Check("ИИ: лечится аптечкой, когда здоровье ниже порога", () =>
            {
                var state = WeakenedEnemy(
                    services,
                    settings,
                    Enemy("ai_medkit", "wpn_ak74", 5, 1.0, "itm_medkit_normal", 2),
                    9,
                    100,
                    out _,
                    out var ai);

                True(state.Enemy.Health / state.Enemy.MaxHealth < 0.3, "здоровье ниже 30%");
                var damaged = state.Enemy.Health;

                var plan = ai.Plan(state);
                Equal(EnemyTurnActionKind.Medkit, plan[0].Kind, "первое действие — аптечка");
                Equal(2, plan[0].ApCost, "цена применения в ОД");

                var result = ai.TakeTurn(state);
                Equal(EnemyTurnActionKind.Medkit, result.Actions[0].Step.Kind, "аптечка выполнена");
                Equal(30, result.Actions[0].Healed, "вылечено аптечкой");
                Equal(damaged + 30, state.Enemy.Health, "здоровье выросло");
                Equal(1, state.EnemyMedkitCharges, "заряд израсходован");
                True(state.Enemy.Ap < 5, "ОД потрачены");
                True(result.Actions.Count > 1, "остаток хода доигран планом");
            });

            Check("ИИ: порог аптечки — строгая граница ниже доли", () =>
            {
                var enemy = Enemy("ai_threshold", "wpn_ak74", 5, 1.0, "itm_medkit_normal", 2);
                var damagedState = WeakenedEnemy(services, settings, enemy, 9, 100, out _, out _);
                var ratio = damagedState.Enemy.Health / damagedState.Enemy.MaxHealth;

                var atThreshold = WeakenedEnemy(services, Settings(ratio), enemy, 9, 100, out _, out var atThresholdAi);
                False(HasMedkit(atThresholdAi.Plan(atThreshold)), "на самом пороге не лечится");

                var belowThreshold = WeakenedEnemy(
                    services,
                    Settings(ratio + 1e-6),
                    enemy,
                    9,
                    100,
                    out _,
                    out var belowThresholdAi);
                True(HasMedkit(belowThresholdAi.Plan(belowThreshold)), "ниже порога лечится");
            });

            Check("ИИ: добивание важнее аптечки", () =>
            {
                var state = WeakenedEnemy(
                    services,
                    settings,
                    Enemy("ai_finish", "wpn_ak74", 5, 1.0, "itm_medkit_normal", 2),
                    9,
                    5,
                    out _,
                    out var ai);

                True(state.Enemy.Health / state.Enemy.MaxHealth < 0.3, "здоровье ниже порога");
                False(HasMedkit(ai.Plan(state)), "сначала добивание");

                var result = ai.TakeTurn(state);
                True(result.PlayerKilled, "игрок добит");
                Equal(2, state.EnemyMedkitCharges, "аптечка не тронута");
            });

            Check("ИИ: на полном здоровье аптечку не трогает", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0));
                var ai = new CombatAi(combat, items, settings);
                var state = combat.Start(
                    Player(100),
                    EmptyEquipment(services),
                    Enemy("ai_full_hp", "wpn_ak74", 5, 1.0, "itm_medkit_normal", 1),
                    CombatScenario.Ambush);

                False(HasMedkit(ai.Plan(state)), "лечить нечего");
                ai.TakeTurn(state);
                Equal(1, state.EnemyMedkitCharges, "заряд цел");
            });

            Check("ИИ: враг без аптечки в данных не лечится", () =>
            {
                var state = WeakenedEnemy(
                    services,
                    settings,
                    Enemy("ai_no_medkit", "wpn_ak74", 5, 1.0),
                    9,
                    100,
                    out _,
                    out var ai);

                True(state.Enemy.Health / state.Enemy.MaxHealth < 0.3, "здоровье ниже порога");
                False(HasMedkit(ai.Plan(state)), "аптечки нет");
            });

            Check("ИИ: потратив заряды, больше не лечится", () =>
            {
                var state = WeakenedEnemy(
                    services,
                    settings,
                    Enemy("ai_one_charge", "wpn_ak74", 5, 1.0, "itm_medkit_normal", 1),
                    9,
                    100,
                    out var combat,
                    out var ai);

                ai.TakeTurn(state);
                Equal(0, state.EnemyMedkitCharges, "единственный заряд израсходован");

                combat.EndTurn(state);
                var pistol = new ItemInstance("ai_pistol_shots", "wpn_ak74", 1, 100, 100, null, 30);
                for (var i = 0; i < 5; i++)
                {
                    combat.TryAttack(state, pistol, out _);
                }

                combat.EndTurn(state);
                True(state.Enemy.Health > 0, "жив");
                True(state.Enemy.Health / state.Enemy.MaxHealth < 0.3, "здоровье снова низкое");
                False(HasMedkit(ai.Plan(state)), "зарядов нет");
            });

            Check("ИИ: без ОД на аптечку не лечится", () =>
            {
                var state = WeakenedEnemy(
                    services,
                    settings,
                    Enemy("ai_low_ap", "wpn_ak74", 1, 1.0, "itm_medkit_normal", 2),
                    9,
                    100,
                    out _,
                    out var ai);

                True(state.Enemy.Ap < items.Get("itm_medkit_normal").Consumable!.UseTime, "ОД меньше цены");
                False(HasMedkit(ai.Plan(state)), "аптечка не по карману");
            });
        }

        /// <summary>Есть ли в плане шаг аптечки.</summary>
    }
}
