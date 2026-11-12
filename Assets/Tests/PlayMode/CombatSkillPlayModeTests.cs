#nullable enable
using System.Collections;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Боевой экран: скиллы активного оружия нарисованы кнопками, а «Очередь»
    /// автомата списывает три патрона и три отдельных выстрела.
    /// </summary>
    public sealed class CombatSkillPlayModeTests : PlayModeTestBase
    {
        /// <summary>Кнопка «Очередь» доступна в свой ход и тратит три патрона.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Skill_BurstSpendsThreeRounds()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = session.State.Player!;
            var weapon = new ItemInstance("test_ak", "wpn_ak74", 1, 100, 100, null, 30);
            player.Equipment.Set(EquipmentSlot.Main, weapon);

            var actor = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            Assert.IsTrue(
                screen.Sessions.StartCombat(session, actor, screen.Actors.Get("enemy_bandit")),
                "Бой не начался");
            screen.ShowCombat();
            yield return null;

            Click("CombatSkills");
            yield return null;

            var burst = GameObject.Find("CombatSkill_skill_burst");
            Assert.IsNotNull(burst, "Кнопка очереди не нарисована");
            Assert.IsTrue(burst!.GetComponent<Button>().interactable, "Очередь недоступна в свой ход");
            var before = weapon.MagazineRounds;

            Click("CombatSkill_skill_burst");
            yield return null;

            Assert.AreEqual(before - 3, weapon.MagazineRounds, "Очередь не списала три патрона");
        }
    }
}
