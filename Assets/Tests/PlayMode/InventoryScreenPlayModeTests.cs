#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Окно «инвентарь» по прототипу: персонаж слева (слоты, фигура, состояние, ник), инвентарь
    /// справа с сеткой и фильтрами внизу; открывается кнопкой нижней полосы, закрывается крестиком.
    /// </summary>
    public sealed class InventoryScreenPlayModeTests : PlayModeTestBase
    {
        /// <summary>Кнопка «Инвентарь» открывает окно с сеткой, слотами и фильтрами.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_OpensFromBottomBar()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;

            var panel = GameObject.Find("Inventory");
            Assert.IsNotNull(panel, "Окно инвентаря не открылось");
            Assert.IsTrue(panel!.activeSelf, "Окно инвентаря скрыто");
            Assert.IsNotNull(GameObject.Find("InventoryFilterAll"), "Нет фильтра «Все»");
            Assert.IsNotNull(GameObject.Find("CharacterPanel"), "Нет панели персонажа");
            Assert.IsNotNull(GameObject.Find("SlotHelmet"), "Нет слота шлема");
            Assert.Greater(CountItems(), 0, "В сетке нет предметов");
        }

        /// <summary>Панель персонажа стоит слева, окно инвентаря — справа, как в прототипе.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_CharacterPanel_IsLeftOfInventory()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;

            var character = GameObject.Find("CharacterPanel");
            var inventory = GameObject.Find("InventoryFrame");
            Assert.IsNotNull(character, "Нет панели персонажа");
            Assert.IsNotNull(inventory, "Нет окна инвентаря");
            var characterRect = WorldRect((RectTransform)character!.transform);
            var inventoryRect = WorldRect((RectTransform)inventory!.transform);

            Assert.Less(characterRect.xMax, inventoryRect.xMin + 1f, "Панель персонажа должна стоять левее инвентаря");
            Assert.Greater(characterRect.width, inventoryRect.width,
                "По прототипу панель персонажа шире окна инвентаря");
            var ratio = characterRect.width / inventoryRect.width;
            Assert.That(ratio, Is.EqualTo(983f / 465f).Within(0.35f),
                $"Пропорции панелей: персонаж {characterRect.width}, инвентарь {inventoryRect.width}, канвас {(RectTransform)character.transform.root}");
        }

        /// <summary>Фильтры стоят внизу окна инвентаря, под сеткой.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_Filters_SitUnderGrid()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;

            var filter = GameObject.Find("InventoryFilterAll");
            var area = GameObject.Find("GridArea");
            Assert.IsNotNull(filter, "Нет фильтра «Все»");
            Assert.IsNotNull(area, "Нет области сетки");
            var filterRect = WorldRect((RectTransform)filter!.transform);
            var areaRect = WorldRect((RectTransform)area!.transform);

            Assert.LessOrEqual(filterRect.yMax, areaRect.yMin + 1f, "Фильтры должны быть под сеткой");
        }

        /// <summary>Фильтр по категории не показывает больше предметов, чем «Все».</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_Filter_HidesOtherCategories()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;
            var all = CountItems();

            Click("InventoryFilterConsumables");
            yield return null;

            Assert.LessOrEqual(CountItems(), all, "Фильтр показал больше предметов, чем «Все»");
        }

        /// <summary>Ник, полоса состояния и фигура персонажа заполняются на том же окне.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_ShowsCharacterStateAndNickname()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;

            var nickname = GameObject.Find("Nickname");
            Assert.IsNotNull(nickname, "Нет ника на панели персонажа");
            Assert.IsFalse(string.IsNullOrWhiteSpace(nickname!.GetComponent<UnityEngine.UI.Text>().text),
                "Ник должен быть заполнен");
            Assert.IsNotNull(GameObject.Find("Figure"), "Нет фигуры персонажа");
            foreach (var key in new[] { "Health", "Food", "Water", "Stamina" })
            {
                var value = GameObject.Find("Stat" + key + "Value");
                Assert.IsNotNull(value, "Нет значения характеристики " + key);
                StringAssert.Contains("/", value!.GetComponent<UnityEngine.UI.Text>().text,
                    "Значение " + key + " должно быть числом вида 100/100");
            }
        }

        /// <summary>Крестик закрывает окно и возвращает игровое поле.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_Close_ReturnsToGame()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;
            Click("InventoryClose");
            yield return null;

            var closed = FindAnywhere("Inventory");
            Assert.IsNotNull(closed, "Панель инвентаря пропала");
            Assert.IsFalse(closed!.activeSelf, "Окно инвентаря не закрылось");
            Assert.IsNotNull(GamePanel(), "Игровое поле не вернулось");
        }

        /// <summary>
        /// Слоты и панель персонажа обрамлены рамкой из текстуры ржавой панели, как окна диалогов:
        /// у слотов 9-slice-спрайт, у панели — спрайт с прозрачностью.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Inventory_Slots_HaveRustFrames()
        {
            yield return StartGame();
            FindScreen().ShowGame(false);
            yield return null;

            OpenInventory();
            yield return null;

            foreach (var name in new[] { "SlotHelmet", "SlotArmor", "ArtifactSlot_0", "QuickSlot_0" })
            {
                var slot = GameObject.Find(name);
                Assert.IsNotNull(slot, "Нет слота " + name);
                var image = slot!.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(image, "У слота " + name + " нет изображения");
                Assert.IsNotNull(image!.sprite, "У слота " + name + " нет рамки-текстуры");
                Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, image.type,
                    "Рамка слота " + name + " должна растягиваться 9-slice");
                Assert.Greater(image.color.a, 0.9f, "Рамка слота " + name + " должна быть видимой");
            }

            var panel = GameObject.Find("CharacterPanel");
            Assert.IsNotNull(panel, "Нет панели персонажа");
            var panelImage = panel!.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(panelImage, "У панели персонажа нет изображения");
            Assert.IsNotNull(panelImage!.sprite, "У панели персонажа нет текстуры-рамы");
            Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, panelImage.type,
                "Рама панели персонажа должна растягиваться 9-slice");

            var stats = GameObject.Find("StatsPanel");
            Assert.IsNotNull(stats, "Нет панели характеристик");
            var statsImage = stats!.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(statsImage, "У панели характеристик нет изображения");
            Assert.IsNotNull(statsImage!.sprite, "У панели характеристик нет текстуры-рамы");
            Assert.AreEqual(UnityEngine.UI.Image.Type.Sliced, statsImage.type,
                "Рама панели характеристик должна растягиваться 9-slice");
        }

        /// <summary>Считает нарисованные предметы сетки по их именам.</summary>
        /// <returns>Число предметов на экране.</returns>
        private static int CountItems()
        {
            var grid = GameObject.Find("InventoryGrid");
            if (grid == null)
            {
                return 0;
            }

            var total = 0;
            foreach (Transform child in grid.transform)
            {
                if (child.name.StartsWith("Item_", System.StringComparison.Ordinal))
                {
                    total++;
                }
            }

            return total;
        }
    }
}
