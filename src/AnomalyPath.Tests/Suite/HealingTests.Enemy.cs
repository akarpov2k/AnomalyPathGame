#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="HealingTests"/>: аптечка противника.</summary>
    public sealed partial class HealingTests
    {
        private void EnemyMedkitTests(ServiceContainer services, CombatSettingsTemplate settings)
        {
            Check("враг: аптечка лечит за ОД и кончается", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var state = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_merc"));
                var pistol = new ItemInstance("pm", "wpn_pm", 1, 100, 100, null, 8);
                True(combat.TryAttack(state, pistol, out var attack), "выстрел по наёмнику");
                True(attack.Hit, "попадание");
                var damaged = state.Enemy.Health;

                combat.EndTurn(state);
                var first = combat.TryEnemyUseMedkit(state);

                True(first.Succeeded, "первое применение");
                Equal(Math.Min(60, state.Enemy.MaxHealth - (int)damaged), first.HealthRestored, "вылечено");
                Equal(4 - 2, state.Enemy.Ap, "ОД противника");
                Equal(1, first.ChargesLeft, "осталось");
                False(first.ItemConsumed, "предмет не израсходован");

                combat.EndTurn(state);
                True(combat.TryAttack(state, pistol, out _), "второй выстрел");
                combat.EndTurn(state);
                var second = combat.TryEnemyUseMedkit(state);
                True(second.Succeeded, "второе применение");
                Equal(0, second.ChargesLeft, "заряды кончились");
                True(second.ItemConsumed, "израсходован");

                combat.EndTurn(state);
                True(combat.TryAttack(state, pistol, out _), "третий выстрел");
                combat.EndTurn(state);
                False(combat.TryEnemyUseMedkit(state).Succeeded, "аптечки больше нет");
            });

            Check("враг: без аптечки, не в свой ход или при полном здоровье — отказ", () =>
            {
                var combat = NewCombat(services, settings, new StubRandom(0.0));
                var mercState = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_merc"));
                False(combat.TryEnemyUseMedkit(mercState).Succeeded, "ход игрока");

                var ambush = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_merc"),
                    CombatScenario.Ambush);
                False(combat.TryEnemyUseMedkit(ambush).Succeeded, "здоровье полное");
                Equal(2, ambush.EnemyMedkitCharges, "заряды не потрачены");

                var mutant = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("mutant_bear"),
                    CombatScenario.Ambush);
                False(combat.TryEnemyUseMedkit(mutant).Succeeded, "аптечки нет");

                var bandit = combat.Start(
                    services.Resolve<IActorRepository>().Get("actor_player"),
                    PlayerEquipment(services),
                    services.Resolve<IActorRepository>().Get("enemy_bandit"),
                    CombatScenario.Ambush);
                False(combat.TryEnemyUseMedkit(bandit).Succeeded, "у бандита аптечки нет");
                Equal(0, bandit.EnemyMedkitCharges, "зарядов нет");
            });
        }
    }
}
