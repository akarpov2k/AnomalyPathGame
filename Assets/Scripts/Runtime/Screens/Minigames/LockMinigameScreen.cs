#nullable enable
using System;
using System.Collections.Generic;
using System.Text;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Minigames
{
    /// <summary>
    /// Окно взлома тайника: код из символов, подсказки «на месте» и «есть, но не там»,
    /// семь попыток. Разметка приходит из префаба, код заполняет слоты, символы и историю.
    /// </summary>
    public sealed class LockMinigameScreen : PrefabScreenBase<IScreenHost>
    {
        [Tooltip("Заголовок окна")]
        [SerializeField]
        private Text? title = null;

        [Tooltip("Строка остатка попыток")]
        [SerializeField]
        private Text? attempts = null;

        [Tooltip("Строка длины кода и алфавита")]
        [SerializeField]
        private Text? code = null;

        [Tooltip("Строка сообщения")]
        [SerializeField]
        private Text? status = null;

        [Tooltip("Корень слотов кода: у каждого ребёнка Button и Text")]
        [SerializeField]
        private RectTransform? slotsRoot = null;

        [Tooltip("Корень кнопок символов: у каждого ребёнка Button и Text")]
        [SerializeField]
        private RectTransform? symbolsRoot = null;

        [Tooltip("Корень строк истории: у каждой строки Code, Exact и Partial")]
        [SerializeField]
        private RectTransform? historyRoot = null;

        [Tooltip("Кнопка «Стереть»")]
        [SerializeField]
        private Button? clear = null;

        [Tooltip("Кнопка «Ввести»")]
        [SerializeField]
        private Button? submit = null;

        [Tooltip("Кнопка «Уйти»")]
        [SerializeField]
        private Button? leave = null;

        private readonly List<Button> _slots = new List<Button>();
        private readonly List<Text> _slotLabels = new List<Text>();
        private readonly List<Button> _symbolButtons = new List<Button>();
        private StashLockState? _state;
        private char[] _input = Array.Empty<char>();
        private int _selected;

        /// <inheritdoc />
        protected override void OnBound()
        {
            if (clear != null)
            {
                clear.onClick.AddListener(ClearInput);
            }

            if (submit != null)
            {
                submit.onClick.AddListener(Submit);
            }

            if (leave != null)
            {
                leave.onClick.AddListener(Leave);
            }
        }

        /// <summary>Открывает окно взлома текущего тайника.</summary>
        public void Show()
        {
            var session = Host.Session;
            _state = session == null ? null : Host.Sessions.LockState(session);
            if (_state == null)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Lock_Not_Configured), true);
                ApplyTheme();
                Host.Router.Show(Panel);
                return;
            }

            _input = new char[_state.CodeLength];
            _selected = 0;
            if (_state.Solved)
            {
                ApplyTheme();
                Host.Router.Show(Panel);
                OpenStash();
                return;
            }

            BuildSlots();
            BuildSymbols();
            ApplyTheme();
            Host.Router.Show(Panel);
            SetStatus(
                _state.Solved
                    ? UiText.Get(Host.Texts, UiTextKey.Lock_Solved)
                    : _state.Failed
                        ? UiText.Get(Host.Texts, UiTextKey.Lock_Failed)
                        : string.Empty,
                _state.Failed,
                _state.Solved);
            Refresh();
        }

        /// <inheritdoc />
        public override bool HandleEscape()
        {
            Leave();
            return true;
        }

        private void BuildSlots()
        {
            _slots.Clear();
            _slotLabels.Clear();
            if (slotsRoot == null)
            {
                return;
            }

            if (_state == null)
            {
                for (var index = 0; index < slotsRoot.childCount; index++)
                {
                    slotsRoot.GetChild(index).gameObject.SetActive(false);
                }

                return;
            }

            for (var index = 0; index < slotsRoot.childCount; index++)
            {
                var child = slotsRoot.GetChild(index);
                var active = index < _state.CodeLength;
                child.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var button = child.GetComponent<Button>();
                var label = child.GetComponentInChildren<Text>(true);
                if (button == null || label == null)
                {
                    continue;
                }

                var slot = index;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SelectSlot(slot));
                _slots.Add(button);
                _slotLabels.Add(label);
            }
        }

        private void BuildSymbols()
        {
            _symbolButtons.Clear();
            if (symbolsRoot == null)
            {
                return;
            }

            if (_state == null)
            {
                for (var index = 0; index < symbolsRoot.childCount; index++)
                {
                    symbolsRoot.GetChild(index).gameObject.SetActive(false);
                }

                return;
            }

            for (var index = 0; index < symbolsRoot.childCount; index++)
            {
                var child = symbolsRoot.GetChild(index);
                var active = index < _state.AlphabetSize;
                child.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var button = child.GetComponent<Button>();
                var label = child.GetComponentInChildren<Text>(true);
                if (button == null || label == null)
                {
                    continue;
                }

                label.text = (index + 1).ToString();
                var digit = index + 1;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => Choose(digit));
                _symbolButtons.Add(button);
            }
        }

        private void SelectSlot(int index)
        {
            if (index < 0 || index >= _input.Length)
            {
                return;
            }

            _selected = index;
            RefreshSlots();
        }

        private void Choose(int digit)
        {
            if (_state == null || !_state.CanGuess || _input.Length == 0)
            {
                return;
            }

            var digitChar = (char)('0' + digit);
            for (var index = 0; index < _input.Length; index++)
            {
                if (index != _selected && _input[index] == digitChar)
                {
                    _input[index] = '\0';
                }
            }

            _input[_selected] = digitChar;
            _selected = NextEmpty(_selected);
            RefreshSlots();
            SetStatus(string.Empty, false);
        }

        private int NextEmpty(int from)
        {
            for (var offset = 1; offset <= _input.Length; offset++)
            {
                var index = (from + offset) % _input.Length;
                if (_input[index] == '\0')
                {
                    return index;
                }
            }

            return from;
        }

        private void ClearInput()
        {
            Array.Clear(_input, 0, _input.Length);
            _selected = 0;
            RefreshSlots();
            SetStatus(string.Empty, false);
        }

        private void Submit()
        {
            if (_state == null)
            {
                return;
            }

            var seen = new bool[10];
            foreach (var symbol in _input)
            {
                if (symbol == '\0')
                {
                    SetStatus(UiText.Get(Host.Texts, UiTextKey.Lock_Invalid), true);
                    return;
                }

                if (seen[symbol - '0'])
                {
                    SetStatus(UiText.Get(Host.Texts, UiTextKey.Lock_Invalid), true);
                    return;
                }

                seen[symbol - '0'] = true;
            }

            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var next = Host.Sessions.GuessStashLock(session, new string(_input));
            if (next == null)
            {
                return;
            }

            _state = next;
            if (next.Solved)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Lock_Solved), false, true);
                OpenStash();
                return;
            }

            if (next.Failed)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Lock_Failed), true);
            }
            else if (next.LastGuess != null)
            {
                SetStatus(
                    UiText.Format(
                        Host.Texts,
                        UiTextKey.Lock_Wrong,
                        next.LastGuess.Exact,
                        next.LastGuess.Partial),
                    false);
            }

            Array.Clear(_input, 0, _input.Length);
            _selected = 0;
            Refresh();
        }

        private void OpenStash()
        {
            var session = Host.Session;
            if (session == null)
            {
                Leave();
                return;
            }

            var result = Host.Sessions.OpenStash(session);
            if (result.Status == AnomalyPath.Core.Loot.StashOpenStatus.LockFailed)
            {
                SetStatus(UiText.Get(Host.Texts, UiTextKey.Lock_Failed), true);
                return;
            }

            if (result.Status != AnomalyPath.Core.Loot.StashOpenStatus.Opened)
            {
                return;
            }

            if (result.Jackpot)
            {
                Host.Toast(UiText.Get(Host.Texts, UiTextKey.Game_Jackpot));
            }

            if (result.Loot.Count > 0)
            {
                Host.ShowRewards(result.Loot, 0, RewardSource.Stash, ignoreWeight: result.Jackpot);
                return;
            }

            Host.ShowGame(false);
        }

        private void Refresh()
        {
            if (_state == null)
            {
                return;
            }

            if (title != null)
            {
                title.text = UiText.Get(Host.Texts, UiTextKey.Lock_Title);
            }

            if (attempts != null)
            {
                attempts.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Lock_Attempts,
                    _state.AttemptsLeft,
                    _state.AttemptsTotal);
            }

            if (code != null)
            {
                code.text = UiText.Format(
                    Host.Texts,
                    UiTextKey.Lock_Code,
                    _state.CodeLength,
                    _state.AlphabetSize);
            }

            RefreshSlots();
            RefreshHistory();
            if (submit != null)
            {
                submit.interactable = _state.CanGuess;
            }

            if (clear != null)
            {
                clear.interactable = _state.CanGuess;
            }
        }

        private void RefreshSlots()
        {
            for (var index = 0; index < _slotLabels.Count; index++)
            {
                var symbol = index < _input.Length ? _input[index] : '\0';
                _slotLabels[index].text = symbol == '\0' ? "·" : symbol.ToString();
                var image = _slots[index].targetGraphic;
                if (image != null)
                {
                    image.color = index == _selected ? MinigamePalette.CellSelected : Color.white;
                }
            }
        }

        private void RefreshHistory()
        {
            if (historyRoot == null || _state == null)
            {
                return;
            }

            for (var index = 0; index < historyRoot.childCount; index++)
            {
                var row = historyRoot.GetChild(index);
                var active = index < _state.History.Count;
                row.gameObject.SetActive(active);
                if (!active)
                {
                    continue;
                }

                var item = _state.History[index];
                SetChild(row, "Code", ColoredGuess(item));
                SetChild(row, "Exact", item.Exact.ToString());
                SetChild(row, "Partial", item.Partial.ToString());
            }
        }

        private static string ColoredGuess(LockGuessResult guess)
        {
            var builder = new StringBuilder(guess.Guess.Length * 24);
            for (var index = 0; index < guess.Guess.Length; index++)
            {
                var digit = guess.Guess[index];
                var mark = index < guess.Marks.Count ? guess.Marks[index] : LockDigitMark.None;
                var color = mark switch
                {
                    LockDigitMark.CorrectPlace => MinigamePalette.Success,
                    LockDigitMark.WrongPlace => MinigamePalette.WrongPlace,
                    _ => (Color?)null,
                };
                if (color == null)
                {
                    builder.Append(digit);
                    continue;
                }

                builder.Append("<color=#")
                    .Append(ColorUtility.ToHtmlStringRGB(color.Value))
                    .Append('>')
                    .Append(digit)
                    .Append("</color>");
            }

            return builder.ToString();
        }

        private static void SetChild(Transform row, string name, string value)
        {
            var child = row.Find(name);
            if (child != null && child.TryGetComponent<Text>(out var label))
            {
                label.text = value;
            }
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

        private void Leave() => Host.ShowGame(false);
    }
}
