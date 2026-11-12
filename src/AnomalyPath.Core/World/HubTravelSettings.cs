#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.World
{
    /// <summary>Правила переходов между хабами: цена пешком и с проводником.</summary>
    public sealed class HubTravelSettings
    {
        /// <summary>Создаёт настройку переходов между хабами.</summary>
        /// <param name="onFoot">Пеший переход: часы, выносливость и шанс встречи.</param>
        /// <param name="guide">Переход с проводником: часы и деньги.</param>
        /// <exception cref="ArgumentNullException">Часть настройки не задана.</exception>
        public HubTravelSettings(OnFootTravelSettings onFoot, GuideTravelSettings guide)
        {
            OnFoot = onFoot ?? throw new ArgumentNullException(nameof(onFoot));
            Guide = guide ?? throw new ArgumentNullException(nameof(guide));
        }

        /// <summary>Пеший переход.</summary>
        public OnFootTravelSettings OnFoot { get; }

        /// <summary>Переход с проводником.</summary>
        public GuideTravelSettings Guide { get; }

        /// <summary>Читает блок <c>travel</c> топологии.</summary>
        /// <param name="value">Блок настроек.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Правила переходов между хабами.</returns>
        /// <exception cref="DataException">Блок отсутствует или задан неверно.</exception>
        public static HubTravelSettings FromJson(JsonValue value, string sourcePath)
        {
            if (!value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "travel");
            }

            if (!value.TryGet("onFoot", out var onFoot) || !onFoot.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "travel.onFoot");
            }

            if (!value.TryGet("guide", out var guide) || !guide.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "travel.guide");
            }

            return new HubTravelSettings(
                OnFootTravelSettings.FromJson(onFoot, sourcePath),
                GuideTravelSettings.FromJson(guide, sourcePath));
        }
    }
}
