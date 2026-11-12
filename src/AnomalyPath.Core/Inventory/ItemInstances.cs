#nullable enable
using System;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Создание новых экземпляров предметов из шаблонов: прочность новой вещи и пробный
    /// экземпляр для проверки места в сетке. Один код на все «новые» предметы.
    /// </summary>
    public static class ItemInstances
    {
        /// <summary>
        /// Прочность новой вещи из шаблона: у оружия — максимум, у брони — её прочность,
        /// у остальных категорий прочности нет.
        /// </summary>
        /// <param name="template">Шаблон предмета.</param>
        /// <returns>Прочность или <c>null</c>, если предмет не изнашивается.</returns>
        /// <exception cref="ArgumentNullException">Шаблон не задан.</exception>
        public static int? NewDurability(ItemTemplate template)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            if (template.Weapon != null)
            {
                return template.Weapon.DurabilityMax;
            }

            return template.ArmorDurabilityMax > 0 ? template.ArmorDurabilityMax : null;
        }

        /// <summary>Создаёт пробный экземпляр с прочностью новой вещи — для проверки места.</summary>
        /// <param name="instanceId">Идентификатор пробного экземпляра.</param>
        /// <param name="template">Шаблон предмета.</param>
        /// <param name="count">Количество в стопке.</param>
        /// <returns>Экземпляр, годный только для проверки вместимости.</returns>
        /// <exception cref="ArgumentNullException">Шаблон не задан.</exception>
        public static ItemInstance CreateProbe(string instanceId, ItemTemplate template, int count)
        {
            if (template == null)
            {
                throw new ArgumentNullException(nameof(template));
            }

            return new ItemInstance(instanceId, template.Id, count, NewDurability(template), null);
        }
    }
}
