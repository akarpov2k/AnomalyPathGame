#nullable enable
using System.Collections.Generic;
using System.Linq;
using AnomalyPath.Core.Anomalies;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Minigames
{
    /// <summary>
    /// ÐžÐºÐ½Ð¾ Ð¿Ð¾Ð¸ÑÐºÐ° Ð°Ñ€Ñ‚ÐµÑ„Ð°ÐºÑ‚Ð°: Ð¿Ð¾Ð»Ðµ Ñ ÐºÐ»ÐµÑ‚ÐºÐ°Ð¼Ð¸, Ð±ÐµÑÐ¿Ð»Ð°Ñ‚Ð½Ñ‹Ðµ ÑÐºÐ°Ð½Ñ‹ Ð¸ Ð¾Ð±Ñ‹ÑÐº ÐºÐ»ÐµÑ‚ÐºÐ¸ Ð·Ð° Ð¿Ð¾Ð¿Ñ‹Ñ‚ÐºÑƒ.
    /// ÐÑ€Ñ‚ÐµÑ„Ð°ÐºÑ‚Ñ‹ Ñ€Ð°Ð·Ð»Ð¾Ð¶ÐµÐ½Ñ‹ Ð¿Ñ€Ð¸ Ð¾Ñ‚ÐºÑ€Ñ‹Ñ‚Ð¸Ð¸ Ð°Ð½Ð¾Ð¼Ð°Ð»Ð¸Ð¸, Ñ€Ð°Ð´Ð°Ñ€ Ñ‚Ð¾Ð»ÑŒÐºÐ¾ Ð¿Ð¾ÐºÐ°Ð·Ñ‹Ð²Ð°ÐµÑ‚ ÑÐ¸Ð»Ñƒ ÑÐ¸Ð³Ð½Ð°Ð»Ð°.
    /// </summary>
    public sealed class RadarMinigameScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Ð—Ð°Ð³Ð¾Ð»Ð¾Ð²Ð¾Ðº Ð¾ÐºÐ½Ð°")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Ð¡Ñ‚Ñ€Ð¾ÐºÐ° Ð¿Ð¾Ð¿Ñ‹Ñ‚Ð¾Ðº Ð¾Ð±Ñ‹ÑÐºÐ°")]
        [SerializeField]
        private Text? attempts = null;

        [Tooltip("Ð¡Ñ‚Ñ€Ð¾ÐºÐ° Ð·Ð´Ð¾Ñ€Ð¾Ð²ÑŒÑ")]
        [SerializeField]
        private Text? health = null;

        [Tooltip("Ð¡Ñ‚Ñ€Ð¾ÐºÐ° Ð¾ÑÑ‚Ð°Ñ‚ÐºÐ° ÑÐºÐ°Ð½Ð¾Ð²")]
        [SerializeField]
        private Text? scans = null;

        [Tooltip("Ð¡Ñ‚Ñ€Ð¾ÐºÐ° ÑÐ¾Ð¾Ð±Ñ‰ÐµÐ½Ð¸Ñ")]
        [SerializeField]
        private Text? status = null;

        [Tooltip("Ð–ÑƒÑ€Ð½Ð°Ð» ÑÐºÐ°Ð½Ð¾Ð²")]
        [SerializeField]
        private Text? log = null;

        [Tooltip("ÐšÐ¾Ñ€ÐµÐ½ÑŒ ÐºÐ»ÐµÑ‚Ð¾Ðº Ð¿Ð¾Ð»Ñ: Ñƒ ÐºÐ°Ð¶Ð´Ð¾Ð³Ð¾ Ñ€ÐµÐ±Ñ‘Ð½ÐºÐ° Button, Image Ð¸ Text")]
        [SerializeField]
        private RectTransform? gridRoot = null;

        [Tooltip("ÐšÐ½Ð¾Ð¿ÐºÐ° ÑÐºÐ°Ð½Ð¸Ñ€Ð¾Ð²Ð°Ð½Ð¸Ñ")]
        [SerializeField]
        private Button? scan = null;

        [Tooltip("ÐšÐ½Ð¾Ð¿ÐºÐ° Ð¾Ð±Ñ‹ÑÐºÐ°")]
        [SerializeField]
        private Button? search = null;

        [Tooltip("ÐšÐ½Ð¾Ð¿ÐºÐ° Ð²Ñ‹Ñ…Ð¾Ð´Ð°")]
        [SerializeField]
        private Button? leave = null;

        private readonly List<Button> _cells = new List<Button>();
        private readonly List<Text> _labels = new List<Text>();
        private readonly List<Image> _images = new List<Image>();
        private readonly Dictionary<int, int> _signals = new Dictionary<int, int>();
        private readonly List<string> _log = new List<string>();
        private RadarState? _state;
        private AnomalyLocationObject? _anomaly;
        private int _selected = -1;

        /// <summary>Ð¢ÐµÐ¼Ð° Ñ„Ð¾Ð½Ð° â€” Ñ‚ÐµÐºÑƒÑ‰Ð°Ñ Ð»Ð¾ÐºÐ°Ñ†Ð¸Ñ.</summary>
        protected override ScreenTheme Theme
        {
            get
            {
                var world = Host.Session?.State.World;
                return world == null ? ScreenTheme.Default : ScreenTheme.Location(world.CurrentLocation.Background);
            }
        }

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (scan != null)
            {
                scan.onClick.AddListener(ScanSelected);
            }

            if (search != null)
            {
                search.onClick.AddListener(SearchSelected);
            }

            if (leave != null)
            {
                leave.onClick.AddListener(Leave);
            }
        }

        /// <summary>ÐžÑ‚ÐºÑ€Ñ‹Ð²Ð°ÐµÑ‚ Ð¾ÐºÐ½Ð¾ Ñ€Ð°Ð´Ð°Ñ€Ð° Ð´Ð»Ñ Ð°Ð½Ð¾Ð¼Ð°Ð»Ð¸Ð¸.</summary>
        /// <param name="anomaly">ÐžÐ±ÑŠÐµÐºÑ‚-Ð°Ð½Ð¾Ð¼Ð°Ð»Ð¸Ñ Ñ‚ÐµÐºÑƒÑ‰ÐµÐ¹ Ð»Ð¾ÐºÐ°Ñ†Ð¸Ð¸.</param>
        public void Show(AnomalyLocationObject anomaly)
        {
            _anomaly = anomaly;
            _selected = -1;
            _signals.Clear();
            _log.Clear();
            var session = Host.Session;
            _state = session == null ? null : Host.Sessions.OpenAnomalyRadar(session, anomaly);
            if (_state == null)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Radar_Not_Configured), true);
                ApplyTheme();
                Host.Router.Show(Panel);
                return;
            }

            BuildCells();
            for (var index = 0; index < _state.ScannedCells.Count; index++)
            {
                var cell = _state.ScannedCells[index];
                var result = Host.Sessions.ScanAnomaly(session!, anomaly, cell);
                if (result.Status == AnomalySearchStatus.Success)
                {
                    _signals[cell] = result.Signal;
                }
            }

            ApplyTheme();
            Host.Router.Show(Panel);
            SetStatus(string.Empty, false);
            Refresh();
            ShowRewardScreen();
        }

        /// <inheritdoc />
        public override bool HandleEscape()
        {
            Leave();
            return true;
        }

        private void BuildCells()
        {
            _cells.Clear();
            _labels.Clear();
            _images.Clear();
            if (gridRoot == null)
            {
                return;
            }

            if (_state == null)
            {
                for (var index = 0; index < gridRoot.childCount; index++)
                {
                    gridRoot.GetChild(index).gameObject.SetActive(false);
                }

                return;
            }

            for (var index = 0; index < gridRoot.childCount; index++)
            {
                var child = gridRoot.GetChild(index);
                var active = index < _state.CellCount;
                child.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var button = child.GetComponent<Button>();
                var label = child.GetComponentInChildren<Text>(true);
                var image = child.GetComponent<Image>();
                if (button == null || label == null || image == null)
                {
                    continue;
                }

                var cell = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectCell(cell));
                _cells.Add(button);
                _labels.Add(label);
                _images.Add(image);
            }
        }

        private void SelectCell(int cell)
        {
            _selected = cell;
            SetStatus(
                UiText.Format(Host.Texts, UiTextKey.Radar_Selected, CellName(cell)),
                false);
            // Кнопки «Сканировать» и «Обыскать» включаются только при выбранной клетке:
            // без полного обновления они остаются неактивными после выбора.
            Refresh();
        }

        private void ScanSelected()
        {
            if (_state == null || _anomaly == null || _selected < 0)
            {
                return;
            }

            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var result = Host.Sessions.ScanAnomaly(session, _anomaly, _selected);
            if (result.Status == AnomalySearchStatus.NoScans)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Radar_No_Scans), true);
                return;
            }

            if (result.Status == AnomalySearchStatus.WrongCell)
            {
                return;
            }

            _signals[_selected] = result.Signal;
            _log.Add(UiText.Format(
                Host.Texts,
                UiTextKey.Radar_Log,
                CellName(_selected),
                result.Signal));
            while (_log.Count > 4)
            {
                _log.RemoveAt(0);
            }

            SetStatus(
                UiText.Format(Host.Texts, UiTextKey.Radar_Scanned, result.Signal),
                false);
            _state = Host.Sessions.OpenAnomalyRadar(session, _anomaly) ?? _state;
            Refresh();
        }

        private void SearchSelected()
        {
            if (_state == null || _anomaly == null || _selected < 0)
            {
                return;
            }

            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var result = Host.Sessions.SearchAnomalyCell(session, _anomaly, _selected);
            if (result.Status == AnomalySearchStatus.AlreadySearched)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Radar_Already), true);
                return;
            }

            if (result.Status == AnomalySearchStatus.NoAttempts)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Radar_No_Attempts), true);
                return;
            }

            if (result.Status != AnomalySearchStatus.Success)
            {
                return;
            }

            _state = Host.Sessions.OpenAnomalyRadar(session, _anomaly) ?? _state;
            Refresh();
            if (result.Died)
            {
                Host.ShowGame(false);
                return;
            }

            if (result.Found)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Radar_Found), false, true);
                ShowRewardScreen();
                return;
            }

            SetStatus(UiText.Get(Host.Texts, UiTextKey.Radar_Empty), false);
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            if (title != null)
            {
                title.text = UiText.Get(Host.Texts, UiTextKey.Radar_Title);
            }

            if (attempts != null)
            {
                attempts.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Radar_Attempts,
                    _state.AttemptsLeft,
                    _state.AttemptsTotal);
            }

            if (health != null)
            {
                var player = Host.Session?.State.Player;
                health.text = player == null
                    ? string.Empty
                    : UiText.Format(
                        Host.Texts,
                        UiTextKey.Radar_Health,
                        player.Health,
                        CharacterStats.MaxHealth(WidgetHost, player));
            }

            if (scans != null)
            {
                scans.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Radar_Scans,
                    _state.ScansLeft,
                    _state.ScansTotal);
            }

            if (log != null)
            {
                log.text = string.Join("\n", _log);
            }

            if (scan != null)
            {
                scan.interactable = _selected >= 0 && _state.ScansLeft > 0;
            }

            if (search != null)
            {
                search.interactable = _selected >= 0 && _state.AttemptsLeft > 0;
            }

            RefreshCells();
        }

        private void RefreshCells()
        {
            if (_state == null)
            {
                return;
            }

            for (var index = 0; index < _cells.Count; index++)
            {
                var searched = _state.SearchedCells.Contains(index);
                var scanned = _state.ScannedCells.Contains(index);
                var signal = _signals.TryGetValue(index, out var value) ? value : 0;
                _images[index].color = index == _selected
                    ? MinigamePalette.CellSelected
                    : searched
                        ? MinigamePalette.CellSearched
                        : MinigamePalette.ForSignal(signal);
                _labels[index].text = searched
                    ? "Ã—"
                    : scanned
                        ? signal.ToString()
                        : "?";
                var strong = signal == 4 && !searched;
                _labels[index].fontStyle = strong ? FontStyle.Bold : FontStyle.Normal;
                _labels[index].fontSize = strong ? 32 : 26;
                _labels[index].color = strong ? Color.white : MinigamePalette.Text;
            }
        }

        private string CellName(int cell)
        {
            if (_state == null)
            {
                return cell.ToString();
            }

            var column = (char)('A' + cell % _state.Width);
            var row = cell / _state.Width + 1;
            return column.ToString() + row;
        }

        private void SetStatus(string text, bool error, bool success = false)
        {
            if (status == null)
            {
                return;
            }

            status.text = text;
            status.color = error
                ? MinigamePalette.Error
                : success
                    ? MinigamePalette.Success
                    : MinigamePalette.Muted;
        }

        /// <summary>ÐŸÐ¾ÐºÐ°Ð·Ñ‹Ð²Ð°ÐµÑ‚ Ð½Ð°Ð¹Ð´ÐµÐ½Ð½Ñ‹Ðµ Ð°Ñ€Ñ‚ÐµÑ„Ð°ÐºÑ‚Ñ‹, Ñ‡Ñ‚Ð¾Ð±Ñ‹ Ð¸Ð³Ñ€Ð¾Ðº Ð·Ð°Ð±Ñ€Ð°Ð» Ð¸Ñ… Ð¸Ð· Ð°Ð½Ð¾Ð¼Ð°Ð»Ð¸Ð¸.</summary>
        private void ShowRewardScreen()
        {
            if (_anomaly == null || !_anomaly.HasLoot)
            {
                return;
            }

            Host.ShowLootFromObject(
                _anomaly.Loot,
                item => _anomaly.Take(item.InstanceId),
                () => Host.Router.Show(Panel));
        }

        private void Leave() => Host.ShowGame(false);
    }
}
