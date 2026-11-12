#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Rules
{
    /// <summary>Кривая неба: ключевые кадры по часам и их плавная выборка.</summary>
    public sealed class SkySettings
    {
        /// <summary>Значения по умолчанию: ночь, предрассветные сумерки, рассвет, полдень, вечер, закат, сумерки.</summary>
        public static SkySettings Default { get; } = new SkySettings(new[]
        {
            new SkyKeyframe(0, new SkyColor(0.72f, 0.82f, 1f), new SkyColor(0.05f, 0.09f, 0.2f), 0.5, 0.9),
            new SkyKeyframe(5, new SkyColor(0.55f, 0.6f, 0.85f), new SkyColor(0.1f, 0.12f, 0.28f), 0.35, 0.7),
            new SkyKeyframe(6.5, new SkyColor(1f, 0.65f, 0.4f), new SkyColor(0.95f, 0.55f, 0.35f), 0, 1),
            new SkyKeyframe(9, new SkyColor(1f, 0.93f, 0.82f), new SkyColor(1f, 0.95f, 0.9f), 0, 1),
            new SkyKeyframe(12, new SkyColor(1f, 0.97f, 0.9f), new SkyColor(1f, 1f, 1f), 0, 1),
            new SkyKeyframe(17.5, new SkyColor(1f, 0.8f, 0.55f), new SkyColor(1f, 0.85f, 0.6f), 0, 1),
            new SkyKeyframe(19.5, new SkyColor(1f, 0.5f, 0.22f), new SkyColor(0.95f, 0.45f, 0.22f), 0, 0.9),
            new SkyKeyframe(21, new SkyColor(0.6f, 0.65f, 0.95f), new SkyColor(0.12f, 0.16f, 0.35f), 0.3, 0.8),
            new SkyKeyframe(24, new SkyColor(0.72f, 0.82f, 1f), new SkyColor(0.05f, 0.09f, 0.2f), 0.5, 0.9),
        });

        /// <summary>Создаёт кривую неба.</summary>
        /// <param name="keyframes">Ключевые кадры, отсортированные по часу, не меньше двух.</param>
        /// <exception cref="ArgumentException">Кадров меньше двух или часы не возрастают.</exception>
        public SkySettings(IReadOnlyList<SkyKeyframe> keyframes)
        {
            if (keyframes == null)
            {
                throw new ArgumentNullException(nameof(keyframes));
            }

            if (keyframes.Count < 2)
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RulesRange, "sky.keyframes"),
                    nameof(keyframes));
            }

            var copy = new SkyKeyframe[keyframes.Count];
            for (var index = 0; index < keyframes.Count; index++)
            {
                copy[index] = keyframes[index];
                if (index > 0 && copy[index].Hour <= copy[index - 1].Hour)
                {
                    throw new ArgumentException(
                        CoreText.Format(CoreErrors.RulesRange, "sky.keyframes"),
                        nameof(keyframes));
                }
            }

            Keyframes = copy;
        }

        /// <summary>Ключевые кадры в порядке часов.</summary>
        public IReadOnlyList<SkyKeyframe> Keyframes { get; }

        /// <summary>Читает блок <c>sky</c> из правил.</summary>
        /// <param name="value">Объект блока <c>sky</c>.</param>
        /// <param name="path">Путь для сообщения об ошибке.</param>
        /// <returns>Кривая неба.</returns>
        /// <exception cref="DataException">Ключевые кадры заданы неверно.</exception>
        public static SkySettings FromJson(JsonValue value, string path)
        {
            if (!value.TryGet("keyframes", out var frames) || !frames.IsArray || frames.Items.Count < 2)
            {
                throw new DataException(path, CoreErrors.RulesRange, "sky.keyframes");
            }

            var list = new List<SkyKeyframe>(frames.Items.Count);
            foreach (var item in frames.Items)
            {
                var hour = DataMap.RequireDouble(item, "hour", path);
                var light = ReadColor(item, "light", path);
                var tint = ReadColor(item, "tint", path);
                var tintAlpha = DataMap.OptionalDouble(item, "tintAlpha") ?? 0;
                var glow = DataMap.OptionalDouble(item, "glow") ?? 1;
                try
                {
                    list.Add(new SkyKeyframe(hour, light, tint, tintAlpha, glow));
                }
                catch (ArgumentOutOfRangeException)
                {
                    throw new DataException(path, CoreErrors.RulesRange, "sky.keyframes");
                }
            }

            try
            {
                return new SkySettings(list);
            }
            catch (ArgumentException)
            {
                throw new DataException(path, CoreErrors.RulesRange, "sky.keyframes");
            }
        }

        /// <summary>Смешивает ключевые кадры для заданного часа, включая переход через полночь.</summary>
        /// <param name="hour">Час суток, 0..24.</param>
        /// <returns>Кадр с интерполированными цветами и силой оттенка.</returns>
        public SkyKeyframe ToneAt(double hour)
        {
            var frames = Keyframes;
            var last = frames.Count - 1;
            if (hour >= frames[last].Hour)
            {
                var span = (frames[0].Hour + 24) - frames[last].Hour;
                return Blend(frames[last], frames[0], hour - frames[last].Hour, span, hour);
            }

            if (hour < frames[0].Hour)
            {
                var from = frames[last].Hour - 24;
                var span = frames[0].Hour - from;
                return Blend(frames[last], frames[0], hour - from, span, hour);
            }

            for (var index = 0; index < last; index++)
            {
                if (hour >= frames[index].Hour && hour <= frames[index + 1].Hour)
                {
                    var span = frames[index + 1].Hour - frames[index].Hour;
                    return Blend(frames[index], frames[index + 1], hour - frames[index].Hour, span, hour);
                }
            }

            return frames[last];
        }

        private static SkyKeyframe Blend(SkyKeyframe from, SkyKeyframe to, double offset, double span, double hour)
        {
            var t = span <= 0 ? 0 : (float)Math.Clamp(offset / span, 0, 1);
            return new SkyKeyframe(
                Math.Clamp(hour, 0, 24),
                SkyColor.Lerp(from.Light, to.Light, t),
                SkyColor.Lerp(from.Tint, to.Tint, t),
                from.TintAlpha + ((to.TintAlpha - from.TintAlpha) * t),
                from.Glow + ((to.Glow - from.Glow) * t));
        }

        private static SkyColor ReadColor(JsonValue item, string field, string path)
        {
            if (!item.TryGet(field, out var value) || !value.IsArray || value.Items.Count != 3)
            {
                throw new DataException(path, CoreErrors.MissingNumberField, "sky." + field);
            }

            var channels = new float[3];
            for (var index = 0; index < 3; index++)
            {
                if (value.Items[index].Kind != JsonKind.Number)
                {
                    throw new DataException(path, CoreErrors.MissingNumberField, "sky." + field);
                }

                channels[index] = (float)value.Items[index].Number;
            }

            try
            {
                return new SkyColor(channels[0], channels[1], channels[2]);
            }
            catch (ArgumentOutOfRangeException)
            {
                throw new DataException(path, CoreErrors.RulesRange, "sky." + field);
            }
        }
    }
}
