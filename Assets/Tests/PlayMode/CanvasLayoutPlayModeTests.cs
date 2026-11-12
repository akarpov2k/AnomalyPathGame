#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Канвас игры: масштабирование под любое разрешение окна. Режим <c>Expand</c> держит
    /// опорную область не меньше справочной, поэтому при 16:10, 4:3 и 21:9 разметка не обрезается.
    /// </summary>
    public sealed class CanvasLayoutPlayModeTests : PlayModeTestBase
    {
        /// <summary>Канвас расширяется и не меньше опорной области своей ориентации.</summary>
        [UnityTest]
        public IEnumerator Canvas_UsesExpand_AndKeepsReferenceArea()
        {
            yield return LoadMain();

            var canvas = GameObject.Find("Canvas");
            Assert.IsNotNull(canvas, "Нет канваса");
            var scaler = canvas!.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "У канваса нет CanvasScaler");
            Assert.AreEqual(
                CanvasScaler.ScreenMatchMode.Expand,
                scaler!.screenMatchMode,
                "Канвас должен расширяться, а не обрезать разметку");

            var expected = LayoutOrientation.IsPortrait
                ? new Vector2(LayoutOrientation.PortraitWidth, 1920f)
                : new Vector2(LayoutOrientation.LandscapeWidth, 1080f);
            Assert.AreEqual(expected, scaler.referenceResolution, "Опорное разрешение канваса");

            var rect = ((RectTransform)canvas.transform).rect;
            Assert.GreaterOrEqual(rect.width + 0.01f, expected.x, "Канвас уже опорной ширины");
            Assert.GreaterOrEqual(rect.height + 0.01f, expected.y, "Канвас ниже опорной высоты");
        }
    }
}
