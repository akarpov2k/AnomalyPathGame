#nullable enable
using System.Collections;
using AnomalyPath.Core.Spawns;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Побег из боя через экран: кнопка «Выйти из боя» спрашивает подтверждение, Esc и «Отмена»
    /// оставляют бой, подтверждение закрывает бой без победы и возвращает на локацию без противника.
    /// </summary>
    public sealed class CombatFleePlayModeTests : PlayModeTestBase
    {
        /// <summary>Кнопка побега требует подтверждения; Esc и «Отмена» бой не закрывают.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_FleeAsksConfirmation_CancelAndEscapeKeepCombat()
        {
            yield return StartCombat();
            var screen = FindScreen();

            Click("CombatFlee");
            yield return null;

            var dialog = GameObject.Find("ConfirmDialog");
            Assert.IsNotNull(dialog, "подтверждение побега не открылось");
            Assert.IsTrue(dialog!.activeSelf, "подтверждение побега не открылось");

            Assert.IsTrue(screen.HandleEscape(), "Esc в подтверждении побега не обработан");
            yield return null;
            Assert.IsFalse(dialog.activeSelf, "Esc не закрыл подтверждение побега");
            Assert.IsTrue(screen.Session!.InCombat, "Esc закрыл бой");

            Click("CombatFlee");
            yield return null;
            Click("Отмена");
            yield return null;
            Assert.IsFalse(dialog.activeSelf, "«Отмена» не закрыла подтверждение побега");
            Assert.IsTrue(screen.Session!.InCombat, "«Отмена» закрыла бой");
        }

        /// <summary>Подтверждённый побег закрывает бой без победы, лута и противника на локации.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_FleeConfirmed_EndsCombatWithoutVictory()
        {
            yield return StartCombat();
            var screen = FindScreen();
            var session = screen.Session!;
            var location = session.State.World.CurrentLocation;
            var enemy = location.FindEnemy("enemy_bandit");
            Assert.IsNotNull(enemy, "противник локации для теста не найден");

            Click("CombatFlee");
            yield return null;
            Click("Сбежать");
            yield return null;

            Assert.IsFalse(session.InCombat, "побег не закрыл бой");

            var attack = FindAnywhere("CombatAttack");
            Assert.IsFalse(attack != null && attack.activeInHierarchy, "боевой экран остался открытым");
            Assert.IsNotNull(GameObject.Find("InGame"), "побег не вернул на экран локации");

            var rewards = FindAnywhere("Rewards");
            Assert.IsFalse(rewards != null && rewards.activeInHierarchy, "побег не должен открывать награды");

            Assert.IsTrue(location.EnemyGone, "побег не отметил противника ушедшим с локации");
            Assert.IsTrue(enemy!.Looted, "сбежавший противник больше не появляется на локации");
        }

        /// <summary>Начинает новую игру, уходит из хаба и поднимает бой обычным сценарием.</summary>
        /// <returns>Корутина подготовки.</returns>
        private static IEnumerator StartCombat()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            screen.Sessions.LeaveHub(session);
            var location = session.State.World.CurrentLocation;
            if (location.FindEnemy("enemy_bandit") == null)
            {
                location.AddObject(new EnemyLocationObject(
                    "spawn_enemy_bandit",
                    "spawn.spawn_enemy_bandit.title",
                    "enemy_bandit"));
            }

            Assert.IsTrue(
                screen.Sessions.StartCombat(
                    session,
                    screen.Actors.Get("actor_player"),
                    screen.Actors.Get("enemy_bandit")),
                "бой не начался");
            screen.ShowCombat();
            yield return null;
        }
    }
}
