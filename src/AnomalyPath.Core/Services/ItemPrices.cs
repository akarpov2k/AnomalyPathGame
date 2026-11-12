#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Services
{
    /// <summary>
    /// Базовая цена предмета без множителей торговца: для покупки — цена покупки,
    /// для продажи — цена продажи, иначе база. Общая точка для торговли и ремонта.
    /// Редкость экземпляра повышает только цену продажи.
    /// </summary>
    public static class ItemPrices
    {
        /// <summary>Возвращает базовую цену предмета.</summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="isBuy">Цена для покупки игроком; иначе — для продажи.</param>
        /// <param name="price">Цена; 0, если её нет.</param>
        /// <returns><c>true</c>, если у предмета задана цена.</returns>
        public static bool TryGetBasePrice(ItemTemplate? template, bool isBuy, out double price)
        {
            price = 0;
            if (template?.Price == null)
            {
                return false;
            }

            var value = isBuy
                ? template.Price.Buy ?? template.Price.Base
                : template.Price.Sell ?? template.Price.Base;
            if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0)
            {
                return false;
            }

            price = value.Value;
            return true;
        }
        /// <summary>
        /// Стоимость предмета для ремонта: у репутационной цены — база, у фиксированной — цена покупки.
        /// Включённый шаблон может оставить чужую цену покупки, поэтому режим важнее поля.
        /// </summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="price">Стоимость; 0, если её нет.</param>
        /// <returns><c>true</c>, если у предмета задана цена.</returns>
        public static bool TryGetItemValue(ItemTemplate? template, out double price)
        {
            price = 0;
            if (template?.Price == null)
            {
                return false;
            }

            var block = template.Price;
            var value = block.Mode == PriceMode.Reputation
                ? block.Base ?? block.Buy ?? block.Sell
                : block.Buy ?? block.Base ?? block.Sell;
            if (!value.HasValue || double.IsNaN(value.Value) || double.IsInfinity(value.Value) || value.Value < 0)
            {
                return false;
            }

            price = value.Value;
            return true;
        }

        /// <summary>
        /// Множитель цены продажи по редкости экземпляра: <c>1 + sellPriceBonus</c> ступени.
        /// У категорий без редкости и при отсутствии записи ступени множитель 1.
        /// </summary>
        /// <param name="instance">Экземпляр предмета; <c>null</c> — множитель 1.</param>
        /// <param name="template">Шаблон предмета; <c>null</c> — известна только ступень варианта.</param>
        /// <param name="rarities">Редкости: прибавки к цене продажи по ступеням.</param>
        /// <returns>Множитель не меньше 1; 1 — прибавки нет.</returns>
        /// <exception cref="ArgumentNullException">Репозиторий редкостей не задан.</exception>
        public static double RaritySellMultiplier(
            ItemInstance? instance,
            ItemTemplate? template,
            IRarityRepository rarities)
        {
            if (rarities == null)
            {
                throw new ArgumentNullException(nameof(rarities));
            }

            var rarity = ItemRarity.Resolve(instance, template);
            if (rarity == null)
            {
                return 1;
            }

            return rarities.TryGet(RarityIds.Id(rarity.Value), out var rule)
                ? 1 + rule.SellPriceBonus
                : 1;
        }
    }
}
