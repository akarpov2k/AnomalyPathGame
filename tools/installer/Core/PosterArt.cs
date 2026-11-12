#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Постер игры как фон окна установщика: картинка вписывается в клиентскую область
    /// без искажения пропорций и обрезается по краям («cover»).
    /// </summary>
    internal sealed class PosterArt : IDisposable
    {
        private readonly Bitmap _image;

        private PosterArt(Bitmap image)
        {
            _image = image;
        }

        /// <summary>Загружает постер из потока ресурса.</summary>
        /// <param name="stream">Поток PNG.</param>
        /// <returns>Готовый к отрисовке постер.</returns>
        public static PosterArt FromStream(Stream stream)
        {
            using var source = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
            return new PosterArt(new Bitmap(source));
        }

        /// <summary>Рисует постер на всю область, сохраняя пропорции.</summary>
        /// <param name="graphics">Контекст отрисовки.</param>
        /// <param name="bounds">Область окна.</param>
        public void DrawCover(Graphics graphics, Rectangle bounds)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            float scale = Math.Max((float)bounds.Width / _image.Width, (float)bounds.Height / _image.Height);
            int width = (int)Math.Ceiling(_image.Width * scale);
            int height = (int)Math.Ceiling(_image.Height * scale);
            var destination = new Rectangle(
                bounds.X + ((bounds.Width - width) / 2),
                bounds.Y + ((bounds.Height - height) / 2),
                width,
                height);

            var state = graphics.Save();
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            using var attributes = new ImageAttributes();
            attributes.SetWrapMode(WrapMode.TileFlipXY);
            graphics.DrawImage(_image, destination, 0, 0, _image.Width, _image.Height, GraphicsUnit.Pixel, attributes);
            graphics.Restore(state);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _image.Dispose();
        }
    }
}
