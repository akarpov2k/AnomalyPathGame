#nullable enable
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Templates
{
    /// <summary>
    /// Редкость как сущность данных: ступень, ключ названия, поправка к меткости, вес
    /// выпадения у найденного предмета и прибавка к статам оружия.
    /// </summary>
    public sealed class RarityTemplate
    {
        /// <summary>Создаёт редкость.</summary>
        /// <param name="id">Id вида <c>rar_rare</c>.</param>
        /// <param name="rarity">Ступень редкости.</param>
        /// <param name="nameKey">Ключ названия в каталоге строк.</param>
        /// <param name="order">Порядок ступени от 0.</param>
        /// <param name="accuracyBonus">Поправка к меткости актёра этой редкости, не меньше нуля.</param>
        /// <param name="dropWeight">Вес выпадения ступени у найденного предмета, не меньше нуля; 0 — не выпадает.</param>
        /// <param name="weaponStatBonus">Прибавка к урону и точности оружия, не меньше нуля; 0.1 — +10%.</param>
        /// <param name="sellPriceBonus">Прибавка к цене продажи предмета, не меньше нуля; 0.1 — +10%.</param>
        public RarityTemplate(
            string id,
            Rarity rarity,
            string nameKey,
            int order,
            double accuracyBonus,
            double dropWeight = 0,
            double weaponStatBonus = 0,
            double sellPriceBonus = 0)
        {
            Id = id;
            Rarity = rarity;
            NameKey = nameKey;
            Order = order;
            AccuracyBonus = accuracyBonus;
            DropWeight = dropWeight;
            WeaponStatBonus = weaponStatBonus;
            SellPriceBonus = sellPriceBonus;
        }

        /// <summary>Id редкости в данных.</summary>
        public string Id { get; }

        /// <summary>Ступень редкости.</summary>
        public Rarity Rarity { get; }

        /// <summary>Ключ названия в каталоге строк.</summary>
        public string NameKey { get; }

        /// <summary>Порядок ступени: 0 — обычная, дальше по возрастанию.</summary>
        public int Order { get; }

        /// <summary>Поправка к меткости актёра этой редкости.</summary>
        public double AccuracyBonus { get; }

        /// <summary>
        /// Вес выпадения ступени у найденного предмета: чем больше, тем чаще ступень.
        /// 0 — ступень не выпадает. Сумма весов всех ступеней нормирует бросок.
        /// </summary>
        public double DropWeight { get; }

        /// <summary>
        /// Прибавка к урону и точности оружия этой ступени: 0.1 — +10%, 0 — без прибавки.
        /// Множитель статов равен <c>1 + WeaponStatBonus</c>; действует на все оружия со ступенью.
        /// </summary>
        public double WeaponStatBonus { get; }

        /// <summary>
        /// Прибавка к цене продажи предмета этой ступени: 0.1 — +10%, 0 — без прибавки.
        /// Множитель цены продажи равен <c>1 + SellPriceBonus</c>; цена покупки не меняется.
        /// </summary>
        public double SellPriceBonus { get; }

        /// <summary>Читает редкость из записи.</summary>
        /// <param name="value">Загруженная запись.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Редкость.</returns>
        /// <exception cref="DataException">Поля заданы неверно.</exception>
        public static RarityTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            if (!RarityIds.TryParse(id, out var rarity))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownRarity, id);
            }

            var nameKey = DataMap.RequireString(value, "nameKey", sourcePath);
            var order = DataMap.RequireInt(value, "order", sourcePath);
            if (order < 0 || order >= RarityIds.Count)
            {
                throw new DataException(sourcePath, CoreErrors.RarityOrderRange, RarityIds.Count - 1);
            }

            var accuracyBonus = DataMap.OptionalDouble(value, "accuracyBonus") ?? 0;
            if (accuracyBonus < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "accuracyBonus");
            }

            var dropWeight = DataMap.OptionalDouble(value, "dropWeight") ?? 0;
            if (dropWeight < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "dropWeight");
            }

            var weaponStatBonus = DataMap.OptionalDouble(value, "weaponStatBonus") ?? 0;
            if (weaponStatBonus < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "weaponStatBonus");
            }

            var sellPriceBonus = DataMap.OptionalDouble(value, "sellPriceBonus") ?? 0;
            if (sellPriceBonus < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "sellPriceBonus");
            }

            return new RarityTemplate(
                id,
                rarity,
                nameKey,
                order,
                accuracyBonus,
                dropWeight,
                weaponStatBonus,
                sellPriceBonus);
        }
    }
}
