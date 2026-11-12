#nullable enable
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Окно удаления: постер фоном, подтверждение, прогресс удаления файлов.
    /// Сама уборка идёт в фоновом потоке, окно читает счётчики по таймеру.
    /// </summary>
    internal sealed class UninstallForm : Form
    {
        private readonly PosterArt _poster;
        private readonly InstallInfo _info;
        private readonly string _installDir;
        private readonly Font _fontTitle = SetupTheme.Font(13f, FontStyle.Bold);
        private readonly Font _fontText = SetupTheme.Font(10f);
        private readonly Font _fontSmall = SetupTheme.Font(8.75f);
        private readonly Font _fontButton = SetupTheme.Font(10f);

        private readonly FlatButton _remove;
        private readonly FlatButton _cancel;
        private readonly System.Windows.Forms.Timer _ticker;
        private readonly object _sync = new();

        private Thread? _worker;
        private string _status = "Удалить игру и все её файлы?";
        private string _detail = string.Empty;
        private int _totalFiles;
        private int _doneFiles;
        private bool _finished;
        private bool _failed;
        private Phase _phase = Phase.Confirm;
        private Bitmap? _background;
        private float _scale = 1f;

        /// <summary>Создаёт окно удаления.</summary>
        /// <param name="poster">Постер игры.</param>
        /// <param name="info">Данные поставки.</param>
        /// <param name="installDir">Папка установленной игры.</param>
        public UninstallForm(PosterArt poster, InstallInfo info, string installDir)
        {
            _poster = poster;
            _info = info;
            _installDir = installDir;
            _detail = installDir;

            SuspendLayout();
            Text = "Удаление " + info.DisplayName;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Color.FromArgb(10, 12, 11);
            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch (ArgumentException)
            {
                // Системная иконка — допустимый запасной вариант.
            }

            _remove = new FlatButton("Удалить", primary: true, _fontButton);
            _remove.Click += OnRemove;
            _cancel = new FlatButton("Отмена", primary: false, _fontButton);
            _cancel.Click += (_, _) => Close();
            _ticker = new System.Windows.Forms.Timer { Interval = 40 };
            _ticker.Tick += OnTick;

            Controls.Add(_remove);
            Controls.Add(_cancel);
            AcceptButton = _remove;
            ResumeLayout(performLayout: false);
            SetPhase(Phase.Confirm);
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Как и у установщика: размер задаётся в OnLoad, поэтому позицию считаем сами,
            // иначе CenterScreen поставил бы окно по размеру формы по умолчанию.
            _scale = DeviceDpi / 96f;
            ClientSize = new Size(Px(560), Px(330));
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            Location = new Point(
                area.Left + Math.Max(0, (area.Width - Width) / 2),
                area.Top + Math.Max(0, (area.Height - Height) / 2));

            RebuildBackground();
            LayoutControls();
        }

        /// <inheritdoc />
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Activate();
        }

        /// <inheritdoc />
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            RebuildBackground();
            LayoutControls();
        }

        /// <inheritdoc />
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_worker != null && !_finished && !_failed)
            {
                e.Cancel = true;
                return;
            }

            base.OnFormClosing(e);
        }

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

            int overlayTop = ClientSize.Height - Px(170);
            var area = new Rectangle(0, overlayTop - Px(60), ClientSize.Width, ClientSize.Height - overlayTop + Px(60));
            if (area.Height > 0)
            {
                using var brush = new LinearGradientBrush(
                    area,
                    Color.FromArgb(0, 9, 12, 10),
                    Color.FromArgb(246, 9, 11, 10),
                    LinearGradientMode.Vertical);
                graphics.FillRectangle(brush, area);
            }

            int margin = Px(28);
            int width = ClientSize.Width - (margin * 2);

            DrawText(
                graphics,
                _failed ? "Удаление не удалось" : "Удаление игры",
                _fontTitle,
                _failed ? SetupTheme.Danger : SetupTheme.Text,
                new Rectangle(margin, overlayTop + Px(10), width, Px(26)));

            DrawText(
                graphics,
                _status,
                _fontText,
                SetupTheme.Text,
                new Rectangle(margin, overlayTop + Px(42), width, Px(20)));

            DrawText(
                graphics,
                _detail,
                _fontSmall,
                SetupTheme.TextDim,
                new Rectangle(margin, overlayTop + Px(64), width, Px(18)));

            if (_phase == Phase.Running)
            {
                var bar = new Rectangle(margin, overlayTop + Px(92), width, Px(22));
                float fraction = _totalFiles > 0 ? Math.Min(1f, (float)_doneFiles / _totalFiles) : 0f;
                DrawProgress(graphics, bar, fraction);
            }
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ticker.Dispose();
                _background?.Dispose();
                _fontTitle.Dispose();
                _fontText.Dispose();
                _fontSmall.Dispose();
                _fontButton.Dispose();
            }

            base.Dispose(disposing);
        }

        private void OnRemove(object? sender, EventArgs e)
        {
            SetPhase(Phase.Running);
            _worker = new Thread(Run) { IsBackground = true, Name = "AnomalyPathUninstall" };
            _worker.Start();
            _ticker.Start();
        }

        private void Run()
        {
            try
            {
                DeletionEngine.Execute(_info, _installDir, (done, total, _) =>
                {
                    lock (_sync)
                    {
                        _doneFiles = done;
                        _totalFiles = total;
                    }
                });

                lock (_sync)
                {
                    _finished = true;
                }
            }
            catch (Exception exception)
            {
                lock (_sync)
                {
                    _failed = true;
                    _finished = true;
                    _status = exception.Message;
                }
            }
        }

        private void OnTick(object? sender, EventArgs e)
        {
            lock (_sync)
            {
                if (_failed)
                {
                    _ticker.Stop();
                    _phase = Phase.Failed;
                    _remove.Visible = true;
                    _cancel.Visible = false;
                    _remove.Text = "Закрыть";
                    _remove.Click -= OnRemove;
                    _remove.Click += (_, _) => Close();
                    LayoutControls();
                    Invalidate();
                    return;
                }

                if (!_finished)
                {
                    _status = "Удаление файлов";
                    _detail = _totalFiles > 0 ? _doneFiles + " из " + _totalFiles : _installDir;
                    Invalidate();
                    return;
                }
            }

            _ticker.Stop();
            _status = "Игра удалена";
            _detail = "Ярлыки и запись в списке программ убраны.";
            _phase = Phase.Done;
            _remove.Visible = false;
            _cancel.Visible = false;
            Invalidate();
            Close();
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            _remove.Visible = phase == Phase.Confirm || phase == Phase.Failed;
            _cancel.Visible = phase == Phase.Confirm;
            _remove.Enabled = phase == Phase.Confirm;
            LayoutControls();
            Invalidate();
        }

        private void LayoutControls()
        {
            int margin = Px(28);
            int height = Px(42);
            int top = ClientSize.Height - Px(28) - height;
            _remove.SetBounds(margin, top, Px(164), height);
            _cancel.SetBounds(margin + Px(178), top, Px(120), height);
        }

        private void RebuildBackground()
        {
            if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            var next = new Bitmap(ClientSize.Width, ClientSize.Height);
            using (var graphics = Graphics.FromImage(next))
            {
                _poster.DrawCover(graphics, new Rectangle(0, 0, ClientSize.Width, ClientSize.Height));
            }

            var previous = _background;
            _background = next;
            previous?.Dispose();
        }

        private void DrawProgress(Graphics graphics, Rectangle bar, float fraction)
        {
            using (var track = Shapes.Rounded(bar, Px(7)))
            using (var brush = new SolidBrush(SetupTheme.Track))
            using (var pen = new Pen(SetupTheme.Border))
            {
                graphics.FillPath(brush, track);
                graphics.DrawPath(pen, track);
            }

            int inset = Px(3);
            int width = (int)((bar.Width - (inset * 2)) * Math.Max(0f, Math.Min(1f, fraction)));
            if (width > 0)
            {
                var inner = new Rectangle(bar.X + inset, bar.Y + inset, width, bar.Height - (inset * 2));
                using var fill = Shapes.Rounded(inner, Px(5));
                using var brush = new LinearGradientBrush(
                    new Rectangle(inner.X, inner.Y, Math.Max(inner.Width, 1), inner.Height),
                    SetupTheme.AccentDark,
                    SetupTheme.Accent,
                    LinearGradientMode.Horizontal);
                graphics.FillPath(brush, fill);
            }
        }

        private void DrawText(Graphics graphics, string text, Font font, Color color, Rectangle bounds)
        {
            using var brush = new SolidBrush(color);
            using var format = new StringFormat
            {
                Trimming = StringTrimming.EllipsisPath,
                FormatFlags = StringFormatFlags.NoWrap
            };
            graphics.DrawString(text, font, brush, new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height), format);
        }

        private int Px(int value)
        {
            return (int)Math.Round(value * _scale);
        }

        private enum Phase
        {
            Confirm,
            Running,
            Done,
            Failed
        }
    }
}
