#nullable enable
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Menu
{
    /// <summary>
    /// Идущие титры экрана авторов: полотно с текстом непрерывно едет снизу вверх внутри
    /// видимой области и, дойдя до верха, снова начинается снизу. Смещение считается в единицах
    /// канваса, поэтому скорость одинакова при любом разрешении окна.
    /// </summary>
    public sealed class CreditsScroller : MonoBehaviour
    {
        [Tooltip("Видимая область титров: она обрезает полотно")]
        [SerializeField]
        private RectTransform? viewport = null;

        [Tooltip("Полотно титров: оно двигается вверх внутри видимой области")]
        [SerializeField]
        private RectTransform? track = null;

        [Tooltip("Текст титров: по его высоте считается длина полотна")]
        [SerializeField]
        private Text? label = null;

        [Tooltip("Скорость титров в единицах канваса в секунду")]
        [SerializeField]
        private float speed = 0f;

        private float _offset;
        private bool _running;

        /// <summary>Видимая область титров; <c>null</c>, пока префаб не назначен.</summary>
        public RectTransform? Viewport => viewport;

        /// <summary>Полотно титров; <c>null</c>, пока префаб не назначен.</summary>
        public RectTransform? Track => track;

        /// <summary>Смещение полотна титров от низа видимой области, единицы канваса.</summary>
        public float Offset => _offset;

        /// <summary>Скорость титров в единицах канваса в секунду.</summary>
        public float Speed => speed;

        /// <summary>Титры идут.</summary>
        public bool IsRunning => _running;

        /// <summary>Ставит титры под нижним краем области и включает движение.</summary>
        public void Restart()
        {
            Prepare();
            _offset = -Height();
            _running = true;
            Apply();
        }

        /// <summary>Останавливает титры на текущем месте.</summary>
        public void Stop() => _running = false;

        /// <summary>Показывает титры с заданного смещения, не меняя состояние движения.</summary>
        /// <param name="offset">Смещение полотна от низа видимой области, единицы канваса.</param>
        public void Seek(float offset)
        {
            _offset = offset;
            Apply();
        }

        /// <summary>Продвигает титры на заданное время; за верхом области начинает их заново.</summary>
        /// <param name="delta">Шаг времени в секундах.</param>
        public void Advance(float delta)
        {
            if (!_running || viewport == null || track == null)
            {
                return;
            }

            var length = viewport.rect.height + Height();
            if (length <= 0f)
            {
                return;
            }

            _offset += speed * delta;
            while (_offset > viewport.rect.height)
            {
                _offset -= length;
            }

            Apply();
        }

        private void Update() => Advance(Time.deltaTime);

        /// <summary>Меряет текст и ставит полотну высоту, от которой считается конец титров.</summary>
        private void Prepare()
        {
            if (track == null || label == null || viewport == null)
            {
                return;
            }

            Canvas.ForceUpdateCanvases();
            var height = label.preferredHeight + (2f * AuthorsMetrics.CreditsPadding);
            track.sizeDelta = new Vector2(track.sizeDelta.x, height);
        }

        private float Height() => track != null ? track.sizeDelta.y : 0f;

        private void Apply()
        {
            if (track == null)
            {
                return;
            }

            track.anchoredPosition = new Vector2(track.anchoredPosition.x, _offset);
        }
    }
}
