#nullable enable
using System;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Анимированная фигура игрока в режиме похода: покадровая ходьба, бег, покой и шаг назад.
    /// Кадры приходят из префаба, код только переключает спрайты.
    /// </summary>
    public sealed class WalkPlayerView : MonoBehaviour
    {
        [Tooltip("Картинка игрока")]
        [SerializeField]
        private Image? image = null;

        [Tooltip("Кадры ходьбы по порядку")]
        [SerializeField]
        private Sprite[] walkFrames = Array.Empty<Sprite>();

        [Tooltip("Кадры бега по порядку")]
        [SerializeField]
        private Sprite[] runFrames = Array.Empty<Sprite>();

        [Tooltip("Кадр покоя")]
        [SerializeField]
        private Sprite? idleFrame = null;

        [Tooltip("Сколько кадров ходьбы в секунду")]
        [SerializeField]
        private float walkFramesPerSecond = WalkMetrics.WalkFramesPerSecond;

        [Tooltip("Сколько кадров бега в секунду")]
        [SerializeField]
        private float runFramesPerSecond = WalkMetrics.RunFramesPerSecond;

        private float clock;
        private int frame = -1;
        private bool moving;
        private bool running;
        private bool backward;

        /// <summary>Идёт ли игрок сейчас.</summary>
        public bool Moving => moving;

        /// <summary>Бежит ли игрок сейчас.</summary>
        public bool Running => running;

        /// <summary>Идёт ли игрок назад (лицом влево).</summary>
        public bool Backward => backward;

        /// <summary>Номер видимого кадра в своём наборе; -1 — покой.</summary>
        public int FrameIndex => frame;

        /// <summary>Текущий спрайт игрока.</summary>
        public Sprite? Current => image != null ? image.sprite : null;

        /// <summary>Кадры ходьбы из префаба.</summary>
        public Sprite[] WalkFrames => walkFrames;

        /// <summary>Кадры бега из префаба.</summary>
        public Sprite[] RunFrames => runFrames;

        /// <summary>Кадр покоя из префаба.</summary>
        public Sprite? IdleFrame => idleFrame;

        /// <summary>Ставит состояние движения и сразу показывает подходящий кадр.</summary>
        /// <param name="isMoving">Идёт ли игрок.</param>
        /// <param name="isRunning">Бежит ли игрок.</param>
        /// <param name="isBackward">Идёт ли игрок назад.</param>
        public void SetState(bool isMoving, bool isRunning, bool isBackward)
        {
            if (!isMoving)
            {
                Reset();
                return;
            }

            if (moving == isMoving && running == isRunning && backward == isBackward)
            {
                return;
            }

            moving = isMoving;
            running = isRunning;
            backward = isBackward;
            clock = 0f;
            frame = -1;
            ApplyFacing();
            Advance(0f);
        }

        /// <summary>Возвращает игрока в позу покоя.</summary>
        public void Reset()
        {
            moving = false;
            running = false;
            backward = false;
            clock = 0f;
            frame = -1;
            if (image != null && idleFrame != null)
            {
                image.sprite = idleFrame;
            }

            ApplyFacing();
        }

        /// <summary>Продвигает анимацию на заданное время; вызывается из <see cref="Update"/> и тестов.</summary>
        /// <param name="deltaTime">Сколько времени прошло, секунды.</param>
        public void Advance(float deltaTime)
        {
            if (!moving)
            {
                return;
            }

            var frames = running ? runFrames : walkFrames;
            if (frames.Length == 0)
            {
                if (image != null && idleFrame != null)
                {
                    image.sprite = idleFrame;
                }

                return;
            }

            var rate = running ? runFramesPerSecond : walkFramesPerSecond;
            clock += Mathf.Max(0f, deltaTime);
            if (rate <= 0f)
            {
                return;
            }

            var cycle = frames.Length / rate;
            if (clock >= cycle)
            {
                clock %= cycle;
            }

            var index = Mathf.FloorToInt(clock * rate) % frames.Length;
            if (backward)
            {
                index = frames.Length - 1 - index;
            }

            if (index == frame)
            {
                return;
            }

            frame = index;
            if (image != null)
            {
                image.sprite = frames[index];
            }
        }

        private void Update()
        {
            if (moving)
            {
                Advance(Time.deltaTime);
            }
        }

        private void ApplyFacing()
        {
            var scale = transform.localScale;
            scale.x = backward ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
            transform.localScale = scale;
        }
    }
}
