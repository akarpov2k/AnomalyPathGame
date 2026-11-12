#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Dialogs
{
    /// <summary>
    /// Окно диалога: меню разделов и окно разговора. Меню показывает заказанные полосы
    /// (например, «Поговорить», «Поторгуем?», «Прощай» у торговца или одно «Поговорить»
    /// у NPC); окно разговора — небольшую область с ответом собеседника сверху и
    /// прокручиваемый список фраз игрока снизу. Окно живёт поверх экрана и не перекрывает
    /// его нижние значки. Разметка и полосы меню — объекты префаба, код скрывает лишние
    /// и заполняет разговор.
    /// </summary>
    public sealed class DialogOverlayView : MonoBehaviour
    {
        [Tooltip("Прозрачная плашка поверх экрана: перекрывает клики мимо окна")]
        [SerializeField]
        private Image? blocker = null;

        [Tooltip("Окно меню разделов")]
        [SerializeField]
        private RectTransform? menu = null;

        [Tooltip("Полоса «Поговорить»")]
        [SerializeField]
        private Button? talkButton = null;

        [Tooltip("Полоса «Поторгуем?»")]
        [SerializeField]
        private Button? tradeButton = null;

        [Tooltip("Полоса «Прощай»")]
        [SerializeField]
        private Button? farewellButton = null;

        [Tooltip("Окно разговора")]
        [SerializeField]
        private RectTransform? talk = null;

        [Tooltip("Имя собеседника")]
        [SerializeField]
        private Text? speaker = null;

        [Tooltip("Ответ собеседника")]
        [SerializeField]
        private Text? reply = null;

        [Tooltip("Прокрутка фраз игрока")]
        [SerializeField]
        private ScrollArea? phrases = null;

        [Tooltip("Неактивная кнопка-фраза — образец для списка")]
        [SerializeField]
        private Button? phraseTemplate = null;

        private IGameTexts _texts = null!;
        private Action<string> _choose = null!;
        private readonly List<string> _menuNames = new List<string>();
        private readonly List<Button> _menuButtons = new List<Button>();
        private readonly List<GameObject> _phraseButtons = new List<GameObject>();

        /// <summary>Корень окна диалога.</summary>
        public RectTransform Root => (RectTransform)transform;

        /// <summary>Показано ли окно целиком.</summary>
        public bool IsShown => gameObject.activeSelf;

        /// <summary>Показано ли меню разделов.</summary>
        public bool MenuShown => menu != null && menu.gameObject.activeSelf;

        /// <summary>Показано ли окно разговора.</summary>
        public bool TalkShown => talk != null && talk.gameObject.activeSelf;

        /// <summary>Область прокрутки фраз.</summary>
        public ScrollArea PhraseScroll => phrases!;

        /// <summary>Связывает префаб с каталогом строк и заказанными полосами меню.</summary>
        /// <param name="texts">Каталог текстов для подписей.</param>
        /// <param name="menu">Полосы меню в порядке показа.</param>
        /// <param name="choose">Что выполнить с выбранной фразой разговора.</param>
        /// <param name="blockClicks">Перекрывать ли клики мимо окна прозрачной плашкой.</param>
        /// <exception cref="ArgumentNullException">Обязательная зависимость не задана.</exception>
        /// <exception cref="ArgumentException">Меню пусто.</exception>
        public void Bind(
            IGameTexts texts,
            IReadOnlyList<MenuEntry> menu,
            Action<string> choose,
            bool blockClicks)
        {
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            _choose = choose ?? throw new ArgumentNullException(nameof(choose));
            if (menu == null || menu.Count == 0)
            {
                throw new ArgumentException(null, nameof(menu));
            }

            if (blocker != null)
            {
                blocker.gameObject.SetActive(blockClicks);
            }

            foreach (var label in GetComponentsInChildren<LocalizedText>(true))
            {
                label.Apply(texts);
            }

            if (talkButton != null)
            {
                talkButton.gameObject.SetActive(false);
            }

            if (tradeButton != null)
            {
                tradeButton.gameObject.SetActive(false);
            }

            if (farewellButton != null)
            {
                farewellButton.gameObject.SetActive(false);
            }

            _menuNames.Clear();
            _menuButtons.Clear();
            foreach (var entry in menu)
            {
                var button = ButtonOf(entry.Name);
                if (button == null)
                {
                    throw new ArgumentException(null, nameof(menu));
                }

                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = UiText.Get(texts, entry.TextKey);
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => entry.Click());
                _menuNames.Add(entry.Name);
                _menuButtons.Add(button);
            }

            Hide();
        }

        /// <summary>Показывает меню разделов с перечисленными полосами.</summary>
        /// <param name="visible">Имена полос в порядке показа; остальные скрыты.</param>
        /// <exception cref="ArgumentNullException">Список не задан.</exception>
        public void ShowMenu(IReadOnlyList<string> visible)
        {
            if (visible == null)
            {
                throw new ArgumentNullException(nameof(visible));
            }

            ClearPhrases();
            if (talk != null)
            {
                talk.gameObject.SetActive(false);
            }

            if (menu != null)
            {
                menu.gameObject.SetActive(true);
            }

            for (var index = 0; index < _menuButtons.Count; index++)
            {
                _menuButtons[index].gameObject.SetActive(Contains(visible, _menuNames[index]));
            }

            LayoutMenuButtons();
            ShowRoot();
        }

        /// <summary>Показывает окно разговора с ответом собеседника и фразами игрока.</summary>
        /// <param name="speaker">Имя собеседника.</param>
        /// <param name="reply">Ответ собеседника.</param>
        /// <param name="options">Доступные фразы в порядке показа.</param>
        public void ShowTalk(string speaker, string reply, IReadOnlyList<DialogOptionView> options)
        {
            if (this.speaker != null)
            {
                this.speaker.text = speaker;
            }

            if (this.reply != null)
            {
                this.reply.text = reply;
            }

            if (menu != null)
            {
                menu.gameObject.SetActive(false);
            }

            if (talk != null)
            {
                talk.gameObject.SetActive(true);
            }

            ShowRoot();
            BuildPhrases(options);
        }

        /// <summary>Скрывает окно диалога, оставляя экран позади на месте.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            if (menu != null)
            {
                menu.gameObject.SetActive(false);
            }

            if (talk != null)
            {
                talk.gameObject.SetActive(false);
            }

            ClearPhrases();
        }

        private Button? ButtonOf(string name)
        {
            if (string.Equals(name, MenuTalk, StringComparison.Ordinal))
            {
                return talkButton;
            }

            if (string.Equals(name, MenuTrade, StringComparison.Ordinal))
            {
                return tradeButton;
            }

            if (string.Equals(name, MenuFarewell, StringComparison.Ordinal))
            {
                return farewellButton;
            }

            return null;
        }

        private static bool Contains(IReadOnlyList<string> names, string name)
        {
            for (var index = 0; index < names.Count; index++)
            {
                if (string.Equals(names[index], name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowRoot()
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        private void LayoutMenuButtons()
        {
            var index = 0;
            foreach (var button in _menuButtons)
            {
                if (!button.gameObject.activeSelf)
                {
                    continue;
                }

                var rect = (RectTransform)button.transform;
                var top = DialogMetrics.WindowPadding +
                          (index * (DialogMetrics.MenuButtonHeight + DialogMetrics.MenuButtonGap));
                rect.anchoredPosition = new Vector2(0f, -top);
                index++;
            }

            if (menu != null)
            {
                menu.sizeDelta = new Vector2(
                    DialogMetrics.WindowWidth,
                    DialogMetrics.MenuHeightFor(index));
            }
        }

        private void BuildPhrases(IReadOnlyList<DialogOptionView> options)
        {
            ClearPhrases();
            if (phrases == null || phraseTemplate == null)
            {
                return;
            }

            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];
                var button = Instantiate(phraseTemplate, phrases.Content, false);
                button.gameObject.name = "DialogPhrase_" + option.OptionId;
                button.gameObject.SetActive(true);
                button.interactable = option.Available;
                var label = button.GetComponentInChildren<Text>();
                if (label != null)
                {
                    label.text = _texts.Get(option.TextKey);
                    label.fontSize = DialogMetrics.PhraseFontSize;
                }

                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _choose(option.OptionId));
                PlacePhrase((RectTransform)button.transform, index);
                _phraseButtons.Add(button.gameObject);
            }

            var height = (2f * DialogMetrics.PhrasePadding) +
                         (options.Count * DialogMetrics.PhraseHeight) +
                         (Mathf.Max(0, options.Count - 1) * DialogMetrics.PhraseGap);
            phrases.SetContentHeight(Mathf.Max(phrases.ViewportHeight, height));
        }

        private static void PlacePhrase(RectTransform rect, int index)
        {
            var top = DialogMetrics.PhrasePadding +
                      (index * (DialogMetrics.PhraseHeight + DialogMetrics.PhraseGap));
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.offsetMin = new Vector2(DialogMetrics.PhrasePadding, -(top + DialogMetrics.PhraseHeight));
            rect.offsetMax = new Vector2(-DialogMetrics.PhrasePadding, -top);
        }

        private void ClearPhrases()
        {
            foreach (var item in _phraseButtons)
            {
                if (item == null)
                {
                    continue;
                }

                item.SetActive(false);
                Destroy(item);
            }

            _phraseButtons.Clear();
        }

        /// <summary>Имя полосы меню «Поговорить».</summary>
        public const string MenuTalk = "DialogTalk";

        /// <summary>Имя полосы меню «Поторгуем?».</summary>
        public const string MenuTrade = "DialogTrade";

        /// <summary>Имя полосы меню «Прощай».</summary>
        public const string MenuFarewell = "DialogFarewell";

        /// <summary>Полоса меню: имя объекта, ключ подписи и действие.</summary>
        public sealed class MenuEntry
        {
            /// <summary>Создаёт полосу меню.</summary>
            /// <param name="name">Имя объекта в иерархии.</param>
            /// <param name="textKey">Ключ подписи в каталоге строк.</param>
            /// <param name="click">Что выполнить по нажатию.</param>
            /// <exception cref="ArgumentException">Имя полосы пусто.</exception>
            /// <exception cref="ArgumentNullException">Действие не задано.</exception>
            public MenuEntry(string name, UiTextKey textKey, Action click)
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new ArgumentException(null, nameof(name));
                }

                Name = name;
                TextKey = textKey;
                Click = click ?? throw new ArgumentNullException(nameof(click));
            }

            /// <summary>Имя объекта полосы в иерархии.</summary>
            public string Name { get; }

            /// <summary>Ключ подписи в каталоге строк.</summary>
            public UiTextKey TextKey { get; }

            /// <summary>Что выполнить по нажатию.</summary>
            public Action Click { get; }
        }
    }
}
