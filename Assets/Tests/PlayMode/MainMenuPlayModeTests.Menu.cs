#nullable enable
using System;
using System.Collections;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="MainMenuPlayModeTests"/>: меню и новая игра.</summary>
    public sealed partial class MainMenuPlayModeTests
    {
        /// <summary>Сцена Main создаёт GameScreen и активное стартовое меню.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator MainScene_Start_ShowsMainMenu()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");

            var mainMenu = GameObject.Find("MainMenu");
            Assert.IsNotNull(mainMenu, "Панель MainMenu не создана");
            Assert.IsTrue(mainMenu.activeSelf, "Стартовое меню не активно");
        }

        /// <summary>Кнопка «Выйти» в игре открывает диалог с двумя вариантами выхода.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator InGame_ExitButton_ShowsModalDialog()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Gear");
            yield return null;
            Click("Выйти");
            yield return null;

            var dialog = GameObject.Find("ExitDialog");
            Assert.IsNotNull(dialog, "Модальный диалог выхода не создан");
            Assert.IsTrue(dialog.activeSelf, "Модальный диалог выхода не активен");
            Assert.IsNotNull(GameObject.Find("В меню"), "Нет кнопки «В меню»");
            Assert.IsNotNull(GameObject.Find("На рабочий стол"), "Нет кнопки «На рабочий стол»");
        }

        /// <summary>Экран новой игры показывает наборы по именам из данных, «Назад» возвращает в меню.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator NewGame_ShowsLoadoutNames_BackReturnsToMenu()
        {
            yield return LoadMain();

            var mainMenu = GameObject.Find("MainMenu");
            Assert.IsNotNull(mainMenu, "Стартовое меню не найдено в сцене Main");

            Click("Новая игра");
            yield return null;

            var newGame = GameObject.Find("NewGame");
            Assert.IsNotNull(newGame, "Панель новой игры не создана");
            Assert.IsTrue(newGame.activeSelf, "Панель новой игры не активна");
            Assert.IsNotNull(GameObject.Find("Стрелок"), "Набор не показан по имени из каталога текстов");
            Assert.IsFalse(mainMenu.activeSelf, "Стартовое меню осталось активным при открытии новой игры");

            Click("Назад");
            yield return null;

            Assert.IsFalse(newGame.activeSelf, "Панель новой игры осталась активной");
            Assert.IsTrue(mainMenu.activeSelf, "Стартовое меню не вернулось");
        }

        /// <summary>Пункт «Авторы» открывает экран авторов, «В меню» возвращает в меню.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Authors_Back_ReturnsToMenu()
        {
            yield return LoadMain();

            var mainMenu = GameObject.Find("MainMenu");
            Click("Авторы");
            yield return null;

            var authors = GameObject.Find("Authors");
            Assert.IsNotNull(authors, "Экран авторов не создан");
            Assert.IsTrue(authors!.activeSelf, "Экран авторов не активен");
            Assert.IsFalse(mainMenu!.activeSelf, "Стартовое меню осталось активным");

            Click("AuthorsBack");
            yield return null;

            Assert.IsFalse(authors.activeSelf, "Экран авторов остался активным");
            Assert.IsTrue(GameObject.Find("MainMenu")!.activeSelf, "Стартовое меню не вернулось");
        }

        /// <summary>Окно инвентаря показывает слоты снаряжения и сетку, крестик возвращает в игру.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Character_ShowsEquipmentAndInventory_BackReturnsToGame()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Backpack");
            yield return null;

            var character = GameObject.Find("Inventory");
            Assert.IsNotNull(character, "Панель персонажа не создана");
            Assert.IsTrue(character.activeSelf, "Панель персонажа не активна");
            Assert.IsNotNull(GameObject.Find("SlotMain"), "Нет слота основного оружия");
            Assert.IsNotNull(GameObject.Find("SlotSecondary"), "Нет слота дополнительного оружия");
            Assert.IsNotNull(GameObject.Find("SlotHelmet"), "Нет слота шлема");
            Assert.IsNotNull(GameObject.Find("SlotArmor"), "Нет слота брони");
            Assert.IsNotNull(GameObject.Find("SlotBackpack"), "Нет слота рюкзака");
            Assert.IsNotNull(GameObject.Find("SlotBelt"), "Нет слота пояса артефактов");
            Assert.IsNotNull(GameObject.Find("ArtifactSlot_0"), "Нет слотов артефактов");
            Assert.IsNotNull(GameObject.Find("InventoryMoney"), "Нет плашки валюты");
            Assert.Greater(CountChildren(character, "Cell_"), 0, "Клетки инвентаря не созданы");
            Assert.Greater(CountChildren(character, "ItemLabel_"), 0, "Предметы инвентаря не показаны");

            Click("InventoryClose");
            yield return null;

            Assert.IsTrue(GamePanel()!.activeSelf, "Игровая панель не вернулась");
            Assert.IsFalse(character.activeSelf, "Панель персонажа осталась активной");
        }

        /// <summary>Шестерёнка открывает меню без кнопки персонажа, рюкзак — экран персонажа.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator PlayScreen_GearAndBackpack_OpenMenuAndCharacter()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Assert.IsTrue(GamePanel()!.activeSelf, "Основной экран не открылся");

            Click("Gear");
            yield return null;
            var menu = GameObject.Find("GameMenu");
            Assert.IsNotNull(menu, "Шестерёнка не открыла меню игры");
            Assert.IsTrue(menu.activeSelf, "Меню игры не активно");
            Assert.IsNull(GameObject.Find("Персонаж"), "В меню не должно быть кнопки персонажа");
            Assert.IsNotNull(GameObject.Find("Продолжить"), "В меню нет кнопки «Продолжить»");

            Click("Продолжить");
            yield return null;
            Assert.IsTrue(GamePanel()!.activeSelf, "Меню не вернуло в игру");

            Click("Backpack");
            yield return null;
            Assert.IsTrue(GameObject.Find("Inventory").activeSelf, "Рюкзак не открыл окно инвентаря");
        }

        /// <summary>
        /// После старта открывается большой хаб — общий экран локации со всеми действиями;
        /// торговля открывает комнату торговца, её дверь возвращает в большой хаб,
        /// а «Покинуть хаб» ведёт на маршрут.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Hub_Start_ShowsBigHub_ThenTraderRoomAndPath()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;

            var bigHub = GameObject.Find("BigHub");
            Assert.IsNotNull(bigHub, "После старта не открылся большой хаб");
            Assert.IsTrue(bigHub!.activeSelf, "Большой хаб не активен");
            Assert.IsNull(GameObject.Find("InGame"), "В большом хабе не должно быть общего экрана локации");
            Assert.IsNull(GameObject.Find("Hub"), "Комната торговца не должна открываться на старте");

            var interior = bigHub.transform.Find("Content/Stage/Interior")?.GetComponent<Image>();
            Assert.IsNotNull(interior, "У большого хаба нет картинки интерьера");
            Assert.IsNotNull(interior!.sprite, "У большого хаба нет фона локации");
            StringAssert.StartsWith("bg_", interior.sprite!.name, "Фон должен быть картинкой локации");

            Assert.IsNotNull(GameObject.Find("Map"), "В большом хабе нет карты мира");
            Assert.IsNotNull(GameObject.Find("QuestBoard"), "В большом хабе нет доски объявлений");
            Assert.IsNotNull(GameObject.Find("LeaveHub"), "В большом хабе нет двери");
            Assert.IsNotNull(GameObject.Find("Traders"), "В большом хабе нет значка торговцев");
            Assert.IsNotNull(GameObject.Find("Backpack"), "Нет значка инвентаря");
            Assert.IsNotNull(GameObject.Find("ClockHand"), "У часов хаба нет стрелки");
            Assert.IsNull(GameObject.Find("LocationSummary"), "Сводки локации на экране быть не должно");
            Assert.IsNull(GameObject.Find("Money"), "Денег на экране быть не должно");

            Click("Traders");
            yield return null;
            Click("TraderCard_trd_hub_smuggler");
            yield return null;

            var hub = GameObject.Find("Hub");
            Assert.IsNotNull(hub, "Торговец не открыл комнату торговца");
            Assert.IsTrue(hub!.activeSelf, "Комната торговца не активна");
            Assert.IsNull(GameObject.Find("BigHub"), "В комнате торговца не должно быть большого хаба");
            Assert.IsNull(GameObject.Find("InGame"), "В комнате торговца не должно быть общего экрана");
            Assert.IsNotNull(GameObject.Find("Merchant"), "Нет торговца за стойкой");
            Assert.IsNotNull(GameObject.Find("LeaveHub"), "Нет двери комнаты торговца");

            Click("LeaveHub");
            yield return null;

            Assert.IsNotNull(GameObject.Find("BigHub"), "Дверь комнаты торговца не вернула в большой хаб");
            Assert.IsNull(GameObject.Find("Hub"), "Комната торговца осталась открытой");

            Click("LeaveHub");
            yield return null;

            var world = FindScreen().Session!.State.World;
            Assert.AreEqual(LocationSubtype.Path, world.CurrentLocation.Subtype, "Не вышли на маршрут");
            var path = GameObject.Find("InGame");
            Assert.IsNotNull(path, "После выхода из хаба не открылся экран локации");
            Assert.IsNotNull(GameObject.Find("Clock"), "На экране нет часов");
            Assert.IsNull(GameObject.Find("LocationSummary"), "Сводки локации на экране быть не должно");
            Assert.IsNull(GameObject.Find("Money"), "Денег на экране быть не должно");
            Assert.IsNull(FindAction("LeaveHub"), "На дороге нет выхода из хаба");
            Assert.IsNotNull(FindAction("ReturnToHub"), "На дороге нет возврата в хаб");
            Assert.IsNull(FindAction("Travel_"), "Переходы по маршруту — объекты сцены, а не кнопки действий");
        }

        /// <summary>Кнопка «Покинуть хаб» строит маршрут и ведёт на первую дорогу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator PlayScreen_LeaveHub_GeneratesRoute()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            var world = screen!.Session!.State.World;
            Assert.AreEqual(LocationSubtype.Hub, world.CurrentLocation.Subtype, "Игра начинается в хабе");

            var leave = FindAction("LeaveHub");
            Assert.IsNotNull(leave, "Нет кнопки «Покинуть хаб»");
            leave!.onClick.Invoke();
            yield return null;

            Assert.AreEqual(LocationSubtype.Path, world.CurrentLocation.Subtype, "Вышли на дорогу");
            Assert.AreEqual(1, world.CurrentLocation.RouteEdge, "Первое звено маршрута");
            Assert.AreEqual(1, world.CurrentLocation.RouteStep, "Первый шаг звена");
            Assert.IsNull(FindAction("LeaveHub"), "На дороге нет действия «Покинуть хаб»");
            Assert.IsNotNull(FindAnyTransition(), "На дороге нет объекта-перехода «дальше по дороге»");
            Assert.IsNull(FindAction("Travel_"), "Переходы по маршруту — объекты сцены, а не кнопки действий");
            Assert.IsNotNull(FindAction("ReturnToHub"), "На дороге нет возврата в хаб");
        }

        /// <summary>Кнопка перехода по маршруту уводит на следующую локацию и снимает пройденную.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator PlayScreen_Travel_MovesToAnotherLocation()
        {
            yield return LoadMain();

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            var world = screen!.Session!.State.World;
            Click("LeaveHub");
            yield return null;
            var before = world.CurrentLocationId;
            var travel = FindAnyTransition();
            Assert.IsNotNull(travel, "Нет объекта-перехода");
            travel!.onClick.Invoke();
            yield return null;

            Assert.AreNotEqual(before, world.CurrentLocationId, "Переход не сменил локацию");
            Assert.IsFalse(world.Map.ContainsLocation(before), "Пройденная локация осталась на карте");
            Assert.IsTrue(GameObject.Find("InGame")!.activeSelf, "После перехода пропал основной экран");
        }

        private static Button? FindAction(string prefix)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))
            {
                if (button.name.StartsWith(prefix, StringComparison.Ordinal))
                {
                    return button!;
                }
            }

            return null;
        }

        private static int CountChildren(GameObject root, string prefix)
        {
            var count = 0;
            foreach (var child in root.GetComponentsInChildren<Transform>(includeInactive: true))
            {
                if (child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    count++;
                }
            }

            return count;
        }
    }
}
