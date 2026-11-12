#nullable enable
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Листалка страниц сохранений «&lt;&lt; &lt; N / M &gt; &gt;&gt;».
    /// Кнопки и подпись — объекты префаба; компонент только показывает нужные стрелки и номер страницы.
    /// </summary>
    public sealed class SaveSlotPager : MonoBehaviour
    {
        [Tooltip("Кнопка первой страницы «<<»")]
        [SerializeField]
        private Button? firstPage = null;

        [Tooltip("Кнопка предыдущей страницы «<»")]
        [SerializeField]
        private Button? previousPage = null;

        [Tooltip("Номер текущей страницы")]
        [SerializeField]
        private Text? pageLabel = null;

        [Tooltip("Кнопка следующей страницы «>»")]
        [SerializeField]
        private Button? nextPage = null;

        [Tooltip("Кнопка последней страницы «>>»")]
        [SerializeField]
        private Button? lastPage = null;

        private IGameTexts? _texts;

        /// <summary>Кнопка первой страницы; <c>null</c> — в префабе такой кнопки нет.</summary>
        public Button? FirstPage => firstPage;

        /// <summary>Кнопка предыдущей страницы; <c>null</c> — в префабе такой кнопки нет.</summary>
        public Button? PreviousPage => previousPage;

        /// <summary>Кнопка следующей страницы; <c>null</c> — в префабе такой кнопки нет.</summary>
        public Button? NextPage => nextPage;

        /// <summary>Кнопка последней страницы; <c>null</c> — в префабе такой кнопки нет.</summary>
        public Button? LastPage => lastPage;

        /// <summary>Подпись номера страницы; <c>null</c> — в префабе такой подписи нет.</summary>
        public Text? PageLabel => pageLabel;

        /// <summary>Передаёт листалке каталог текстов для подписи страницы.</summary>
        /// <param name="texts">Каталог текстов игры.</param>
        public void Bind(IGameTexts texts) => _texts = texts;

        /// <summary>Показывает номер страницы и прячет недоступные стрелки.</summary>
        /// <param name="page">Текущая страница с нуля.</param>
        /// <param name="pageCount">Всего страниц.</param>
        public void Refresh(int page, int pageCount)
        {
            var multiple = pageCount > 1;
            var hasPrevious = multiple && page > 0;
            var hasNext = multiple && page < pageCount - 1;
            Show(firstPage, hasPrevious);
            Show(previousPage, hasPrevious);
            Show(nextPage, hasNext);
            Show(lastPage, hasNext);
            if (pageLabel != null)
            {
                pageLabel.gameObject.SetActive(multiple);
                if (multiple && _texts != null)
                {
                    pageLabel.text = UiText.Format(_texts, UiTextKey.SaveSlot_Pager, page + 1, pageCount);
                }
            }
        }

        private static void Show(Button? button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
    }
}
