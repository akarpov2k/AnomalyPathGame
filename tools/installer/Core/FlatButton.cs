#nullable enable
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Кнопка установщика: скруглённая плашка, нарисованная вручную, чтобы не зависеть
    /// от системной темы кнопок. Основная кнопка заливается цветом аномалии.
    /// </summary>
    internal sealed class FlatButton : Control, IButtonControl
    {
        private readonly bool _primary;
        private bool _hover;

        /// <summary>Создаёт кнопку.</summary>
        /// <param name="text">Подпись кнопки.</param>
        /// <param name="primary">Основная кнопка (зелёная заливка).</param>
        /// <param name="font">Шрифт подписи.</param>
        public FlatButton(string text, bool primary, Font font)
        {
            Text = text;
            _primary = primary;
            Font = font;
            Cursor = Cursors.Hand;
            SetStyle(
                ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw | ControlStyles.SupportsTransparentBackColor,
                true);
            BackColor = Color.Transparent;
        }

        /// <inheritdoc />
        public DialogResult DialogResult { get; set; } = DialogResult.None;

        /// <inheritdoc />
        public void NotifyDefault(bool value)
        {
        }

        /// <inheritdoc />
        public void PerformClick()
        {
            if (Enabled)
            {
                OnClick(EventArgs.Empty);
            }
        }

        /// <inheritdoc />
        protected override void OnPaint(PaintEventArgs e)
        {
            var graphics = e.Graphics;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = Math.Max(4, Height / 5);
            Color fill;
            Color border;
            Color text;
            if (_primary)
            {
                fill = Enabled
                    ? (_hover ? ControlPaint.Light(SetupTheme.AccentDark, 0.25f) : SetupTheme.AccentDark)
                    : Color.FromArgb(46, 52, 47);
                border = Enabled ? SetupTheme.Accent : SetupTheme.Border;
                text = Enabled ? SetupTheme.Text : SetupTheme.TextDim;
            }
            else
            {
                fill = _hover && Enabled ? SetupTheme.PanelHover : SetupTheme.Panel;
                border = Enabled ? SetupTheme.Border : Color.FromArgb(58, 64, 58);
                text = Enabled ? SetupTheme.Text : SetupTheme.TextDim;
            }

            using (var path = Shapes.Rounded(rect, radius))
            using (var brush = new SolidBrush(fill))
            using (var pen = new Pen(border))
            {
                graphics.FillPath(brush, path);
                graphics.DrawPath(pen, path);
            }

            using var textBrush = new SolidBrush(text);
            using var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.DrawString(Text, Font, textBrush, new RectangleF(4, 0, Width - 8, Height), format);
        }

        /// <inheritdoc />
        protected override void OnMouseEnter(EventArgs e)
        {
            _hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeave(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        /// <inheritdoc />
        protected override void OnEnabledChanged(EventArgs e)
        {
            _hover = false;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        /// <inheritdoc />
        protected override void OnTextChanged(EventArgs e)
        {
            Invalidate();
            base.OnTextChanged(e);
        }
    }
}
