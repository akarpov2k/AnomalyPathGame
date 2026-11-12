#nullable enable
using System.Drawing;
using System.Drawing.Drawing2D;

namespace AnomalyPath.Setup
{
    /// <summary>Геометрические помощники отрисовки: прямоугольники со скруглением.</summary>
    internal static class Shapes
    {
        /// <summary>Строит путь прямоугольника со скруглёнными углами.</summary>
        /// <param name="rect">Прямоугольник в координатах устройства.</param>
        /// <param name="radius">Радиус скругления в пикселях.</param>
        /// <returns>Путь; владелец — вызывающий код.</returns>
        public static GraphicsPath Rounded(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            if (rect.Width <= 0 || rect.Height <= 0)
            {
                return path;
            }

            int r = System.Math.Min(radius, System.Math.Min(rect.Width, rect.Height) / 2);
            if (r <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int d = r * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
