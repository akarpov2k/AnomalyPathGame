#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Вариант ответа в узле диалога: условие, последствия и куда он ведёт.</summary>
    public sealed class DialogOption
    {
        /// <summary>Создаёт вариант ответа.</summary>
        /// <param name="id">Идентификатор варианта.</param>
        /// <param name="textKey">Ключ текста.</param>
        /// <param name="requirements">Условия доступности.</param>
        /// <param name="effects">Последствия выбора.</param>
        /// <param name="nextNodeId">Следующий узел или <c>null</c>.</param>
        /// <param name="end">Закрывает ли вариант диалог.</param>
        /// <param name="hiddenIfUnavailable">Скрывать ли вариант, если условия не выполнены.</param>
        public DialogOption(
            string id,
            string textKey,
            RequirementsTemplate requirements,
            IReadOnlyList<DialogEffect>? effects = null,
            string? nextNodeId = null,
            bool end = false,
            bool hiddenIfUnavailable = false)
        {
            Id = id;
            TextKey = textKey;
            Requirements = requirements ?? RequirementsTemplate.Empty;
            Effects = effects ?? Array.Empty<DialogEffect>();
            NextNodeId = nextNodeId;
            End = end;
            HiddenIfUnavailable = hiddenIfUnavailable;
        }

        /// <summary>Идентификатор варианта.</summary>
        public string Id { get; }

        /// <summary>Ключ текста.</summary>
        public string TextKey { get; }

        /// <summary>Условия доступности.</summary>
        public RequirementsTemplate Requirements { get; }

        /// <summary>Последствия выбора.</summary>
        public IReadOnlyList<DialogEffect> Effects { get; }

        /// <summary>Следующий узел или <c>null</c>.</summary>
        public string? NextNodeId { get; }

        /// <summary>Закрывает ли вариант диалог.</summary>
        public bool End { get; }

        /// <summary>Скрывать ли вариант, если условия не выполнены.</summary>
        public bool HiddenIfUnavailable { get; }

        /// <summary>Читает вариант ответа из записи данных.</summary>
        /// <param name="value">Запись варианта.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Вариант ответа.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static DialogOption FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var textKey = DataMap.RequireString(value, "textKey", sourcePath);
            var requirements = RequirementsTemplate.Read(value, sourcePath);
            var effects = ReadEffects(value, sourcePath);
            var nextNodeId = DataMap.OptionalString(value, "nextNodeId");
            var end = DataMap.OptionalBool(value, "end", false);
            if (end == (nextNodeId != null))
            {
                throw new DataException(sourcePath, CoreErrors.DialogOptionEnd, id);
            }

            return new DialogOption(
                id,
                textKey,
                requirements,
                effects,
                nextNodeId,
                end,
                DataMap.OptionalBool(value, "hiddenIfUnavailable", false));
        }

        private static IReadOnlyList<DialogEffect> ReadEffects(JsonValue value, string sourcePath)
        {
            var result = new List<DialogEffect>();
            if (!value.TryGet("effects", out var effects) || !effects.IsArray)
            {
                return result;
            }

            foreach (var item in effects.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                result.Add(DialogEffect.FromJson(item, sourcePath));
            }

            return result;
        }
    }
}
