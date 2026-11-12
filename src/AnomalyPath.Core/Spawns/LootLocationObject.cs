#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Spawns
{
    /// <summary>
    /// Объект лута: при осмотре показывает лут, выпавший при спавне. Пока в объекте
    /// остаются предметы, его можно осматривать повторно; забранный лут в объект не возвращается.
    /// </summary>
    public sealed class LootLocationObject : LocationObject
    {
        private readonly List<ItemInstance> _items = new List<ItemInstance>();

        /// <summary>Создаёт объект лута.</summary>
        /// <param name="spawnId">Id записи спавна.</param>
        /// <param name="titleKey">Ключ названия в каталоге строк.</param>
        /// <param name="lootTableId">Id таблицы лута.</param>
        /// <exception cref="ArgumentException">Id таблицы пуст.</exception>
        public LootLocationObject(string spawnId, string titleKey, string lootTableId)
            : base(spawnId, titleKey, LocationObjectKind.Loot)
        {
            if (string.IsNullOrWhiteSpace(lootTableId))
            {
                throw new ArgumentException(
                    CoreText.Format(CoreErrors.RequiredField, nameof(lootTableId)),
                    nameof(lootTableId));
            }

            LootTableId = lootTableId;
        }

        /// <summary>Id таблицы лута.</summary>
        public string LootTableId { get; }

        /// <summary>Лут, выпавший объекту при спавне; изменяется по мере забора предметов.</summary>
        public IReadOnlyList<ItemInstance> Loot => _items;

        /// <summary>Остался ли в объекте лут.</summary>
        public bool HasLoot => _items.Count > 0;

        /// <summary>Заполняет объект лутом, выпавшим при спавне; повторно не вызывается.</summary>
        /// <param name="items">Выпавшие экземпляры; <c>null</c> — пусто.</param>
        public void SetLoot(IReadOnlyList<ItemInstance>? items)
        {
            _items.Clear();
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                _items.Add(item);
            }
        }

        /// <summary>Забирает предмет из объекта по id экземпляра.</summary>
        /// <param name="instanceId">Id экземпляра предмета.</param>
        /// <returns><c>true</c>, если предмет был в объекте и забран.</returns>
        public bool Take(string instanceId)
        {
            if (string.IsNullOrWhiteSpace(instanceId))
            {
                return false;
            }

            for (var index = 0; index < _items.Count; index++)
            {
                if (!string.Equals(_items[index].InstanceId, instanceId, StringComparison.Ordinal))
                {
                    continue;
                }

                _items.RemoveAt(index);
                if (_items.Count == 0)
                {
                    MarkLooted();
                }

                return true;
            }

            return false;
        }
    }
}
