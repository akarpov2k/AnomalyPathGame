#nullable enable
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Ручной кадр окна «Инвентарь» целиком, с задником текущей локации: показывает,
    /// что окно стоит на фоне хаба, а не на плоском цвете. Требует Game View,
    /// поэтому запускается только в обычном режиме редактора (не batch).
    /// </summary>
    public sealed class InventoryScreenScreenshotPlayModeTests : PlayModeTestBase
    {
        private const string Folder = "Logs/Screenshots";

        /// <summary>Снимает окно инвентаря, открытое в хабе.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        [Explicit]
        [Category("Manual")]
        public IEnumerator Screen_CapturesInventory()
        {
            Directory.CreateDirectory(Folder);
            yield return StartGame();
            yield return null;
            OpenInventory();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(Folder, "inventory-live.png"));
            yield return null;
            yield return null;
            Assert.IsTrue(
                File.Exists(Path.Combine(Folder, "inventory-live.png")),
                "Кадр окна инвентаря не записан");
        }
    }
}
