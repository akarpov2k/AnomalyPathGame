#nullable enable
using System;

namespace AnomalyPath.Core.Ui
{
    /// <summary>
    /// Стартовый размер окна: вписывает желаемый размер в монитор с сохранением пропорций.
    /// Нужен, чтобы окно без рамки не оказалось больше экрана на небольших мониторах,
    /// а на больших открывалось в заданном размере (Full HD, 2K и другие стандартные).
    /// </summary>
    public static class WindowLayout
    {
        /// <summary>Вписывает желаемый размер окна в размер монитора.</summary>
        /// <param name="display">Размер монитора в пикселях.</param>
        /// <param name="desired">Желаемый размер окна в пикселях.</param>
        /// <returns>Размер не больше монитора и с теми же пропорциями, что у желаемого.</returns>
        public static WindowSize Fit(WindowSize display, WindowSize desired)
        {
            var scale = Math.Min(
                1d,
                Math.Min((double)display.Width / desired.Width, (double)display.Height / desired.Height));
            return new WindowSize(FitSide(desired.Width, scale), FitSide(desired.Height, scale));
        }

        private static int FitSide(int value, double scale)
        {
            var scaled = (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);
            return scaled > 0 ? scaled : 1;
        }
    }
}
