#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>
    /// Вписывает подпись кнопки-действия в металлическую табличку кадра: подбирает самый крупный
    /// кегль, при котором текст остаётся одной строкой и не выходит за ширину таблички.
    /// </summary>
    /// <remarks>
    /// Собственная подгонка нужна потому, что <see cref="Text.resizeTextForBestFit"/> уменьшает
    /// кегль по высоте прямоугольника, а ширину с переносом не держит: длинная надпись вроде
    /// «Выйти из боя» вылезала за табличку на тёмную кромку кадра и становилась нечитаемой.
    /// </remarks>
    [RequireComponent(typeof(Text))]
    public sealed class ActionButtonLabel : MonoBehaviour
    {
        /// <summary>Компонент текста; пусто — берётся с этого объекта.</summary>
        [SerializeField]
        private Text? label = null;

        /// <summary>Кегль, с которого начинается подгонка.</summary>
        [SerializeField]
        private int maxFontSize = ActionButtonMetrics.LabelFontSize;

        /// <summary>Предел уменьшения подписи.</summary>
        [SerializeField]
        private int minFontSize = ActionButtonMetrics.LabelMinFontSize;

        private string _fittedText = string.Empty;
        private Vector2 _fittedBox;

        /// <summary>Задаёт кегли подгонки и сразу вписывает текущий текст.</summary>
        /// <param name="maxSize">Кегль, с которого начинается подгонка.</param>
        /// <param name="minSize">Предел уменьшения.</param>
        public void Configure(int maxSize, int minSize)
        {
            maxFontSize = Mathf.Max(minSize, maxSize);
            minFontSize = Mathf.Max(1, minSize);
            label ??= GetComponent<Text>();
            Fit();
        }

        /// <summary>Подбирает кегль под текущий текст и ширину таблички.</summary>
        public void Fit()
        {
            label ??= GetComponent<Text>();
            if (label == null)
            {
                return;
            }

            var box = label.rectTransform.rect;
            var text = label.text ?? string.Empty;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = false;
            if (string.IsNullOrEmpty(text))
            {
                label.fontSize = maxFontSize;
                Remember(text, box.size);
                return;
            }

            label.fontSize = maxFontSize;
            if (label.preferredWidth <= 0f)
            {
                // Текст ещё не измерен (префаб собран вне сцены): подгонку повторит LateUpdate.
                return;
            }

            var limit = box.width * ActionButtonMetrics.LabelFill;
            var chosen = minFontSize;
            for (var size = maxFontSize; size >= minFontSize; size--)
            {
                label.fontSize = size;
                chosen = size;
                if (label.preferredWidth <= limit)
                {
                    break;
                }
            }

            label.fontSize = chosen;
            Remember(text, box.size);
        }

        /// <summary>Вписывает все подписи под деревом: кадр снимается без кадров Update.</summary>
        /// <param name="root">Корень экрана.</param>
        public static void FitAll(Transform root)
        {
            foreach (var fitter in root.GetComponentsInChildren<ActionButtonLabel>(true))
            {
                fitter.Fit();
            }
        }

        private void Remember(string text, Vector2 box)
        {
            _fittedText = text;
            _fittedBox = box;
        }

        private void OnEnable() => Fit();

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                Fit();
            }
        }

        private void LateUpdate()
        {
            if (label == null)
            {
                return;
            }

            var box = label.rectTransform.rect;
            if (label.text == _fittedText && box.size == _fittedBox)
            {
                return;
            }

            Fit();
        }
    }
}
