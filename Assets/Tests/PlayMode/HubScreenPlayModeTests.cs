#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Screens.Game.Hub;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Большой хаб — общий экран локации; комната торговца открывается только при взаимодействии
    /// с торговцем: интерьер, выбранный торговец за стойкой, дверь в большой хаб, часы,
    /// настройки и инвентарь.
    /// </summary>
    public sealed class HubScreenPlayModeTests : PlayModeTestBase
    {
        /// <summary>После старта открывается большой хаб, а не комната торговца.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Start_ShowsBigHub()
        {
            yield return StartGame();

            Assert.IsNotNull(GameObject.Find("BigHub"), "Большой хаб не открылся");
            Assert.IsNull(GameObject.Find("InGame"), "В большом хабе не должно быть общего экрана локации");
            Assert.IsNull(GameObject.Find("Hub"), "Комната торговца не должна открываться на старте");
            Assert.IsNotNull(GameObject.Find("Map"), "В большом хабе нет карты мира");
            Assert.IsNotNull(GameObject.Find("QuestBoard"), "В большом хабе нет доски объявлений");
            Assert.IsNotNull(GameObject.Find("LeaveHub"), "В большом хабе нет двери");
            Assert.IsNotNull(GameObject.Find("Gear"), "В большом хабе нет шестерёнки");
            Assert.IsNotNull(GameObject.Find("Traders"), "В большом хабе нет значка торговцев");
            Assert.IsNotNull(GameObject.Find("Rest"), "В большом хабе нет значка отдыха");
            Assert.IsNotNull(GameObject.Find("Backpack"), "В большом хабе нет рюкзака");
            Assert.IsNotNull(GameObject.Find("PlayerXpBar"), "В большом хабе нет полосы опыта");
            Assert.IsNull(GameObject.Find("ActionPanel"), "Панели действий в большом хабе быть не должно");
            Assert.IsNull(GameObject.Find("LocationSummary"), "Сводки локации быть не должно");
            Assert.IsNull(GameObject.Find("Money"), "Денег на экране быть не должно");
        }

        /// <summary>Карта на стене открывает карту мира, доска — задания.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_WallMapAndBoard_OpenScreens()
        {
            yield return StartGame();

            Click("Map");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Map"), "Карта мира не открылась");

            FindScreen().ShowGame(false);
            yield return null;
            Click("QuestBoard");
            yield return null;
            Assert.IsNotNull(GameObject.Find("QuestBoardScreen"), "Доска объявлений не открылась");
        }

        /// <summary>Мешочек открывает выпадашку, карточка торговца — комнату торговца.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Traders_OpenTraderRoom()
        {
            yield return StartGame();

            Click("Traders");
            yield return null;
            Assert.IsNotNull(GameObject.Find("TraderMenu"), "Выпадашка торговцев не открылась");

            Click("TraderCard_trd_hub_smuggler");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Hub"), "Карточка торговца не открыла комнату");
            Assert.AreEqual(
                "trd_hub_smuggler",
                GameObject.Find("Hub")!.GetComponent<HubScreen>()!.TraderId,
                "Открылся не выбранный торговец");
        }

        /// <summary>В комнате торговца показывается интерьер, выбранный торговец и часы.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_TraderRoom_ShowsInteriorMerchantAndClock()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            var hub = GameObject.Find("Hub");
            Assert.IsNotNull(hub, "Комната торговца не открылась");
            Assert.IsNull(GameObject.Find("BigHub"), "В комнате торговца не должно быть большого хаба");
            Assert.IsNull(GameObject.Find("InGame"), "В комнате торговца не должно быть общего экрана");

            var interior = hub!.transform.Find("Content/Stage/Interior")?.GetComponent<Image>();
            Assert.IsNotNull(interior, "У комнаты торговца нет картинки интерьера");
            Assert.IsNotNull(interior!.sprite, "У интерьера нет фона");
            StringAssert.StartsWith("bg_", interior.sprite!.name, "Фон интерьера берётся из картинок локаций");

            var room = hub.GetComponent<HubScreen>();
            Assert.IsNotNull(room, "Панель комнаты торговца собрана не из префаба");
            Assert.AreEqual("trd_hub_smuggler", room!.TraderId, "За стойкой стоит выбранный торговец");
            StringAssert.Contains("10:00", room.TimeText, "Часы показывают время начала игры");

            var merchant = GameObject.Find("Merchant")?.GetComponent<Image>();
            Assert.IsNotNull(merchant, "В комнате нет картинки торговца");
            Assert.IsNotNull(merchant!.sprite, "У торговца нет спрайта");
            StringAssert.Contains("trd_hub_smuggler_full", merchant.sprite!.name, "Торговец показан не своей картинкой");

            Assert.IsNull(GameObject.Find("Money"), "Денег в комнате торговца нет");
            Assert.IsNull(GameObject.Find("PlayerXpBar"), "Полосы опыта в комнате торговца нет");
            Assert.IsNull(GameObject.Find("Map"), "Карты в комнате торговца нет");
            Assert.IsNull(GameObject.Find("QuestBoard"), "Доски объявлений в комнате торговца нет");
            Assert.IsNotNull(GameObject.Find("Traders"), "В комнате нет значка выбора торговцев");
            Assert.IsNull(GameObject.Find("Rest"), "Значка отдыха в комнате торговца нет");
        }

        /// <summary>Комната торговца ставит за стойку именно выбранного торговца.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_TraderRoom_ShowsSelectedTrader()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowTraderRoom("trd_hub_medic");
            yield return null;

            var room = GameObject.Find("Hub")!.GetComponent<HubScreen>()!;
            Assert.AreEqual("trd_hub_medic", room.TraderId, "За стойкой должен стоять фельдшер");
            var merchant = GameObject.Find("Merchant")!.GetComponent<Image>()!;
            StringAssert.Contains("trd_hub_medic_full", merchant.sprite!.name, "Показан не выбранный торговец");
        }

        /// <summary>В комнате мешочек открывает выбор торговцев и переключает комнату.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_TraderRoom_TradersSwitch()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            Click("Traders");
            yield return null;
            Assert.IsNotNull(GameObject.Find("TraderMenu"), "Выпадашка торговцев не открылась");
            var portrait = GameObject.Find("TraderCard_trd_hub_medic")
                ?.transform.Find("Portrait")?.GetComponent<Image>();
            Assert.IsNotNull(portrait, "У карточки торговца нет портрета");
            StringAssert.Contains("_thumb", portrait!.sprite!.name, "В карточке должен быть мини-портрет");

            Click("TraderCard_trd_hub_medic");
            yield return null;
            var room = GameObject.Find("Hub")!.GetComponent<HubScreen>()!;
            Assert.AreEqual("trd_hub_medic", room.TraderId, "Комната не переключилась на фельдшера");
            Assert.IsFalse(room.TradersShown, "Выпадашка должна закрыться после выбора");
        }

        /// <summary>Кнопка ремонта видна только у торговца с ремонтом.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_TraderRoom_RepairOnlyForMechanic()
        {
            yield return StartGame();
            var screen = FindScreen();

            screen.ShowTraderRoom("trd_hub_smuggler");
            yield return null;
            Assert.IsTrue(FindAnywhere("Repair")!.activeSelf, "У Контрабандиста нет кнопки ремонта");

            screen.ShowTraderRoom("trd_hub_medic");
            yield return null;
            Assert.IsFalse(FindAnywhere("Repair")!.activeSelf, "У Фельдшера не должно быть ремонта");
        }

        /// <summary>Ремонт — значок гаечного ключа без подписи рядом с инвентарём, имя — в подсказке.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_TraderRoom_Repair_IsWrenchIconNextToInventory()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            var stage = GameObject.Find("Hub")!.transform.Find("Content/Stage")!;
            var repair = stage.Find("Repair");
            Assert.IsNotNull(repair, "В комнате торговца нет значка ремонта");
            var repairTransform = repair!;
            Assert.IsTrue(repairTransform.gameObject.activeSelf, "У Контрабандиста значок ремонта должен быть виден");
            Assert.IsNull(repairTransform.Find("Label"), "У значка ремонта не должно быть подписи");
            var icon = repairTransform.Find("Icon")?.GetComponent<Image>();
            Assert.IsNotNull(icon, "У значка ремонта нет картинки");
            var iconImage = icon!;
            Assert.IsNotNull(iconImage.sprite, "У значка ремонта нет спрайта");
            StringAssert.Contains("ui_hub_wrench", iconImage.sprite!.name, "Ремонт должен показываться гаечным ключом");

            var stageRect = WorldRect((RectTransform)stage);
            var repairRect = WorldRect((RectTransform)repairTransform);
            var backpackRect = WorldRect((RectTransform)stage.Find("Backpack")!);
            Assert.AreEqual(backpackRect.yMin, repairRect.yMin, stageRect.height * 0.01f, "Значки стоят на одной линии");
            Assert.Less(repairRect.xMax, backpackRect.xMin, "Значок ремонта стоит слева от инвентаря");

            var room = GameObject.Find("Hub")!.GetComponent<HubScreen>()!;
            var zone = repairTransform.GetComponent<HubZone>();
            Assert.IsNotNull(zone, "У значка ремонта нет подсказки");
            var repairZone = zone!;
            repairZone.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return new WaitForSeconds(HubMetrics.TooltipDelay + 0.2f);
            var hubTooltip = room.Tooltip;
            Assert.IsNotNull(hubTooltip, "У комнаты торговца нет подсказки");
            Assert.IsTrue(hubTooltip!.IsShown, "Подсказка ремонта не появилась");
            Assert.AreEqual("Починить всё", hubTooltip.ShownText, "Подсказка показывает действие ремонта");
        }

        /// <summary>Дверь в покое без свечения, при наведении — со свечением.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Door_Hover_ShowsGlowArt()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            var zone = GameObject.Find("LeaveHub")!.GetComponent<HubZone>()!;
            var art = zone.transform.Find("Object")?.GetComponent<Image>();
            Assert.IsNotNull(art, "У двери нет картинки");
            Assert.IsNotNull(art!.sprite, "У двери нет спрайта");
            StringAssert.DoesNotContain("_glow", art.sprite!.name, "В покое дверь показана со свечением");

            zone.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return null;
            StringAssert.Contains("_glow", art.sprite!.name, "Наведение не включило свечение двери");

            zone.OnPointerExit(new PointerEventData(EventSystem.current));
            yield return null;
            StringAssert.DoesNotContain("_glow", art.sprite!.name, "Уход курсора не вернул дверь без свечения");
        }

        /// <summary>Клик по торговцу открывает диалог, а передний стол нарисован поверх фигуры.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Merchant_Click_OpensDialog()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            var hit = GameObject.Find("MerchantHit");
            Assert.IsNotNull(hit, "У торговца нет кликабельной зоны");
            Assert.IsTrue(hit!.GetComponent<Button>()!.interactable, "Зона торговца недоступна");

            var merchant = GameObject.Find("Merchant");
            var counter = GameObject.Find("Counter");
            Assert.IsNotNull(merchant, "В комнате нет фигуры торговца");
            Assert.IsNotNull(counter, "В комнате нет передней стойки");
            Assert.Greater(
                counter!.transform.GetSiblingIndex(),
                merchant!.transform.GetSiblingIndex(),
                "Стойка должна рисоваться поверх фигуры торговца");

            var room = GameObject.Find("Hub")!.GetComponent<HubScreen>()!;
            Assert.IsNotNull(room.Dialog, "У комнаты нет диалога торговца");
            room.Dialog!.Hide();
            yield return null;
            Assert.IsNull(GameObject.Find("TraderDialog"), "Диалог должен скрыться перед проверкой клика");

            Click("MerchantHit");
            yield return null;
            Assert.IsNotNull(GameObject.Find("TraderDialog"), "Клик по торговцу не открыл диалог");
        }

        /// <summary>Шестерёнка открывает меню игры, рюкзак — окно инвентаря.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_GearAndBackpack_OpenMenuAndInventory()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            Click("Gear");
            yield return null;
            Assert.IsNotNull(GameObject.Find("GameMenu"), "Шестерёнка не открыла меню игры");

            Click("Продолжить");
            yield return null;
            Assert.IsNotNull(GameObject.Find("BigHub"), "Меню не вернуло в большой хаб");

            screen.ShowTraderRoom("trd_hub_smuggler");
            yield return null;
            Click("Backpack");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Inventory"), "Рюкзак не открыл окно инвентаря");
        }

        /// <summary>Настройки стоят внизу слева, инвентарь — внизу справа, как в прототипе комнаты.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_GearAndBackpack_SitInBottomCorners()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            var hub = GameObject.Find("Hub")!.transform;
            var stage = WorldRect((RectTransform)hub.Find("Content/Stage")!);
            var gear = WorldRect((RectTransform)hub.Find("Content/Stage/Gear")!);
            var backpack = WorldRect((RectTransform)hub.Find("Content/Stage/Backpack")!);

            Assert.Less(gear.center.x, stage.center.x, "Настройки должны стоять слева");
            Assert.Greater(backpack.center.x, stage.center.x, "Инвентарь должен стоять справа");
            Assert.AreEqual(gear.yMin, backpack.yMin, stage.height * 0.01f, "Значки стоят на одной линии");
            Assert.Less((gear.yMin - stage.yMin) / stage.height, 0.1f, "Настройки должны быть внизу");
            Assert.Less((gear.xMin - stage.xMin) / stage.width, 0.1f, "Настройки должны быть у левого края");
            Assert.Less((stage.xMax - backpack.xMax) / stage.width, 0.1f, "Инвентарь должен быть у правого края");
        }

        /// <summary>Название объекта появляется подсказкой только после задержки наведения.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Tooltip_ShowsNameAfterDelay()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;
            var screen = GameObject.Find("Hub")!.GetComponent<HubScreen>()!;
            var zone = GameObject.Find("LeaveHub")!.GetComponent<HubZone>()!;

            zone.OnPointerEnter(new PointerEventData(EventSystem.current));
            yield return null;
            Assert.IsFalse(screen.Tooltip!.IsShown, "Подсказка не должна появляться мгновенно");

            yield return new WaitForSeconds(HubMetrics.TooltipDelay + 0.2f);
            Assert.IsTrue(screen.Tooltip.IsShown, "Подсказка не появилась после задержки");
            Assert.AreEqual("Выйти в хаб", screen.Tooltip.ShownText, "Подсказка показывает действие двери");

            zone.OnPointerExit(new PointerEventData(EventSystem.current));
            yield return null;
            Assert.IsFalse(screen.Tooltip.IsShown, "Подсказка не скрылась после ухода курсора");
        }

        /// <summary>Esc возвращает из комнаты торговца в большой хаб.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Escape_ReturnsToBigHub()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            Assert.IsTrue(FindScreen().HandleEscape(), "Esc должен вернуть в большой хаб");
            Assert.IsNotNull(GameObject.Find("BigHub"), "Esc не вернул в большой хаб");
            Assert.IsNull(GameObject.Find("GameMenu"), "Esc не должен открывать меню игры из комнаты торговца");
        }

        /// <summary>Дверь возвращает из комнаты торговца в большой хаб, а не на маршрут.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Door_ReturnsToBigHub()
        {
            yield return StartGame();
            var screen = FindScreen();
            var world = screen.Session!.State.World;
            screen.ShowTraderRoom("trd_hub_smuggler");
            yield return null;

            Click("LeaveHub");
            yield return null;

            Assert.IsNotNull(GameObject.Find("BigHub"), "Дверь не вернула в большой хаб");
            Assert.IsNull(GameObject.Find("Hub"), "Комната торговца осталась открытой");
            Assert.AreEqual(AnomalyPath.Core.World.LocationSubtype.Hub, world.CurrentLocation.Subtype, "Игрок остался в хабе");
        }
    }
}
