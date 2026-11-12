#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="HealingTests"/>: применение медицины вне боя.</summary>
    public sealed partial class HealingTests
    {
        private void OutOfCombatTests(IConsumableService medicines, IItemRepository items, ServiceContainer services)
        {
            Check("вне боя: аптечка лечит до предела и тратит применение", () =>
            {
                var player = NewPlayer(50);
                var medkit = AddToInventory(player, items, "itm_medkit_normal");
                Equal(2, medkit.Charges ?? -1, "зарядов до");

                var first = medicines.UseOutOfCombat(player, medkit, 100);
                True(first.Succeeded, "применение");
                Equal(30, first.HealthRestored, "вылечено");
                Equal(80, player.Health, "здоровье");
                Equal(1, first.ChargesLeft, "осталось");
                False(first.ItemConsumed, "предмет на месте");
                True(player.Inventory.Find(medkit.InstanceId) != null, "в сетке");

                var second = medicines.UseOutOfCombat(player, medkit, 100);
                True(second.Succeeded, "повтор");
                Equal(20, second.HealthRestored, "вылечено до предела");
                Equal(100, player.Health, "предел");
                True(second.ItemConsumed, "израсходован");
                True(player.Inventory.Find(medkit.InstanceId) == null, "убран из сетки");
            });

            Check("вне боя: шина снимает руки и ноги, но не голову", () =>
            {
                var player = NewPlayer(50);
                player.ApplyInjury(new InjuryState("injury_broken_arm", InjuryDurationKind.UntilHealed, 0));
                player.ApplyInjury(new InjuryState("injury_head_concussion", InjuryDurationKind.Hours, 4));
                var splint = AddToInventory(player, items, "itm_splint");

                var result = medicines.UseOutOfCombat(player, splint, 100);

                True(result.Succeeded, "применение");
                Equal(10, result.HealthRestored, "здоровье");
                Equal(1, result.InjuriesHealed.Count, "снята одна травма");
                Equal("injury_broken_arm", result.InjuriesHealed[0], "рука");
                Equal(1, player.Injuries.Count, "ушиб головы остался");
                Equal("injury_head_concussion", player.Injuries[0].Id, "голова");
            });

            Check("вне боя: сурв и большая аптечка снимают все травмы", () =>
            {
                var player = NewPlayer(50);
                player.ApplyInjury(new InjuryState("injury_broken_arm", InjuryDurationKind.UntilHealed, 0));
                player.ApplyInjury(new InjuryState("injury_broken_leg", InjuryDurationKind.UntilHealed, 0));
                player.ApplyInjury(new InjuryState("injury_head_concussion", InjuryDurationKind.Hours, 3));
                var surv = AddToInventory(player, items, "itm_surv");

                var result = medicines.UseOutOfCombat(player, surv, 100);
                True(result.Succeeded, "сурв");
                Equal(3, result.InjuriesHealed.Count, "все травмы");
                Equal(0, player.Injuries.Count, "травм нет");
                Equal(90, player.Health, "здоровье");

                var large = AddToInventory(player, items, "itm_medkit_large");
                player.SetHealth(40);
                var second = medicines.UseOutOfCombat(player, large, 100);
                Equal(60, second.HealthRestored, "до предела");
                Equal(100, player.Health, "предел");
            });

            Check("вне боя: лечить нечего — предмет не тратится", () =>
            {
                var player = NewPlayer(100);
                var medkit = AddToInventory(player, items, "itm_medkit_normal");
                var result = medicines.UseOutOfCombat(player, medkit, 100);

                False(result.Succeeded, "отказ");
                Equal(2, medkit.Charges ?? -1, "заряды целы");
                True(player.Inventory.Find(medkit.InstanceId) != null, "предмет на месте");
            });

            Check("вне боя: без зарядов медицина не применяется", () =>
            {
                var player = NewPlayer(50);
                var item = new ItemInstance("empty", "itm_medkit_normal", 1, charges: 0);
                False(medicines.CanUse(item, false), "нельзя");
                False(medicines.UseOutOfCombat(player, item, 100).Succeeded, "отказ");
            });

            Check("вне боя: последнее применение убирает предмет из быстрого слота", () =>
            {
                var player = NewPlayer(50);
                var splint = new ItemInstance("quick", "itm_splint", 1, charges: 1);
                player.Equipment.SetQuick(0, splint);

                var result = medicines.UseOutOfCombat(player, splint, 100);

                True(result.Succeeded, "применение");
                True(result.ItemConsumed, "израсходован");
                True(player.Equipment.GetQuick(0) == null, "слот пуст");
            });

            Check("медицина: боевая допускается вне боя, небоевая — нет", () =>
            {
                var player = NewPlayer(50);
                var medkit = AddToInventory(player, items, "itm_medkit_normal");
                var splint = AddToInventory(player, items, "itm_splint");
                var weapon = new ItemInstance("w", "wpn_ak74", 1, 100, 100, null, 30);

                True(medicines.CanUse(medkit, true), "аптечка в бою");
                True(medicines.CanUse(medkit, false), "аптечка вне боя");
                False(medicines.CanUse(splint, true), "шина в бою");
                True(medicines.CanUse(splint, false), "шина вне боя");
                False(medicines.CanUse(weapon, false), "оружие не медицина");
            });
        }
    }
}
