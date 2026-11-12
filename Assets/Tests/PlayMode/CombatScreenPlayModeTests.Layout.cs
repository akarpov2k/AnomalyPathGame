#nullable enable
using System.Collections;
using System.Reflection;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Часть класса <see cref="CombatScreenPlayModeTests"/>: подпись на металлической табличке
    /// кадра кнопки и оружие в руках.
    /// </summary>
    public sealed partial class CombatScreenPlayModeTests
    {
        private static readonly string[] ActionButtons =
        {
            "CombatAttack",
            "CombatSkills",
            "CombatEndTurn",
            "CombatFlee"
        };

        /// <summary>
        /// Подписи кнопок формы стоят по центру металлической таблички кадра, а не всего
        /// прямоугольника, остаются внутри кадра и написаны коричневым, а не белым.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_ActionLabels_SitOnFramePlate()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            foreach (var name in ActionButtons)
            {
                var button = GameObject.Find(name);
                Assert.IsNotNull(button, $"Нет кнопки «{name}»");
                var label = button!.GetComponentInChildren<Text>(true);
                Assert.IsNotNull(label, $"У кнопки «{name}» нет подписи");

                var frame = WorldRect((RectTransform)button.transform);
                var caption = WorldRect(label!.rectTransform);
                var plate = ActionButtonMetrics.PlateRect(frame.width, frame.height, 0f);
                var plateCenter = new Vector2(frame.xMin + plate.center.x, frame.yMin + plate.center.y);

                Assert.IsTrue(
                    frame.Contains(caption.min) && frame.Contains(caption.max),
                    $"Подпись «{name}» выходит за кадр кнопки");
                Assert.AreEqual(
                    plateCenter.x,
                    caption.center.x,
                    0.5f,
                    $"Подпись «{name}» сдвинута от центра таблички по горизонтали");
                Assert.AreEqual(
                    plateCenter.y,
                    caption.center.y,
                    0.5f,
                    $"Подпись «{name}» стоит не по центру таблички по вертикали");
                Assert.AreEqual(
                    ActionButtonPalette.Label,
                    label.color,
                    $"Подпись «{name}» не коричневая, как остальные кнопки формы");
                Assert.AreNotEqual(Color.white, label.color, $"Подпись «{name}» осталась белой");
                Assert.LessOrEqual(
                    label.preferredWidth,
                    label.rectTransform.rect.width + 0.5f,
                    $"Подпись «{name}» шире таблички кадра");
            }
        }

        /// <summary>Оружие держат в руках: кисть бойца лежит на самом оружии, а не рядом с ним.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Weapon_SitsInHands()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            foreach (var name in new[] { "PlayerFigure", "EnemyFigure" })
            {
                var figure = GameObject.Find(name);
                Assert.IsNotNull(figure, $"Нет фигуры «{name}»");
                var view = figure!.GetComponent<CombatFigureView>();
                Assert.IsNotNull(view, $"У фигуры «{name}» нет компонента боя");
                Assert.IsTrue(view!.HasWeapon, $"у фигуры «{name}» нет оружия в руках");

                var body = (RectTransform)figure.transform;
                var weapon = (RectTransform)figure.transform.Find("Weapon")!;
                var hand = HandPoint(view, body);
                var box = Rect.MinMaxRect(
                    weapon.anchoredPosition.x - (weapon.rect.width * 0.5f),
                    weapon.anchoredPosition.y - (weapon.rect.height * 0.5f),
                    weapon.anchoredPosition.x + (weapon.rect.width * 0.5f),
                    weapon.anchoredPosition.y + (weapon.rect.height * 0.5f));

                Assert.IsTrue(box.Contains(hand), $"у «{name}» кисть не на оружии, а рядом с ним");
                Assert.LessOrEqual(
                    Mathf.Abs(hand.y - weapon.anchoredPosition.y),
                    weapon.rect.height * 0.5f,
                    $"у «{name}» оружие висит ниже кистей");
            }
        }

        /// <summary>Точка кисти в координатах тела: та доля, которую задаёт компонент фигуры.</summary>
        /// <param name="view">Компонент фигуры.</param>
        /// <param name="body">Прямоугольник фигуры.</param>
        /// <returns>Точка хвата оружия в координатах тела.</returns>
        private static Vector2 HandPoint(CombatFigureView view, RectTransform body)
        {
            var field = typeof(CombatFigureView).GetField(
                "weaponMount",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "у компонента фигуры нет точки хвата weaponMount");
            var mount = (Vector2)field!.GetValue(view)!;
            return new Vector2(mount.x * body.rect.width, mount.y * body.rect.height);
        }
    }
}
