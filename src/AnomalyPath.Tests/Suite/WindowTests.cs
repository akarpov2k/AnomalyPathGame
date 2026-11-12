#nullable enable
using System;
using AnomalyPath.Core.Ui;

namespace AnomalyPath.Tests
{
    /// <summary>Стартовый размер окна: вписывание в монитор без потери пропорций и границы значений.</summary>
    public sealed class WindowTests : TestBase
    {
        /// <inheritdoc />
        public override void Run()
        {
            FullHdFitsOnLargeDisplay();
            FitsOnSmallerDisplay();
            KeepsAspectRatio();
            NeverExceedsDisplay();
            EqualSizeStays();
            TinyDisplayKeepsPositiveSize();
            ZeroSizeThrows();
            NegativeSizeThrows();
        }

        private void FullHdFitsOnLargeDisplay()
        {
            Check("окно: Full HD на 2K-мониторе остаётся Full HD", () =>
            {
                var size = WindowLayout.Fit(new WindowSize(2560, 1440), new WindowSize(1920, 1080));

                Equal(1920, size.Width, "ширина");
                Equal(1080, size.Height, "высота");
            });
        }

        private void FitsOnSmallerDisplay()
        {
            Check("окно: на мониторе меньше Full HD окно уменьшается", () =>
            {
                var size = WindowLayout.Fit(new WindowSize(1280, 1024), new WindowSize(1920, 1080));

                Equal(1280, size.Width, "ширина");
                Equal(720, size.Height, "высота");
            });
        }

        private void KeepsAspectRatio()
        {
            Check("окно: пропорции желаемого размера сохраняются", () =>
            {
                var size = WindowLayout.Fit(new WindowSize(1600, 1200), new WindowSize(1920, 1080));

                Equal(1600, size.Width, "ширина");
                Equal(900, size.Height, "высота");
            });
        }

        private void NeverExceedsDisplay()
        {
            Check("окно: размер не больше монитора ни в одном разрешении", () =>
            {
                int[][] displays =
                {
                    new[] { 1024, 768 },
                    new[] { 1366, 768 },
                    new[] { 1600, 900 },
                    new[] { 1920, 1200 },
                    new[] { 2560, 1440 },
                    new[] { 3440, 1440 },
                    new[] { 3840, 2160 },
                };

                foreach (var display in displays)
                {
                    var size = WindowLayout.Fit(new WindowSize(display[0], display[1]), new WindowSize(1920, 1080));
                    True(size.Width <= display[0], $"ширина {size.Width} для {display[0]}x{display[1]}");
                    True(size.Height <= display[1], $"высота {size.Height} для {display[0]}x{display[1]}");
                }
            });
        }

        private void EqualSizeStays()
        {
            Check("окно: совпадающий с монитором размер не меняется", () =>
            {
                var size = WindowLayout.Fit(new WindowSize(1920, 1080), new WindowSize(1920, 1080));

                Equal(1920, size.Width, "ширина");
                Equal(1080, size.Height, "высота");
            });
        }

        private void TinyDisplayKeepsPositiveSize()
        {
            Check("окно: на крошечном мониторе размер остаётся положительным", () =>
            {
                var size = WindowLayout.Fit(new WindowSize(100, 100), new WindowSize(1920, 1080));

                Equal(100, size.Width, "ширина");
                Equal(56, size.Height, "высота");
            });
        }

        private void ZeroSizeThrows()
        {
            Check("окно: нулевой размер запрещён", () =>
            {
                Throws<ArgumentOutOfRangeException>(() => new WindowSize(0, 1080), "нулевая ширина");
                Throws<ArgumentOutOfRangeException>(() => new WindowSize(1920, 0), "нулевая высота");
            });
        }

        private void NegativeSizeThrows()
        {
            Check("окно: отрицательный размер запрещён", () =>
            {
                Throws<ArgumentOutOfRangeException>(() => new WindowSize(-1, 1080), "отрицательная ширина");
                Throws<ArgumentOutOfRangeException>(() => new WindowSize(1920, -1), "отрицательная высота");
            });
        }
    }
}
