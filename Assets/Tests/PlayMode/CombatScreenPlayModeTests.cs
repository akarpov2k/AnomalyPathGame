#nullable enable
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Screens.Combat;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Боевой экран на префабе по прототипу «бой»: плашки бойцов, форма действий
    /// с выпадашками, быстрые слоты, журнал действий и конец хода.
    /// </summary>
    public sealed partial class CombatScreenPlayModeTests : PlayModeTestBase
    {
        /// <summary>Экран открывается префабом: имя, здоровье, ОД и полоса опыта заполнены.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Open_FillsCombatantsAndPrefabParts()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            Assert.IsTrue(
                screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit")),
                "Бой не начался");
            screen.ShowCombat();
            yield return null;

            var combat = session.Combat!;
            Assert.IsNotNull(GameObject.Find("Combat"), "Боевой экран не открылся");
            Assert.AreEqual(
                screen.Texts.Get(player.NameKey),
                Label("PlayerPlate", "Name"),
                "Имя игрока в плашке");
            StringAssert.Contains(
                Mathf.RoundToInt((float)combat.Player.Health).ToString(),
                Label("PlayerPlate", "HealthBar", "Health"),
                "Здоровье игрока в полосе");
            StringAssert.Contains(
                Mathf.RoundToInt((float)combat.Enemy.Health).ToString(),
                Label("EnemyPlate", "HealthBar", "Health"),
                "Здоровье противника в полосе");
            Assert.AreEqual(
                combat.Player.MaxAp,
                CountByNamePrefix("ApRow", "CombatAp"),
                "Кругов очков действия столько же, сколько в ходу");
            Assert.IsNotEmpty(Label("CombatXp", "Label"), "Полоса опыта заполнена подписью");
            Assert.IsNotNull(GameObject.Find("PlayerWeapon"), "Плашка оружия игрока");
            Assert.IsNotNull(GameObject.Find("CombatQuickFrame"), "Рамка быстрых слотов");
        }

        /// <summary>Фигуры бойцов — профили из данных, у каждого в руках своё оружие.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Figures_ShowCostumeArtFromData()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var combat = session.Combat!;
            var armor = combat.Player.Equipment?.Get(EquipmentSlot.Armor);
            Assert.IsNotNull(armor, "в наборе новой игры нет брони");
            Assert.IsTrue(
                screen.Sprites.TryGet(CharacterFigure.CombatCostumeId(armor!.ItemId), out var costume),
                "боевого костюма надетой брони нет в каталоге");
            var playerFigure = GameObject.Find("PlayerFigure")!.GetComponent<Image>();
            Assert.AreSame(costume, playerFigure.sprite, "фигура игрока — боевой костюм надетой брони");

            var enemyCostume = combat.Enemy.Template.Costume;
            Assert.IsFalse(string.IsNullOrEmpty(enemyCostume), "у противника в данных нет боевого костюма");
            Assert.IsTrue(
                screen.Sprites.TryGet(enemyCostume!, out var enemySprite),
                "костюма противника нет в каталоге");
            var enemyFigure = GameObject.Find("EnemyFigure")!.GetComponent<Image>();
            Assert.AreSame(enemySprite, enemyFigure.sprite, "фигура противника — костюм из данных");
        }

        /// <summary>В руках у фигуры — иконка текущего оружия, выстрел включает вспышку у ствола.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Figures_ShowWeaponAndFireEffect()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var view = GameObject.Find("PlayerFigure")!.GetComponent<CombatFigureView>();
            Assert.IsNotNull(view, "у фигуры игрока нет компонента боя");
            var weapon = session.Combat!.Player.Equipment?.Get(session.Combat.PlayerWeaponSlot);
            Assert.IsNotNull(weapon, "у игрока нет оружия");
            Assert.IsTrue(
                screen.Sprites.TryGet(screen.Items.Get(weapon!.ItemId).Icon, out var weaponSprite),
                "иконки оружия игрока нет в каталоге");
            var weaponImage = GameObject.Find("PlayerFigure")!.transform.Find("Weapon")!.GetComponent<Image>();
            Assert.IsTrue(weaponImage.enabled, "оружие в руках не показано");
            Assert.AreSame(weaponSprite, weaponImage.sprite, "в руках не текущее оружие");

            var muzzle = GameObject.Find("PlayerFigure")!.transform.Find("Muzzle")!.GetComponent<Image>();
            var shots = view.ShotsPlayed;
            ClickIfInteractable("CombatAttack");
            Assert.IsTrue(view.IsFiring, "выстрел не запустил анимацию");
            Assert.IsTrue(muzzle.enabled, "вспышка у ствола не показана");
            Assert.AreEqual(shots + 1, view.ShotsPlayed, "одиночный выстрел — одна вспышка");
            Assert.IsNull(GameObject.Find("PlayerTracer"), "трасса выстрела должна быть убрана");
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(view.IsFiring, "анимация выстрела не закончилась");
            Assert.IsFalse(muzzle.enabled, "вспышка осталась после выстрела");
            Assert.AreEqual(Vector3.one, muzzle.rectTransform.localScale, "вспышка не вернула размер");
        }

        /// <summary>Повторный выстрел не ломает отдачу, а выпад мутанта играет без оружия.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Figures_RapidFire_IsStable()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var playerView = GameObject.Find("PlayerFigure")!.GetComponent<CombatFigureView>();
            var enemyView = GameObject.Find("EnemyFigure")!.GetComponent<CombatFigureView>();
            var weapon = GameObject.Find("PlayerFigure")!.transform.Find("Weapon")!.GetComponent<Image>();
            var muzzle = GameObject.Find("PlayerFigure")!.transform.Find("Muzzle")!.GetComponent<Image>();
            var home = weapon.rectTransform.anchoredPosition;
            playerView.Fire(enemyView, true);
            playerView.Fire(enemyView, false);
            Assert.IsTrue(playerView.IsFiring, "повторный выстрел не запустился");
            yield return new WaitForSeconds(0.4f);
            Assert.IsFalse(playerView.IsFiring, "повторный выстрел не закончился");
            Assert.IsFalse(muzzle.enabled, "вспышка осталась после повторного выстрела");
            Assert.AreEqual(home, weapon.rectTransform.anchoredPosition, "отдача не вернула оружие");
        }

        /// <summary>Выпад мутанта играет без оружия и заканчивается.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_MutantLunge_PlaysWithoutWeapon()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("mutant_wolf"));
            screen.ShowCombat();
            yield return null;

            var playerView = GameObject.Find("PlayerFigure")!.GetComponent<CombatFigureView>();
            var wolfView = GameObject.Find("EnemyFigure")!.GetComponent<CombatFigureView>();
            Assert.IsFalse(wolfView.HasWeapon, "у мутанта не должно быть оружия");
            wolfView.Lunge(playerView, true);
            Assert.IsTrue(wolfView.IsFiring, "выпад мутанта не запустился");
            yield return new WaitForSeconds(0.25f);
            Assert.IsFalse(wolfView.IsFiring, "выпад мутанта не закончился");
        }

        /// <summary>Ход противника с оружием проигрывает выстрел, а после него вспышка гаснет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_EnemyTurn_PlaysShotAndStops()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var enemyView = GameObject.Find("EnemyFigure")!.GetComponent<CombatFigureView>();
            var muzzle = GameObject.Find("EnemyFigure")!.transform.Find("Muzzle")!.GetComponent<Image>();
            Click("CombatEndTurn");
            var sawShot = false;
            var deadline = Time.time + 15f;
            while (Time.time < deadline)
            {
                if (enemyView.IsFiring || muzzle.enabled)
                {
                    sawShot = true;
                }

                var combat = session.Combat;
                if (combat == null || combat.IsFinished ||
                    ReferenceEquals(combat.Current, combat.Player))
                {
                    break;
                }

                yield return null;
            }

            Assert.IsTrue(sawShot, "ход противника не показал выстрел");
            Assert.IsFalse(enemyView.IsFiring, "выстрел противника не закончился");
            Assert.IsFalse(muzzle.enabled, "вспышка противника осталась");
        }

        /// <summary>Все три профиля брони и тяжёлое оружие грузятся и укладываются в поле.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_ArmorProfilesAndHeavyWeapons_Load()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var view = GameObject.Find("PlayerFigure")!.GetComponent<CombatFigureView>();
            var rect = (RectTransform)view.transform;
            var armorIds = new[]
            {
                "itm_armor_jacket",
                "itm_armor_plate",
                "itm_armor_exoskeleton"
            };
            foreach (var armorId in armorIds)
            {
                var id = CharacterFigure.CombatCostumeId(armorId);
                Assert.IsTrue(screen.Sprites.TryGet(id, out var costume), "нет профиля " + id);
                view.Show(costume, Color.white);
                Assert.Greater(rect.sizeDelta.y, 100f, "профиль сложился по высоте");
                Assert.LessOrEqual(rect.sizeDelta.y, 250.1f, "профиль выше поля");
                Assert.LessOrEqual(rect.sizeDelta.x, 150.1f, "профиль шире поля");
            }

            foreach (var icon in new[] { "icons/wpn_ks23", "icons/wpn_sv98" })
            {
                Assert.IsTrue(screen.Sprites.TryGet(icon, out var weapon), "нет иконки " + icon);
                view.SetWeapon(weapon);
                Assert.IsTrue(view.HasWeapon, "оружие не встало в руки: " + icon);
                var image = view.transform.Find("Weapon")!.GetComponent<Image>();
                Assert.Greater(image.rectTransform.sizeDelta.x, 0f, "ширина оружия не посчитана");
                Assert.Greater(image.rectTransform.sizeDelta.y, 0f, "высота оружия не посчитана");
            }

            foreach (var actorId in new[]
                     {
                         "enemy_bandit",
                         "enemy_merc",
                         "mutant_wolf",
                         "mutant_bear",
                         "mutant_boar"
                     })
            {
                var costume = screen.Actors.Get(actorId).Costume;
                Assert.IsFalse(string.IsNullOrEmpty(costume), "у врага нет костюма: " + actorId);
                Assert.IsTrue(screen.Sprites.TryGet(costume!, out _), "костюма нет в каталоге: " + actorId);
            }
        }

        /// <summary>«Скиллы» открывает список скиллов с перезарядкой, Esc закрывает его.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_SkillsMenu_ListsSkillsAndClosesByEscape()
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
            Assert.IsNotNull(GameObject.Find("CombatReload"), "В выпадашке нет перезарядки");
            Assert.IsNotNull(GameObject.Find("CombatSwap"), "В выпадашке нет смены оружия");
            Assert.IsNotNull(
                FindByPrefix(GameObject.Find("CombatSkillMenu")!.transform, "CombatSkill_"),
                "В выпадашке нет скиллов оружия");

            Assert.IsTrue(FindScreen().Combat.HandleEscape(), "Esc должен закрыть выпадашку");
            yield return null;
            Assert.IsNull(GameObject.Find("CombatSkillMenu"), "Esc не закрыл выпадашку");
        }

        /// <summary>В подсказке скилла виден шанс попадания тем же расчётом, что и выстрел.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_SkillTooltip_ShowsHitChance()
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
            var skill = FindByPrefix(GameObject.Find("CombatSkillMenu")!.transform, "CombatSkill_");
            Assert.IsNotNull(skill, "В выпадашке нет скилла");
            var skillId = skill!.name.Substring("CombatSkill_".Length);
            var pointer = new PointerEventData(EventSystem.current);
            ExecuteEvents.Execute(skill, pointer, ExecuteEvents.pointerEnterHandler);
            yield return null;

            var combat = session.Combat!;
            var weapon = combat.Player.Equipment?.Get(combat.PlayerWeaponSlot);
            var preview = screen.CombatEngine.PreviewSkill(combat, weapon, skillId);
            var expected = UiText.Format(
                screen.Texts,
                UiTextKey.Combat_Hit_Chance,
                Mathf.RoundToInt((float)(preview.HitChance * 100)));
            StringAssert.Contains(expected, Label("CombatTooltip", "Text"), "В подсказке нет шанса попадания");
        }

        /// <summary>После атаки над целью появляется число урона или слово «Промах».</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Attack_ShowsFloatingResult()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var enemy = GameObject.Find("EnemyFigure")!.GetComponent<CombatFigureView>();
            ClickIfInteractable("CombatAttack");
            yield return null;

            Assert.IsTrue(enemy.IsFloatingVisible, "Над противником нет всплывающей подписи");
            Assert.IsFalse(string.IsNullOrEmpty(enemy.FloatingTextValue), "Подпись пустая");
        }

        /// <summary>Журнал открывается кнопкой, показывает действия и закрывается кликом в любом месте.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_Journal_ShowsLogAndClosesByClick()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            ClickIfInteractable("CombatAttack");
            yield return null;

            Click("CombatJournal");
            yield return null;
            var panel = GameObject.Find("CombatJournalPanel");
            Assert.IsNotNull(panel, "Журнал не открылся");
            Assert.IsNotEmpty(Label("CombatJournalPanel", "Text"), "В журнале нет строк действий");

            Click("CombatJournalCatcher");
            yield return null;
            Assert.IsNull(GameObject.Find("CombatJournalPanel"), "Клик в любом месте не закрыл журнал");
        }

        /// <summary>Конец хода отдаёт ход противнику, и каждое его действие попадает в журнал.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Combat_EndTurn_EnemyActionsGoToLog()
        {
            yield return StartGame();
            var screen = FindScreen();
            var session = screen.Session!;
            var player = screen.Actors.Get(screen.Rules.Require().PlayerActorId);
            screen.Sessions.StartCombat(session, player, screen.Actors.Get("enemy_bandit"));
            screen.ShowCombat();
            yield return null;

            var before = screen.Combat.LogLines.Count;
            Click("CombatEndTurn");
            var deadline = Time.time + 15f;
            while (Time.time < deadline && session.Combat != null && !session.Combat.IsFinished &&
                   !ReferenceEquals(session.Combat.Current, session.Combat.Player))
            {
                yield return null;
            }

            Assert.GreaterOrEqual(screen.Combat.LastEnemyActions.Count, 1, "Противник должен сходить");
            Assert.AreEqual(
                before + screen.Combat.LastEnemyActions.Count,
                screen.Combat.LogLines.Count,
                "На каждое действие противника — строка журнала");
        }

        /// <summary>Боевой фон выбирается по фону локации, у дороги — придорожный кадр.</summary>
        [Test]
        public void CombatBackdrop_LocationAndRoad_UseBattleArt()
        {
            Assert.AreEqual(
                "backdrops/combat_bg_route_forest",
                CombatBackdrop.IdFor("bg_route_forest"),
                "фон локации даёт боевую картинку");
            Assert.AreEqual(
                "backdrops/combat_bg_route_highway",
                CombatBackdrop.IdFor(CombatBackdrop.Road),
                "у дороги — придорожная боевая картинка");
            Assert.AreEqual(
                "backdrops/screen",
                CombatBackdrop.IdFor(null),
                "без фона — общий задник");
        }

        private static string Label(params string[] path)
        {
            var target = Node(path);
            Assert.IsNotNull(target, "Нет подписи " + string.Join("/", path));
            return target!.GetComponent<Text>().text;
        }

        private static int CountByNamePrefix(string row, string prefix)
        {
            var root = GameObject.Find(row);
            Assert.IsNotNull(root, "Нет ряда " + row);
            var count = 0;
            foreach (Transform child in root!.transform)
            {
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal) && child.gameObject.activeSelf)
                {
                    count++;
                }
            }

            return count;
        }

        private static GameObject? Node(IReadOnlyList<string> path)
        {
            Transform? current = null;
            foreach (var name in path)
            {
                var next = current == null ? GameObject.Find(name)?.transform : current.Find(name);
                if (next == null)
                {
                    return null;
                }

                current = next;
            }

            return current?.gameObject;
        }
    }
}
