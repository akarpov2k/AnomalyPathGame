#nullable enable
using AnomalyPath.Core.Ui;

namespace AnomalyPath.Tests
{
    /// <summary>Жест удержания: порог, отмена, повтор и отсутствие «дребезга» после срабатывания.</summary>
    public sealed class GestureTests : TestBase
    {
        /// <inheritdoc />
        public override void Run()
        {
            DoesNotFireBeforeThreshold();
            FiresAtThresholdOnce();
            CancelStopsGesture();
            NewPressAfterFire();
            NegativeTimeIsIgnored();
            ZeroThresholdFiresImmediately();
            NegativeThresholdBecomesZero();
        }

        private void DoesNotFireBeforeThreshold()
        {
            Check("жест: до порога не срабатывает", () =>
            {
                var gesture = new LongPressGesture(0.45f);
                gesture.Begin();

                False(gesture.Advance(0.2f), "первый шаг");
                False(gesture.Advance(0.2f), "второй шаг");
                True(gesture.IsPending, "ждёт порога");
                False(gesture.Fired, "не сработал");
            });
        }

        private void FiresAtThresholdOnce()
        {
            Check("жест: на пороге срабатывает ровно один раз", () =>
            {
                var gesture = new LongPressGesture(0.45f);
                gesture.Begin();

                True(gesture.Advance(0.45f), "порог пройден");
                True(gesture.Fired, "сработал");
                False(gesture.IsPending, "больше не ждёт");
                False(gesture.Advance(1f), "повторно не срабатывает");
            });
        }

        private void CancelStopsGesture()
        {
            Check("жест: отмена сбрасывает накопленное время", () =>
            {
                var gesture = new LongPressGesture(0.45f);
                gesture.Begin();
                gesture.Advance(0.4f);
                gesture.Cancel();

                False(gesture.IsPending, "отменён");
                False(gesture.Advance(1f), "после отмены не срабатывает");
            });
        }

        private void NewPressAfterFire()
        {
            Check("жест: новое нажатие сбрасывает прошлое срабатывание", () =>
            {
                var gesture = new LongPressGesture(0.45f);
                gesture.Begin();
                gesture.Advance(0.45f);
                True(gesture.Fired, "первое срабатывание");

                gesture.Begin();
                False(gesture.Fired, "после нового нажатия флаг снят");
                False(gesture.Advance(0.1f), "время считается заново");
                True(gesture.Advance(0.35f), "второе срабатывание");
            });
        }

        private void NegativeTimeIsIgnored()
        {
            Check("жест: отрицательное время не ускоряет", () =>
            {
                var gesture = new LongPressGesture(0.45f);
                gesture.Begin();

                False(gesture.Advance(-5f), "отрицательное время");
                False(gesture.Advance(0.2f), "обычный шаг");
            });
        }

        private void ZeroThresholdFiresImmediately()
        {
            Check("жест: нулевой порог срабатывает на первом шаге", () =>
            {
                var gesture = new LongPressGesture(0f);
                gesture.Begin();

                True(gesture.Advance(0f), "сразу");
            });
        }

        private void NegativeThresholdBecomesZero()
        {
            Check("жест: отрицательный порог считается нулём", () =>
            {
                var gesture = new LongPressGesture(-1f);

                Equal(0f, gesture.Threshold, "порог");
            });
        }
    }
}
