#nullable enable
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Фигура персонажа: иконка надетого костюма в полный рост. Спрайт берётся из каталога
    /// по id <c>costumes/&lt;id предмета брони&gt;</c>; если иконки нет, остаётся запасная плашка.
    /// </summary>
    public static class CharacterFigure
    {
        /// <summary>Папка каталога спрайтов с полноростовыми иконками костюмов.</summary>
        public const string CostumeFolder = "costumes/";

        /// <summary>Id нейтрального силуэта: показывается, когда костюма нет.</summary>
        public const string SilhouetteId = CostumeFolder + "silhouette";

        /// <summary>Id силуэта в профиль для боевого экрана.</summary>
        public const string CombatSilhouetteId = CostumeFolder + "combat_silhouette";

        /// <summary>Id полноростовой иконки костюма в каталоге спрайтов.</summary>
        /// <param name="itemId">Id предмета брони.</param>
        /// <returns>Id спрайта для <see cref="ISpriteCatalog"/>.</returns>
        public static string CostumeId(string itemId) => CostumeFolder + itemId;

        /// <summary>Id костюма в профиль для боевого экрана.</summary>
        /// <param name="itemId">Id предмета брони.</param>
        /// <returns>Id спрайта боевой фигуры.</returns>
        public static string CombatCostumeId(string itemId) => CostumeFolder + "combat_" + itemId;

        /// <summary>Надетая броня игрока или <c>null</c>.</summary>
        /// <param name="player">Состояние игрока.</param>
        /// <returns>Экземпляр брони.</returns>
        public static ItemInstance? WornArmor(PlayerState player) => player.Equipment.Get(EquipmentSlot.Armor);

        /// <summary>Ставит иконку надетой брони; без спрайта оставляет запасной цвет.</summary>
        /// <param name="host">Хост интерфейса.</param>
        /// <param name="target">Изображение фигуры.</param>
        /// <param name="armor">Надетая броня или <c>null</c>.</param>
        /// <param name="fallback">Запасной цвет плашки.</param>
        /// <returns><c>true</c>, если спрайт костюма найден и показан.</returns>
        public static bool Apply(IUiHost host, Image target, ItemInstance? armor, Color fallback)
        {
            var template = armor == null ? null : CharacterWidgets.Template(host, armor);
            if (template != null && host.Sprites.TryGet(CostumeId(template.Id), out var sprite) && sprite != null)
            {
                target.sprite = sprite;
                target.color = Color.white;
                target.preserveAspect = true;
                return true;
            }

            target.sprite = null;
            target.color = fallback;
            target.preserveAspect = false;
            return false;
        }
    }
}
