#nullable enable
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Боевой экран: быстрый слот с медициной применяет расходник за ОД и убирает его
    /// после последнего заряда; травма игрока видна строкой на экране.
    /// </summary>
    public sealed class CombatQuickSlotPlayModeTests : PlayModeTestBase
    {
        /// <summary>Медицина из быстрого слота тратится, а травма игрока показана в бою.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_QuickSlot_MedicineAndInjuryShown()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = session.State.Player!;
            var stim = new ItemInstance("quick_stim", "itm_stim_adrenaline", 1, charges: 1);
            player.Equipment.SetQuick(0, stim);
            player.SetInjuries(new List<InjuryState>
            {
                // Травма «до лечения» сама не истекает: строка травм останется видимой весь бой.
                new InjuryState("injury_broken_leg", InjuryDurationKind.UntilHealed, 0)
            });

            var actor = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            Assert.IsTrue(
                screen.Sessions.StartCombat(session, actor, screen.Actors.Get("enemy_bandit")),
                "Бой не начался");
            screen.ShowCombat();
            yield return null;

            var quick = GameObject.Find("CombatQuick0");
            Assert.IsNotNull(quick, "Быстрый слот не нарисован");
            Assert.IsTrue(quick!.GetComponent<Button>().interactable, "Медицину нельзя применить в свой ход");

            // Травма ноги добавляет ОД к любому действию, но полного запаса хватает на медицину.
            Click("CombatQuick0");
            yield return null;

            Assert.IsNull(player.Equipment.QuickSlots[0], "Израсходованный расходник остался в слоте");

            var injuries = GameObject.Find("CombatInjuries");
            Assert.IsNotNull(injuries, "Строка травм не создана");
            Assert.AreNotEqual(
                screen.Texts.Get("ui.combat_no_injuries"),
                injuries!.GetComponent<Text>().text,
                "Травма игрока не показана в бою");
        }

        /// <summary>Оружие из быстрого слота встаёт в активный слот, прежнее уходит в быстрый слот.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_QuickSlotWeapon_SwapsActiveWeapon()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = session.State.Player!;
            Assert.IsTrue(screen.Items.TryGet("wpn_sv98", out _), "Нет шаблона СВ-98");
            var activeBefore = player.Equipment.Get(EquipmentSlot.Main)?.ItemId;
            Assert.IsNotNull(activeBefore, "В основном слоте нет оружия");
            player.Equipment.SetQuick(0, new ItemInstance("quick_sv98", "wpn_sv98", 1, 100, 100, null, 10));

            var actor = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            Assert.IsTrue(
                screen.Sessions.StartCombat(session, actor, screen.Actors.Get("enemy_bandit")),
                "Бой не начался");
            screen.ShowCombat();
            yield return null;

            var quick = GameObject.Find("CombatQuick0");
            Assert.IsNotNull(quick, "Быстрый слот с оружием не нарисован");
            Assert.IsTrue(quick!.GetComponent<Button>().interactable, "Оружие нельзя взять в свой ход");

            Click("CombatQuick0");
            yield return null;

            var equipment = session.Combat!.Player.Equipment!;
            Assert.AreEqual(
                "wpn_sv98",
                equipment.Get(EquipmentSlot.Main)?.ItemId,
                "Оружие из быстрого слота не встало в активный слот");
            Assert.AreEqual(
                activeBefore,
                equipment.GetQuick(0)?.ItemId,
                "Прежнее активное оружие не встало в быстрый слот");
        }
    }
}
