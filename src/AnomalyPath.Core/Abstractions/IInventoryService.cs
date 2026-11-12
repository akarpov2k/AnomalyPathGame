#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Inventory;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>Операции с контейнерами: добавление, стаки, автоподбор, перемещение, удаление.</summary>
    public interface IInventoryService
    {
        /// <summary>
        /// Добавляет предметы в контейнер: стакает, затем ищет места, остаток — в переполнение.
        /// Если суммарный вес превысит <see cref="GridContainer.WeightLimit"/>, ничего не добавляется.
        /// </summary>
        /// <param name="container">Целевой контейнер.</param>
        /// <param name="itemId">Id шаблона предмета.</param>
        /// <param name="count">Сколько добавить.</param>
        /// <param name="overflow">Экземпляры, которым не хватило места, или не добавленная стопка.</param>
        /// <returns><c>true</c>, если предметы приняты; <c>false</c> — не хватило веса.</returns>
        bool TryAdd(GridContainer container, string itemId, int count, out IReadOnlyList<ItemInstance> overflow);

        /// <summary>Добавляет предметы с заданной прочностью и вариантом.</summary>
        /// <param name="container">Целевой контейнер.</param>
        /// <param name="itemId">Id шаблона предмета.</param>
        /// <param name="count">Сколько добавить.</param>
        /// <param name="durability">Прочность; <c>null</c> — взять максимум из шаблона.</param>
        /// <param name="variantKey">Ключ варианта (редкость, скиллы).</param>
        /// <param name="overflow">Экземпляры, которым не хватило места, или не добавленная стопка.</param>
        /// <returns><c>true</c>, если предметы приняты; <c>false</c> — не хватило веса.</returns>
        bool TryAdd(
            GridContainer container,
            string itemId,
            int count,
            int? durability,
            string? variantKey,
            out IReadOnlyList<ItemInstance> overflow);

        /// <summary>Добавляет предметы с заданной прочностью, вариантом и числом патронов в магазине.</summary>
        /// <param name="container">Целевой контейнер.</param>
        /// <param name="itemId">Id шаблона предмета.</param>
        /// <param name="count">Сколько добавить.</param>
        /// <param name="durability">Прочность; <c>null</c> — взять максимум из шаблона.</param>
        /// <param name="variantKey">Ключ варианта (редкость, скиллы).</param>
        /// <param name="magazine">Патроны в магазине; у оружия без значения — пустой магазин.</param>
        /// <param name="overflow">Экземпляры, которым не хватило места, или не добавленная стопка.</param>
        /// <param name="ammoItemId">Id патрона в магазине; <c>null</c> — магазин пуст.</param>
        /// <returns><c>true</c>, если предметы приняты; <c>false</c> — не хватило веса.</returns>
        bool TryAdd(
            GridContainer container,
            string itemId,
            int count,
            int? durability,
            string? variantKey,
            int? magazine,
            out IReadOnlyList<ItemInstance> overflow,
            string? ammoItemId = null);

        /// <summary>Суммарный вес содержимого контейнера, включая переполнение, в килограммах.</summary>
        /// <param name="container">Контейнер.</param>
        /// <returns>Сумма весов всех единиц предметов.</returns>
        double TotalWeight(GridContainer container);

        /// <summary>Проверяет, поместятся ли все экземпляры по клеткам и по весу, не меняя контейнер.</summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="items">Экземпляры для проверки.</param>
        /// <returns><c>true</c>, если места и веса хватает всем.</returns>
        bool CanFit(GridContainer container, IReadOnlyList<ItemInstance> items);

        /// <summary>Убирает экземпляр из контейнера.</summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <returns><c>true</c>, если экземпляр был удалён.</returns>
        bool Remove(GridContainer container, string instanceId);

        /// <summary>Перемещает экземпляр в свободную позицию.</summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="instanceId">Id экземпляра.</param>
        /// <param name="x">Колонка.</param>
        /// <param name="y">Строка.</param>
        /// <param name="rotated">Поворот.</param>
        /// <returns><c>true</c>, если перемещение удалось.</returns>
        bool TryMove(GridContainer container, string instanceId, int x, int y, bool rotated);

        /// <summary>Суммарное количество предметов по id, включая стопки и переполнение.</summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="itemId">Id шаблона предмета.</param>
        /// <returns>Суммарное количество.</returns>
        int Count(GridContainer container, string itemId);

        /// <summary>Берёт патроны указанного калибра, начиная с самых маленьких стопок.</summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="caliber">Калибр.</param>
        /// <param name="count">Сколько патронов нужно.</param>
        /// <returns>Сколько удалось взять.</returns>
        int TakeAmmo(GridContainer container, string caliber, int count);

        /// <summary>
        /// Берёт патроны одного вида: указанного или того, чья стопка меньше.
        /// Один заряд магазина всегда одного вида, поэтому смешивать патроны нельзя.
        /// </summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="caliber">Калибр.</param>
        /// <param name="count">Сколько патронов нужно.</param>
        /// <param name="ammoItemId">Нужный вид патрона; <c>null</c> — выбрать самый маленький стек.</param>
        /// <param name="takenItemId">Вид взятых патронов или <c>null</c>, если ничего не взято.</param>
        /// <returns>Сколько удалось взять.</returns>
        int TakeAmmo(GridContainer container, string caliber, int count, string? ammoItemId, out string? takenItemId);

        /// <summary>Берёт патроны из конкретной стопки, не трогая остальные.</summary>
        /// <param name="container">Контейнер.</param>
        /// <param name="instanceId">Id стопки патронов.</param>
        /// <param name="count">Сколько патронов нужно.</param>
        /// <returns>Сколько удалось взять.</returns>
        int TakeAmmoFrom(GridContainer container, string instanceId, int count);
    }
}
