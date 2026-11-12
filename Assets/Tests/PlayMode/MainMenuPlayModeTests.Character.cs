#nullable enable
using System.Collections;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Items;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="MainMenuPlayModeTests"/>: экран персонажа.</summary>
    public sealed partial class MainMenuPlayModeTests
    {
        /// <summary>Правый клик по слоту открывает меню, «Снять» возвращает предмет в инвентарь.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Character_RightClickSlot_ShowsMenuAndUnequips()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;

            var slot = GameObject.Find("SlotHelmet");
            Assert.IsNotNull(slot, "Нет слота шлема");
            var trigger = slot!.GetComponentInChildren<ItemMenuTrigger>(true);
            Assert.IsNotNull(trigger, "У слота нет меню предмета");
            RightClick(trigger!.gameObject, new Vector2(400f, 300f));
            yield return null;

            Assert.IsNotNull(GameObject.Find("ItemMenu"), "Меню не открылось");
            var unequip = GameObject.Find("Снять");
            Assert.IsNotNull(unequip, "В меню нет пункта «Снять»");
            unequip.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var value = GameObject.Find("SlotHelmetValue");
            Assert.IsNotNull(value, "Нет подписи слота шлема");
            Assert.AreEqual("—", value.GetComponent<Text>().text, "Шлем не снялся");
        }

        /// <summary>Перетаскивание из слота в сетку инвентаря снимает предмет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Character_DragFromSlotToInventory_Unequips()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;

            var slot = GameObject.Find("SlotMain");
            var inventory = GameObject.Find("InventoryFrame");
            Assert.IsNotNull(slot, "Нет слота основного оружия");
            Assert.IsNotNull(inventory, "Нет фрейма инвентаря");
            var source = slot!.GetComponentInChildren<ItemDragSource>(true);
            Assert.IsNotNull(source, "У слота нет источника перетаскивания");
            Assert.IsNotNull(inventory!.GetComponent<ItemDropZone>(), "У инвентаря нет зоны сброса");

            var data = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(960f, 540f),
                button = PointerEventData.InputButton.Left
            };
            ExecuteEvents.Execute(source!.gameObject, data, ExecuteEvents.beginDragHandler);
            ExecuteEvents.Execute(inventory, data, ExecuteEvents.dropHandler);
            yield return null;

            var value = GameObject.Find("SlotMainValue");
            Assert.IsNotNull(value, "Нет подписи слота основного оружия");
            Assert.AreEqual("—", value.GetComponent<Text>().text, "Оружие не снялось перетаскиванием");
        }

        /// <summary>Ник заполнен по умолчанию, кубик выдаёт новое имя.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NewGame_Nickname_IsFilledAndRandomizable()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;

            var field = GameObject.Find("NicknameInput");
            Assert.IsNotNull(field, "Нет поля имени");
            var input = field.GetComponent<TextInputField>();
            Assert.IsNotNull(input, "У поля нет TextInputField");
            Assert.IsFalse(string.IsNullOrWhiteSpace(input.Text), "Имя должно быть заполнено по умолчанию");
            Assert.IsNotNull(GameObject.Find("DiceButton"), "Нет кнопки-кубика");

            Click("DiceButton");
            yield return null;

            Assert.IsFalse(string.IsNullOrWhiteSpace(input.Text), "Имя после генерации");
            Assert.LessOrEqual(input.Text.Length, 24, "Длина имени");
        }

        /// <summary>Состояние показано как в Таркове: пиктограмма и число рядом, ник и уровень — сверху.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Character_StatsInsideEquipmentFrame_ShowNumericValues()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;

            Assert.IsNotNull(GameObject.Find("Nickname"), "Нет ника на экране персонажа");
            Assert.IsNotNull(GameObject.Find("Level"), "Нет уровня на экране персонажа");
            foreach (var key in new[] { "Health", "Food", "Water", "Stamina" })
            {
                var icon = GameObject.Find("Stat" + key + "Icon");
                Assert.IsNotNull(icon, "Нет пиктограммы состояния " + key);
                Assert.IsNotNull(icon!.GetComponent<Image>().sprite, "У пиктограммы " + key + " нет спрайта");
                var value = GameObject.Find("Stat" + key + "Value");
                Assert.IsNotNull(value, "Нет значения характеристики " + key);
                StringAssert.Contains("/", value!.GetComponent<Text>().text,
                    "Значение " + key + " должно быть числом вида 100/100");
                Assert.IsNull(GameObject.Find("Stat" + key + "Bar"), "Полосы быть не должно: " + key);
            }
        }

        /// <summary>Активная травма показывается маленькой иконкой под полосой здоровья.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Character_Injury_ShownAsIconUnderHealthBar()
        {
            yield return LoadMain();
            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;

            Assert.IsNull(GameObject.Find("InjuryIcon_injury_broken_leg"), "Без травм иконок быть не должно");

            var screen = FindScreen();
            var player = screen.Session!.State.Player!;
            player.ApplyInjury(new InjuryState("injury_broken_leg", InjuryDurationKind.UntilHealed, 0));
            screen.ShowInventory();
            yield return null;

            var icon = GameObject.Find("InjuryIcon_injury_broken_leg");
            Assert.IsNotNull(icon, "Под строкой здоровья нет иконки травмы");
            var health = GameObject.Find("StatHealthRow");
            Assert.IsNotNull(health, "Нет строки здоровья");
            var iconRect = WorldRect((RectTransform)icon!.transform);
            var healthRect = WorldRect((RectTransform)health!.transform);
            Assert.Less(iconRect.yMax, healthRect.yMin, "Иконка травмы должна лежать под строкой здоровья");
        }
    }
}
