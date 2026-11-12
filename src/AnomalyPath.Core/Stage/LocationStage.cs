#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Stage
{
    /// <summary>
    /// Сцена локации: точки земли и общий тон, под который подгоняются фигуры. Арт объекта
    /// остаётся один на сущность, а место, размер и цвет берутся из сцены — поэтому новая
    /// картинка не нужна ни на каждый исход, ни на каждую пару «локация × объект».
    /// </summary>
    public sealed class LocationStage
    {
        /// <summary>Создаёт сцену локации.</summary>
        /// <param name="anchors">Точки земли; ни одной быть не может, слоты не повторяются.</param>
        /// <param name="tint">Цвет подгонки в формате <c>#rrggbb</c>; <c>null</c> — без подгонки.</param>
        /// <param name="brightness">Множитель яркости фигур, больше нуля.</param>
        /// <param name="stashHeight">Высота тайника в метрах; <c>null</c> — тайник без высоты сцены.</param>
        /// <exception cref="ArgumentException">Точек нет, слот повторяется или цвет не в формате <c>#rrggbb</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Яркость или высота тайника не положительны.</exception>
        /// <exception cref="ArgumentNullException">Список точек не задан.</exception>
        public LocationStage(
            IReadOnlyList<StageAnchor> anchors,
            string? tint = null,
            double brightness = 1,
            double? stashHeight = null)
        {
            if (anchors == null)
            {
                throw new ArgumentNullException(nameof(anchors));
            }

            if (anchors.Count == 0)
            {
                throw new ArgumentException(CoreText.Get(CoreErrors.StageAnchorsEmpty), nameof(anchors));
            }

            if (brightness <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(brightness));
            }

            if (stashHeight is <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stashHeight));
            }

            if (tint != null && !IsHexColor(tint))
            {
                throw new ArgumentException(CoreText.Format(CoreErrors.StageColor, tint), nameof(tint));
            }

            var slots = new HashSet<StageSlot>();
            foreach (var anchor in anchors)
            {
                if (!slots.Add(anchor.Slot))
                {
                    throw new ArgumentException(
                        CoreText.Format(CoreErrors.StageSlotDuplicate, SchemaEnum.ToCode(anchor.Slot)),
                        nameof(anchors));
                }
            }

            Anchors = anchors;
            Tint = tint;
            Brightness = brightness;
            StashHeight = stashHeight;
        }

        /// <summary>Точки земли сцены.</summary>
        public IReadOnlyList<StageAnchor> Anchors { get; }

        /// <summary>Цвет подгонки фигур в формате <c>#rrggbb</c>; <c>null</c> — без подгонки.</summary>
        public string? Tint { get; }

        /// <summary>Множитель яркости фигур.</summary>
        public double Brightness { get; }

        /// <summary>Высота тайника в метрах; <c>null</c> — тайник без высоты сцены.</summary>
        public double? StashHeight { get; }

        /// <summary>Точка земли слота.</summary>
        /// <param name="slot">Слот сцены.</param>
        /// <returns>Точка или <c>null</c>, если слот не размечен.</returns>
        public StageAnchor? Anchor(StageSlot slot)
        {
            for (var index = 0; index < Anchors.Count; index++)
            {
                if (Anchors[index].Slot == slot)
                {
                    return Anchors[index];
                }
            }

            return null;
        }

        /// <summary>Читает сцену локации из блока <c>stage</c>.</summary>
        /// <param name="value">Объект блока.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Сцена локации.</returns>
        /// <exception cref="DataException">Блок пуст, точек нет, слот повторяется или значение вне границ.</exception>
        public static LocationStage FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            if (!value.TryGet("anchors", out var anchorsValue) || !anchorsValue.IsArray)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "anchors");
            }

            var anchors = new List<StageAnchor>(anchorsValue.Items.Count);
            foreach (var item in anchorsValue.Items)
            {
                anchors.Add(StageAnchor.FromJson(item, sourcePath));
            }

            if (anchors.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.StageAnchorsEmpty);
            }

            var tint = DataMap.OptionalString(value, "tint");
            if (tint != null && !IsHexColor(tint))
            {
                throw new DataException(sourcePath, CoreErrors.StageColor, tint);
            }

            var brightness = DataMap.OptionalDouble(value, "brightness") ?? 1;
            if (brightness <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.StageRange, "brightness");
            }

            var stashHeight = DataMap.OptionalDouble(value, "stashHeight");
            if (stashHeight is <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.StageRange, "stashHeight");
            }

            try
            {
                return new LocationStage(anchors, tint, brightness, stashHeight);
            }
            catch (ArgumentException error)
            {
                throw new DataException(sourcePath, CoreErrors.StageSlotDuplicate, error.Message);
            }
        }

        private static bool IsHexColor(string value)
        {
            if (value.Length != 7 || value[0] != '#')
            {
                return false;
            }

            for (var index = 1; index < value.Length; index++)
            {
                var symbol = value[index];
                var isDigit = symbol >= '0' && symbol <= '9';
                var isUpper = symbol >= 'A' && symbol <= 'F';
                var isLower = symbol >= 'a' && symbol <= 'f';
                if (!isDigit && !isUpper && !isLower)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
