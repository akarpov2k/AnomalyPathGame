#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests
{
    /// <summary>
    /// Настройка доски объявлений хаба: слоты по сложности и пул квестов,
    /// из которого слот выбирает предложение.
    /// </summary>
    public sealed class QuestBoardTemplate
    {
        /// <summary>Создаёт настройку доски.</summary>
        /// <param name="slots">Слоты доски; не меньше одного.</param>
        /// <param name="questPool">Идентификаторы квестов, доступных на этой доске.</param>
        /// <exception cref="ArgumentException">Слотов или квестов в пуле нет.</exception>
        public QuestBoardTemplate(
            IReadOnlyList<QuestBoardSlotTemplate> slots,
            IReadOnlyList<string> questPool)
        {
            if (slots == null)
            {
                throw new ArgumentNullException(nameof(slots));
            }

            if (questPool == null)
            {
                throw new ArgumentNullException(nameof(questPool));
            }

            if (slots.Count == 0)
            {
                throw new ArgumentException(nameof(slots));
            }

            if (questPool.Count == 0)
            {
                throw new ArgumentException(nameof(questPool));
            }

            Slots = slots;
            QuestPool = questPool;
        }

        /// <summary>Слоты доски в порядке показа.</summary>
        public IReadOnlyList<QuestBoardSlotTemplate> Slots { get; }

        /// <summary>Пул квестов, привязанный к хабу.</summary>
        public IReadOnlyList<string> QuestPool { get; }

        /// <summary>Читает блок <c>questBoard</c> шаблона хаба.</summary>
        /// <param name="value">Объект блока.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Настройка доски.</returns>
        /// <exception cref="DataException">Поля отсутствуют или заданы неверно.</exception>
        public static QuestBoardTemplate FromJson(JsonValue value, string sourcePath)
        {
            if (value == null || !value.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            if (!value.TryGet("slots", out var slotsValue) || !slotsValue.IsArray)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "slots");
            }

            if (slotsValue.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.QuestBoardEmpty);
            }

            var slots = new List<QuestBoardSlotTemplate>();
            foreach (var item in slotsValue.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var difficultyCode = DataMap.RequireString(item, "difficulty", sourcePath);
                if (!SchemaEnum.TryParse(difficultyCode, out QuestDifficulty difficulty))
                {
                    throw new DataException(sourcePath, CoreErrors.UnknownQuestDifficulty, difficultyCode);
                }

                var refreshHours = DataMap.RequireInt(item, "refreshHours", sourcePath);
                if (refreshHours < 1)
                {
                    throw new DataException(sourcePath, CoreErrors.MinimumOne, "refreshHours");
                }

                slots.Add(new QuestBoardSlotTemplate(difficulty, refreshHours));
            }

            if (!value.TryGet("questPool", out var poolValue) || !poolValue.IsArray)
            {
                throw new DataException(sourcePath, CoreErrors.MissingArrayField, "questPool");
            }

            var questPool = new List<string>();
            foreach (var item in poolValue.Items)
            {
                if (item.Kind != JsonKind.String || string.IsNullOrWhiteSpace(item.Text))
                {
                    throw new DataException(sourcePath, CoreErrors.MissingStringField, "questPool");
                }

                if (questPool.Contains(item.Text))
                {
                    throw new DataException(sourcePath, CoreErrors.QuestBoardPoolDuplicate, item.Text);
                }

                questPool.Add(item.Text);
            }

            if (questPool.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.QuestBoardEmpty);
            }

            return new QuestBoardTemplate(slots, questPool);
        }
    }
}
