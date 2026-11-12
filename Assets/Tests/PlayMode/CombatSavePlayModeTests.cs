#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Сохранение во время боя: с боевого экрана нет пути в меню, пункт сохранения
    /// в меню игры неактивен, экран сохранения не открывается.
    /// </summary>
    public sealed class CombatSavePlayModeTests : PlayModeTestBase
    {
        /// <summary>На боевом экране нет кнопки меню, а сохранение в меню игры неактивно.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_NoMenuPath_AndSaveButtonDisabled()
        {
            yield return StartCombat();

            Assert.IsNull(GameObject.Find("Gear"), "на боевом экране нет кнопки меню");
            Assert.IsNull(GameObject.Find("GameMenu"), "меню игры из боя не открывается");

            FindScreen().ShowGameMenu();
            yield return null;

            var save = GameObject.Find("Сохранить игру");
            Assert.IsNotNull(save, "пункт сохранения в меню не найден");
            Assert.IsFalse(
                save!.GetComponent<Button>().interactable,
                "во время боя пункт сохранения неактивен");
        }

        /// <summary>Экран сохранения в бою не открывается.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_SaveScreen_DoesNotOpen()
        {
            yield return StartCombat();

            FindScreen().ShowSave();
            yield return null;

            var screen = GameObject.Find("SaveGame");
            Assert.IsTrue(
                screen == null || !screen.activeSelf,
                "в бою экран сохранения не открывается");
        }

        /// <summary>Начинает новую игру, уходит из хаба и поднимает бой с противником.</summary>
        /// <returns>Корутина подготовки.</returns>
        private static IEnumerator StartCombat()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            screen.Sessions.LeaveHub(session);
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
