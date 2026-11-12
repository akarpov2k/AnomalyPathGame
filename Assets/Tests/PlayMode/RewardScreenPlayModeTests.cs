#nullable enable
using System.Collections;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Экран наград: две колонки, «Взять всё», предмет по клику, меню «Выбросить» у предмета
    /// инвентаря, деньги награды и крестик с подтверждением, когда лут остался.
    /// </summary>
    public sealed class RewardScreenPlayModeTests : PlayModeTestBase
    {
        /// <summary>Окно открывается с добычей слева и инвентарём игрока справа.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_Victory_ShowsTrophiesAndPlayerInventory()
        {
            yield return StartGame();
            var screen = FindScreen();

            screen.ShowRewards(
                new[] { new ItemInstance("reward_test_1", "ammo_9x18", 10) },
                0,
                RewardSource.Victory);
            yield return null;

            var panel = GameObject.Find("Rewards");
            Assert.IsNotNull(panel, "Экран наград не открылся");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Rewards_Source_Victory),
                Label(panel!, "RewardSource"),
                "Слева — трофеи противника");
            Assert.IsNotNull(FindByPrefix(panel!.transform, "RewardItem_"), "Слева нет добычи");
            Assert.IsNotNull(FindByPrefix(panel.transform, "RewardPlayerItem_"), "Справа нет предметов игрока");

            var reward = WorldRect((RectTransform)FindByPrefix(panel.transform, "RewardPanel")!.transform);
            var player = WorldRect((RectTransform)FindByPrefix(panel.transform, "PlayerPanel")!.transform);
            Assert.Less(reward.center.x, player.center.x, "Добыча слева, инвентарь игрока справа");
        }

        /// <summary>Клик по предмету забирает только его.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_TakeItem_MovesOnlyIt()
        {
            yield return StartGame();
            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            var before = Count(player.Inventory, "ammo_9x18");

            screen.ShowRewards(
                new[]
                {
                    new ItemInstance("reward_test_2", "ammo_9x18", 5),
                    new ItemInstance("reward_test_3", "ammo_545", 5)
                },
                0,
                RewardSource.Stash);
            yield return null;

            Click("RewardItem_reward_test_2");
            yield return null;

            Assert.AreEqual(before + 5, Count(player.Inventory, "ammo_9x18"), "забран один предмет");
            Assert.AreEqual(1, screen.Rewards.Remaining, "второй остался в колонке");
        }

        /// <summary>«Взять всё» переносит добычу и закрывает окно.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_TakeAll_MovesItemsAndCloses()
        {
            yield return StartGame();
            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            var before = Count(player.Inventory, "ammo_9x18");

            screen.ShowRewards(
                new[] { new ItemInstance("reward_test_4", "ammo_9x18", 7) },
                0,
                RewardSource.Victory);
            yield return null;

            Click("RewardTakeAll");
            yield return null;

            Assert.AreEqual(before + 7, Count(player.Inventory, "ammo_9x18"), "добыча в инвентаре");
            Assert.IsFalse(IsOpen(), "пустое окно закрывается само");
            Assert.IsNotNull(GameObject.Find("BigHub") ?? GameObject.Find("InGame"), "возврат на игровой экран");
        }

        /// <summary>Крестик с оставшимся лутом спрашивает подтверждение; «Отмена» оставляет окно.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_CloseWithLoot_AsksConfirmation()
        {
            yield return StartGame();
            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            var before = Count(player.Inventory, "ammo_9x18");

            screen.ShowRewards(
                new[] { new ItemInstance("reward_test_5", "ammo_9x18", 4) },
                0,
                RewardSource.Stash);
            yield return null;

            Click("RewardClose");
            yield return null;

            var dialog = GameObject.Find("ConfirmDialog");
            Assert.IsNotNull(dialog, "Подтверждение потери лута не открылось");
            Assert.IsTrue(dialog!.activeSelf, "Подтверждение скрыто");

            ClickIn("ConfirmDialog", "Отмена");
            yield return null;

            Assert.IsTrue(IsOpen(), "«Отмена» оставила окно наград");
            Assert.AreEqual(before, Count(player.Inventory, "ammo_9x18"), "лут не перенесён");

            Click("RewardClose");
            yield return null;
            ClickIn("ConfirmDialog", UiText.Get(screen.Texts, UiTextKey.Loot_Close));
            yield return null;

            Assert.IsFalse(IsOpen(), "подтверждение закрыло окно наград");
            Assert.AreEqual(before, Count(player.Inventory, "ammo_9x18"), "оставленный лут пропал");
        }

        /// <summary>Награда за задание показывает деньги и подпись «Награда».</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_Quest_ShowsMoneyWithoutLeftovers()
        {
            yield return StartGame();
            var screen = FindScreen();

            screen.ShowRewards(System.Array.Empty<ItemInstance>(), 500, RewardSource.Quest);
            yield return null;

            var panel = GameObject.Find("Rewards");
            Assert.IsNotNull(panel, "Экран наград не открылся");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Rewards_Source_Quest),
                Label(panel!, "RewardSource"),
                "Слева — награда за задание");
            Assert.AreEqual(
                UiText.Format(screen.Texts, UiTextKey.Rewards_Money, 500),
                Label(panel, "RewardMoney"),
                "Строка денег награды");

            Click("RewardClose");
            yield return null;

            Assert.IsNull(GameObject.Find("ConfirmDialog"), "Без лута подтверждение не нужно");
            Assert.IsFalse(IsOpen(), "Пустое окно закрывается крестиком");
        }

        /// <summary>
        /// В заголовке инвентаря игрока виден вес и предел переноски: по нему понятно,
        /// почему тяжёлая вещь не забирается.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_PlayerPanel_ShowsWeight()
        {
            yield return StartGame();
            var screen = FindScreen();
            var player = screen.Session!.State.Player!;

            screen.ShowRewards(new[] { new ItemInstance("reward_weight", "ammo_9x18", 1) }, 0, RewardSource.Victory);
            yield return null;

            var panel = GameObject.Find("Rewards");
            Assert.IsNotNull(panel, "Экран наград не открылся");
            var total = screen.Inventory.TotalWeight(player.Inventory);
            var limit = player.Inventory.WeightLimit;
            Assert.AreEqual(
                UiText.Format(screen.Texts, UiTextKey.Character_Weight, total, limit),
                Label(panel!, "PlayerWeight"),
                "Вес игрока на панели инвентаря");
        }

        /// <summary>
        /// Правый клик по предмету в инвентаре игрока открывает меню с пунктом «Выбросить»:
        /// так освобождают место под лут, не закрывая окно наград.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_PlayerItem_RightClickOpensDiscardMenu()
        {
            yield return StartGame();
            var screen = FindScreen();
            var player = screen.Session!.State.Player!;

            screen.ShowRewards(
                new[] { new ItemInstance("reward_test_6", "ammo_9x18", 2) },
                0,
                RewardSource.Victory);
            yield return null;

            var panel = GameObject.Find("Rewards");
            Assert.IsNotNull(panel, "Экран наград не открылся");
            var playerItem = FindByPrefix(panel!.transform, "RewardPlayerItem_");
            Assert.IsNotNull(playerItem, "В инвентаре игрока нет предметов");
            Assert.IsNull(playerItem!.GetComponent<Button>(), "Предмет инвентаря игрока не кнопка");
            var instanceId = playerItem.name.Substring("RewardPlayerItem_".Length);

            RightClick(playerItem, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            var menu = GameObject.Find("ItemMenu");
            Assert.IsNotNull(menu, "Правый клик не открыл контекстное меню");
            Assert.IsTrue(menu!.activeSelf, "Контекстное меню скрыто");

            ClickMenuEntry(UiText.Get(screen.Texts, UiTextKey.Character_Menu_Discard));
            yield return null;

            Assert.IsNull(player.Inventory.Find(instanceId), "Предмет должен исчезнуть из инвентаря");
            Assert.IsTrue(IsOpen(), "Выброс предмета не закрывает окно наград");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Character_Action_Discarded),
                Label(panel, "RewardToast"),
                "Сообщение о выброшенном предмете");
        }

        /// <summary>Esc закрывает меню предмета и оставляет окно наград открытым.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_ItemMenu_EscapeClosesMenuOnly()
        {
            yield return StartGame();
            var screen = FindScreen();

            screen.ShowRewards(
                new[] { new ItemInstance("reward_test_7", "ammo_9x18", 1) },
                0,
                RewardSource.Stash);
            yield return null;

            var panel = GameObject.Find("Rewards");
            Assert.IsNotNull(panel, "Экран наград не открылся");
            var playerItem = FindByPrefix(panel!.transform, "RewardPlayerItem_");
            Assert.IsNotNull(playerItem, "В инвентаре игрока нет предметов");
            RightClick(playerItem!, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;
            var menu = FindByPrefix(panel!.transform, "ItemMenu");
            Assert.IsNotNull(menu, "Меню не открылось");
            Assert.IsTrue(menu!.activeSelf, "Меню скрыто");

            Assert.IsTrue(screen.HandleEscape(), "Esc должен обработать меню");
            yield return null;

            Assert.IsFalse(menu.activeSelf, "Esc не закрыл меню");
            Assert.IsTrue(IsOpen(), "Esc закрыл всё окно вместо меню");
        }

        /// <summary>Esc закрывает пустое окно наград.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_Escape_ClosesEmptyWindow()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowRewards(System.Array.Empty<ItemInstance>(), 100, RewardSource.Quest);
            yield return null;

            Assert.IsTrue(screen.HandleEscape(), "Esc должен обработать окно наград");
            yield return null;

            Assert.IsFalse(IsOpen(), "Esc не закрыл окно наград");
        }

        /// <summary>Лут объекта открывает экран наград и сообщает о забранном предмете.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_ObjectLoot_UsesRewardScreenAndCallback()
        {
            yield return StartGame();
            var screen = FindScreen();
            var taken = false;

            screen.ShowLootFromObject(
                new[] { new ItemInstance("object_loot_1", "ammo_9x18", 1) },
                _ => taken = true);
            yield return null;

            Assert.IsNull(GameObject.Find("LootDialog"), "Диалога лута быть не должно");
            var panel = GameObject.Find("Rewards");
            Assert.IsNotNull(panel, "Лут объекта не открыл экран наград");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Rewards_Source_Loot),
                Label(panel!, "RewardSource"),
                "Слева — добыча объекта");

            Click("RewardItem_object_loot_1");
            yield return null;

            Assert.IsTrue(taken, "Забранный предмет должен убраться из объекта");
            Assert.IsNull(GameObject.Find("LootDialog"), "Диалог лута не создаётся");
        }

        /// <summary>Окантовка предмета в добыче показывает брошенную редкость; у обычной рамки нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Rewards_Rarity_DrawsColoredBorder()
        {
            yield return StartGame();
            var screen = FindScreen();

            screen.ShowRewards(
                new[]
                {
                    new ItemInstance(
                        "reward_rarity_epic",
                        "wpn_ak74",
                        1,
                        variantKey: RarityVariantCodec.Format(Rarity.Epic)),
                    new ItemInstance(
                        "reward_rarity_common",
                        "wpn_pm",
                        1,
                        variantKey: RarityVariantCodec.Format(Rarity.Common))
                },
                0,
                RewardSource.Victory);
            yield return null;

            var panel = GameObject.Find("Rewards")!;
            var epic = FindByPrefix(panel.transform, "RewardItem_reward_rarity_epic");
            Assert.IsNotNull(epic, "Нет клетки эпического предмета");
            var border = epic!.transform.Find("RarityBorderTop");
            Assert.IsNotNull(border, "У эпического предмета нет окантовки");
            Assert.AreEqual(RarityPalette.Epic, border!.GetComponent<Image>().color, "Цвет окантовки эпической ступени");

            var common = FindByPrefix(panel.transform, "RewardItem_reward_rarity_common");
            Assert.IsNotNull(common, "Нет клетки обычного предмета");
            Assert.IsNull(common!.transform.Find("RarityBorderTop"), "У обычной ступени рамки быть не должно");
        }

        private static bool IsOpen()
        {
            var panel = FindAnywhere("Rewards");
            return panel != null && panel.activeSelf;
        }

        private static string Label(GameObject panel, string name)
        {
            var target = FindDeep(panel.transform, name);
            Assert.IsNotNull(target, $"Нет подписи «{name}»");
            return target!.GetComponent<Text>().text;
        }

        private static Transform? FindDeep(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDeep(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static void ClickIn(string panelName, string buttonName)
        {
            var panel = GameObject.Find(panelName);
            Assert.IsNotNull(panel, $"Нет панели «{panelName}»");
            var target = FindDeep(panel!.transform, buttonName);
            Assert.IsNotNull(target, $"Нет кнопки «{buttonName}» в «{panelName}»");
            var button = target!.GetComponent<Button>();
            Assert.IsNotNull(button, $"«{buttonName}» — не кнопка");
            button!.onClick.Invoke();
        }

        private static void ClickMenuEntry(string label)
        {
            var menu = GameObject.Find("ItemMenu");
            Assert.IsNotNull(menu, "Контекстное меню не открыто");
            foreach (var text in menu!.GetComponentsInChildren<Text>(true))
            {
                if (text.text == label)
                {
                    var button = text.GetComponentInParent<Button>();
                    Assert.IsNotNull(button, $"Пункт «{label}» — не кнопка");
                    button!.onClick.Invoke();
                    return;
                }
            }

            Assert.Fail($"В контекстном меню нет пункта «{label}»");
        }

        private static int Count(GridContainer container, string itemId)
        {
            var total = 0;
            foreach (var item in container.Items)
            {
                if (string.Equals(item.ItemId, itemId, System.StringComparison.Ordinal))
                {
                    total += item.Stack;
                }
            }

            return total;
        }
    }
}
