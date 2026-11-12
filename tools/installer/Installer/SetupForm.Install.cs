#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace AnomalyPath.Setup
{
    /// <summary>Часть класса <see cref="SetupForm"/>: выбор папки, запуск установки и её ход.</summary>
    internal sealed partial class SetupForm
    {
        private void OnBrowse(object? sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "Куда установить " + _info.DisplayName,
                SelectedPath = Directory.Exists(_pathBox.Text) ? _pathBox.Text : _installDir
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                _pathBox.Text = Path.Combine(dialog.SelectedPath, _info.App);
                UpdateLayout();
            }
        }

        private void OnPrimaryClick(object? sender, EventArgs e)
        {
            switch (_phase)
            {
                case Phase.Welcome:
                    StartInstall();
                    break;
                case Phase.Done:
                    Finish();
                    break;
                case Phase.Failed:
                    Close();
                    break;
            }
        }

        private void OnSecondaryClick(object? sender, EventArgs e)
        {
            if (_phase == Phase.Installing)
            {
                _engine?.Cancel();
                _secondary.Enabled = false;
                _secondary.Text = "Отмена…";
                return;
            }

            Close();
        }

        private void StartInstall()
        {
            string folder;
            try
            {
                folder = Path.GetFullPath(_pathBox.Text.Trim());
            }
            catch (Exception exception) when (exception is ArgumentException || exception is NotSupportedException || exception is PathTooLongException)
            {
                Warn("Путь содержит недопустимые символы. Выберите другую папку.");
                return;
            }

            if (Path.GetPathRoot(folder)?.TrimEnd('\\') == folder.TrimEnd('\\'))
            {
                Warn("Выберите папку для игры, а не корень диска.");
                return;
            }

            _installDir = folder;
            try
            {
                _engine = new InstallerEngine(
                    EmbeddedResources.ReadAll(EmbeddedResources.Payload),
                    EmbeddedResources.ReadAll(EmbeddedResources.Uninstaller),
                    _installDir,
                    _desktopCheck.Checked,
                    _info);
                _engine.Prepare();
            }
            catch (Exception exception)
            {
                _engine = null;
                Warn("Не удалось начать установку: " + exception.Message);
                return;
            }

            _previewFraction = -1f;
            _engine.Start();
            _ticker.Start();
            SetPhase(Phase.Installing);
        }

        private void OnTick(object? sender, EventArgs e)
        {
            if (_engine == null)
            {
                _ticker.Stop();
                return;
            }

            InvalidateOverlay();
            if (!_engine.Finished)
            {
                return;
            }

            _ticker.Stop();
            if (_engine.Error != null)
            {
                _failure = _engine.Error.Message;
                SetPhase(Phase.Failed);
                return;
            }

            if (_engine.Canceled)
            {
                Close();
                return;
            }

            SetPhase(Phase.Done);
        }

        private void Finish()
        {
            if (_launchCheck.Checked)
            {
                string executable = Path.Combine(_installDir, _info.ExeName);
                try
                {
                    Process.Start(new ProcessStartInfo(executable) { WorkingDirectory = _installDir, UseShellExecute = true });
                }
                catch (Exception exception) when (exception is System.ComponentModel.Win32Exception || exception is FileNotFoundException)
                {
                    MessageBox.Show(
                        this,
                        "Не удалось запустить игру: " + exception.Message,
                        "AnomalyPath",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }

            Close();
        }

        private void Warn(string message)
        {
            MessageBox.Show(
                this,
                message,
                "AnomalyPath — установка",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }

        private float Fraction()
        {
            if (_engine == null || _engine.TotalBytes <= 0)
            {
                return 0f;
            }

            return (float)Math.Min(1d, (double)_engine.DoneBytes / _engine.TotalBytes);
        }
    }
}
