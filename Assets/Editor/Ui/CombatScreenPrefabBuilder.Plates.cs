#nullable enable
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.Ui
{
    /// <summary>Часть класса <see cref="CombatScreenPrefabBuilder"/>: плашки здоровья и фигуры бойцов.</summary>
    public static partial class CombatScreenPrefabBuilder
    {
        private static void BuildHealthPlate(RectTransform field, CombatScreen screen, bool player)
        {
            var plate = CreateImage(field, player ? "PlayerPlate" : "EnemyPlate", CombatPalette.CombatantPlate);
            var rect = plate.rectTransform;
            if (player)
            {
                PlaceAt(rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(PlateInset, -20f), new Vector2(PlateWidth, PlateHeight));
            }
            else
            {
                PlaceAt(rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-PlateInset, -20f), new Vector2(PlateWidth, PlateHeight));
            }

            var name = CreateText(
                plate.transform,
                "Name",
                string.Empty,
                20,
                player ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight,
                UiFactory.TextColor,
                bold: true);
            PlaceTopLeft(
                name.rectTransform,
                new Vector2(PlatePadding, -PlatePadding),
                new Vector2(PlateWidth - (2f * PlatePadding), 24f));

            var bar = CreateImage(plate.transform, "HealthBar", CombatPalette.HealthBack);
            PlaceTopLeft(
                bar.rectTransform,
                new Vector2(PlatePadding, -HealthBarTop),
                new Vector2(PlateWidth - (2f * PlatePadding), HealthBarHeight));
            var fill = CreateImage(bar.transform, "Fill", player ? CombatPalette.HealthPlayer : CombatPalette.HealthEnemy);
            Stretch(fill.rectTransform, 2f);
            ApplySprite(fill, BarFillPath);
            MakeFilled(fill);
            var hp = CreateText(
                bar.transform,
                "Health",
                string.Empty,
                15,
                TextAnchor.MiddleCenter,
                UiFactory.TextColor);
            Stretch(hp.rectTransform, 0f);

            if (player)
            {
                var row = CreateRect(plate.transform, "ApRow");
                PlaceTopLeft(row, new Vector2(PlatePadding, -ApRowTop), new Vector2(PlateWidth - (2f * PlatePadding), ApRowHeight));
                var group = row.gameObject.AddComponent<HorizontalLayoutGroup>();
                group.childAlignment = TextAnchor.MiddleLeft;
                group.spacing = 6f;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
                group.childControlWidth = false;
                group.childControlHeight = false;
                var circle = CreateImage(row, "ApTemplate", CombatPalette.ApOn);
                ApplySprite(circle, ApCirclePath);
                circle.rectTransform.sizeDelta = new Vector2(ApCircle, ApCircle);
                circle.gameObject.SetActive(false);
                SetObject(screen, "playerApRow", row);
                SetObject(screen, "playerApTemplate", circle);
            }
            else
            {
                SetObject(screen, "enemyName", name);
                SetObject(screen, "enemyHp", hp);
                SetObject(screen, "enemyHpFill", fill);
            }

            if (player)
            {
                SetObject(screen, "playerName", name);
                SetObject(screen, "playerHp", hp);
                SetObject(screen, "playerHpFill", fill);
                var injuries = CreateText(
                    plate.transform,
                    "CombatInjuries",
                    string.Empty,
                    13,
                    TextAnchor.MiddleLeft,
                    UiFactory.MutedTextColor);
                PlaceTopLeft(
                    injuries.rectTransform,
                    new Vector2(PlatePadding, -(ApRowTop + ApRowHeight)),
                    new Vector2(PlateWidth - (2f * PlatePadding), 22f));
                SetObject(screen, "playerInjuries", injuries);
            }
        }

        /// <summary>Фигура бойца на поле: тело в профиль, слой оружия и вспышка у ствола.</summary>
        /// <param name="field">Прямоугольник поля.</param>
        /// <param name="screen">Собираемый экран.</param>
        /// <param name="player">Фигура игрока или противника.</param>
        /// <returns>Компонент фигуры.</returns>
        private static CombatFigureView BuildFigure(RectTransform field, CombatScreen screen, bool player)
        {
            var figure = CreateImage(
                field,
                player ? "PlayerFigure" : "EnemyFigure",
                player ? CombatPalette.FigurePlayer : CombatPalette.FigureEnemy);
            figure.preserveAspect = true;
            var x = player ? PlayerFigureX : EnemyFigureX;
            PlaceAt(
                figure.rectTransform,
                new Vector2(x, FigureFeet),
                new Vector2(0.5f, 0f),
                Vector2.zero,
                new Vector2(135f, 250f));

            var weapon = CreateImage(figure.transform, "Weapon", Color.white);
            weapon.preserveAspect = true;
            weapon.enabled = false;
            weapon.rectTransform.sizeDelta = new Vector2(68f, 34f);
            weapon.rectTransform.anchorMin = Vector2.zero;
            weapon.rectTransform.anchorMax = Vector2.zero;
            weapon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            weapon.rectTransform.localScale = new Vector3(-1f, 1f, 1f);

            var muzzle = CreateImage(figure.transform, "Muzzle", Color.white);
            ApplySprite(muzzle, MuzzleFlashPath);
            muzzle.preserveAspect = true;
            muzzle.enabled = false;
            muzzle.rectTransform.sizeDelta = new Vector2(56f, 56f);

            var floating = CreateText(
                figure.transform,
                "FloatingText",
                string.Empty,
                30,
                TextAnchor.MiddleCenter,
                Color.white,
                bold: true);
            floating.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            floating.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            floating.rectTransform.pivot = new Vector2(0.5f, 0f);
            floating.rectTransform.anchoredPosition = new Vector2(0f, 12f);
            floating.rectTransform.sizeDelta = new Vector2(220f, 46f);
            var outline = floating.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2f, -2f);
            floating.gameObject.SetActive(false);

            var view = figure.gameObject.AddComponent<CombatFigureView>();
            SetObject(view, "body", figure);
            SetObject(view, "weapon", weapon);
            SetObject(view, "muzzle", muzzle);
            SetObject(view, "floatingText", floating);
            SetFloat(view, "bodyMaxWidth", player ? 150f : 260f);
            SetVector2(view, "weaponMount", player ? PlayerWeaponMount : EnemyWeaponMount);
            if (player)
            {
                SetObject(screen, "playerFigure", view);
            }
            else
            {
                SetObject(screen, "enemyFigure", view);
            }

            return view;
        }

        /// <summary>Плашка оружия и патронов: иконка у своего края и число патронов.</summary>
        /// <param name="field">Прямоугольник поля.</param>
        /// <param name="screen">Собираемый экран.</param>
        /// <param name="player">Плашка игрока или противника.</param>
    }
}
