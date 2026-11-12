#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Подсказка предмета на экране персонажа: описание при наведении на надетую вещь и предмет
    /// в сетке, характеристики без описания, удержание в границах экрана и скрытие при уходе
    /// курсора или начале перетаскивания.
    /// </summary>
    public sealed class InventoryTooltipPlayModeTests : PlayModeTestBase
    {
        /// <summary>Наведение на надетое оружие показывает название, описание и характеристики.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator InventoryTooltip_EquippedWeapon_ShowsDescriptionAndFacts()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var slot = GameObject.Find("SlotMainPlate");
            Assert.IsNotNull(slot, "Нет плашки основного оружия SlotMainPlate");
            Hover(slot!, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            var tooltip = GameObject.Find("ItemTooltip");
            Assert.IsNotNull(tooltip, "Подсказка не показана при наведении на оружие");
            Assert.AreEqual("АК-74", Title(tooltip!), "Заголовок подсказки — название оружия");
            StringAssert.Contains(
                "Потёртый автомат",
                Description(tooltip!),
                "В подсказке должно быть описание из каталога строк");
            var facts = Facts(tooltip!);
            StringAssert.Contains("Урон: 8", facts, "В подсказке должен быть урон оружия");
            StringAssert.Contains("Магазин: 30", facts, "В подсказке должна быть вместимость магазина");
            StringAssert.Contains("Калибр: 5.45x39", facts, "В подсказке должен быть калибр");
            StringAssert.Contains("Вес: 3.7", facts, "В подсказке должен быть вес");

            Leave(slot!);
            yield return null;
            Assert.IsNull(GameObject.Find("ItemTooltip"), "Подсказка должна скрыться, когда курсор ушёл");
        }

        /// <summary>У предмета без описания подсказка показывает только характеристики.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator InventoryTooltip_ItemWithoutDescription_ShowsFactsOnly()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var slot = GameObject.Find("SlotHelmetPlate");
            Assert.IsNotNull(slot, "Нет плашки шлема SlotHelmetPlate");
            Hover(slot!, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            var tooltip = GameObject.Find("ItemTooltip");
            Assert.IsNotNull(tooltip, "Подсказка не показана при наведении на шлем");
            var description = tooltip!.transform.Find("Description");
            Assert.IsNotNull(description, "В подсказке нет строки описания");
            Assert.IsFalse(description!.gameObject.activeSelf, "Без описания строка не показывается");
            var facts = Facts(tooltip);
            StringAssert.Contains("Защита (", facts, "В подсказке должна быть защита шлема");
            StringAssert.Contains("Прочность:", facts, "В подсказке должна быть прочность шлема");
        }

        /// <summary>Подсказка предмета в сетке инвентаря показывает название и характеристики.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator InventoryTooltip_GridItem_ShowsTooltip()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var grid = GameObject.Find("InventoryGrid");
            Assert.IsNotNull(grid, "Нет сетки инвентаря");
            var item = FindByPrefix(grid!.transform, "Item_");
            Assert.IsNotNull(item, "В стартовом инвентаре нет ни одного предмета");
            Hover(item!, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            var tooltip = GameObject.Find("ItemTooltip");
            Assert.IsNotNull(tooltip, "Подсказка не показана при наведении на предмет в сетке");
            Assert.IsNotEmpty(Title(tooltip!), "У подсказки должен быть заголовок");
            StringAssert.Contains("Вес:", Facts(tooltip!), "Вес есть у любого предмета");
        }

        /// <summary>Подсказка не выходит за края экрана, даже когда курсор у самого угла.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator InventoryTooltip_NearScreenCorner_StaysInsideScreen()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var slot = GameObject.Find("SlotMainPlate");
            Assert.IsNotNull(slot, "Нет плашки основного оружия SlotMainPlate");
            Hover(slot!, new Vector2(1f, Screen.height - 1f));
            yield return null;

            var tooltip = GameObject.Find("ItemTooltip");
            Assert.IsNotNull(tooltip, "Подсказка не показана");
            var rect = WorldRect((RectTransform)tooltip!.transform);
            Assert.GreaterOrEqual(rect.xMin, -0.5f, "Подсказка вышла за левый край экрана");
            Assert.GreaterOrEqual(rect.yMin, -0.5f, "Подсказка вышла за нижний край экрана");
            Assert.LessOrEqual(rect.xMax, Screen.width + 0.5f, "Подсказка вышла за правый край экрана");
            Assert.LessOrEqual(rect.yMax, Screen.height + 0.5f, "Подсказка вышла за верхний край экрана");
        }

        /// <summary>Начало перетаскивания скрывает подсказку, чтобы она не мешала вести предмет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator InventoryTooltip_DragStart_HidesTooltip()
        {
            yield return StartGame();
            OpenInventory();
            yield return null;

            var slot = GameObject.Find("SlotMainPlate");
            Assert.IsNotNull(slot, "Нет плашки основного оружия SlotMainPlate");
            var data = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(slot!, data, ExecuteEvents.pointerEnterHandler);
            yield return null;
            Assert.IsNotNull(GameObject.Find("ItemTooltip"), "Подсказка не показана до перетаскивания");

            ExecuteEvents.Execute(slot!, data, ExecuteEvents.beginDragHandler);
            yield return null;
            Assert.IsNull(GameObject.Find("ItemTooltip"), "Начало перетаскивания должно скрыть подсказку");
            ExecuteEvents.Execute(slot!, data, ExecuteEvents.endDragHandler);
        }

        private static void Hover(GameObject target, Vector2 position)
        {
            var data = new PointerEventData(EventSystem.current) { position = position };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerEnterHandler);
        }

        private static void Leave(GameObject target)
        {
            var data = new PointerEventData(EventSystem.current) { position = Vector2.zero };
            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerExitHandler);
        }

        private static string Title(GameObject tooltip) => TextOf(tooltip, "Title");

        private static string Description(GameObject tooltip) => TextOf(tooltip, "Description");

        private static string Facts(GameObject tooltip) => TextOf(tooltip, "Facts");

        private static string TextOf(GameObject tooltip, string child)
        {
            var found = tooltip.transform.Find(child);
            Assert.IsNotNull(found, $"В подсказке нет строки {child}");
            var text = found!.GetComponent<Text>();
            Assert.IsNotNull(text, $"У строки {child} нет Text");
            return text!.text;
        }
    }
}
