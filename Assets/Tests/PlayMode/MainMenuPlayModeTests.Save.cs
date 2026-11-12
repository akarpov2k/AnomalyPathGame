#nullable enable
using System.Collections;
using AnomalyPath.Core.Save;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Часть класса <see cref="MainMenuPlayModeTests"/>: сохранения.</summary>
    public sealed partial class MainMenuPlayModeTests
    {
        /// <summary>Esc закрывает окно загрузки и модальный диалог выхода.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Escape_LoadAndExitDialog()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");

            Click("Загрузить игру");
            yield return null;
            Assert.IsTrue(GameObject.Find("LoadGame").activeSelf, "Окно загрузки не открылось");
            Assert.IsTrue(screen.HandleEscape(), "Esc в окне загрузки не обработан");
            yield return null;
            Assert.IsTrue(GameObject.Find("MainMenu").activeSelf, "Esc не вернул из загрузки в меню");

            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
            Click("Gear");
            yield return null;
            Click("Выйти");
            yield return null;

            var dialog = GameObject.Find("ExitDialog");
            Assert.IsNotNull(dialog, "Нет диалога выхода");
            Assert.IsTrue(dialog.activeSelf, "Диалог выхода не открылся");
            Assert.IsTrue(screen.HandleEscape(), "Esc в диалоге выхода не обработан");
            yield return null;

            Assert.IsFalse(dialog.activeSelf, "Esc не закрыл диалог выхода");
            Assert.IsTrue(GameObject.Find("GameMenu").activeSelf, "Меню игры пропало после закрытия диалога");
            Assert.IsTrue(screen.HandleEscape(), "Esc в меню игры не обработан");
            yield return null;
            Assert.IsTrue(GamePanel()!.activeSelf, "Esc не вернул из меню в игру");
        }

        /// <summary>
        /// Экран сохранения пишет сейв по кнопке строки с названием из поля, а список загрузки
        /// показывает название, уровень и деньги отдельными подписями строки.
        /// </summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator SaveScreen_SavesNamedSlot_AndLoadListShowsProgress()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            DeleteSaves(screen, 20);
            var slot = FreeSlot(screen);

            yield return StartNewGame();

            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;

            var panel = GameObject.Find("SaveGame");
            Assert.IsNotNull(panel, "Экран сохранения не создан");
            Assert.IsTrue(panel.activeSelf, "Экран сохранения не открылся");

            var newRow = FindSaveRow(slot);
            Assert.IsNotNull(newRow, "Нет строки свободной ячейки");
            var cardField = newRow!.NameField;
            Assert.IsNotNull(cardField, "В свободной карточке нет поля названия");
            Assert.IsTrue(
                cardField!.transform.IsChildOf(newRow.transform),
                "Поле названия должно быть частью последней карточки");
            StringAssert.StartsWith(
                "Новое сохранение ",
                cardField.Text,
                "Имя по умолчанию должно стоять в поле сразу");
            var fields = Object.FindObjectsByType<TextInputField>(FindObjectsInactive.Exclude);
            Assert.AreEqual(1, fields.Length, "Поле названия должно быть одно — в свободной карточке");
            Assert.AreSame(cardField, fields[0], "Поле названия должно принадлежать последней карточке");
            Assert.Greater(
                ((RectTransform)newRow.transform).rect.height,
                0f,
                "У карточки нет высоты");

            cardField.SetText("Тестовый сейв");
            ClickButton(newRow.Save);
            yield return null;

            Assert.IsTrue(screen.Saves.Exists(slot), "Сохранение не записано");
            Assert.IsTrue(GamePanel()!.activeSelf, "После сохранения не вернулись в игру");

            var saved = FindSlotInfo(screen, slot);
            Assert.IsNotNull(saved, "Сохранение не видно в списке");
            var player = screen.Session!.State.Player!;
            Assert.AreEqual("Тестовый сейв", saved!.Title, "Название сохранения");
            Assert.AreEqual(player.Level, saved.Level, "Уровень в сохранении");
            Assert.AreEqual(player.Money, saved.Money, "Деньги в сохранении");
            Assert.Greater(saved.Money, 0, "Деньги должны быть заданы условно");

            Click("Gear");
            yield return null;
            Click("Загрузить игру");
            yield return null;

            var loadPanel = GameObject.Find("LoadGame");
            Assert.IsNotNull(loadPanel, "Экран загрузки не создан");
            Assert.IsTrue(loadPanel.activeSelf, "Экран загрузки не открылся");
            var loadRow = FindLoadRow(slot);
            Assert.IsNotNull(loadRow, "В списке загрузки нет строки сохранения");
            StringAssert.Contains("Тестовый сейв", TitleOf(loadRow!), "В строке загрузки нет названия");
            StringAssert.Contains("Ур.", DetailsOf(loadRow!), "В строке загрузки нет уровня");
            StringAssert.Contains("руб", DetailsOf(loadRow!), "В строке загрузки нет денег");
            StringAssert.Contains("День", DayOf(loadRow!), "В строке загрузки нет дня мира");
            Assert.IsNotNull(loadRow!.Load, "В строке загрузки нет кнопки «Загрузить»");
            Assert.IsNotNull(loadRow.Delete, "В строке загрузки нет кнопки «Удалить»");

            ClickButton(loadRow.Load);
            yield return null;
            Assert.IsTrue(GamePanel()!.activeSelf, "Кнопка «Загрузить» не открыла игру");
            screen.Saves.Delete(slot);
        }

        /// <summary>Имя по умолчанию стоит в поле сразу и сохраняется, если его не менять.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator SaveScreen_DefaultName_PrefilledAndSaved()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            DeleteSaves(screen, 20);
            var slot = FreeSlot(screen);

            yield return StartNewGame();

            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;

            var newRow = FindSaveRow(slot);
            Assert.IsNotNull(newRow, "Нет строки свободной ячейки");
            var field = newRow!.NameField;
            Assert.IsNotNull(field, "В свободной карточке нет поля названия");
            StringAssert.StartsWith("Новое сохранение ", field!.Text, "Имя по умолчанию должно быть в поле");
            var titleObject = newRow.transform.Find("Title");
            Assert.IsTrue(
                titleObject == null || !titleObject.gameObject.activeSelf,
                "Над полем не должно быть подписи «Новое сохранение»");
            ClickButton(newRow.Save);
            yield return null;

            var saved = FindSlotInfo(screen, slot);
            Assert.IsNotNull(saved, "Сохранение не записано");
            Assert.AreEqual(field.Text, saved!.Title, "Сохранение должно получить имя из поля");
            screen.Saves.Delete(slot);
        }

        /// <summary>Совпадение названия спрашивает подтверждение: Esc отменяет, зелёная кнопка перезаписывает.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator SaveScreen_RepeatedName_AsksOverwrite()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            DeleteSaves(screen, 20);
            var existing = FreeSlot(screen);

            yield return StartNewGame();

            screen.Sessions.Save(screen.Session!, existing, title: "Дубль");
            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;

            var newRow = FindNewSlotRow();
            Assert.IsNotNull(newRow, "Нет строки свободной ячейки");
            var target = newRow!.Slot;
            Assert.AreNotEqual(existing, target, "Свободная ячейка не должна совпадать с существующим сохранением");
            Assert.IsNotNull(newRow.NameField, "В свободной карточке нет поля названия");
            newRow.NameField!.SetText("Дубль");
            ClickButton(newRow.Save);
            yield return null;

            var dialog = GameObject.Find("ConfirmDialog");
            Assert.IsNotNull(dialog, "Нет подтверждения перезаписи");
            Assert.IsTrue(dialog.activeSelf, "Подтверждение перезаписи не открылось");
            StringAssert.Contains("Дубль", ConfirmMessage(dialog), "В подтверждении нет названия сохранения");

            Assert.IsTrue(screen.HandleEscape(), "Esc в подтверждении не обработан");
            yield return null;
            Assert.IsFalse(dialog.activeSelf, "Esc не закрыл подтверждение");
            Assert.IsFalse(screen.Saves.Exists(target), "Esc перезаписал сохранение");

            ClickButton(FindSaveRow(target)!.Save);
            yield return null;
            ClickIn("ConfirmDialog", "Сохранить");
            yield return null;

            var overwritten = FindSlotInfo(screen, existing);
            Assert.IsNotNull(overwritten, "Подтверждение не перезаписало сохранение");
            Assert.AreEqual("Дубль", overwritten!.Title, "Имя перезаписанного сохранения");
            Assert.IsFalse(
                screen.Saves.Exists(target),
                "Сохранение с тем же названием не должно появляться во второй ячейке");
            screen.Saves.Delete(existing);
        }

        /// <summary>Удаление сохранения спрашивает подтверждение; Esc и «Отмена» его закрывают, «Удалить» удаляет.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator LoadScreen_DeleteSave_AsksConfirmation()
        {
            yield return LoadMain();

            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            var slot = SaveSlots.Slot(2);
            screen.Saves.Delete(slot);

            yield return StartNewGame();

            screen.Sessions.Save(screen.Session!, slot, title: "Удаляемый");
            Click("Gear");
            yield return null;
            Click("Загрузить игру");
            yield return null;

            var row = FindLoadRow(slot);
            Assert.IsNotNull(row, "Нет строки сохранения");
            Assert.IsNotNull(row!.Delete, "Нет кнопки удаления в строке");
            ClickButton(row.Delete);
            yield return null;

            var dialog = GameObject.Find("ConfirmDialog");
            Assert.IsNotNull(dialog, "Нет подтверждения удаления");
            Assert.IsTrue(dialog.activeSelf, "Подтверждение не открылось");
            Assert.IsTrue(GameObject.Find("LoadGame").activeSelf, "Кнопка удаления сразу загрузила игру");

            Assert.IsTrue(screen.HandleEscape(), "Esc в подтверждении не обработан");
            yield return null;
            Assert.IsFalse(dialog.activeSelf, "Esc не закрыл подтверждение");
            Assert.IsTrue(GameObject.Find("LoadGame").activeSelf, "Esc закрыл и экран загрузки");
            Assert.IsTrue(screen.Saves.Exists(slot), "Esc удалил сохранение");

            ClickButton(FindLoadRow(slot)!.Delete);
            yield return null;
            ClickIn("ConfirmDialog", "Отмена");
            yield return null;
            Assert.IsFalse(dialog.activeSelf, "Отмена не закрыла подтверждение");
            Assert.IsTrue(screen.Saves.Exists(slot), "Отмена удалила сохранение");

            ClickButton(FindLoadRow(slot)!.Delete);
            yield return null;
            ClickIn("ConfirmDialog", "Удалить");
            yield return null;
            Assert.IsFalse(screen.Saves.Exists(slot), "Подтверждение не удалило сохранение");
            Assert.IsTrue(GameObject.Find("LoadGame").activeSelf, "После удаления ушли с экрана загрузки");
        }

        /// <summary>Страницы сохранений: стрелки «&lt;&lt; &lt;» и «&gt; &gt;&gt;» только когда страниц несколько.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator SaveAndLoad_Pagination_ShowsPageArrowsOnlyWhenNeeded()
        {
            yield return LoadMain();

            DeleteSaves(GetScreen(), 20);
            yield return StartNewGame();

            var screen = GetScreen();
            DeleteSaves(screen, 20);
            for (var number = 1; number <= 13; number++)
            {
                screen.Sessions.Save(screen.Session!, SaveSlots.Slot(number), title: "Сейв " + number);
            }

            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;
            AssertPageNavigation("сохранения");

            Click("Назад");
            yield return null;
            Click("Gear");
            yield return null;
            Click("Загрузить игру");
            yield return null;
            AssertPageNavigation("загрузки");

            DeleteSaves(screen, 20);
        }

        private static void AssertPageNavigation(string screenName)
        {
            var label = GameObject.Find("PageLabel");
            Assert.IsNotNull(label, $"На экране {screenName} нет номера страницы");
            StringAssert.StartsWith("1 / ", label!.GetComponent<Text>().text, "Первая страница не первая");
            Assert.IsNull(GameObject.Find("PrevPage"), $"На первой странице {screenName} не должно быть «<»");
            Assert.IsNull(GameObject.Find("FirstPage"), $"На первой странице {screenName} не должно быть «<<»");
            Assert.IsNotNull(GameObject.Find("NextPage"), $"На первой странице {screenName} должна быть «>»");
            Assert.IsNotNull(GameObject.Find("LastPage"), $"На первой странице {screenName} должно быть «>>»");

            Click("NextPage");
            Assert.IsNotNull(GameObject.Find("PrevPage"), $"На второй странице {screenName} нет «<»");
            Assert.IsNotNull(GameObject.Find("FirstPage"), $"На второй странице {screenName} нет «<<»");

            Click("LastPage");
            StringAssert.StartsWith("3 / ", label!.GetComponent<Text>().text, "«>>» не ведёт на последнюю страницу");
            Assert.IsNull(GameObject.Find("NextPage"), $"На последней странице {screenName} не должно быть «>»");
            Assert.IsNull(GameObject.Find("LastPage"), $"На последней странице {screenName} не должно быть «>>»");
            Assert.IsNotNull(GameObject.Find("PrevPage"), $"На последней странице {screenName} должна быть «<»");

            Click("FirstPage");
            StringAssert.StartsWith("1 / ", label!.GetComponent<Text>().text, "«<<» не ведёт на первую страницу");
            Assert.IsNull(GameObject.Find("PrevPage"), $"После «<<» на экране {screenName} осталась «<»");
        }

        /// <summary>Экран сохранения: сначала существующие сохранения, «Новое сохранение» — на следующей странице.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator SaveScreen_ExistingSavesFirst_NewSlotOnNextPage()
        {
            yield return LoadMain();

            DeleteSaves(GetScreen(), 20);
            yield return StartNewGame();

            var screen = GetScreen();
            for (var number = 1; number <= 6; number++)
            {
                screen.Sessions.Save(screen.Session!, SaveSlots.Slot(number), title: "Сейв " + number);
            }

            Click("Gear");
            yield return null;
            Click("Сохранить игру");
            yield return null;

            var label = GameObject.Find("PageLabel");
            Assert.IsNotNull(label, "Нет номера страницы");
            Assert.AreEqual("1 / 2", label!.GetComponent<Text>().text, "Экран должен открываться на первой странице");
            Assert.IsNull(GameObject.Find("PrevPage"), "На первой странице не должно быть «<»");
            Assert.IsNotNull(GameObject.Find("NextPage"), "На первой странице должна быть «>»");
            for (var number = 1; number <= 6; number++)
            {
                var row = FindSaveRow(SaveSlots.Slot(number));
                Assert.IsNotNull(row, $"На первой странице нет сохранения {number}");
                StringAssert.Contains("Сейв " + number, TitleOf(row!), "Первая страница должна быть заполнена существующими сохранениями");
            }

            Assert.AreEqual(6, CountSaveTiles(), "На первой странице должно быть ровно 6 существующих сохранений");
            var viewport = ViewportHeightOf("SaveGame");
            Assert.Greater(viewport, 0f, "У списка нет видимой высоты");
            var cardsHeight = 0f;
            for (var number = 1; number <= 6; number++)
            {
                var card = FindSaveRow(SaveSlots.Slot(number));
                Assert.IsNotNull(card, $"Нет карточки сохранения {number}");
                cardsHeight += ((RectTransform)card!.transform).rect.height;
            }

            Assert.AreEqual(
                viewport,
                cardsHeight + (MenuScreenMetrics.RowGap * 5f),
                2f,
                "Шесть карточек должны занимать всю высоту списка");
            Assert.IsNull(FindNewSlotRow(), "На заполненной первой странице не должно быть «Нового сохранения»");

            Click("NextPage");
            yield return null;
            Assert.AreEqual("2 / 2", label!.GetComponent<Text>().text, "Не переключились на вторую страницу");
            var newRow = FindNewSlotRow();
            Assert.IsNotNull(newRow, "На новой странице нет строки «Новое сохранение»");
            Assert.IsNotNull(newRow!.NameField, "В новой карточке нет поля названия");
            StringAssert.StartsWith(
                "Новое сохранение ",
                newRow.NameField!.Text,
                "Поле новой карточки должно быть заполнено именем по умолчанию");
            Assert.GreaterOrEqual(CountSaveTiles(), 1, "На новой странице должна быть строка нового сохранения");

            Click("Назад");
            yield return null;
            DeleteSaves(screen, 20);
        }

        private static GameScreen GetScreen()
        {
            var screen = Object.FindAnyObjectByType<GameScreen>();
            Assert.IsNotNull(screen, "GameScreen не найден в сцене Main");
            return screen!;
        }

        private static IEnumerator StartNewGame()
        {
            Click("Новая игра");
            yield return null;
            Click("Начать");
            yield return null;
        }

        private static string FreeSlot(GameScreen screen)
        {
            for (var number = 1; number <= 50; number++)
            {
                var candidate = SaveSlots.Slot(number);
                if (!screen.Saves.Exists(candidate))
                {
                    return candidate;
                }
            }

            Assert.Fail("Не найден свободный слот сохранения");
            return string.Empty;
        }

        private static void DeleteSaves(GameScreen screen, int count)
        {
            for (var number = 1; number <= count; number++)
            {
                screen.Saves.Delete(SaveSlots.Slot(number));
            }
        }

        private static SaveSlotInfo? FindSlotInfo(GameScreen screen, string slot)
        {
            foreach (var item in screen.Saves.ListSlots())
            {
                if (item.Slot == slot)
                {
                    return item;
                }
            }

            return null;
        }
    }
}
