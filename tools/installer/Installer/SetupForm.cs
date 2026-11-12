#nullable enable
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>
    /// Окно установки: постер игры на весь фон, внизу — панель с полосой прогресса,
    /// выбором папки и кнопками. Этапы: выбор папки, распаковка, завершение, ошибка.
    /// Отрисовка — в <c>SetupForm.Paint.cs</c>, ход установки — в <c>SetupForm.Install.cs</c>.
    /// </summary>
    internal sealed partial class SetupForm : Form
    {
        private readonly PosterArt _poster;
        private readonly InstallInfo _info;
        private readonly Font _fontTitle = SetupTheme.Font(14f, FontStyle.Bold);
        private readonly Font _fontHead = SetupTheme.Font(11.5f);
        private readonly Font _fontText = SetupTheme.Font(10f);
        private readonly Font _fontSmall = SetupTheme.Font(8.75f);
        private readonly Font _fontButton = SetupTheme.Font(10.5f);
        private readonly Font _fontBar = SetupTheme.Font(8.75f, FontStyle.Bold);

        private TextBox _pathBox = null!;
        private FlatButton _browseButton = null!;
        private FlatButton _primary = null!;
        private FlatButton _secondary = null!;
        private CheckBox _desktopCheck = null!;
        private CheckBox _launchCheck = null!;
        private Timer _ticker = null!;
        private Bitmap? _background;

        private InstallerEngine? _engine;
        private Phase _phase = Phase.Welcome;
        private string _failure = string.Empty;
        private string _installDir = string.Empty;
        private float _previewFraction = -1f;
        private string? _previewStatus;

        private float _scale = 1f;
        private int _margin;
        private int _overlayTop;
        private int _contentWidth;
        private int _buttonHeight;
        private int _buttonTop;

        /// <summary>Создаёт окно установки.</summary>
        /// <param name="poster">Постер игры.</param>
        /// <param name="info">Данные поставки.</param>
        /// <param name="defaultDir">Папка установки по умолчанию.</param>
        public SetupForm(PosterArt poster, InstallInfo info, string defaultDir)
        {
            _poster = poster;
            _info = info;
            _installDir = defaultDir;

            SuspendLayout();
            Text = info.DisplayName + " — установка";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
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
                // Иконка не критична: окно покажет системную.
            }

            CreateControls();
            ResumeLayout(performLayout: false);
            SetPhase(Phase.Welcome);
        }

        /// <summary>Показывает окно на этапе распаковки с заданным прогрессом — для снимков.</summary>
        /// <param name="fraction">Доля готовности 0..1.</param>
        /// <param name="status">Подпись этапа.</param>
        public void PreviewInstalling(float fraction, string status)
        {
            _previewFraction = Math.Max(0f, Math.Min(1f, fraction));
            _previewStatus = status;
            SetPhase(Phase.Installing);
        }

        /// <summary>Показывает окно на этапе завершения — для снимков.</summary>
        public void PreviewDone()
        {
            _installDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", _info.App);
            SetPhase(Phase.Done);
        }

        /// <inheritdoc />
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // Размер окна задаётся здесь, а не в конструкторе, поэтому CenterScreen посчитал бы
            // положение по размеру формы по умолчанию (300×300) и окно свисало бы за нижний край
            // экрана. Считаем масштаб от DPI и от рабочей области и ставим окно по центру сами.
            float dpiScale = DeviceDpi / 96f;
            var area = Screen.FromPoint(Cursor.Position).WorkingArea;
            float required = (894f * dpiScale) + (40f * dpiScale);
            float fit = Math.Min(1f, (area.Height - 24f) / Math.Max(required, 1f));
            _scale = dpiScale * Math.Max(0.65f, fit);

            ClientSize = new Size(Px(600), Px(894));
            Location = new Point(
                area.Left + Math.Max(0, (area.Width - Width) / 2),
                area.Top + Math.Max(0, (area.Height - Height) / 2));

            RebuildBackground();
            UpdateLayout();
            _pathBox.Select(_pathBox.TextLength, 0);
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
            UpdateLayout();
        }

        /// <inheritdoc />
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_engine != null && !_engine.Finished && _engine.Error == null)
            {
                var answer = MessageBox.Show(
                    this,
                    "Установка ещё идёт. Прервать её?",
                    "AnomalyPath — установка",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                _engine.Cancel();
            }

            base.OnFormClosing(e);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _ticker?.Dispose();
                _background?.Dispose();
                _fontTitle.Dispose();
                _fontHead.Dispose();
                _fontText.Dispose();
                _fontSmall.Dispose();
                _fontButton.Dispose();
                _fontBar.Dispose();
            }

            base.Dispose(disposing);
        }

        private void CreateControls()
        {
            _pathBox = new TextBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SetupTheme.Panel,
                ForeColor = SetupTheme.Text,
                Font = _fontText,
                AutoSize = false,
                Text = _installDir
            };

            _browseButton = new FlatButton("Обзор…", primary: false, _fontButton);
            _browseButton.Click += OnBrowse;

            _primary = new FlatButton("Установить", primary: true, _fontButton);
            _primary.Click += OnPrimaryClick;

            _secondary = new FlatButton("Выход", primary: false, _fontButton);
            _secondary.Click += OnSecondaryClick;

            _desktopCheck = new CheckBox
            {
                Text = "Создать ярлык на рабочем столе",
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = SetupTheme.Text,
                BackColor = Color.Transparent,
                Font = _fontText
            };

            _launchCheck = new CheckBox
            {
                Text = "Запустить игру",
                AutoSize = true,
                Checked = true,
                FlatStyle = FlatStyle.Flat,
                ForeColor = SetupTheme.Text,
                BackColor = Color.Transparent,
                Font = _fontText
            };

            _ticker = new Timer { Interval = 40 };
            _ticker.Tick += OnTick;

            Controls.Add(_pathBox);
            Controls.Add(_browseButton);
            Controls.Add(_desktopCheck);
            Controls.Add(_launchCheck);
            Controls.Add(_primary);
            Controls.Add(_secondary);

            AcceptButton = _primary;
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

        private void UpdateLayout()
        {
            _margin = Px(38);
            _contentWidth = ClientSize.Width - (_margin * 2);
            _overlayTop = ClientSize.Height - Px(300);
            _buttonHeight = Px(46);
            _buttonTop = ClientSize.Height - Px(38) - _buttonHeight;

            int primaryWidth = Px(210);
            int secondaryWidth = Px(150);
            _primary.SetBounds(_margin, _buttonTop, primaryWidth, _buttonHeight);
            _secondary.SetBounds(_margin + primaryWidth + Px(14), _buttonTop, secondaryWidth, _buttonHeight);

            int pathTop = _overlayTop + Px(74);
            int pathHeight = Px(32);
            int browseWidth = Px(112);
            _pathBox.SetBounds(_margin, pathTop, _contentWidth - browseWidth - Px(12), pathHeight);
            _browseButton.SetBounds(_pathBox.Right + Px(12), pathTop, browseWidth, pathHeight);

            _desktopCheck.Location = new Point(_margin, pathTop + pathHeight + Px(50));
            _launchCheck.Location = new Point(_margin, _overlayTop + Px(88));
            Invalidate();
        }

        private void SetPhase(Phase phase)
        {
            _phase = phase;
            bool welcome = phase == Phase.Welcome;
            _pathBox.Visible = welcome;
            _browseButton.Visible = welcome;
            _desktopCheck.Visible = welcome;
            _launchCheck.Visible = phase == Phase.Done;
            _primary.Visible = phase != Phase.Installing;
            _secondary.Visible = phase == Phase.Welcome || phase == Phase.Installing;
            _primary.Enabled = true;

            switch (phase)
            {
                case Phase.Welcome:
                    _primary.Text = "Установить";
                    _secondary.Text = "Выход";
                    break;
                case Phase.Installing:
                    _secondary.Text = "Отмена";
                    break;
                case Phase.Done:
                    _primary.Text = "Готово";
                    break;
                case Phase.Failed:
                    _primary.Text = "Закрыть";
                    break;
            }

            UpdateLayout();
            Invalidate();
        }

        private int Px(int value)
        {
            return (int)Math.Round(value * _scale);
        }

        private enum Phase
        {
            Welcome,
            Installing,
            Done,
            Failed
        }
    }
}
