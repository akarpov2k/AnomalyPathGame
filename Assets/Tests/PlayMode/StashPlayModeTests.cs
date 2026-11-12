#nullable enable
using System.Collections;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Minigames;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Тайник на игровом экране: действие локации сразу открывает префабный замок и его итог.</summary>
    public sealed class StashPlayModeTests : PlayModeTestBase
    {
        /// <summary>Код вводится кнопками символов, «Ввести» вскрывает замок.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Stash_LockMinigame_EntersCode()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.Sessions.LeaveHub(screen.Session!);
            var location = screen!.Session!.State.World.CurrentLocation;
            location.MarkStashRolled();
            location.MarkStashPresent();

            var state = screen.Sessions.LockState(screen.Session!)!;
            var puzzle = new LockPuzzle(
                StableHash.Combine(location.Seed, "stash_lock:" + location.InstanceId),
                state.CodeLength,
                state.AlphabetSize);

            screen.ShowGame(false);
            yield return null;
            Click("Stash");
            yield return null;

            foreach (var symbol in puzzle.CodeText())
            {
                Click("Symbol" + (symbol - '1'));
                yield return null;
            }

            Click("LockSubmit");
            yield return null;

            Assert.IsTrue(location.StashLockSolved, "Замок не вскрылся введённым кодом");
        }

        /// <summary>Без тайника действия тайника на экране нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Stash_WithoutStash_ActionHidden()
        {
            yield return StartGame();
            var screen = FindScreen();

            screen.ShowGame(false);
            yield return null;

            Assert.IsNull(GameObject.Find("Stash"), "Без тайника действия тайника быть не должно");
        }

        /// <summary>Тайник открывается отдельным экраном, вскрывается кнопкой и после этого неактивен.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Stash_Present_OpenedByClickAndDisabled()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.Sessions.LeaveHub(screen.Session!);
            var location = screen!.Session!.State.World.CurrentLocation;
            location.MarkStashRolled();
            location.MarkStashPresent();
            var state = screen.Sessions.LockState(screen.Session!)!;
            var puzzle = new LockPuzzle(
                StableHash.Combine(location.Seed, "stash_lock:" + location.InstanceId),
                state.CodeLength,
                state.AlphabetSize);
            screen.Sessions.GuessStashLock(screen.Session!, puzzle.CodeText());

            screen.ShowGame(false);
            yield return null;

            var action = GameObject.Find("Stash");
            Assert.IsNotNull(action, "Действие тайника не найдено");
            Assert.IsTrue(action!.GetComponent<Button>().interactable, "Тайник должен быть доступен");

            Click("Stash");
            yield return null;

            Assert.IsTrue(location.StashOpened, "Тайник вскрыт");
            var rewards = FindAnywhere("Rewards");
            if (rewards != null && rewards.activeSelf)
            {
                Click("RewardTakeAll");
                yield return null;
            }

            var after = GameObject.Find("Stash");
            Assert.IsNotNull(after, "Вскрытый тайник остаётся неактивным действием");
            Assert.IsFalse(after!.GetComponent<Button>().interactable, "Вскрытый тайник неактивен");
        }
    }
}
