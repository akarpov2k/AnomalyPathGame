#nullable enable
using System.Collections;
using System.IO;
using AnomalyPath.Core.World;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Ручные кадры полного игрового экрана: хаб днём, дорога днём и дорога ночью.
    /// Требует Game View, поэтому запускается только в обычном режиме редактора (не batch).
    /// </summary>
    public sealed class LocationScreenScreenshotPlayModeTests : PlayModeTestBase
    {
        private const string Folder = "Logs/Screenshots";

        /// <summary>Снимает экран локации с интерфейсом в трёх состояниях.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        [Explicit]
        [Category("Manual")]
        public IEnumerator Screen_CapturesHubAndPath()
        {
            Directory.CreateDirectory(Folder);
            yield return StartGame();
            yield return null;
            yield return Capture("location-screen-hub");

            Click("LeaveHub");
            yield return null;
            for (var step = 0; step < 8; step++)
            {
                if (FindTransition("Right") != null || FindTransition("Left") != null)
                {
                    break;
                }

                var forward = FindTransition("Center");
                if (forward == null)
                {
                    break;
                }

                forward.onClick.Invoke();
                yield return null;
            }

            yield return Capture("location-screen-path-day");

            var screen = FindScreen();
            var world = screen.Session!.State.World;
            world.Clock.Advance(12);
            screen.ShowGame(false);
            yield return null;
            Assert.AreEqual(TimePhase.Night, world.Clock.Phase, "третий кадр должен быть ночью");
            yield return Capture("location-screen-path-night");
        }

        private static IEnumerator Capture(string name)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, name + ".png"));
            yield return null;
            yield return null;
            Assert.IsTrue(
                File.Exists(Path.Combine(Folder, name + ".png")),
                $"Кадр не записан: {name}");
        }
    }
}
