#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>Часть класса <see cref="SetupForm"/>: отрисовка постера и панели этапов.</summary>
    internal sealed partial class SetupForm
    {
        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            if (_background != null)
            {
                graphics.DrawImageUnscaled(_background, 0, 0);
            }
            else
            {
                _poster.DrawCover(graphics, ClientRectangle);
            }

            DrawOverlay(graphics);
            switch (_phase)
            {
                case Phase.Welcome:
                    DrawWelcome(graphics);
                    break;
                case Phase.Installing:
                    DrawInstalling(graphics);
                    break;
                case Phase.Done:
                    DrawDone(graphics);
                    break;
                case Phase.Failed:
                    DrawFailed(graphics);
                    break;
            }
        }

        private void DrawOverlay(Graphics graphics)
        {
            int fadeTop = _overlayTop - Px(110);
            var fade = new Rectangle(0, fadeTop, ClientSize.Width, Px(110));
            if (fade.Height > 0)
            {
                using var brush = new LinearGradientBrush(
                    fade,
                    Color.FromArgb(0, 9, 12, 10),
                    Color.FromArgb(250, 9, 11, 10),
                    LinearGradientMode.Vertical);
                graphics.FillRectangle(brush, fade);
            }

            var panel = new Rectangle(0, _overlayTop, ClientSize.Width, ClientSize.Height - _overlayTop);
            if (panel.Height > 0)
            {
                using var brush = new SolidBrush(Color.FromArgb(255, 9, 11, 10));
                graphics.FillRectangle(brush, panel);
            }

            using var pen = new Pen(Color.FromArgb(120, SetupTheme.Accent));
            graphics.DrawLine(pen, _margin, _overlayTop, ClientSize.Width - _margin, _overlayTop);
        }

        private void DrawWelcome(Graphics graphics)
        {
            DrawText(
                graphics,
                "Установка игры",
                _fontTitle,
                SetupTheme.Text,
                new Rectangle(_margin, _overlayTop + Px(18), _contentWidth, Px(28)));

            DrawText(
                graphics,
                "ПАПКА УСТАНОВКИ",
                _fontSmall,
                SetupTheme.TextDim,
                new Rectangle(_margin, _overlayTop + Px(50), _contentWidth, Px(18)));

            string drive = Path.GetPathRoot(_installDir) ?? string.Empty;
            string free = string.Empty;
            try
            {
                if (drive.Length > 0)
                {
                    var info = new DriveInfo(drive);
                    if (info.IsReady)
                    {
                        free = "Свободно на диске " + drive.TrimEnd('\\') + ": " + InstallerEngine.FormatSize(info.AvailableFreeSpace) + "  ·  ";
                    }
                }
            }
            catch (ArgumentException)
            {
                // Неизвестный диск — подпись о свободном месте просто не выводится.
            }

            DrawText(
                graphics,
                free + "требуется " + InstallerEngine.FormatSize(_info.Bytes) + "  ·  файлов: " + _info.FileCount,
                _fontSmall,
                SetupTheme.TextDim,
                new Rectangle(_margin, _overlayTop + Px(112), _contentWidth, Px(18)));

            DrawText(
                graphics,
                "Версия " + _info.Version + "  ·  " + _info.Publisher,
                _fontSmall,
                SetupTheme.TextDim,
                new Rectangle(_margin, _buttonTop - Px(26), _contentWidth, Px(18)));
        }

        private void DrawInstalling(Graphics graphics)
        {
            string status = _previewStatus ?? _engine?.Status ?? "Распаковка файлов";
            DrawText(graphics, status, _fontHead, SetupTheme.Text, new Rectangle(_margin, _overlayTop + Px(20), _contentWidth, Px(24)));

            string file = _previewFraction >= 0f ? string.Empty : ShortenPath(_engine?.CurrentFile);
            DrawText(graphics, file, _fontSmall, SetupTheme.TextDim, new Rectangle(_margin, _overlayTop + Px(48), _contentWidth, Px(18)));

            int barTop = _overlayTop + Px(78);
            var bar = new Rectangle(_margin, barTop, _contentWidth, Px(30));
            float fraction = _previewFraction >= 0f ? _previewFraction : Fraction();
            DrawProgress(graphics, bar, fraction);

            if (_previewFraction < 0f && _engine != null)
            {
                string counter = Math.Min(_engine.FileNumber, _info.FileCount) + " / " + _info.FileCount + " файлов";
                DrawText(graphics, counter, _fontSmall, SetupTheme.TextDim, new Rectangle(_margin, barTop + Px(34), _contentWidth, Px(18)));
            }
        }

        private void DrawDone(Graphics graphics)
        {
            DrawText(graphics, "Установка завершена", _fontTitle, SetupTheme.Accent, new Rectangle(_margin, _overlayTop + Px(20), _contentWidth, Px(26)));
            DrawText(
                graphics,
                "Игра установлена в: " + _installDir,
                _fontSmall,
                SetupTheme.TextDim,
                new Rectangle(_margin, _overlayTop + Px(50), _contentWidth, Px(18)));
            DrawText(
                graphics,
                "Ярлык — в меню «Пуск»" + (_desktopCheck.Checked ? " и на рабочем столе" : string.Empty) + ".",
                _fontSmall,
                SetupTheme.TextDim,
                new Rectangle(_margin, _buttonTop - Px(26), _contentWidth, Px(18)));

            string warning = _engine?.Warning ?? string.Empty;
            if (warning.Length > 0)
            {
                DrawText(
                    graphics,
                    "Замечание: " + warning,
                    _fontSmall,
                    SetupTheme.Danger,
                    new Rectangle(_margin, _overlayTop + Px(116), _contentWidth, Px(56)),
                    wrap: true);
            }
        }

        private void DrawFailed(Graphics graphics)
        {
            DrawText(graphics, "Установка не удалась", _fontTitle, SetupTheme.Danger, new Rectangle(_margin, _overlayTop + Px(20), _contentWidth, Px(26)));
            DrawText(
                graphics,
                string.IsNullOrEmpty(_failure) ? "Неизвестная ошибка." : _failure,
                _fontText,
                SetupTheme.Text,
                new Rectangle(_margin, _overlayTop + Px(52), _contentWidth, Px(84)),
                wrap: true);
        }

        private void DrawProgress(Graphics graphics, Rectangle bar, float fraction)
        {
            fraction = Math.Max(0f, Math.Min(1f, fraction));

            using (var track = Shapes.Rounded(bar, Px(8)))
            using (var brush = new SolidBrush(SetupTheme.Track))
            using (var pen = new Pen(SetupTheme.Border))
            {
                graphics.FillPath(brush, track);
                graphics.DrawPath(pen, track);
            }

            int inset = Px(3);
            int width = (int)((bar.Width - (inset * 2)) * fraction);
            if (width > 0)
            {
                var inner = new Rectangle(bar.X + inset, bar.Y + inset, width, bar.Height - (inset * 2));
                using var fill = Shapes.Rounded(inner, Px(6));
                using var brush = new LinearGradientBrush(
                    new Rectangle(inner.X, inner.Y, Math.Max(inner.Width, 1), inner.Height),
                    SetupTheme.AccentDark,
                    SetupTheme.Accent,
                    LinearGradientMode.Horizontal);
                graphics.FillPath(brush, fill);
            }

            var label = new Rectangle(bar.Right - Px(64), bar.Y, Px(56), bar.Height);
            DrawText(
                graphics,
                ((int)Math.Round(fraction * 100f)) + " %",
                _fontBar,
                SetupTheme.Text,
                label,
                StringAlignment.Far);
        }

        private void InvalidateOverlay()
        {
            Invalidate(new Rectangle(0, _overlayTop - Px(110), ClientSize.Width, ClientSize.Height - _overlayTop + Px(110)));
        }

        private void DrawText(Graphics graphics, string text, Font font, Color color, Rectangle bounds, StringAlignment alignment = StringAlignment.Near, bool wrap = false)
        {
            using var brush = new SolidBrush(color);
            using var format = new StringFormat
            {
                Alignment = alignment,
                LineAlignment = StringAlignment.Near,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = wrap ? 0 : StringFormatFlags.NoWrap
            };
            graphics.DrawString(text, font, brush, new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), format);
        }

        private static string ShortenPath(string? path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return string.Empty;
            }

            string name = Path.GetFileName(path);
            string? folder = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(folder))
            {
                return name;
            }

            string last = Path.GetFileName(folder);
            return string.IsNullOrEmpty(last) ? name : last + Path.DirectorySeparatorChar + name;
        }
    }
}
