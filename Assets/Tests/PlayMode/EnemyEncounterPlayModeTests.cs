#nullable enable
using System.Collections;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Бой с противником локации: объект пула даёт действие «Войти в бой»,
    /// клик по нему открывает боевой экран, побеждённый противник действий не даёт.
    /// </summary>
    public sealed class EnemyEncounterPlayModeTests : PlayModeTestBase
    {
        /// <summary>Id записи спавна противника, которую тест кладёт на локацию.</summary>
        private const string SpawnId = "spawn_enemy_bandit";

        /// <summary>Id актёра того же противника: название действия берётся из его записи.</summary>
        private const string ActorId = "enemy_bandit";

        /// <summary>Противник локации виден действием с названием из каталога и по клику начинает бой.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Enemy_OnExpedition_ActionStartsCombat()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen!.Session!;
            screen.Sessions.LeaveHub(session);
            var location = session.State.World.CurrentLocation;
            ClearObjects(location);
            EnsureEnemy(location);

            screen.ShowGame(false);
            yield return null;

            var action = FindObjectAction(SpawnId);
            Assert.IsNotNull(action, "противник не попал в действия локации");
            foreach (var text in action!.GetComponentsInChildren<Text>(true))
            {
                Assert.IsFalse(text.gameObject.activeSelf, "у фигуры противника нет подписей");
            }

            var icon = action.transform.Find("Icon");
            Assert.IsNotNull(icon, "у противника есть слой Icon");
            var portrait = screen.Actors.Get(ActorId).Portrait;
            Assert.IsNotNull(portrait, "у актёра есть портрет");
            Assert.AreEqual(
                portrait!.Split('/')[^1],
                icon!.GetComponent<Image>().sprite.name,
                "противник рисует портрет из данных");

            action.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.IsTrue(session.InCombat, "бой с противником локации должен начаться");
            Assert.IsNotNull(GameObject.Find("CombatAttack"), "боевой экран не открылся");
            var play = FindAnywhere("InGame");
            Assert.IsFalse(play != null && play.activeSelf, "экран локации скрыт за боевым");
        }

        /// <summary>Побеждённый противник больше не появляется и бой не предлагает.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Enemy_Defeated_NoAction()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen!.Session!;
            screen.Sessions.LeaveHub(session);
            var location = session.State.World.CurrentLocation;
            ClearObjects(location);
            var enemy = EnsureEnemy(location);
            enemy.MarkLooted();

            screen.ShowGame(false);
            yield return null;

            Assert.IsNull(location.Enemy, "на локации больше нет противника");
            Assert.IsNull(FindObjectAction(SpawnId), "побеждённый противник не показывается");
        }

        /// <summary>
        /// Противник для теста: живой противник локации, а если его нет — добавленный вручную.
        /// Второго противника с тем же актёром не добавляем: победа отмечает противника по id актёра.
        /// </summary>
        /// <param name="location">Текущая локация.</param>
        /// <returns>Объект-противник.</returns>
        private static EnemyLocationObject EnsureEnemy(LocationInstance location)
        {
            if (location.Enemy is EnemyLocationObject existing)
            {
                return existing;
            }

            var enemy = new EnemyLocationObject(SpawnId, "spawn.spawn_enemy_bandit.title", ActorId);
            location.AddObject(enemy);
            return enemy;
        }

        /// <summary>Убирает из локации все объекты, которые дал спавн, чтобы тест не зависел от seed.</summary>
        /// <param name="location">Текущая локация.</param>
        private static void ClearObjects(LocationInstance location)
        {
            foreach (var locationObject in location.Objects)
            {
                locationObject.MarkLooted();
            }
        }

        /// <summary>Ищет фигуру противника на сцене по id записи спавна.</summary>
        /// <param name="spawnId">Id записи спавна объекта.</param>
        /// <returns>Кнопка объекта или <c>null</c>.</returns>
        private static GameObject? FindObjectAction(string spawnId)
        {
            var root = GameObject.Find("InGame");
            if (root == null)
            {
                return null;
            }

            var actionName = "Object_" + spawnId;
            foreach (var button in root.GetComponentsInChildren<Button>(true))
            {
                if (string.Equals(button.name, actionName, System.StringComparison.Ordinal))
                {
                    return button.gameObject;
                }
            }

            return null;
        }
    }
}
