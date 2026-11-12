#nullable enable
using System.Collections;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Задники экранов приходят из префабов: у каждого экрана проставлен свой id фона каталога
    /// спрайтов, поэтому на нём не остаётся общей заглушки <c>backdrops/screen</c>.
    /// </summary>
    public sealed class ScreenBackdropPlayModeTests : PlayModeTestBase
    {
        private const string TraderId = "trd_hub_smuggler";
        private const string AnomalySpawnId = "spawn_test_anomaly";
        private const string AnomalyTitleKey = "spawn.spawn_anomaly_chem.title";
        private const string AnomalyId = "anomaly_chem";

        /// <summary>Все готовые фоны экранов лежат в каталоге спрайтов сцены Main.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_Catalog_HasEveryScreenBackdrop()
        {
            yield return LoadMain();

            var catalog = FindScreen().Sprites;
            var ids = new[]
            {
                ScreenBackdropIds.Base,
                ScreenBackdropIds.Quests,
                ScreenBackdropIds.Trade,
                ScreenBackdropIds.Reward,
                ScreenBackdropIds.Talents,
                ScreenBackdropIds.Lock,
                ScreenBackdropIds.Radar
            };
            foreach (var id in ids)
            {
                Assert.IsTrue(catalog.TryGet(id, out var sprite), $"в каталоге спрайтов нет фона {id}");
                Assert.IsNotNull(sprite, $"спрайт фона {id} не загружен");
            }
        }

        /// <summary>Настройки, клавиши, новая игра, загрузка и авторы берут общий фон base.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_MenuScreens_UseBaseId()
        {
            yield return LoadMain();

            Click("Настройки");
            yield return null;
            AssertBackdrop("Settings", ScreenBackdropIds.Base);

            Click("Настройка клавиш");
            yield return null;
            AssertBackdrop("KeyBindings", ScreenBackdropIds.Base);

            Click("Назад");
            yield return null;
            Click("Назад");
            yield return null;

            Click("Новая игра");
            yield return null;
            AssertBackdrop("NewGame", ScreenBackdropIds.Base);
            Click("Назад");
            yield return null;

            Click("Загрузить игру");
            yield return null;
            AssertBackdrop("LoadGame", ScreenBackdropIds.Base);
            Click("Назад");
            yield return null;

            Click("Авторы");
            yield return null;
            AssertBackdrop("Authors", ScreenBackdropIds.Base);
        }

        /// <summary>Экран сохранения в игре берёт общий фон base.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_SaveScreen_UsesBaseId()
        {
            yield return StartGame();

            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;

            AssertBackdrop("SaveGame", ScreenBackdropIds.Base);
        }

        /// <summary>Доска объявлений и журнал заданий берут общий фон заданий.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_QuestScreens_UseQuestsId()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.ShowQuests(screen.Session!.State.World.CurrentLocation.InstanceId);
            yield return null;
            AssertBackdrop("QuestBoardScreen", ScreenBackdropIds.Quests);

            Click("BoardClose");
            yield return null;
            screen.ShowQuests(null);
            yield return null;
            AssertBackdrop("QuestJournalScreen", ScreenBackdropIds.Quests);
        }

        /// <summary>Окно торговли берёт свой фон торговли.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_TradeScreen_UsesTradeId()
        {
            yield return StartGame();

            FindScreen().ShowTrade(TraderId);
            yield return null;

            AssertBackdrop("Trade", ScreenBackdropIds.Trade);
        }

        /// <summary>Экран наград берёт свой фон наград.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_RewardScreen_UsesRewardId()
        {
            yield return StartGame();

            FindScreen().ShowRewards(
                new[] { new ItemInstance("backdrop_reward_test", "ammo_9x18", 5) },
                0,
                RewardSource.Victory);
            yield return null;

            AssertBackdrop("Rewards", ScreenBackdropIds.Reward);
        }

        /// <summary>Экран талантов берёт свой фон талантов.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_TalentScreen_UsesTalentsId()
        {
            yield return StartGame();

            FindScreen().ShowTalents();
            yield return null;

            AssertBackdrop("Talents", ScreenBackdropIds.Talents);
        }

        /// <summary>Окно взлома тайника берёт свой фон замка.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_LockMinigame_UsesLockId()
        {
            yield return StartGame();

            FindScreen().ShowLockMinigame();
            yield return null;

            AssertBackdrop("LockMinigame", ScreenBackdropIds.Lock);
        }

        /// <summary>Окно поиска артефакта берёт свой фон радара.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Backdrop_RadarMinigame_UsesRadarId()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.Sessions.LeaveHub(screen.Session!);
            var location = screen.Session!.State.World.CurrentLocation;
            foreach (var existing in location.Objects)
            {
                existing.MarkLooted();
            }

            var anomaly = new AnomalyLocationObject(AnomalySpawnId, AnomalyTitleKey, AnomalyId);
            location.MarkSpawned();
            location.AddObject(anomaly);
            screen.ShowGame(false);
            yield return null;
            Assert.IsNotNull(GameObject.Find("InGame"), "Экран локации не открылся");

            screen.ShowRadar(anomaly);
            yield return null;

            AssertBackdrop("RadarMinigame", ScreenBackdropIds.Radar);
        }

        /// <summary>Сверяет id фона у панели экрана.</summary>
        /// <param name="panelName">Имя панели, которую показывает роутер.</param>
        /// <param name="expectedId">Ожидаемый id фона каталога спрайтов.</param>
        private static void AssertBackdrop(string panelName, string expectedId)
        {
            var panel = GameObject.Find(panelName);
            Assert.IsNotNull(panel, $"Экран «{panelName}» не открылся");
            var backdrop = panel!.transform.Find("ScreenBackdrop");
            Assert.IsNotNull(backdrop, $"У экрана «{panelName}» нет задника");
            var component = backdrop!.GetComponent<ScreenBackdrop>();
            Assert.IsNotNull(component, $"У задника «{panelName}» нет ScreenBackdrop");
            Assert.AreEqual(expectedId, component!.SpriteId, $"У экрана «{panelName}» не тот фон");
        }
    }
}
