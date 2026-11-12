#nullable enable
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Rules
{
    /// <summary>Кривая неба по часам: толкования ключевых кадров для солнца и луны.</summary>
    public static class SkyCurve
    {
        /// <summary>Считает состояние неба в текущий момент часов мира.</summary>
        /// <param name="clock">Часы мира с началом дня и ночи.</param>
        /// <param name="settings">Ключевые кадры неба.</param>
        /// <returns>Положение светила и цвета этого часа.</returns>
        /// <exception cref="System.ArgumentNullException">Часы или кривая не заданы.</exception>
        public static SkyState Evaluate(WorldClock clock, SkySettings settings)
        {
            if (clock == null)
            {
                throw new System.ArgumentNullException(nameof(clock));
            }

            if (settings == null)
            {
                throw new System.ArgumentNullException(nameof(settings));
            }

            var night = clock.Phase == TimePhase.Night;
            var tone = settings.ToneAt(clock.Hours);
            var start = night ? clock.NightStartHour : clock.DayStartHour;
            var end = night ? clock.DayStartHour + 24 : clock.NightStartHour;
            var hour = clock.Hours < start && night ? clock.Hours + 24 : clock.Hours;
            var span = end - start;
            var travel = span <= 0 ? 0 : (hour - start) / span;
            travel = System.Math.Clamp(travel, 0, 1);
            var elevation = (float)System.Math.Sin(System.Math.PI * travel);
            return new SkyState(
                night,
                elevation,
                (float)travel,
                tone.Light,
                tone.Tint,
                tone.TintAlpha,
                tone.Glow);
        }
    }
}
