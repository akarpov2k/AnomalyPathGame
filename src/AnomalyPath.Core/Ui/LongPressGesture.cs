#nullable enable
namespace AnomalyPath.Core.Ui
{
    /// <summary>
    /// Удержание указателя: копит время нажатия и один раз сообщает, что порог пройден.
    /// Отпускание, перетаскивание и отмена сбрасывают жест. Время приходит снаружи, поэтому
    /// поведение полностью детерминировано и проверяется тестом без сцены.
    /// </summary>
    public sealed class LongPressGesture
    {
        private readonly float _threshold;
        private float _held;

        /// <summary>Создаёт жест с порогом удержания.</summary>
        /// <param name="thresholdSeconds">Сколько секунд держать; отрицательное значение считается нулём.</param>
        public LongPressGesture(float thresholdSeconds)
        {
            _threshold = thresholdSeconds > 0f ? thresholdSeconds : 0f;
        }

        /// <summary>Порог удержания в секундах.</summary>
        public float Threshold => _threshold;

        /// <summary><c>true</c>, пока жест ждёт порога и не отменён.</summary>
        public bool IsPending { get; private set; }

        /// <summary><c>true</c>, если жест уже сработал; сбрасывается новым нажатием.</summary>
        public bool Fired { get; private set; }

        /// <summary>Начинает удержание: сбрасывает счётчик и прошлое срабатывание.</summary>
        public void Begin()
        {
            IsPending = true;
            Fired = false;
            _held = 0f;
        }

        /// <summary>Отменяет удержание: отпускание, перетаскивание или потеря указателя.</summary>
        public void Cancel()
        {
            IsPending = false;
            _held = 0f;
        }

        /// <summary>Продвигает время удержания.</summary>
        /// <param name="deltaSeconds">Сколько прошло; отрицательное значение не учитывается.</param>
        /// <returns><c>true</c> в тот кадр, когда порог впервые пройден.</returns>
        public bool Advance(float deltaSeconds)
        {
            if (!IsPending)
            {
                return false;
            }

            _held += deltaSeconds > 0f ? deltaSeconds : 0f;
            if (_held < _threshold)
            {
                return false;
            }

            IsPending = false;
            Fired = true;
            return true;
        }
    }
}
