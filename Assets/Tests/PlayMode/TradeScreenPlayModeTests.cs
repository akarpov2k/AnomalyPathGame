#nullable enable
using System.Collections;
using AnomalyPath.Core.Trade;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Проверяет новое окно торговли: товары торговца слева, инвентарь игрока справа,
    /// подсказка предмета при наведении, выбор предмета, количество, покупку, продажу
    /// и закрытие крестиком.
    /// </summary>
    public sealed class TradeScreenPlayModeTests : PlayModeTestBase
    {
        private const string TraderId = "trd_hub_smuggler";

        /// <summary>Окно открывается кнопкой хоста, показывает торговца и обе сетки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_Show_OpensWindowWithTwoGrids()
        {
            yield return StartGame();
            FindScreen().ShowTrade(TraderId);
            yield return null;

            var panel = GameObject.Find("Trade");
            Assert.IsNotNull(panel, "Окно торговли не открылось");
            Assert.AreEqual("Контрабандист", Label("Title"), "Заголовок — имя торговца");
            Assert.IsNotNull(FindByPrefix(panel!.transform, "TradeItem_"), "Слева нет товаров торговца");
            Assert.IsNotNull(FindByPrefix(panel.transform, "PlayerItem_"), "Справа нет предметов игрока");

            var trader = WorldRect((RectTransform)FindByPrefix(panel.transform, "TraderPanel")!.transform);
            var player = WorldRect((RectTransform)FindByPrefix(panel.transform, "PlayerPanel")!.transform);
            Assert.Less(trader.center.x, player.center.x, "Товары торговца стоят слева, инвентарь игрока — справа");
        }

        /// <summary>Хаб без комнаты торговца открывает префабное окно торговли, а не старый экран.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator ShowTraderRoom_NoRoom_OpensTradeScreen()
        {
            yield return StartGame();
            var host = FindScreen();
            var travel = host.Sessions.LeaveHub(host.Session!);
            Assert.IsTrue(travel.Succeeded, "Не удалось выйти из хаба на маршрут");

            host.ShowTraderRoom(TraderId);
            yield return null;

            Assert.IsNotNull(GameObject.Find("Trade"), "Хаб без комнаты должен открыть окно торговли");
            Assert.IsNull(GameObject.Find("Hub"), "Комната торговца не должна открываться");
        }

        /// <summary>Выбор товара показывает цену, количество и кнопку «Купить» с итогом.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_SelectItem_ShowsPriceAndQuantity()
        {
            yield return StartGame();
            FindScreen().ShowTrade(TraderId);
            yield return null;

            ClickItem("TradeItem_wpn_pm");
            yield return null;

            Assert.AreEqual("Пистолет Макарова", Label("SelectedName"), "Название выбранного");
            Assert.AreEqual("275 руб / шт", Label("SelectedPrice"), "Цена за штуку");
            Assert.AreEqual("1", Label("Quantity"), "Количество по умолчанию");
            StringAssert.Contains("Купить", Label("TradeAction/Label", true), "Кнопка покупки");

            Click("TradePlus");
            yield return null;
            Assert.AreEqual("2", Label("Quantity"), "Количество увеличилось");
            StringAssert.Contains("550", Label("TradeAction/Label", true), "Итоговая цена за два");

            Click("TradeMax");
            yield return null;
            Assert.AreEqual("3", Label("Quantity"), "Максимум — остаток торговца");
        }

        /// <summary>Покупка списывает деньги и кладёт предмет в инвентарь.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_Buy_SpendsMoneyAndAddsItem()
        {
            yield return StartGame();
            var host = FindScreen();
            host.ShowTrade(TraderId);
            yield return null;

            ClickItem("TradeItem_ammo_9x18");
            yield return null;

            var player = host.Session!.State.Player!;
            var moneyBefore = player.Money;
            var ammoBefore = RequirementsTemplate.CountItem(player, "ammo_9x18");

            Click("TradePlus");
            yield return null;
            Click("TradeAction");
            yield return null;

            Assert.AreEqual(moneyBefore - 12, player.Money, "Деньги списаны за два патрона");
            Assert.AreEqual(ammoBefore + 2, RequirementsTemplate.CountItem(player, "ammo_9x18"), "Патроны куплены");
            StringAssert.Contains("Куплено", Label("TradeToast"), "Сообщение о покупке");
        }

        /// <summary>Нажатие на свой предмет меняет кнопку на «Продать» и продаёт стопку.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_SelectPlayerItem_SellsStack()
        {
            yield return StartGame();
            var host = FindScreen();
            host.ShowTrade(TraderId);
            yield return null;

            var offers = host.Trade.GetSellOffers(host.Session!, TraderId);
            Assert.IsNotEmpty(offers, "Торговец не готов купить ни одного предмета игрока");
            var instanceId = offers[0].InstanceId;
            ClickItem("PlayerItem_" + instanceId);
            yield return null;

            StringAssert.Contains("Продать", Label("TradeAction/Label", true), "Кнопка переключилась на продажу");

            var player = host.Session!.State.Player!;
            var moneyBefore = player.Money;
            Click("TradeAction");
            yield return null;

            Assert.Greater(player.Money, moneyBefore, "Деньги выросли после продажи");
            StringAssert.Contains("Продано", Label("TradeToast"), "Сообщение о продаже");
        }

        /// <summary>Наведение на товар торговца показывает подсказку по данным шаблона.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_HoverTraderItem_ShowsTooltip()
        {
            yield return StartGame();
            FindScreen().ShowTrade(TraderId);
            yield return null;

            var item = GameObject.Find("TradeItem_wpn_pm");
            Assert.IsNotNull(item, "Нет товара wpn_pm");
            Hover(item!, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            var tooltip = GameObject.Find("ItemTooltip");
            Assert.IsNotNull(tooltip, "Наведение на товар не показало подсказку");
            StringAssert.Contains("Пистолет Макарова", TextOf(tooltip!, "Title"), "Заголовок — название товара");
            var facts = TextOf(tooltip!, "Facts");
            StringAssert.Contains("Вес:", facts, "В подсказке товара должен быть вес");
            StringAssert.Contains("Урон:", facts, "В подсказке оружия должен быть урон");
            StringAssert.Contains("Калибр:", facts, "В подсказке оружия должен быть калибр");

            Leave(item!);
            yield return null;
            Assert.IsNull(GameObject.Find("ItemTooltip"), "Подсказка должна скрыться, когда курсор ушёл");
        }

        /// <summary>Наведение на свой предмет в окне торговли показывает подсказку экземпляра.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_HoverPlayerItem_ShowsTooltip()
        {
            yield return StartGame();
            FindScreen().ShowTrade(TraderId);
            yield return null;

            var panel = GameObject.Find("Trade");
            Assert.IsNotNull(panel, "Окно торговли не открылось");
            var item = FindByPrefix(panel!.transform, "PlayerItem_");
            Assert.IsNotNull(item, "В инвентаре игрока нет предметов");
            Hover(item!, new Vector2(Screen.width * 0.5f, Screen.height * 0.5f));
            yield return null;

            var tooltip = GameObject.Find("ItemTooltip");
            Assert.IsNotNull(tooltip, "Наведение на свой предмет не показало подсказку");
            Assert.IsNotEmpty(TextOf(tooltip!, "Title"), "У подсказки должен быть заголовок");
            StringAssert.Contains("Вес:", TextOf(tooltip!, "Facts"), "Вес есть у любого предмета");
        }

        /// <summary>Крестик закрывает окно и возвращает в хаб.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_Close_ReturnsToGame()
        {
            yield return StartGame();
            FindScreen().ShowTrade(TraderId);
            yield return null;

            Click("TradeClose");
            yield return null;

            Assert.IsNull(GameObject.Find("Trade"), "Окно торговли не закрылось");
            Assert.IsNotNull(GameObject.Find("BigHub"), "После закрытия должен открыться хаб");
        }

        /// <summary>Esc закрывает окно торговли.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Trade_Escape_ClosesWindow()
        {
            yield return StartGame();
            var host = FindScreen();
            host.ShowTrade(TraderId);
            yield return null;

            Assert.IsTrue(host.HandleEscape(), "Esc должен обработать окно торговли");
            yield return null;
            Assert.IsNull(GameObject.Find("Trade"), "Esc не закрыл окно торговли");
        }

        private static void ClickItem(string name)
        {
            var item = GameObject.Find(name);
            Assert.IsNotNull(item, $"Нет предмета «{name}»");
            item!.GetComponent<Button>().onClick.Invoke();
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

        private static string TextOf(GameObject tooltip, string child)
        {
            var found = tooltip.transform.Find(child);
            Assert.IsNotNull(found, $"В подсказке нет строки {child}");
            var text = found!.GetComponent<Text>();
            Assert.IsNotNull(text, $"У строки {child} нет Text");
            return text!.text;
        }

        private static string Label(string name, bool nested = false)
        {
            var panel = GameObject.Find("Trade");
            Assert.IsNotNull(panel, "Окно торговли не открыто");
            Transform? target;
            if (nested)
            {
                var parts = name.Split('/');
                target = FindDeep(panel!.transform, parts[0]);
                for (var index = 1; index < parts.Length && target != null; index++)
                {
                    target = target.Find(parts[index]);
                }
            }
            else
            {
                target = FindDeep(panel!.transform, name);
            }

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
    }
}
