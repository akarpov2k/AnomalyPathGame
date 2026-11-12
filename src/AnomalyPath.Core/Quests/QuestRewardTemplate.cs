#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Quests
{
    /// <summary>Награда за квест: деньги, предметы и открытия.</summary>
    public sealed class QuestRewardTemplate
    {
        /// <summary>Пустая награда.</summary>
        public static readonly QuestRewardTemplate Empty = new QuestRewardTemplate(
            0,
            Array.Empty<QuestItemTarget>(),
            Array.Empty<string>());

        /// <summary>Создаёт награду.</summary>
        /// <param name="money">Деньги.</param>
        /// <param name="items">Предметы.</param>
        /// <param name="unlocks">Открытия.</param>
        public QuestRewardTemplate(
            int money,
            IReadOnlyList<QuestItemTarget> items,
            IReadOnlyList<string> unlocks)
        {
            Money = money;
            Items = items ?? Array.Empty<QuestItemTarget>();
            Unlocks = unlocks ?? Array.Empty<string>();
        }

        /// <summary>Деньги.</summary>
        public int Money { get; }

        /// <summary>Предметы.</summary>
        public IReadOnlyList<QuestItemTarget> Items { get; }

        /// <summary>Открытия.</summary>
        public IReadOnlyList<string> Unlocks { get; }

        /// <summary>Читает награду из необязательного блока <c>rewards</c>.</summary>
        /// <param name="value">Объект, в котором ищется поле <c>rewards</c>.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Награда; пустая, если поля нет или оно <c>null</c>.</returns>
        /// <exception cref="DataException">Блок задан неверно.</exception>
        public static QuestRewardTemplate Read(JsonValue value, string sourcePath)
        {
            if (!value.TryGet("rewards", out var rewards) || rewards.Kind == JsonKind.Null)
            {
                return Empty;
            }

            if (!rewards.IsObject)
            {
                throw new DataException(sourcePath, CoreErrors.JsonNotObject);
            }

            var money = DataMap.OptionalInt(rewards, "money", 0);
            if (money < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "money");
            }

            var items = ReadItems(rewards, "items", sourcePath);
            var unlocks = DataMap.OptionalStringArray(rewards, "unlocks");
            return new QuestRewardTemplate(money, items, unlocks);
        }

        /// <summary>Читает список предметов из поля-массива.</summary>
        /// <param name="value">Объект, в котором ищется поле.</param>
        /// <param name="field">Имя поля.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Предметы; пусто, если поля нет.</returns>
        /// <exception cref="DataException">Элемент задан неверно.</exception>
        public static IReadOnlyList<QuestItemTarget> ReadItems(
            JsonValue value,
            string field,
            string sourcePath)
        {
            var result = new List<QuestItemTarget>();
            if (!value.TryGet(field, out var items) || !items.IsArray)
            {
                return result;
            }

            foreach (var item in items.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.JsonNotObject);
                }

                var itemId = DataMap.RequireString(item, "itemId", sourcePath);
                var count = DataMap.RequireInt(item, "count", sourcePath);
                if (count < 1)
                {
                    throw new DataException(sourcePath, CoreErrors.MinimumOne, "count");
                }

                result.Add(new QuestItemTarget(itemId, count));
            }

            return result;
        }
    }
}
