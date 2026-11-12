#nullable enable
using AnomalyPath.Core.World;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>
    /// Небо режима похода: полоса неба сверху, солнце или луна на дуге внутри неё
    /// и слабый общий оттенок сцены. Внутри помещения полосы и светил нет.
    /// </summary>
    public sealed class WalkSkyView : MonoBehaviour
    {
        [Tooltip("Полоса неба: дневные облака")]
        [SerializeField]
        private Image? skyDay = null;

        [Tooltip("Полоса неба: ночные облака, проявляется к ночи")]
        [SerializeField]
        private Image? skyNight = null;

        [Tooltip("Солнце")]
        [SerializeField]
        private Image? sun = null;

        [Tooltip("Луна")]
        [SerializeField]
        private Image? moon = null;

        [Tooltip("Оттенок сцены на весь экран")]
        [SerializeField]
        private Image? tint = null;

        /// <summary>Дневная полоса неба.</summary>
        public Image? SkyDay => skyDay;

        /// <summary>Ночная полоса неба.</summary>
        public Image? SkyNight => skyNight;

        /// <summary>Солнце.</summary>
        public Image? Sun => sun;

        /// <summary>Луна.</summary>
        public Image? Moon => moon;

        /// <summary>Оттенок сцены.</summary>
        public Image? Tint => tint;

        /// <summary>Последнее показанное состояние неба.</summary>
        public SkyState State { get; private set; }

        /// <summary>Доля ночных облаков, 0..1.</summary>
        public float NightSky { get; private set; }

        /// <summary>Ставит светило по часам в полосу неба и окрашивает сцену.</summary>
        /// <param name="state">Состояние неба из кривой часов.</param>
        /// <param name="indoor">Локация внутри помещения: полосы и светил нет, оттенок слабее.</param>
        public void Show(SkyState state, bool indoor)
        {
            State = state;
            NightSky = Mathf.Clamp01((state.TintAlpha - 0.05f) / 0.15f);
            if (skyDay != null)
            {
                var light = state.Light;
                skyDay!.color = new Color(light.R, light.G, light.B, 1f);
                skyDay.gameObject.SetActive(!indoor);
            }

            if (skyNight != null)
            {
                var light = state.Light;
                skyNight!.color = new Color(light.R * 0.85f, light.G * 0.95f, light.B, NightSky);
                skyNight.gameObject.SetActive(!indoor && NightSky > 0.01f);
            }

            if (sun != null)
            {
                sun!.gameObject.SetActive(!indoor && !state.Night);
            }

            if (moon != null)
            {
                moon!.gameObject.SetActive(!indoor && state.Night);
            }

            if (tint != null)
            {
                var tintAlpha = indoor ? state.TintAlpha * WalkMetrics.IndoorTintFactor : state.TintAlpha;
                tint!.color = new Color(state.Tint.R, state.Tint.G, state.Tint.B, tintAlpha);
                tint.gameObject.SetActive(tintAlpha > 0.001f);
            }

            if (indoor)
            {
                return;
            }

            var luminary = state.Night ? moon : sun;
            if (luminary == null)
            {
                return;
            }

            var color = state.Light;
            var lightAlpha = state.Night ? state.Glow : state.Glow * 0.72f;
            luminary.color = new Color(color.R, color.G, color.B, lightAlpha);
            var point = new Vector2(
                Mathf.Lerp(WalkMetrics.SkySunLeft, WalkMetrics.SkySunRight, state.Azimuth),
                (1f - WalkMetrics.SkyBandHeight) +
                (WalkMetrics.SkyBandHeight * Mathf.Lerp(WalkMetrics.SkySunLow, WalkMetrics.SkySunHigh, state.Elevation)));
            luminary.rectTransform.anchorMin = point;
            luminary.rectTransform.anchorMax = point;
            luminary.rectTransform.anchoredPosition = Vector2.zero;
            luminary.rectTransform.localScale = Vector3.one;
        }
    }
}
