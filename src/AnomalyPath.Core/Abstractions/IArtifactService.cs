#nullable enable
using AnomalyPath.Core.Artifacts;
using AnomalyPath.Core.Inventory;
using EquipmentState = AnomalyPath.Core.Inventory.Equipment;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Артефакты: раскатка редкости найденного экземпляра и сборка модификаторов надетых
    /// артефактов. Базовые числа берутся из шаблона, редкость их множит по данным.
    /// </summary>
    public interface IArtifactService
    {
        /// <summary>Проверяет, является ли предмет артефактом.</summary>
        /// <param name="itemId">Id шаблона предмета.</param>
        /// <returns><c>true</c>, если шаблон существует и его категория — артефакт.</returns>
        bool IsArtifact(string itemId);

        /// <summary>
        /// Бросает вариант найденного артефакта: ступень редкости и номера доп. свойств.
        /// Для не-артефакта и неизвестного id возвращает <c>null</c>.
        /// </summary>
        /// <param name="itemId">Id шаблона предмета.</param>
        /// <param name="seed">Детерминированный seed броска.</param>
        /// <returns>Код варианта для экземпляра или <c>null</c>.</returns>
        string? RollVariantKey(string itemId, int seed);

        /// <summary>Собирает модификаторы одного экземпляра с учётом его редкости и доп. свойств.</summary>
        /// <param name="artifact">Экземпляр артефакта.</param>
        /// <returns>Модификаторы экземпляра; без варианта действуют базовые числа данных.</returns>
        /// <exception cref="System.ArgumentNullException">Экземпляр не задан.</exception>
        ArtifactModifiers GetModifiers(ItemInstance artifact);

        /// <summary>Собирает модификаторы всех надетых артефактов, кроме сломанных.</summary>
        /// <param name="equipment">Экипировка персонажа.</param>
        /// <returns>Суммарные модификаторы с убывающей отдачей повторяющихся свойств.</returns>
        /// <exception cref="System.ArgumentNullException">Экипировка не задана.</exception>
        ArtifactModifiers GetEquippedModifiers(EquipmentState equipment);
    }
}
