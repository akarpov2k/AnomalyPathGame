#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Art;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="CombatScreenScreenshot"/>: профили бойцов и кадр выстрела.</summary>
    public static partial class CombatScreenScreenshot
    {
        /// <summary>Показывает на кадре наведение на строку выпадашки.</summary>
        /// <param name="panel">Собранный экран.</param>
        /// <param name="name">Имя строки в префабе.</param>
        private static void HoverEntry(GameObject panel, string name)
        {
            var target = FindDeep(panel.transform, name);
            var art = target == null ? null : target.GetComponent<ButtonArt>();
            if (art == null)
            {
                return;
            }

            art.OnPointerEnter(new PointerEventData(EventSystem.current));
        }

        /// <summary>Показывает на кадре один выстрел: раскрытую вспышку у ствола.</summary>
        /// <param name="panel">Собранный экран.</param>
        private static void ShowShot(GameObject panel)
        {
            var player = FindDeep(panel.transform, "PlayerFigure");
            if (player == null)
            {
                return;
            }

            var muzzle = player.Find("Muzzle")?.GetComponent<Image>();
            if (muzzle == null)
            {
                return;
            }

            muzzle.enabled = true;
            muzzle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 14f);
            muzzle.rectTransform.localScale = new Vector3(1.15f, 1.15f, 1f);
            muzzle.color = new Color(1f, 1f, 1f, 0.85f);
        }

        /// <summary>
        /// Показывает на кадре всплывающую подпись над фигурой: число урона или «Промах».
        /// Без корутины — кадр статичный, подпись стоит на месте.
        /// </summary>
        /// <param name="panel">Собранный экран.</param>
        /// <param name="figureName">Имя фигуры в префабе.</param>
        /// <param name="text">Текст подписи.</param>
        /// <param name="color">Цвет подписи.</param>
        private static void ShowFloatingLabel(GameObject panel, string figureName, string text, Color color)
        {
            var figure = FindDeep(panel.transform, figureName);
            var floating = figure == null ? null : figure.Find("FloatingText")?.GetComponent<Text>();
            if (floating == null)
            {
                return;
            }

            floating.text = text;
            floating.color = color;
            floating.gameObject.SetActive(true);
        }

        /// <summary>
        /// Дописывает в подсказку первого скилла строку шанса попадания: кадр показывает
        /// ту же подсказку, что игрок увидит в бою.
        /// </summary>
        /// <param name="panel">Собранный экран.</param>
        /// <param name="combatService">Боевые правила.</param>
        /// <param name="combat">Состояние боя.</param>
        /// <param name="texts">Тексты игры.</param>
        private static void ShowSkillChance(
            GameObject panel,
            ICombatService combatService,
            CombatState combat,
            IGameTexts texts)
        {
            var label = FindPath(panel.transform, "CombatTooltip/Text")?.GetComponent<Text>();
            if (label == null)
            {
                return;
            }

            var weapon = combatService.PlayerWeapon(combat);
            foreach (var action in combatService.GetActions(combat, weapon))
            {
                if (action.Kind != CombatActionKind.Skill || action.SkillId == null || !action.IsAvailable)
                {
                    continue;
                }

                var preview = combatService.PreviewSkill(combat, weapon, action.SkillId);
                label.text += "\n" + UiText.Format(
                    texts,
                    UiTextKey.Combat_Hit_Chance,
                    (int)Math.Round(preview.HitChance * 100));
                return;
            }
        }

        /// <summary>Ставит фигурам профили: игроку — надетую броню, противнику — из данных.</summary>
        /// <param name="panel">Собранный экран.</param>
        /// <param name="combat">Состояние боя.</param>
        /// <param name="items">Каталог предметов.</param>
        /// <param name="catalog">Каталог спрайтов.</param>
        private static void FillFigures(
            GameObject panel,
            CombatState combat,
            IItemRepository items,
            SpriteCatalog? catalog)
        {
            var armor = combat.Player.Equipment?.Get(EquipmentSlot.Armor);
            var playerCostume = armor == null
                ? CharacterFigure.CombatSilhouetteId
                : CharacterFigure.CombatCostumeId(items.Get(armor.ItemId).Id);
            ApplyFigure(
                panel,
                "PlayerFigure",
                playerCostume,
                WeaponIcon(items, catalog, combat.Player.Equipment?.Get(combat.PlayerWeaponSlot)),
                catalog,
                false,
                CombatPalette.FigurePlayer);

            var enemyCostume = combat.Enemy.Template.Costume;
            ApplyFigure(
                panel,
                "EnemyFigure",
                enemyCostume ?? CharacterFigure.CombatSilhouetteId,
                WeaponIcon(items, catalog, combat.EnemyWeapon),
                catalog,
                true,
                CombatPalette.FigureEnemy);
        }

        /// <summary>Иконка оружия из каталога; <c>null</c> — оружия нет.</summary>
        /// <param name="items">Каталог предметов.</param>
        /// <param name="catalog">Каталог спрайтов.</param>
        /// <param name="weapon">Экземпляр оружия.</param>
        /// <returns>Спрайт иконки или <c>null</c>.</returns>
        private static Sprite? WeaponIcon(IItemRepository items, SpriteCatalog? catalog, ItemInstance? weapon)
        {
            if (weapon == null || catalog == null)
            {
                return null;
            }

            return catalog.TryGet(items.Get(weapon.ItemId).Icon, out var sprite) ? sprite : null;
        }

        /// <summary>Ставит игроку заданный профиль и оружие для проверочного кадра.</summary>
        /// <param name="panel">Собранный экран.</param>
        /// <param name="catalog">Каталог спрайтов.</param>
        /// <param name="costumeId">Id профиля брони.</param>
        /// <param name="weaponId">Id иконки оружия.</param>
        private static void ApplyPlayerFigure(
            GameObject panel,
            SpriteCatalog? catalog,
            string costumeId,
            string weaponId)
        {
            if (catalog == null || !catalog.TryGet(costumeId, out var costume) || costume == null)
            {
                return;
            }

            var weapon = catalog.TryGet(weaponId, out var sprite) ? sprite : null;
            ApplyFigure(panel, "PlayerFigure", costumeId, weapon, catalog, false, CombatPalette.FigurePlayer);
        }

        /// <summary>Ставит профиль и оружие фигуре, если она есть в префабе.</summary>
        /// <param name="panel">Собранный экран.</param>
        /// <param name="name">Имя фигуры в префабе.</param>
        /// <param name="spriteId">Id костюма в каталоге.</param>
        /// <param name="weapon">Спрайт оружия или <c>null</c>.</param>
        /// <param name="catalog">Каталог спрайтов.</param>
        /// <param name="flip">Повернуть фигуру влево.</param>
        /// <param name="fallback">Цвет плашки без спрайта.</param>
        private static void ApplyFigure(
            GameObject panel,
            string name,
            string spriteId,
            Sprite? weapon,
            SpriteCatalog? catalog,
            bool flip,
            Color fallback)
        {
            var target = FindDeep(panel.transform, name);
            if (target == null || catalog == null || !catalog.TryGet(spriteId, out var sprite) || sprite == null)
            {
                return;
            }

            var view = target.GetComponent<CombatFigureView>();
            if (view == null)
            {
                var image = target.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprite;
                    image.color = Color.white;
                    image.preserveAspect = true;
                }

                return;
            }

            view.Show(sprite, fallback);
            view.SetWeapon(weapon);
            view.SetFlip(flip);
        }
    }
}
