#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>
    /// Часть класса <see cref="CombatScreen"/>: полосы здоровья с очками действия,
    /// зоны поля, оружие с патронами и полоса опыта.
    /// </summary>
    public sealed partial class CombatScreen
    {
        /// <summary>Заполняет плашки игрока и противника: имя, здоровье, ОД и травмы.</summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        private void RefreshCombatants(CombatState? combat)
        {
            if (combat == null)
            {
                SetText(playerName, string.Empty);
                SetText(enemyName, UiText.Get(Host.Texts, UiTextKey.Combat_No_Combat));
                SetText(playerHp, string.Empty);
                SetText(enemyHp, string.Empty);
                SetText(playerInjuries, string.Empty);
                SetFill(playerHpFill, 0f);
                SetFill(enemyHpFill, 0f);
                BuildApRow(0, 0);
                return;
            }

            var player = combat.Player;
            var enemy = combat.Enemy;
            SetText(playerName, Host.Texts.Get(player.Template.NameKey));
            SetText(enemyName, Host.Texts.Get(enemy.Template.NameKey));
            SetText(playerHp, UiText.Format(
                Host.Texts,
                UiTextKey.Combat_Hp,
                Math.Round(player.Health),
                player.MaxHealth));
            SetText(enemyHp, UiText.Format(
                Host.Texts,
                UiTextKey.Combat_Hp,
                Math.Round(enemy.Health),
                enemy.MaxHealth));
            SetFill(playerHpFill, Fraction(player.Health, player.MaxHealth));
            SetFill(enemyHpFill, Fraction(enemy.Health, enemy.MaxHealth));
            BuildApRow(player.Ap, player.MaxAp);
            SetText(playerInjuries, InjuriesText(player));
        }

        /// <summary>Показывает фигуры бойцов в профиль: костюм, текущее оружие и поворот.</summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        private void RefreshFigures(CombatState? combat)
        {
            if (playerFigure == null || enemyFigure == null)
            {
                return;
            }

            if (combat == null)
            {
                playerFigure.Show(null, CombatPalette.FigurePlayer);
                enemyFigure.Show(null, CombatPalette.FigureEnemy);
                playerFigure.SetWeapon(null);
                enemyFigure.SetWeapon(null);
                playerFigure.SetFlip(false);
                enemyFigure.SetFlip(true);
                return;
            }

            var armor = combat.Player.Equipment?.Get(EquipmentSlot.Armor);
            Sprite? playerSprite = null;
            if (armor != null)
            {
                var itemId = Host.Items.Get(armor.ItemId).Id;
                playerSprite = TrySprite(CharacterFigure.CombatCostumeId(itemId)) ??
                               TrySprite(CharacterFigure.CostumeId(itemId));
            }

            playerSprite ??= TrySprite(CharacterFigure.CombatSilhouetteId) ??
                             TrySprite(CharacterFigure.SilhouetteId);
            var enemyCostume = combat.Enemy.Template.Costume;
            var enemySprite = TrySprite(enemyCostume) ??
                              TrySprite(CharacterFigure.CombatSilhouetteId) ??
                              TrySprite(CharacterFigure.SilhouetteId);
            playerFigure.Show(playerSprite, CombatPalette.FigurePlayer);
            enemyFigure.Show(enemySprite, CombatPalette.FigureEnemy);
            playerFigure.SetWeapon(WeaponIcon(PlayerWeapon(combat)));
            enemyFigure.SetWeapon(WeaponIcon(combat.EnemyWeapon));
            playerFigure.SetFlip(false);
            enemyFigure.SetFlip(true);
        }

        /// <summary>Иконка оружия из каталога спрайтов; <c>null</c> — оружия нет.</summary>
        /// <param name="weapon">Экземпляр оружия.</param>
        /// <returns>Спрайт иконки или <c>null</c>.</returns>
        private Sprite? WeaponIcon(ItemInstance? weapon) =>
            weapon == null ? null : TrySprite(Host.Items.Get(weapon.ItemId).Icon);

        /// <summary>Спрайт по id каталога; <c>null</c>, если id пуст или картинки нет.</summary>
        /// <param name="id">Id в каталоге спрайтов.</param>
        /// <returns>Спрайт или <c>null</c>.</returns>
        private Sprite? TrySprite(string? id) =>
            !string.IsNullOrWhiteSpace(id) && Host.Sprites.TryGet(id!, out var sprite) && sprite != null
                ? sprite
                : null;

        /// <summary>Показывает оружие и патроны обеих сторон.</summary>
        /// <param name="combat">Состояние боя; <c>null</c> — боя нет.</param>
        private void RefreshWeapons(CombatState? combat)
        {
            if (combat == null)
            {
                ShowWeapon(playerWeaponIcon, playerAmmo, null);
                ShowWeapon(enemyWeaponIcon, enemyAmmo, null);
                return;
            }

            ShowWeapon(playerWeaponIcon, playerAmmo, PlayerWeapon(combat));
            ShowWeapon(enemyWeaponIcon, enemyAmmo, combat.EnemyWeapon);
        }

        /// <summary>Ставит иконку и число патронов одного бойца.</summary>
        /// <param name="icon">Иконка оружия.</param>
        /// <param name="ammo">Подпись патронов.</param>
        /// <param name="weapon">Оружие; <c>null</c> — оружия нет.</param>
        private void ShowWeapon(Image? icon, Text? ammo, ItemInstance? weapon)
        {
            if (weapon == null)
            {
                if (icon != null)
                {
                    icon.enabled = false;
                }

                SetText(ammo, UiText.Get(Host.Texts, UiTextKey.Combat_No_Weapon));
                return;
            }

            var template = Host.Items.Get(weapon.ItemId);
            if (icon != null)
            {
                icon.enabled = Host.Sprites.TryGet(template.Icon, out var sprite) && sprite != null;
                if (icon.enabled && sprite != null)
                {
                    icon.sprite = sprite;
                }
            }

            SetText(
                ammo,
                UiText.Format(
                    Host.Texts,
                    UiTextKey.Combat_Ammo,
                    weapon.MagazineRounds,
                    template.Weapon?.Magazine ?? 0));
        }

        /// <summary>Заполняет полосу опыта уровня игрока.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <param name="combat">Состояние боя.</param>
        private void RefreshProgress(GameSession? session, CombatState? combat)
        {
            var player = session?.State.Player;
            if (player == null)
            {
                SetFill(xpFill, 0f);
                SetText(xpText, string.Empty);
                return;
            }

            var required = Host.Progression.RequiredXp(player.Level);
            var floor = Host.Progression.TotalXpToLevel(player.Level);
            var into = Math.Max(0, player.Xp - floor);
            SetFill(xpFill, Fraction(into, required));
            SetText(
                xpText,
                UiText.Format(Host.Texts, UiTextKey.Combat_Xp, player.Level, into, required));
            if (combat == null)
            {
                SetText(xpText, string.Empty);
            }
        }

        /// <summary>Строит ряд кругов очков действия: доступные горят, потраченные потухшие.</summary>
        /// <param name="ap">Сколько очков осталось.</param>
        /// <param name="maxAp">Сколько очков в ходу.</param>
        private void BuildApRow(int ap, int maxAp)
        {
            foreach (var circle in _apCircles)
            {
                Destroy(circle);
            }

            _apCircles.Clear();
            if (playerApRow == null || playerApTemplate == null)
            {
                return;
            }

            playerApTemplate.gameObject.SetActive(false);
            for (var index = 0; index < maxAp; index++)
            {
                var circle = Instantiate(playerApTemplate, playerApRow);
                circle.gameObject.name = "CombatAp" + index;
                circle.gameObject.SetActive(true);
                circle.color = index < ap ? CombatPalette.ApOn : CombatPalette.ApOff;
                _apCircles.Add(circle.gameObject);
            }
        }

        /// <summary>Строка травм игрока: название и остаток по виду длительности.</summary>
        /// <param name="player">Боец-игрок.</param>
        /// <returns>Видимая строка травм.</returns>
        private string InjuriesText(Combatant player)
        {
            var lines = new List<string>();
            foreach (var status in player.Statuses)
            {
                if (!status.IsInjury)
                {
                    continue;
                }

                var name = Host.Injuries.TryGet(status.Id, out var template)
                    ? Host.Texts.Get(template.NameKey)
                    : status.Id;
                var remaining = status.Kind switch
                {
                    InjuryDurationKind.Turns => UiText.Format(
                        Host.Texts,
                        UiTextKey.Combat_Injury_Turns,
                        status.Remaining),
                    InjuryDurationKind.Hours => UiText.Format(
                        Host.Texts,
                        UiTextKey.Combat_Injury_Hours,
                        status.Remaining),
                    _ => UiText.Get(Host.Texts, UiTextKey.Combat_Injury_Until_Healed)
                };
                lines.Add(UiText.Format(Host.Texts, UiTextKey.Combat_Injury_Line, name, remaining));
            }

            return lines.Count == 0
                ? string.Empty
                : UiText.Get(Host.Texts, UiTextKey.Combat_Injuries_Title) + ": " + string.Join(", ", lines);
        }

        /// <summary>Доля заполнения полосы.</summary>
        /// <param name="value">Текущее значение.</param>
        /// <param name="max">Максимум.</param>
        /// <returns>Значение от 0 до 1.</returns>
        private static float Fraction(double value, double max) =>
            max <= 0 ? 0f : Mathf.Clamp01((float)(value / max));

        /// <summary>Ставит текст подписи, если она задана.</summary>
        /// <param name="label">Подпись префаба.</param>
        /// <param name="text">Новый текст.</param>
        private static void SetText(Text? label, string text)
        {
            if (label != null)
            {
                label.text = text;
            }
        }

        /// <summary>Ставит заполнение полосы, если она задана.</summary>
        /// <param name="fill">Заполнение полосы.</param>
        /// <param name="amount">Доля от 0 до 1.</param>
        private static void SetFill(Image? fill, float amount)
        {
            if (fill != null)
            {
                fill.fillAmount = amount;
            }
        }
    }
}
