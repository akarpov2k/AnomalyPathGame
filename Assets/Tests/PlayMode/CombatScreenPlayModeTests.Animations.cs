#nullable enable
using System.Collections;
using System.Linq;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="CombatScreenPlayModeTests"/>: очередь скилла и подсветка попадания.</summary>
    public sealed partial class CombatScreenPlayModeTests
    {
        /// <summary>Очередь скилла проигрывает по вспышке на каждый выстрел и гасит их в конце.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_SkillBurst_PlaysOneFlashPerShot()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = session.State.Player!;
            player.Equipment.Set(
                EquipmentSlot.Main,
                new ItemInstance("test_screen_ak", "wpn_ak74", 1, 100, 100, null, 30));
            var actor = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, actor, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var view = GameObject.Find("PlayerFigure")!.GetComponent<CombatFigureView>();
            var muzzle = GameObject.Find("PlayerFigure")!.transform.Find("Muzzle")!.GetComponent<Image>();
            var shots = view.ShotsPlayed;
            Click("CombatSkills");
            yield return null;
            Assert.IsNotNull(GameObject.Find("CombatSkill_skill_burst"), "нет кнопки очереди");
            Click("CombatSkill_skill_burst");
            Assert.IsTrue(view.IsFiring, "очередь не начала выстрелы");
            Assert.IsTrue(muzzle.enabled, "очередь не показала первую вспышку");
            Assert.IsNull(GameObject.Find("PlayerTracer"), "трасса выстрела должна быть убрана");
            yield return new WaitForSeconds(1f);
            Assert.IsFalse(view.IsFiring, "очередь не закончилась");
            Assert.AreEqual(shots + 3, view.ShotsPlayed, "на три патрона очереди — три вспышки");
            Assert.IsFalse(muzzle.enabled, "вспышка осталась после очереди");
        }

        /// <summary>Подсветка попадания включает оттенок и возвращает цвет тела.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_HitFlash_TintsAndRestoresBody()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var enemy = GameObject.Find("EnemyFigure")!.GetComponent<CombatFigureView>();
            var body = GameObject.Find("EnemyFigure")!.GetComponent<Image>();
            enemy.FlashHit();
            Assert.AreNotEqual(Color.white, body.color, "попадание не подсветило тело");
            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(Color.white, body.color, "подсветка не вернула цвет тела");
        }

        /// <summary>Выпадашка скиллов рисуется рамкой в 9-slice с border из спрайта.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_SkillMenu_UsesFramedPanel()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            Click("CombatSkills");
            yield return null;
            var panel = GameObject.Find("CombatSkillMenu")!.GetComponent<Image>();
            Assert.IsNotNull(panel.sprite, "у выпадашки нет рамки");
            Assert.AreEqual(Image.Type.Sliced, panel.type, "рамка не в режиме 9-slice");
            Assert.AreEqual(new Vector4(20f, 20f, 20f, 20f), panel.sprite.border, "border рамки не 20 px");
        }

        /// <summary>Строка выпадашки рисуется пластиной, а наведение меняет кадр.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_SkillEntry_UsesPlateAndHighlight()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            Click("CombatSkills");
            yield return null;
            var row = GameObject.Find("CombatReload")!;
            var image = row.GetComponent<Image>();
            var art = row.GetComponent<ButtonArt>();
            Assert.IsNotNull(image.sprite, "у строки нет пластины");
            Assert.AreEqual(Image.Type.Sliced, image.type, "пластина не в режиме 9-slice");
            Assert.AreEqual(new Vector4(8f, 8f, 8f, 8f), image.sprite.border, "border пластины не 8 px");
            Assert.IsNotNull(art, "у строки нет ButtonArt");

            var normal = image.sprite;
            art!.OnPointerEnter(new PointerEventData(EventSystem.current));
            Assert.AreNotEqual(normal, image.sprite, "наведение не сменило кадр пластины");
            art.OnPointerExit(new PointerEventData(EventSystem.current));
            Assert.AreEqual(normal, image.sprite, "кадр пластины не вернулся после наведения");
        }

        /// <summary>Быстрый слот рисуется той же пластиной в 9-slice.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_QuickSlot_UsesPlate()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var frame = GameObject.Find("CombatQuickFrame");
            Assert.IsNotNull(frame, "нет формы быстрых слотов");
            var slot = frame!.transform.Cast<Transform>().FirstOrDefault(child => child.gameObject.activeSelf);
            Assert.IsNotNull(slot, "нет активного быстрого слота");
            var image = slot!.GetComponent<Image>();
            Assert.IsNotNull(image.sprite, "у слота нет пластины");
            Assert.AreEqual(Image.Type.Sliced, image.type, "пластина слота не в режиме 9-slice");
            Assert.AreEqual(new Vector4(8f, 8f, 8f, 8f), image.sprite.border, "border пластины слота не 8 px");
        }
    }
}
