#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game.Hub
{
    /// <summary>
    /// Подсказка с названием объекта хаба: появляется внизу экрана, когда курсор постоял
    /// на объекте дольше задержки из <see cref="HubMetrics"/>, и исчезает, когда курсор ушёл.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HubTooltip : MonoBehaviour
    {
        [Tooltip("Плашка подсказки: прозрачность включает и выключает её")]
        [SerializeField]
        private CanvasGroup? group = null;

        [Tooltip("Подпись с названием объекта")]
        [SerializeField]
        private Text? label = null;

        private IGameTexts? _texts;
        private string? _hoverKey;
        private string? _hoverText;
        private float _hoverAt;

        /// <summary>Показана ли подсказка сейчас.</summary>
        public bool IsShown { get; private set; }

        /// <summary>Текст подсказки; <c>null</c>, пока она скрыта.</summary>
        public string? ShownText => IsShown ? label?.text : null;

        /// <summary>Подключает каталог текстов и прячет подсказку.</summary>
        /// <param name="texts">Каталог текстов игры.</param>
        public void Initialize(IGameTexts texts)
        {
            _texts = texts;
            Hide();
        }

        /// <summary>Курсор настал на объект: через задержку показывается название.</summary>
        /// <param name="key">Код ключа названия в каталоге текстов.</param>
        public void Hover(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _hoverKey = key;
            _hoverText = null;
            _hoverAt = Time.unscaledTime;
            PlaceTop();
            Hide();
        }

        /// <summary>Курсор настал на объект: через задержку показывается готовый текст.</summary>
        /// <param name="text">Текст подсказки; например, действие значка вместе с ценой.</param>
        public void HoverText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _hoverKey = null;
            _hoverText = text;
            _hoverAt = Time.unscaledTime;
            PlaceTop();
            Hide();
        }

        /// <summary>Курсор ушёл с объекта: подсказка этого объекта скрывается.</summary>
        /// <param name="key">Код ключа названия в каталоге текстов.</param>
        public void Leave(string key)
        {
            if (_hoverKey == key)
            {
                _hoverKey = null;
                _hoverText = null;
                Hide();
            }
        }

        /// <summary>Скрывает подсказку и забывает наведённый объект.</summary>
        public void Clear()
        {
            _hoverKey = null;
            _hoverText = null;
            Hide();
        }

        /// <summary>Скрывает подсказку, не забывая наведённый объект.</summary>
        public void HideTooltip() => Hide();

        private void Update()
        {
            if (IsShown || _texts == null)
            {
                return;
            }

            if (_hoverKey == null && string.IsNullOrEmpty(_hoverText))
            {
                return;
            }

            if (Time.unscaledTime - _hoverAt < HubMetrics.TooltipDelay)
            {
                return;
            }

            if (label != null)
            {
                label.text = _hoverKey != null ? UiText.Get(_texts, _hoverKey) : _hoverText!;
            }

            IsShown = true;
            if (group != null)
            {
                group.alpha = 1f;
            }
        }

        private void Hide()
        {
            IsShown = false;
            if (group != null)
            {
                group.alpha = 0f;
            }
        }

        /// <summary>Название объекта появляется у верхнего края сцены.</summary>
        private void PlaceTop()
        {
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -HubMetrics.TooltipTop);
        }
    }
}
