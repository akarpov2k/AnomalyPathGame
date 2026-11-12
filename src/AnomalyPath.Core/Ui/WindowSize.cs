#nullable enable
using System;

namespace AnomalyPath.Core.Ui
{
    /// <summary>Размер окна в пикселях: ширина и высота, обе положительные.</summary>
    public readonly struct WindowSize
    {
        /// <summary>Создаёт размер окна.</summary>
        /// <param name="width">Ширина в пикселях, больше нуля.</param>
        /// <param name="height">Высота в пикселях, больше нуля.</param>
        /// <exception cref="ArgumentOutOfRangeException">Ширина или высота не больше нуля.</exception>
        public WindowSize(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(width));
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(height));
            }

            Width = width;
            Height = height;
        }

        /// <summary>Ширина в пикселях.</summary>
        public int Width { get; }

        /// <summary>Высота в пикселях.</summary>
        public int Height { get; }
    }
}
