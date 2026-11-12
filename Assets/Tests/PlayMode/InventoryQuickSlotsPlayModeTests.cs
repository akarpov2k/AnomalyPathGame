#nullable enable
using System.Collections;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Быстрые слоты окна «Инвентарь»: столбец строится по числу слотов экипировки,
    /// расходник кладётся перетаскиванием и возвращается в сетку перетаскиванием обратно.
    /// </summary>
    public sealed class InventoryQuickSlotsPlayModeTests : PlayModeTestBase
    {
        /// <summary>Столбец содержит столько слотов, сколько их в экипировке, и подписан.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator QuickSlots_ShownOnePerEquipmentSlot()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            var row = GameObject.Find("QuickSlots");
            Assert.IsNotNull(row, "Нет столбца быстрых слотов");
            var slots = 0;
            foreach (Transform child in row!.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    slots++;
                }
            }

            Assert.AreEqual(player.Equipment.QuickSlotCount, slots, "Число быстрых слотов");

            var title = GameObject.Find("QuickTitle");
            Assert.IsNotNull(title, "Нет заголовка «Быстрый доступ»");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Character_Quick_Title),
                title!.GetComponent<Text>().text,
                "Заголовок столбца берётся из каталога строк");

            var first = GameObject.Find("QuickSlot_0");
            Assert.IsNotNull(first, "Нет первого быстрого слота");
            Assert.IsNull(first!.GetComponent<ItemMenuTrigger>(), "В быстром слоте меню предмета не открывается");
        }

        /// <summary>Стимулятор из сетки встаёт в быстрый слот, а перетаскивание обратно возвращает его в сетку.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator QuickSlot_DragMedicine_AssignsAndReturns()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            Assert.IsTrue(
                screen.Inventory.TryAdd(player.Inventory, "itm_stim_adrenaline", 1, out var overflow) &&
                overflow.Count == 0,
                "Стимулятор не добавился в инвентарь");
            screen.ShowInventory();
            yield return null;

            var item = FindItem(player.Inventory, "itm_stim_adrenaline");
            Assert.IsNotNull(item, "Стимулятор не лёг в сетку");
            var itemObject = GameObject.Find("Item_" + item!.InstanceId);
            var slot = GameObject.Find("QuickSlot_0");
            Assert.IsNotNull(itemObject, "Плашка стимулятора не создана");
            Assert.IsNotNull(slot, "Нет первого быстрого слота");

            Drag(itemObject!, slot!);
            yield return null;

            Assert.IsNull(player.Inventory.Find(item.InstanceId), "Стимулятор остался в сетке");
            Assert.AreEqual(item.InstanceId, player.Equipment.GetQuick(0)?.InstanceId, "Стимулятор не попал в слот");

            var filled = GameObject.Find("QuickSlot_0");
            var frame = GameObject.Find("InventoryFrame");
            Assert.IsNotNull(filled, "Слот после назначения не перерисован");
            Assert.IsNotNull(frame, "Нет окна инвентаря для сброса");
            Drag(filled!, frame!);
            yield return null;

            Assert.IsNull(player.Equipment.GetQuick(0), "Предмет остался в быстром слоте");
            Assert.IsNotNull(FindItem(player.Inventory, "itm_stim_adrenaline"), "Предмет не вернулся в сетку");
        }

        private static ItemInstance? FindItem(GridContainer grid, string itemId)
        {
            foreach (var item in grid.Items)
            {
                if (item.ItemId == itemId)
                {
                    return item;
                }
            }

            return null;
        }

        private static void Drag(GameObject source, GameObject target)
        {
            var data = new PointerEventData(EventSystem.current)
            {
                position = ScreenCenter((RectTransform)source.transform),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(source, data, ExecuteEvents.beginDragHandler);
            data.position = ScreenCenter((RectTransform)target.transform);
            ExecuteEvents.Execute(source, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(target, data, ExecuteEvents.dropHandler);
        }
    }
}
