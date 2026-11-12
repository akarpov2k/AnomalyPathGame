#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Trade;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Узел диалога: кто говорит, что говорит и какие есть ответы.</summary>
    public sealed class DialogNode
    {
        /// <summary>Создаёт узел диалога.</summary>
        /// <param name="id">Идентификатор узла.</param>
        /// <param name="textKey">Ключ текста.</param>
        /// <param name="speakerActorId">Кто говорит или <c>null</c>.</param>
        /// <param name="requirements">Условия доступности узла.</param>
        /// <param name="options">Варианты ответа; не пусто.</param>
        /// <exception cref="ArgumentException">Идентификатор или текст пуст.</exception>
        /// <exception cref="ArgumentNullException">Варианты не заданы.</exception>
        public DialogNode(
            string id,
            string textKey,
            string? speakerActorId,
            RequirementsTemplate requirements,
            IReadOnlyList<DialogOption> options)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(id)),
                    nameof(id));
            }

            Id = id;
            TextKey = textKey;
            SpeakerActorId = speakerActorId;
            Requirements = requirements ?? RequirementsTemplate.Empty;
            Options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <summary>Идентификатор узла.</summary>
        public string Id { get; }

        /// <summary>Ключ текста.</summary>
        public string TextKey { get; }

        /// <summary>Кто говорит или <c>null</c>.</summary>
        public string? SpeakerActorId { get; }

        /// <summary>Условия доступности узла.</summary>
        public RequirementsTemplate Requirements { get; }

        /// <summary>Варианты ответа.</summary>
        public IReadOnlyList<DialogOption> Options { get; }

        /// <summary>Ищет вариант ответа по идентификатору.</summary>
        /// <param name="optionId">Идентификатор варианта.</param>
        /// <param name="option">Найденный вариант или <c>null</c>.</param>
        /// <returns><c>true</c>, если вариант найден.</returns>
        public bool TryGetOption(string optionId, out DialogOption? option)
        {
            foreach (var item in Options)
            {
                if (string.Equals(item.Id, optionId, StringComparison.Ordinal))
                {
                    option = item;
                    return true;
                }
            }

            option = null;
            return false;
        }

        /// <summary>Читает узел из записи данных.</summary>
        /// <param name="value">Запись узла.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Узел диалога.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static DialogNode FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var textKey = DataMap.RequireString(value, "textKey", sourcePath);
            var requirements = RequirementsTemplate.Read(value, sourcePath);
            var speakerActorId = DataMap.OptionalString(value, "speakerActorId");
            if (!value.TryGet("options", out var options) || !options.IsArray || options.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.DialogNodeDeadEnd, id);
            }

            var result = new List<DialogOption>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in options.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var option = DialogOption.FromJson(item, sourcePath);
                if (!ids.Add(option.Id))
                {
                    throw new DataException(sourcePath, CoreErrors.DialogOptionDuplicate, option.Id);
                }

                result.Add(option);
            }

            return new DialogNode(id, textKey, speakerActorId, requirements, result);
        }
    }
}
