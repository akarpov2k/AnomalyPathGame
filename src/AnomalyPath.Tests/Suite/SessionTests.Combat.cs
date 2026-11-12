#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.World;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="SessionTests"/>: бой — старт, атака, скиллы, ход ИИ и медицина.</summary>
    public sealed partial class SessionTests
    {
        private void CombatTests(
            ServiceContainer services,
            IGameSessionService sessions,
            IActorRepository actors,
            IItemRepository items)
        {
            Check("сессия: состояние обязательно", () =>
            {
                Throws<ArgumentNullException>(() => new GameSession(null!), "конструктор");
            });

            Check("сессия: бой стартует один раз и сменяет ход", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var player = actors.Get("actor_player");
                var enemy = actors.Get("enemy_bandit");

                True(sessions.StartCombat(session, player, enemy), "старт");
                True(session.InCombat, "бой идёт");
                Equal(1, session.Combat!.Round, "раунд");
                False(sessions.StartCombat(session, player, enemy), "второй бой запрещён");

                sessions.EndTurn(session);
                True(ReferenceEquals(session.Combat!.Current, session.Combat.Enemy), "ход у противника");

                sessions.EndCombat(session);
                False(session.InCombat, "бой завершён");
            });

            Check("сессия: сценарий засады передаётся в бой", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var player = actors.Get("actor_player");
                var enemy = actors.Get("enemy_bandit");

                True(sessions.StartCombat(
                    session, player, enemy, CombatScenario.Ambush), "старт");

                True(ReferenceEquals(session.Combat!.Current, session.Combat.Enemy), "первым ходит противник");
                Equal(CombatScenarioKind.Ambush, session.Combat.Scenario.Kind, "сценарий");
            });

            Check("сессия: бой получает фазу суток и ночной штраф из настроек", () =>
            {
                var session = NewSession(
                    services,
                    withPlayer: true,
                    new NewGameSettings(0, 1, nightAccuracyPenalty: 0.25));
                session.State.World.Clock.Advance(12);

                True(sessions.StartCombat(
                    session,
                    actors.Get("actor_player"),
                    actors.Get("enemy_bandit")), "старт");

                Equal(TimePhase.Night, session.Combat!.Phase, "фаза");
                Equal(0.25, session.Combat.NightAccuracyPenalty, "штраф");
            });

            Check("сессия: результат боя без боя ничего не делает", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var outcome = sessions.ApplyCombatOutcome(session);
                Equal(CombatOutcomeStatus.NoCombat, outcome.Status, "статус");
                False(session.InCombat, "боя нет");
            });

            Check("сессия: ход противника исполняется ИИ и возвращает ход игроку", () =>
            {
                var session = NewSession(services, withPlayer: true);
                True(sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")), "старт");
                sessions.EndTurn(session);

                var result = sessions.RunEnemyTurn(session);

                True(result.Actions.Count > 0, "противник сходил");
                True(ReferenceEquals(session.Combat!.Current, session.Combat.Player), "ход игрока");
                True(session.Combat.Round >= 2, "раунд");
            });

            Check("сессия: ход ИИ без боя и не в свой ход пуст", () =>
            {
                var session = NewSession(services, withPlayer: true);
                Equal(0, sessions.RunEnemyTurn(session).Actions.Count, "без боя");
                False(sessions.TryRunEnemyTurnStep(session, out _), "шаг без боя");

                True(sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")), "старт");
                Equal(0, sessions.RunEnemyTurn(session).Actions.Count, "не ход противника");
                False(sessions.TryRunEnemyTurnStep(session, out _), "шаг не в ход противника");
                True(ReferenceEquals(session.Combat!.Current, session.Combat.Player), "ход не передан");
            });

            Check("сессия: ход противника идёт по одному действию", () =>
            {
                var session = NewSession(services, withPlayer: true);
                True(sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")), "старт");
                sessions.EndTurn(session);

                True(sessions.TryRunEnemyTurnStep(session, out _), "первое действие выполнено");
                Equal("enemy_bandit", session.Combat!.Current.Template.Id, "ход остался у противника");

                var rest = sessions.RunEnemyTurn(session);
                True(rest.Actions.Count > 0, "остаток хода исполнен");
                True(ReferenceEquals(session.Combat.Current, session.Combat.Player), "после хода ходит игрок");
            });

            Check("сессия: атака тратит ОД и патрон", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var player = actors.Get("actor_player");
                var enemy = actors.Get("enemy_bandit");
                True(sessions.StartCombat(session, player, enemy), "старт");

                var weapon = new ItemInstance(
                    "inst_pm",
                    "wpn_pm",
                    1,
                    magazine: items.Get("wpn_pm").Weapon!.Magazine);
                var magazine = weapon.MagazineRounds;
                var ap = session.Combat!.Player.Ap;

                True(sessions.TryAttack(session, weapon, out _), "атака выполнена");
                Equal(ap - 1, session.Combat.Player.Ap, "ОД");
                Equal(magazine - 1, weapon.Magazine, "патроны");
            });

            Check("сессия: без игрока бой не начинается", () =>
            {
                var session = NewSession(services, withPlayer: false);
                False(sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")), "старт");
                False(session.InCombat, "боя нет");
            });

            Check("сессия: боевые действия без боя отклоняются", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var weapon = new ItemInstance("inst_pm", "wpn_pm", 1);

                False(sessions.TryAttack(session, weapon, out _), "атака");
                False(sessions.TryReload(session, weapon), "перезарядка");
            });

            Check("сессия: действия боя включают скиллы автомата", () =>
            {
                var session = NewSession(services, withPlayer: true);
                session.State.Player!.Equipment.Set(
                    EquipmentSlot.Main,
                    new ItemInstance("inst_ak", "wpn_ak74", 1, magazine: 30));
                True(
                    sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")),
                    "старт");

                var burst = Action(sessions.GetCombatActions(session), "skill_burst");

                Equal(CombatActionKind.Skill, burst.Kind, "вид действия");
                Equal(2, burst.ApCost, "цена в ОД");
                Equal(3, burst.AmmoCost, "патроны");
                True(burst.IsAvailable, "очередь доступна");
            });

            Check("сессия: очередь через сессию тратит три патрона", () =>
            {
                var session = NewSession(services, withPlayer: true);
                var weapon = new ItemInstance("inst_ak", "wpn_ak74", 1, magazine: 30);
                session.State.Player!.Equipment.Set(EquipmentSlot.Main, weapon);
                True(
                    sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")),
                    "старт");

                True(sessions.TryUseCombatSkill(session, "skill_burst", out var result), "очередь выполнена");

                Equal(3, result.Hits.Count, "выстрелов");
                Equal(27, weapon.MagazineRounds, "патроны");
                Equal(1, session.Combat!.Player.Ap, "ОД");
            });

            Check("сессия: скилл вне боя отклоняется", () =>
            {
                var session = NewSession(services, withPlayer: true);

                False(sessions.TryUseCombatSkill(session, "skill_burst", out _), "очередь");
                Equal(0, sessions.GetCombatActions(session).Count, "действий нет");
            });

            Check("сессия: в чужой ход действий и скиллов игрока нет", () =>
            {
                var session = NewSession(services, withPlayer: true);
                session.State.Player!.Equipment.Set(
                    EquipmentSlot.Main,
                    new ItemInstance("inst_ak", "wpn_ak74", 1, magazine: 30));
                True(
                    sessions.StartCombat(session, actors.Get("actor_player"), actors.Get("enemy_bandit")),
                    "старт");
                sessions.EndTurn(session);

                Equal(0, sessions.GetCombatActions(session).Count, "действий нет");
                False(sessions.TryUseCombatSkill(session, "skill_burst", out _), "очередь в чужой ход");
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
    }
}
