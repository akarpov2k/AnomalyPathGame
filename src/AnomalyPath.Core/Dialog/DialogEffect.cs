#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Последствие выбора варианта ответа: квест, предмет или открытие.</summary>
    public sealed class DialogEffect
    {
        /// <summary>Создаёт последствие.</summary>
        /// <param name="type">Тип последствия.</param>
        /// <param name="questAction">Действие с квестом для типа <see cref="DialogEffectType.Quest"/>.</param>
        /// <param name="questId">Идентификатор квеста.</param>
        /// <param name="variantId">Вариант квеста или <c>null</c>.</param>
        /// <param name="itemId">Идентификатор предмета.</param>
        /// <param name="count">Сколько выдать.</param>
        /// <param name="unlockId">Идентификатор открытия.</param>
        public DialogEffect(
            DialogEffectType type,
            DialogQuestAction? questAction = null,
            string? questId = null,
            string? variantId = null,
            string? itemId = null,
            int count = 1,
            string? unlockId = null)
        {
            Type = type;
            QuestAction = questAction;
            QuestId = questId;
            VariantId = variantId;
            ItemId = itemId;
            Count = count;
            UnlockId = unlockId;
        }

        /// <summary>Тип последствия.</summary>
        public DialogEffectType Type { get; }

        /// <summary>Действие с квестом или <c>null</c>.</summary>
        public DialogQuestAction? QuestAction { get; }

        /// <summary>Идентификатор квеста или <c>null</c>.</summary>
        public string? QuestId { get; }

        /// <summary>Вариант квеста или <c>null</c>.</summary>
        public string? VariantId { get; }

        /// <summary>Идентификатор предмета или <c>null</c>.</summary>
        public string? ItemId { get; }

        /// <summary>Сколько предметов выдать.</summary>
        public int Count { get; }

        /// <summary>Идентификатор открытия или <c>null</c>.</summary>
        public string? UnlockId { get; }

        /// <summary>Читает последствие из записи данных.</summary>
        /// <param name="value">Запись последствия.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Последствие.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static DialogEffect FromJson(JsonValue value, string sourcePath)
        {
            var code = DataMap.RequireString(value, "type", sourcePath);
            if (!SchemaEnum.TryParse(code, out DialogEffectType type))
            {
                throw new DataException(sourcePath, CoreErrors.DialogUnknownEffect, code);
            }

            switch (type)
            {
                case DialogEffectType.Quest:
                {
                    var actionCode = DataMap.RequireString(value, "action", sourcePath);
                    if (!DialogQuestActionCodec.TryParse(actionCode, out var action))
                    {
                        throw new DataException(sourcePath, CoreErrors.DialogEffectAction, actionCode);
                    }

                    return new DialogEffect(
                        type,
                        action,
                        DataMap.RequireString(value, "questId", sourcePath),
                        DataMap.OptionalString(value, "variantId"));
                }

                case DialogEffectType.Item:
                {
                    var count = DataMap.OptionalInt(value, "count", 1);
                    if (count < 1)
                    {
                        throw new DataException(sourcePath, CoreErrors.MinimumOne, "count");
                    }

                    return new DialogEffect(
                        type,
                        itemId: DataMap.RequireString(value, "itemId", sourcePath),
                        count: count);
                }

                case DialogEffectType.Unlock:
                    return new DialogEffect(
                        type,
                        unlockId: DataMap.RequireString(value, "unlockId", sourcePath));

                default:
                    throw new DataException(sourcePath, CoreErrors.DialogUnknownEffect, code);
            }
        }
    }
}
