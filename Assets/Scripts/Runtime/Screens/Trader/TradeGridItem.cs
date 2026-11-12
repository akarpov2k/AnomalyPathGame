#nullable enable
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Runtime.Screens.Trader
{
    /// <summary>
    /// Предмет в сетке окна торговли: габариты, подпись, иконка, количество и редкость.
    /// Для инвентаря игрока позиция приходит из контейнера, товары торговца раскладываются подряд.
    /// Экземпляр нужен подсказке при наведении; у товара торговца его нет и показывается шаблон.
    /// </summary>
    public sealed class TradeGridItem
    {
        /// <summary>Создаёт элемент сетки.</summary>
        /// <param name="key">Ключ: id экземпляра игрока или id товара торговца.</param>
        /// <param name="itemId">Идентификатор предмета в данных.</param>
        /// <param name="width">Ширина в клетках.</param>
        /// <param name="height">Высота в клетках.</param>
        /// <param name="label">Подпись предмета.</param>
        /// <param name="icon">Id иконки в каталоге спрайтов или <c>null</c>.</param>
        /// <param name="count">Количество: остаток у торговца или размер стопки у игрока.</param>
        /// <param name="x">Колонка в контейнере игрока.</param>
        /// <param name="y">Строка в контейнере игрока.</param>
        /// <param name="rarity">Редкость для окантовки клетки; <c>null</c> — рамки нет.</param>
        /// <param name="instance">Экземпляр предмета; <c>null</c> — товар торговца без экземпляра.</param>
        /// <param name="template">Шаблон предмета; <c>null</c> — не найден в данных.</param>
        public TradeGridItem(
            string key,
            string itemId,
            int width,
            int height,
            string label,
            string? icon,
            int count,
            int x = 0,
            int y = 0,
            Rarity? rarity = null,
            ItemInstance? instance = null,
            ItemTemplate? template = null)
        {
            Key = key;
            ItemId = itemId;
            Width = width < 1 ? 1 : width;
            Height = height < 1 ? 1 : height;
            Label = label;
            Icon = icon;
            Count = count;
            X = x < 0 ? 0 : x;
            Y = y < 0 ? 0 : y;
            Rarity = rarity;
            Instance = instance;
            Template = template;
        }

        /// <summary>Ключ элемента: id экземпляра или id товара.</summary>
        public string Key { get; }

        /// <summary>Идентификатор предмета в данных.</summary>
        public string ItemId { get; }

        /// <summary>Ширина в клетках.</summary>
        public int Width { get; }

        /// <summary>Высота в клетках.</summary>
        public int Height { get; }

        /// <summary>Подпись предмета.</summary>
        public string Label { get; }

        /// <summary>Id иконки в каталоге спрайтов или <c>null</c>.</summary>
        public string? Icon { get; }

        /// <summary>Количество: остаток товара или размер стопки.</summary>
        public int Count { get; }

        /// <summary>Колонка в контейнере игрока.</summary>
        public int X { get; }

        /// <summary>Строка в контейнере игрока.</summary>
        public int Y { get; }

        /// <summary>Редкость для окантовки клетки; <c>null</c> — рамки нет.</summary>
        public Rarity? Rarity { get; }

        /// <summary>Экземпляр предмета; <c>null</c> — товар торговца без экземпляра.</summary>
        public ItemInstance? Instance { get; }

        /// <summary>Шаблон предмета; <c>null</c> — не найден в данных.</summary>
        public ItemTemplate? Template { get; }
    }
}
