#nullable enable
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Безопасная область: содержимое экрана получает отступы выреза и панели жестов.</summary>
    public sealed class SafeAreaPlayModeTests
    {
        /// <summary>Вырез снизу уменьшает область и сдвигает нижнюю границу.</summary>
        [Test]
        public void ApplyInsets_BottomCutout_ShiftsLowerAnchor()
        {
            var rect = NewRect();
            try
            {
                SafeAreaRoot.ApplyInsets(rect, new Rect(0f, 40f, 1080f, 1000f), 1080, 1080);

                Assert.AreEqual(0f, rect.anchorMin.x, 1e-5f, "левая граница");
                Assert.AreEqual(40f / 1080f, rect.anchorMin.y, 1e-5f, "нижняя граница");
                Assert.AreEqual(1f, rect.anchorMax.x, 1e-5f, "правая граница");
                Assert.AreEqual(1040f / 1080f, rect.anchorMax.y, 1e-5f, "верхняя граница");
                Assert.AreEqual(Vector2.zero, rect.offsetMin, "отступ снизу");
                Assert.AreEqual(Vector2.zero, rect.offsetMax, "отступ сверху");
            }
            finally
            {
                Object.DestroyImmediate(rect.gameObject);
            }
        }

        /// <summary>Полная область без выреза оставляет содержимое растянутым на весь экран.</summary>
        [Test]
        public void ApplyInsets_FullScreen_StaysStretched()
        {
            var rect = NewRect();
            try
            {
                SafeAreaRoot.ApplyInsets(rect, new Rect(0f, 0f, 1920f, 1080f), 1920, 1080);

                Assert.AreEqual(Vector2.zero, rect.anchorMin, "нижний левый угол");
                Assert.AreEqual(Vector2.one, rect.anchorMax, "верхний правый угол");
            }
            finally
            {
                Object.DestroyImmediate(rect.gameObject);
            }
        }

        /// <summary>Вырез сбоку (ландшафт с камерой в углу) сдвигает боковую границу.</summary>
        [Test]
        public void ApplyInsets_SideCutout_ShiftsSideAnchor()
        {
            var rect = NewRect();
            try
            {
                SafeAreaRoot.ApplyInsets(rect, new Rect(60f, 0f, 2280f, 1080f), 2400, 1080);

                Assert.AreEqual(60f / 2400f, rect.anchorMin.x, 1e-5f, "левая граница");
                Assert.AreEqual(2340f / 2400f, rect.anchorMax.x, 1e-5f, "правая граница");
            }
            finally
            {
                Object.DestroyImmediate(rect.gameObject);
            }
        }

        /// <summary>Нулевой размер экрана не порождает деления на ноль.</summary>
        [Test]
        public void ApplyInsets_ZeroScreen_KeepsAnchors()
        {
            var rect = NewRect();
            try
            {
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                SafeAreaRoot.ApplyInsets(rect, new Rect(0f, 0f, 0f, 0f), 0, 0);

                Assert.AreEqual(Vector2.zero, rect.anchorMin, "нижний левый угол");
                Assert.AreEqual(Vector2.one, rect.anchorMax, "верхний правый угол");
            }
            finally
            {
                Object.DestroyImmediate(rect.gameObject);
            }
        }

        private static RectTransform NewRect()
        {
            var go = new GameObject("SafeAreaTest", typeof(RectTransform));
            return (RectTransform)go.transform;
        }
    }
}
