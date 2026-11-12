#nullable enable
using System.Collections;
using AnomalyPath.Core.Session;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Ручной прогон боя: открывает боевой экран, делает несколько действий и показывает ход ИИ.
    /// Тест помечен <see cref="ExplicitAttribute"/> и категорией <c>Manual</c>: в «Run All» и в
    /// batch-прогоне без фильтра он не запускается — только выбором теста в Test Runner.
    /// </summary>
    public sealed class CombatPlayModeTests : PlayModeTestBase
    {
        /// <summary>Пауза между действиями: тест для наблюдения за экраном, а не для скорости.</summary>
        private const float StepPause = 2f;

        /// <summary>Защитный предел шагов ручного прогона.</summary>
        private const int MaxSteps = 12;

        /// <summary>Сколько ждать хода противника, прежде чем считать прогон зависшим.</summary>
        private const float EnemyTurnTimeout = 20f;

        /// <summary>
        /// Бой вручную: начать, ударить, перезарядиться, отдать ход ИИ, добить и вернуться.
        /// Каждое действие отделено паузой в две секунды, чтобы всё было видно на экране.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        [Explicit("Ручной прогон боя: долгие паузы по 2 секунды, запускать только из Test Runner")]
        [Category("Manual")]
        public IEnumerator Combat_ManualWalkthrough_WithTwoSecondPauses()
        {
            yield return StartGame();

            var screen = FindScreen();
            var session = screen!.Session!;
            Assert.IsNotNull(session, "новая игра не началась");

            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            var enemy = screen.Actors.Get("enemy_bandit");
            Assert.IsTrue(
                screen.Sessions.StartCombat(session, player, enemy),
                "бой должен начаться");
            screen.ShowCombat();
            yield return new WaitForSeconds(StepPause);

            Assert.IsTrue(session.InCombat, "бой должен начаться");
            Assert.IsNotNull(GameObject.Find("CombatAttack"), "боевой экран не открылся");

            ClickIfInteractable("CombatAttack");
            yield return new WaitForSeconds(StepPause);

            ClickIfInteractable("CombatSkills");
            yield return null;
            ClickIfInteractable("CombatReload");
            yield return new WaitForSeconds(StepPause);

            var logBefore = screen.Combat.LogLines.Count;
            ClickIfInteractable("CombatEndTurn");
            yield return WaitForPlayerTurn(session, EnemyTurnTimeout);

            var enemyActions = screen.Combat.LastEnemyActions;
            Assert.GreaterOrEqual(enemyActions.Count, 2, "противник должен сходить несколько раз подряд");
            Assert.AreEqual(
                logBefore + enemyActions.Count,
                screen.Combat.LogLines.Count,
                "на каждое действие противника — отдельная строка журнала");
            yield return new WaitForSeconds(StepPause);

            for (var step = 0; step < MaxSteps && session.InCombat; step++)
            {
                var combat = session.Combat;
                Assert.IsNotNull(combat, "бой потерялся во время прогона");
                if (combat!.IsFinished)
                {
                    break;
                }

                if (ReferenceEquals(combat.Current, combat.Player))
                {
                    ClickIfInteractable("CombatAttack");
                }
                else
                {
                    ClickIfInteractable("CombatEndTurn");
                }

                yield return WaitForPlayerTurn(session, EnemyTurnTimeout);
                yield return new WaitForSeconds(StepPause);
            }

            var finished = session.Combat == null || session.Combat.IsFinished;
            Assert.IsTrue(finished, "бой должен завершиться за отведённые шаги");

            if (GameObject.Find("CombatBack") != null)
            {
                Click("CombatBack");
                yield return new WaitForSeconds(StepPause);
            }

            Assert.IsFalse(session.InCombat, "после возврата бой должен быть закрыт");
        }

        /// <summary>Ждёт конца хода противника: ход перешёл игроку или бой завершён.</summary>
        private static IEnumerator WaitForPlayerTurn(GameSession session, float timeoutSeconds)
        {
            var deadline = Time.time + timeoutSeconds;
            while (Time.time < deadline)
            {
                var combat = session.Combat;
                if (combat == null || combat.IsFinished || ReferenceEquals(combat.Current, combat.Player))
                {
                    yield break;
                }

                yield return null;
            }
        }
    }
}
