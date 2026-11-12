#nullable enable
using System;
using System.Collections;
using AnomalyPath.Core.Character;
using AnomalyPath.Core.Equipment;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Templates;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Проверяет единообразие сетки, перетаскивание и поворот предметов на экране персонажа.</summary>
    public sealed class CharacterInventoryPlayModeTests : PlayModeTestBase
    {
        /// <summary>Клетки сетки квадратные и считаются от ширины окна; число строк — сколько помещается.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_Grid_FillsFrame()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var grid = screen.Session!.State.Player!.Inventory;
            var root = GameObject.Find("InventoryGrid");
            var frame = GameObject.Find("InventoryFrame");
            var canvas = GameObject.Find("Canvas");
            Assert.IsNotNull(root, "Нет корня сетки InventoryGrid");
            Assert.IsNotNull(frame, "Нет окна инвентаря");
            Assert.IsNotNull(canvas, "Нет канваса");
            Assert.IsTrue(canvas!.GetComponent<Canvas>().pixelPerfect, "Канвас должен включать pixel perfect для чётких линий");

            foreach (var corner in new[] { "GridTop", "GridBottom", "GridLeft", "GridRight" })
            {
                Assert.IsNull(root!.transform.Find(corner), $"У сетки не должно быть внешнего контура: {corner}");
            }

            var areaObject = GameObject.Find("GridArea");
            Assert.IsNotNull(areaObject, "Нет области сетки GridArea");
            var scroll = areaObject!.GetComponent<ScrollArea>();
            Assert.IsNotNull(scroll, "У области сетки нет прокрутки");
            var area = new Vector2(scroll!.ViewportWidth, scroll.ViewportHeight);
            var side = Mathf.Max(
                CharacterMetrics.MinCellSize,
                InventoryGridArt.CellSide(area.x, grid.Width, CharacterMetrics.GridGap));
            var cell = new Vector2(side, side);
            var fitRows = InventoryGridArt.FitRows(area.y, side, CharacterMetrics.GridGap);
            var baseRows = screen.Rules.Require().BaseRows;
            var expectedRows = Math.Max(Math.Max(fitRows, grid.UsedRows), baseRows);
            var step = new Vector2(cell.x + CharacterMetrics.GridGap, cell.y + CharacterMetrics.GridGap);

            Assert.AreEqual(expectedRows, grid.Rows, "Число строк — сколько помещается в окно и занято вещами");
            var gridSize = InventoryGridArt.GridSize(grid.Width, grid.Rows, cell, CharacterMetrics.GridGap);
            var gridRect = (RectTransform)root!.transform;
            Assert.AreEqual(0f, gridRect.anchoredPosition.x, 0.01f,
                "Сетка начинается от левого края области прокрутки");
            Assert.AreEqual(0f, gridRect.anchoredPosition.y, 0.01f,
                "Сетка начинается от верхнего края области прокрутки");
            Assert.AreEqual(gridSize.x, gridRect.rect.width, 0.01f, "Сетка занимает ширину области прокрутки");
            Assert.AreEqual(gridSize.y, gridRect.rect.height, 0.01f, "Высота сетки — сторона клетки на число строк");
            Assert.AreEqual(gridSize.y, scroll.Content.rect.height, 0.01f, "Контент прокрутки по высоте сетки");
            Assert.LessOrEqual(gridSize.y, scroll.ViewportHeight + 0.01f,
                "Строки подобраны так, чтобы сетка помещалась без прокрутки");
            Assert.IsFalse(scroll.Bar.gameObject.activeSelf,
                "Сетка по размеру окна — полоса прокрутки не нужна");

            var cells = 0;
            foreach (Transform child in root.transform)
            {
                if (!child.name.StartsWith("Cell_"))
                {
                    continue;
                }

                var parts = child.name.Split('_');
                var column = int.Parse(parts[1]);
                var row = int.Parse(parts[2]);
                var rect = (RectTransform)child;
                Assert.AreEqual(side, rect.rect.width, 0.01f, "Ширина клетки");
                Assert.AreEqual(side, rect.rect.height, 0.01f, "Клетки должны быть квадратными");
                Assert.AreEqual(column * step.x, rect.anchoredPosition.x, 0.01f, "Шаг колонок");
                Assert.AreEqual(-row * step.y, rect.anchoredPosition.y, 0.01f, "Шаг строк");
                cells++;
            }

            Assert.AreEqual(grid.Width * grid.Rows, cells, "Число клеток должно совпадать с сеткой");

            var line = root.transform.Find("GridV1");
            Assert.IsNotNull(line, "Нет внутренней линии сетки");
            Assert.AreEqual(InventoryGridArt.LineThickness, ((RectTransform)line!).rect.width, 0.01f,
                "Толщина линии сетки");
            Assert.AreEqual(InventoryGridArt.LineColor.a, line.GetComponent<Image>().color.a, 0.01f,
                "Линия должна быть тусклой");
            Assert.Less(InventoryGridArt.LineColor.a, 1f, "Разметка не должна быть сплошной");
        }

        /// <summary>Когда предметы занимают строк больше, чем помещается, появляется полоса прокрутки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_Grid_ShowsScrollbarWhenContentOverflows()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            var grid = player.Inventory;
            var areaObject = GameObject.Find("GridArea");
            Assert.IsNotNull(areaObject, "Нет области сетки GridArea");
            var scroll = areaObject!.GetComponent<ScrollArea>();
            Assert.IsNotNull(scroll, "У области сетки нет прокрутки");
            var side = Mathf.Max(
                CharacterMetrics.MinCellSize,
                InventoryGridArt.CellSide(scroll!.ViewportWidth, grid.Width, CharacterMetrics.GridGap));
            var fitRows = InventoryGridArt.FitRows(scroll.ViewportHeight, side, CharacterMetrics.GridGap);

            Assert.IsTrue(screen.Items.TryGet("ammo_545", out var template), "Нет шаблона патронов 5.45");
            grid.Resize(fitRows + 2);
            var probe = new ItemInstance("scroll_probe", "ammo_545", 1);
            grid.Place(probe, template!, 0, fitRows + 1, false);

            screen.ShowInventory();
            yield return null;

            Assert.Greater(scroll.Content.rect.height, scroll.ViewportHeight + 0.5f,
                "Контент сетки должен быть выше области прокрутки");
            Assert.IsTrue(scroll.Bar.gameObject.activeSelf,
                "При переполнении окна должна появиться полоса прокрутки");
            Assert.AreEqual(fitRows + 2, grid.Rows, "Строки под содержимым сохраняются");
        }

        /// <summary>Патроны, брошенные на надетое оружие, заряжают магазин.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_DropAmmoOnEquippedWeapon_LoadsMagazine()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            var weapon = player.Equipment.Get(EquipmentSlot.Main);
            Assert.IsNotNull(weapon, "В основном слоте нет оружия");
            Assert.AreEqual(30, weapon!.MagazineRounds, "Стартовое оружие должно быть заряжено");
            Assert.AreEqual(
                CharacterActionResult.Ok,
                screen.Actions.Unload(player.Inventory, player.Equipment, weapon.InstanceId, out var unloaded),
                "Оружие не разрядилось");
            Assert.AreEqual(30, unloaded, "Из стартового магазина должно выйти 30 патронов");
            screen.ShowInventory();
            yield return null;

            Assert.AreEqual(0, weapon.MagazineRounds, "Перед зарядкой магазин должен быть пуст");
            var ammo = FindItem(player.Inventory, "ammo_545");
            Assert.IsNotNull(ammo, "В инвентаре нет патронов 5.45");
            var stack = ammo!.Stack;
            Assert.GreaterOrEqual(stack, 30, "Для зарядки нужно не меньше 30 патронов");

            var ammoObject = GameObject.Find("Item_" + ammo.InstanceId);
            var slotObject = GameObject.Find("SlotMain");
            Assert.IsNotNull(ammoObject, "Плашка патронов не создана");
            Assert.IsNotNull(slotObject, "Слот основного оружия не создан");

            var data = new PointerEventData(EventSystem.current)
            {
                position = ScreenCenter((RectTransform)ammoObject!.transform),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(ammoObject, data, ExecuteEvents.beginDragHandler);
            data.position = ScreenCenter((RectTransform)slotObject!.transform);
            ExecuteEvents.Execute(ammoObject, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(slotObject, data, ExecuteEvents.dropHandler);
            yield return null;

            Assert.AreEqual(30, weapon.MagazineRounds, "Магазин должен зарядиться до полного");
            Assert.AreEqual(stack - 30, ammo.Stack, "Из стопки должно уйти ровно 30 патронов");
        }

        /// <summary>Патроны, брошенные на оружие в сетке, заряжают магазин.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_DropAmmoOnWeaponInGrid_LoadsMagazine()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            var equipped = player.Equipment.Get(EquipmentSlot.Main);
            Assert.IsNotNull(equipped, "В основном слоте нет оружия");
            Assert.AreEqual(
                CharacterActionResult.Ok,
                screen.Actions.Unequip(player.Inventory, player.Equipment, EquipmentSlot.Main),
                "Оружие не снялось в инвентарь");
            screen.ShowInventory();
            yield return null;

            var weapon = FindItem(player.Inventory, "wpn_ak74");
            Assert.IsNotNull(weapon, "Оружие не попало в сетку");
            Assert.AreEqual(30, weapon!.MagazineRounds, "Снятое стартовое оружие должно быть заряжено");
            Assert.AreEqual(
                CharacterActionResult.Ok,
                screen.Actions.Unload(player.Inventory, player.Equipment, weapon.InstanceId, out _),
                "Оружие не разрядилось");
            screen.ShowInventory();
            yield return null;

            Assert.AreEqual(0, weapon.MagazineRounds, "Перед зарядкой магазин должен быть пуст");
            var ammo = FindItem(player.Inventory, "ammo_545");
            Assert.IsNotNull(ammo, "В инвентаре нет патронов 5.45");
            var stack = ammo!.Stack;
            Assert.GreaterOrEqual(stack, 30, "Для зарядки нужно не меньше 30 патронов");

            var ammoObject = GameObject.Find("Item_" + ammo.InstanceId);
            var weaponObject = GameObject.Find("Item_" + weapon.InstanceId);
            var gridObject = GameObject.Find("InventoryGrid");
            Assert.IsNotNull(ammoObject, "Плашка патронов не создана");
            Assert.IsNotNull(weaponObject, "Плашка оружия не создана");
            Assert.IsNotNull(gridObject, "Нет сетки для сброса");

            var data = new PointerEventData(EventSystem.current)
            {
                position = ScreenCenter((RectTransform)ammoObject!.transform),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(ammoObject, data, ExecuteEvents.beginDragHandler);
            data.position = ScreenCenter((RectTransform)weaponObject!.transform);
            ExecuteEvents.Execute(ammoObject, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(gridObject!, data, ExecuteEvents.dropHandler);
            yield return null;

            Assert.AreEqual(30, weapon.MagazineRounds, "Магазин должен зарядиться до полного");
            Assert.AreEqual(stack - 30, ammo.Stack, "Из стопки должно уйти ровно 30 патронов");
            Assert.IsNotNull(player.Inventory.Find(weapon.InstanceId), "Оружие должно остаться в сетке");
        }

        /// <summary>Двойной клик по еде в инвентаре применяет её и восполняет потребность.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_DoubleClickFood_RestoresHunger()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            Assert.IsTrue(screen.Items.TryGet("itm_food_stew", out var template), "Нет шаблона тушёнки");
            Assert.IsTrue(TryFindFreeArea(player.Inventory, template!, out var x, out var y), "В сетке нет места под тушёнку");

            var item = new ItemInstance("use_food_test", "itm_food_stew", 1);
            player.Inventory.Place(item, template!, x, y, false);
            player.Needs.SetHunger(10);
            screen.ShowInventory();
            yield return null;

            var itemObject = GameObject.Find("Item_use_food_test");
            Assert.IsNotNull(itemObject, "Плашка еды не создана");
            var data = new PointerEventData(EventSystem.current)
            {
                position = ScreenCenter((RectTransform)itemObject!.transform),
                button = PointerEventData.InputButton.Left,
                clickCount = 2
            };
            ExecuteEvents.Execute(itemObject!, data, ExecuteEvents.pointerClickHandler);
            yield return null;

            Assert.AreEqual(70, player.Needs.Hunger, "Тушёнка должна восполнить 60 еды");
            Assert.IsNull(player.Inventory.Find("use_food_test"), "Съеденная тушёнка должна исчезнуть из сетки");
        }

        /// <summary>Иконка артефакта в сетке занимает почти всю клетку, а подписи у неё нет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_ArtifactIcon_FillsCellWithoutLabel()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            Assert.IsTrue(screen.Items.TryGet("art_gravity_lens", out var template), "Нет шаблона артефакта");
            Assert.IsTrue(TryFindFreeArea(player.Inventory, template!, out var x, out var y), "В сетке нет места под артефакт");

            var item = new ItemInstance("use_artifact_test", "art_gravity_lens", 1);
            player.Inventory.Place(item, template!, x, y, false);
            screen.ShowInventory();
            yield return null;

            var box = GameObject.Find("Item_use_artifact_test");
            var icon = GameObject.Find("ItemIcon_use_artifact_test");
            Assert.IsNotNull(box, "Плашка артефакта не создана");
            Assert.IsNotNull(icon, "Иконка артефакта не создана");
            var boxRect = (RectTransform)box!.transform;
            var iconRect = (RectTransform)icon!.transform;
            Assert.Greater(iconRect.rect.height, boxRect.rect.height * 0.7f,
                "Иконка артефакта должна занимать почти всю клетку");
            Assert.IsNull(GameObject.Find("ItemLabel_use_artifact_test"), "У артефакта не должно быть подписи");
        }

        /// <summary>Пункт «Применить» в контекстном меню лечит здоровье и тратит заряд аптечки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_ContextMenuUseMedkit_RestoresHealth()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            Assert.IsTrue(screen.Items.TryGet("itm_medkit_normal", out var template), "Нет шаблона аптечки");
            Assert.IsTrue(TryFindFreeArea(player.Inventory, template!, out var x, out var y), "В сетке нет места под аптечку");

            var item = new ItemInstance("use_medkit_test", "itm_medkit_normal", 1, charges: 2);
            player.Inventory.Place(item, template!, x, y, false);
            player.SetHealth(1);
            screen.ShowInventory();
            yield return null;

            var itemObject = GameObject.Find("Item_use_medkit_test");
            Assert.IsNotNull(itemObject, "Плашка аптечки не создана");
            RightClick(itemObject!, ScreenCenter((RectTransform)itemObject!.transform));
            yield return null;

            var useButton = GameObject.Find("Применить");
            Assert.IsNotNull(useButton, "В контекстном меню нет пункта «Применить»");
            useButton!.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual(31, player.Health, "Аптечка должна вылечить 30 здоровья");
            Assert.AreEqual(1, item.Charges, "Одна аптечка тратит один заряд из двух");
            Assert.IsNotNull(player.Inventory.Find("use_medkit_test"), "Аптечка с остатком зарядов должна остаться");
        }

        /// <summary>У каждого типа урона из данных есть строка защиты со значением из сервиса.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterStats_Defense_HasRowForEachDamageType()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var state = screen!.Session!.State;
            Assert.Greater(screen.DamageTypes.All.Count, 0, "В данных нет типов урона");
            foreach (var damageType in screen.DamageTypes.All)
            {
                var row = GameObject.Find("StatRow_" + damageType.Id);
                Assert.IsNotNull(row, $"Нет строки защиты для {damageType.Id}");
                Assert.AreEqual(
                    screen.Damage.Protection(state, damageType.Id).ToString(),
                    StatValue(damageType.Id),
                    $"Защита от {damageType.Id} не совпадает с сервисом");
            }
        }

        /// <summary>Защита показывает базу актёра и снаряжение, а после снятия шлема число уменьшается.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterStats_Defense_DropsWhenHelmetRemoved()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            var rules = screen.Rules.Require();
            var actor = screen.Actors.Get(rules.PlayerActorId);
            var baseBullet = actor.Combat != null && actor.Combat.Protection.TryGetValue("dmg_bullet", out var value)
                ? value
                : 0;
            Assert.IsNotNull(player.Equipment.Get(EquipmentSlot.Helmet), "В стартовом наборе нет шлема");

            var before = baseBullet + screen.Equipment.GetProtection(player.Equipment, "dmg_bullet");
            Assert.AreEqual(before.ToString(), StatValue("dmg_bullet"), "Защита от пуль должна учитывать шлем");

            var plate = GameObject.Find("SlotHelmetPlate");
            Assert.IsNotNull(plate, "Нет плашки надетого шлема");
            RightClick(plate!, ScreenCenter((RectTransform)plate!.transform));
            yield return null;
            var unequip = GameObject.Find("Снять");
            Assert.IsNotNull(unequip, "В меню шлема нет пункта «Снять»");
            unequip!.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var after = baseBullet + screen.Equipment.GetProtection(player.Equipment, "dmg_bullet");
            Assert.Less(after, before, "Снятый шлем должен уменьшить защиту");
            Assert.AreEqual(after.ToString(), StatValue("dmg_bullet"), "Статистика не пересчитала защиту после снятия");
        }

        /// <summary>Кнопка-глаз скрывает и снова показывает панель статистики, приглушая иконку.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterStats_Eye_TogglesPanel()
        {
            yield return OpenCharacter();

            var panel = GameObject.Find("StatsPanel");
            var icon = GameObject.Find("StatsEyeIcon");
            Assert.IsNotNull(panel, "Нет панели статистики");
            Assert.IsNotNull(icon, "Нет иконки кнопки-глаза");
            Assert.IsNull(GameObject.Find("StatsMain"), "В панели осталась вкладка «Основные»");
            Assert.IsNull(GameObject.Find("StatRow_health"), "В панели осталась строка здоровья");
            Assert.IsTrue(panel!.activeSelf, "Панель статистики должна быть видна сразу");
            Assert.AreEqual(Color.white, icon!.GetComponent<Image>().color, "Иконка включённого глаза");

            Click("StatsEye");
            yield return null;
            Assert.IsFalse(panel.activeSelf, "Кнопка-глаз не скрыла панель");
            Assert.AreEqual(CharacterPalette.StatsEyeMuted, icon.GetComponent<Image>().color,
                "Иконка скрытого глаза должна быть приглушена");

            Click("StatsEye");
            yield return null;
            Assert.IsTrue(panel.activeSelf, "Кнопка-глаз не показала панель снова");
            Assert.AreEqual(Color.white, icon.GetComponent<Image>().color, "Иконка снова включённого глаза");
        }

        /// <summary>Панель статистики стоит слева от слотов и не заходит на инвентарь; глаз — над панелью.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterStats_Panel_IsLeftOfSlotsAndEyeIsAbove()
        {
            yield return OpenCharacter();

            var panel = GameObject.Find("StatsPanel");
            var helmet = GameObject.Find("SlotHelmet");
            var eye = GameObject.Find("StatsEye");
            var inventory = GameObject.Find("InventoryFrame");
            Assert.IsNotNull(panel, "Нет панели статистики");
            Assert.IsNotNull(helmet, "Нет слота шлема");
            Assert.IsNotNull(eye, "Нет кнопки-глаза");
            Assert.IsNotNull(inventory, "Нет окна инвентаря");

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel!.transform);
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)panel.transform.parent);
            Canvas.ForceUpdateCanvases();

            var panelRect = WorldRect((RectTransform)panel.transform);
            var helmetRect = WorldRect((RectTransform)helmet!.transform);
            var eyeRect = WorldRect((RectTransform)eye!.transform);
            var inventoryRect = WorldRect((RectTransform)inventory!.transform);

            Assert.Greater(((RectTransform)panel.transform).rect.height, 100f,
                "Панель защиты должна быть заполнена строками");
            Assert.Less(panelRect.xMax, helmetRect.xMin, "Панель статистики должна стоять слева от слотов");
            Assert.Greater(eyeRect.yMin, panelRect.yMax, "Кнопка-глаз должна стоять над панелью статистики");
            Assert.Less(panelRect.xMax, inventoryRect.xMin, "Панель статистики не должна заходить на инвентарь");
        }

        private static string StatValue(string code)
        {
            var value = GameObject.Find("StatValue_" + code);
            Assert.IsNotNull(value, $"Нет строки статистики {code}");
            return value!.GetComponent<Text>().text;
        }

        private static bool TryFindFreeArea(GridContainer grid, ItemTemplate template, out int x, out int y)
        {
            for (var row = 0; row + template.FootprintHeight <= grid.Rows; row++)
            {
                for (var column = 0; column + template.FootprintWidth <= grid.Width; column++)
                {
                    if (grid.CanPlace(template, column, row, false, null))
                    {
                        x = column;
                        y = row;
                        return true;
                    }
                }
            }

            x = 0;
            y = 0;
            return false;
        }

        private static ItemInstance? FindItem(GridContainer grid, string itemId)
        {
            foreach (var item in grid.Items)
            {
                if (!item.Overflow && item.ItemId == itemId)
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>Валюта стоит в верхней правой плашке окна инвентаря и равна деньгам игрока.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_Money_IsInTopRightPlate()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var frame = GameObject.Find("InventoryFrame");
            var money = GameObject.Find("InventoryMoney");
            Assert.IsNotNull(frame, "Нет окна инвентаря");
            Assert.IsNotNull(money, "Нет подписи валюты");

            var frameRect = WorldRect((RectTransform)frame.transform);
            var moneyRect = WorldRect((RectTransform)money.transform);
            Assert.Greater(moneyRect.center.x, frameRect.center.x + (frameRect.width * 0.15f),
                "Валюта должна быть в правой плашке окна");
            Assert.Greater(moneyRect.center.y, frameRect.yMin + (frameRect.height * 0.9f),
                "Валюта должна быть в верхней плашке окна");
            Assert.IsTrue(frameRect.Contains(moneyRect.center), "Валюта должна лежать внутри окна");

            var player = screen!.Session!.State.Player!;
            Assert.AreEqual(
                UiText.Format(screen.Texts, UiTextKey.Common_Money, player.Money),
                money!.GetComponent<Text>().text,
                "Валюта должна быть равна деньгам игрока");
        }

        /// <summary>Вес показан в верхней левой части окна и совпадает с суммой веса сетки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_Weight_IsInTopLeftAndMatches()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var frame = GameObject.Find("InventoryFrame");
            var label = GameObject.Find("InventoryWeight");
            Assert.IsNotNull(frame, "Нет окна инвентаря");
            Assert.IsNotNull(label, "Нет подписи веса");

            var frameRect = WorldRect((RectTransform)frame.transform);
            var labelRect = WorldRect((RectTransform)label.transform);
            Assert.Less(labelRect.center.x, frameRect.center.x, "Вес должен быть в левой части окна");
            Assert.Greater(labelRect.center.y, frameRect.yMin + (frameRect.height * 0.9f),
                "Вес должен быть в верхней части окна");
            Assert.IsTrue(frameRect.Contains(labelRect.center), "Вес должен лежать внутри окна");

            var player = screen!.Session!.State.Player!;
            var text = UiText.Format(
                screen.Texts,
                UiTextKey.Character_Weight,
                screen.Inventory.TotalWeight(player.Inventory),
                player.Inventory.WeightLimit);
            Assert.AreEqual(text, label!.GetComponent<Text>().text, "Вес должен совпадать с сеткой");
        }

        /// <summary>Предмет из сетки перетаскивается в свободную клетку с сохранением точки захвата.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_DragItem_ToCell_MovesItem()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var grid = screen.Session!.State.Player!.Inventory;
            var item = FindPlaced(grid, 1, 1);
            Assert.IsNotNull(item, "В стартовом инвентаре нет предмета 1×1");
            Assert.IsTrue(TryFindFreeCell(grid, out var targetX, out var targetY), "В сетке нет свободной клетки");

            var itemObject = GameObject.Find("Item_" + item!.InstanceId);
            var gridObject = GameObject.Find("InventoryGrid");
            var targetCell = GameObject.Find("Cell_" + targetX + "_" + targetY);
            Assert.IsNotNull(itemObject, "Плашка предмета не создана");
            Assert.IsNotNull(gridObject, "Нет сетки для сброса");
            Assert.IsNotNull(targetCell, "Целевая клетка не создана");

            var data = new PointerEventData(EventSystem.current)
            {
                position = ScreenCenter((RectTransform)itemObject!.transform),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(itemObject, data, ExecuteEvents.beginDragHandler);
            data.position = ScreenCenter((RectTransform)targetCell!.transform);
            ExecuteEvents.Execute(itemObject, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(gridObject!, data, ExecuteEvents.dropHandler);
            yield return null;

            Assert.AreEqual(targetX, item.X, "Колонка после перетаскивания");
            Assert.AreEqual(targetY, item.Y, "Строка после перетаскивания");
            Assert.AreSame(item, grid.ItemAt(targetX, targetY), "В целевой клетке должен лежать тот же предмет");
        }

        /// <summary>Во время перетаскивания предмет поворачивается, и сброс фиксирует поворот.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_RotateDuringDrag_CommitsRotation()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            var main = player.Equipment.Get(EquipmentSlot.Main);
            Assert.IsNotNull(main, "В основном слоте нет оружия");

            Assert.AreEqual(
                CharacterActionResult.Ok,
                screen.Actions.Unequip(player.Inventory, player.Equipment, EquipmentSlot.Main),
                "Оружие не снялось в инвентарь");
            screen.ShowInventory();
            yield return null;

            var grid = player.Inventory;
            var item = FindPlaced(grid, 2, 3);
            Assert.IsNotNull(item, "В инвентаре нет предмета 2×3");
            Assert.IsTrue(screen.Items.TryGet(item!.ItemId, out var template), "Нет шаблона предмета");
            Assert.IsTrue(template!.Rotatable, "Предмет должен поддерживать поворот");

            var initialRotated = item.Rotated;
            var expectedRotated = !initialRotated;
            Assert.IsTrue(
                TryFindRotatedTarget(grid, template, item.InstanceId, expectedRotated, out var targetX, out var targetY),
                "Нет свободного места под повёрнутый предмет");

            var itemObject = GameObject.Find("Item_" + item.InstanceId);
            var targetCell = GameObject.Find("Cell_" + (targetX + 1) + "_" + (targetY + 1));
            var gridObject = GameObject.Find("InventoryGrid");
            Assert.IsNotNull(itemObject, "Плашка оружия не создана");
            Assert.IsNotNull(targetCell, "Клетка под курсором не создана");
            Assert.IsNotNull(gridObject, "Нет сетки для сброса");

            var source = itemObject!.GetComponent<ItemDragSource>();
            Assert.IsNotNull(source, "У предмета нет источника перетаскивания");
            var data = new PointerEventData(EventSystem.current)
            {
                position = ScreenCenter((RectTransform)itemObject.transform),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(itemObject, data, ExecuteEvents.beginDragHandler);
            Assert.IsTrue(source!.Rotate(), "Поворот во время перетаскивания не сработал");
            data.position = ScreenCenter((RectTransform)targetCell!.transform);
            ExecuteEvents.Execute(itemObject, data, ExecuteEvents.dragHandler);
            ExecuteEvents.Execute(gridObject!, data, ExecuteEvents.dropHandler);
            yield return null;

            Assert.AreEqual(expectedRotated, item.Rotated, "Сброс не зафиксировал поворот");
            Assert.AreEqual(expectedRotated ? template.FootprintHeight : template.FootprintWidth, item.PlacedWidth, "Ширина после поворота");
            Assert.AreEqual(expectedRotated ? template.FootprintWidth : template.FootprintHeight, item.PlacedHeight, "Высота после поворота");
            Assert.AreEqual(targetX, item.X, $"Колонка после поворота: target={targetX}, item={item.X}");
            Assert.AreEqual(targetY, item.Y, $"Строка после поворота: target={targetY}, item={item.Y}");
        }

        /// <summary>Поворот доступен только во время перетаскивания и только для rotatable-предмета.</summary>
        [Test]
        public void ItemDragContext_ToggleRotation_OnlyWhileDraggingAndRotatable()
        {
            var context = new ItemDragContext();
            Assert.IsFalse(context.ToggleRotation(), "Без перетаскивания поворот запрещён");

            context.Begin("a", DragOrigin.Inventory, 2, 3, rotatable: true, rotated: false, 0.5f, 0.5f, Vector2.zero);
            Assert.AreEqual(2, context.EffectiveWidth, "Ширина до поворота");
            Assert.AreEqual(3, context.EffectiveHeight, "Высота до поворота");
            Assert.IsTrue(context.ToggleRotation(), "Поворот разрешённого предмета");
            Assert.AreEqual(3, context.EffectiveWidth, "Ширина после поворота");
            Assert.AreEqual(2, context.EffectiveHeight, "Высота после поворота");
            context.End();
            Assert.IsFalse(context.ToggleRotation(), "После окончания перетаскивания поворот запрещён");

            context.Begin("b", DragOrigin.Inventory, 2, 3, rotatable: false, rotated: false, 0.5f, 0.5f, Vector2.zero);
            Assert.IsFalse(context.ToggleRotation(), "Неповорачиваемый предмет не должен поворачиваться");
        }

        private static bool TryFindFreeCell(GridContainer grid, out int x, out int y)
        {
            for (var row = 0; row < grid.Rows; row++)
            {
                for (var column = 0; column < grid.Width; column++)
                {
                    if (grid.ItemAt(column, row) == null)
                    {
                        x = column;
                        y = row;
                        return true;
                    }
                }
            }

            x = 0;
            y = 0;
            return false;
        }

        private static ItemInstance? FindPlaced(GridContainer grid, int width, int height)
        {
            foreach (var item in grid.Items)
            {
                if (!item.Overflow && item.PlacedWidth == width && item.PlacedHeight == height)
                {
                    return item;
                }
            }

            return null;
        }

        private static bool TryFindRotatedTarget(
            GridContainer grid,
            ItemTemplate template,
            string instanceId,
            bool rotated,
            out int x,
            out int y)
        {
            for (var row = 0; row + 1 < grid.Rows; row++)
            {
                for (var column = 0; column + 1 < grid.Width; column++)
                {
                    if (grid.CanPlace(template, column, row, rotated, instanceId))
                    {
                        x = column;
                        y = row;
                        return true;
                    }
                }
            }

            x = 0;
            y = 0;
            return false;
        }

        /// <summary>Редкость предмета в сетке и в слоте экипировки рисует окантовку своего цвета.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator CharacterInventory_Rarity_DrawsBorder()
        {
            yield return OpenCharacter();

            var screen = FindScreen();
            var player = screen!.Session!.State.Player!;
            var variant = RarityVariantCodec.Format(Rarity.Unique);
            Assert.IsTrue(
                screen.Inventory.TryAdd(player.Inventory, "wpn_pm", 1, null, variant, out _),
                "ПМ с уникальной ступенью не добавился");

            ItemInstance? added = null;
            foreach (var item in player.Inventory.Items)
            {
                if (string.Equals(item.VariantKey, variant, StringComparison.Ordinal))
                {
                    added = item;
                    break;
                }
            }

            Assert.IsNotNull(added, "ПМ с уникальной ступенью не в инвентаре");
            screen.ShowInventory();
            yield return null;

            var box = GameObject.Find("Item_" + added!.InstanceId);
            Assert.IsNotNull(box, "Плашка предмета в сетке не создана");
            var border = box!.transform.Find("RarityBorderTop");
            Assert.IsNotNull(border, "У уникального предмета нет окантовки");
            Assert.AreEqual(RarityPalette.Unique, border!.GetComponent<Image>().color,
                "Цвет окантовки уникальной ступени");

            var slot = GameObject.Find("SlotMain");
            Assert.IsNotNull(slot, "Слот основного оружия не создан");
            var slotBorder = FindDescendant(slot!.transform, "RarityBorderTop");
            Assert.IsNotNull(slotBorder, "У надетого оружия нет окантовки по редкости шаблона");
            Assert.AreEqual(RarityPalette.Rare, slotBorder!.GetComponent<Image>().color,
                "АК-74 в основном слоте — редкая ступень");
        }

        private static Transform? FindDescendant(Transform root, string name)
        {
            foreach (Transform child in root)
            {
                if (child.name == name)
                {
                    return child;
                }

                var nested = FindDescendant(child, name);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private static IEnumerator OpenCharacter()
        {
            yield return LoadMain();
            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;
        }
    }
}
