#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Часы основного экрана из префаба: круглый циферблат с половинами дня и ночи и стрелкой,
    /// которая идёт по текущей фазе суток. Рядом выводится численное время и номер дня.
    /// </summary>
    public sealed class PlayClock
    {
        private GameObject? _root;
        private RectTransform? _hand;
        private Text? _time;
        private IGameTexts? _texts;

        /// <summary>Связывает часы с готовыми объектами префаба.</summary>
        /// <param name="root">Корень часов с детьми ClockFace, ClockHand и ClockTime.</param>
        /// <param name="texts">Каталог строк.</param>
        /// <exception cref="ArgumentNullException">Корень не задан.</exception>
        public PlayClock(GameObject root, IGameTexts texts)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _texts = texts ?? throw new ArgumentNullException(nameof(texts));
            var hand = root.transform.Find("ClockHand");
            _hand = hand == null ? null : hand as RectTransform;
            var time = root.transform.Find("ClockTime");
            _time = time == null ? null : time.GetComponent<Text>();
        }

        /// <summary>Показывает время и ставит стрелку по часам мира.</summary>
        /// <param name="clock">Часы мира.</param>
        public void Show(WorldClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (_root != null)
            {
                _root.SetActive(true);
            }

            if (_hand != null)
            {
                _hand.localEulerAngles = new Vector3(0f, 0f, Angle(clock));
            }

            if (_time != null && _texts != null)
            {
                var hour = Mathf.FloorToInt((float)clock.Hours);
                var minute = Mathf.FloorToInt((float)((clock.Hours - hour) * 60d));
                _time.text = UiText.Format(_texts, UiTextKey.Game_Clock, clock.Day, hour, minute);
            }
        }

        /// <summary>Скрывает часы, пока игра не начата.</summary>
        public void Hide()
        {
            if (_root != null)
            {
                _root.SetActive(false);
            }
        }

        /// <summary>Время мира строкой <c>ЧЧ:ММ</c> для подписей вне циферблата.</summary>
        /// <param name="clock">Часы мира.</param>
        /// <returns>Время в формате <c>10:30</c>.</returns>
        public static string Time(WorldClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            var minutes = (int)Math.Floor(clock.Hours * 60);
            return (minutes / 60).ToString("00", System.Globalization.CultureInfo.InvariantCulture) +
                   ":" +
                   (minutes % 60).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Угол стрелки для часов мира. День занимает правую половину циферблата,
        /// ночь — левую; внутри фазы стрелка идёт пропорционально времени.
        /// </summary>
        /// <param name="clock">Часы мира.</param>
        /// <returns>Угол в градусах для поворота стрелки.</returns>
        /// <exception cref="ArgumentNullException">Часы не переданы.</exception>
        public static float Angle(WorldClock clock)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }

            if (clock.Phase == TimePhase.Day)
            {
                var length = clock.NightStartHour - clock.DayStartHour;
                var progress = length <= 0d ? 0d : (clock.Hours - clock.DayStartHour) / length;
                return Mathf.Repeat(0f - (180f * Mathf.Clamp01((float)progress)), 360f);
            }

            var nightLength = 24d - clock.NightStartHour + clock.DayStartHour;
            var elapsed = clock.Hours >= clock.NightStartHour
                ? clock.Hours - clock.NightStartHour
                : clock.Hours + 24d - clock.NightStartHour;
            var nightProgress = nightLength <= 0d ? 0d : elapsed / nightLength;
            return Mathf.Repeat(-180f - (180f * Mathf.Clamp01((float)nightProgress)), 360f);
        }
    }
}
