#nullable enable
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Доска объявлений и журнал заданий: карточки слотов, взятие задания, пустая карточка
    /// с таймером, категории журнала и описание выбранного задания.
    /// </summary>
    public sealed class QuestPlayModeTests : PlayModeTestBase
    {
        /// <summary>В хабе открывается доска: карточки, взятие задания и пустая карточка с таймером.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Quest_Board_AcceptsQuest_ShowsTimerCard()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.ShowQuests(screen.Session!.State.World.CurrentLocation.InstanceId);
            yield return null;

            var panel = GameObject.Find("QuestBoardScreen");
            Assert.IsNotNull(panel, "Доска объявлений не открылась");
            var content = BoardContent(panel!);
            var card = ActiveChild(content, "Board_");
            Assert.IsNotNull(card, "На доске нет карточек");
            var title = FindByPrefix(card!.transform, "Title");
            Assert.IsNotNull(title, "У карточки нет названия");
            Assert.IsFalse(string.IsNullOrEmpty(title!.GetComponent<Text>().text), "Название карточки пусто");

            var take = FindByPrefix(card.transform, "Take");
            Assert.IsNotNull(take, "Нет кнопки «Принять»");
            take!.GetComponent<Button>().onClick.Invoke();
            yield return null;

            Assert.AreEqual(1, screen.Quests.GetActive(screen.Session!).Count, "Задание должно быть активным");
            var timerCard = content.Find("Board_0");
            Assert.IsNotNull(timerCard, "Первая карточка пропала");
            var timer = FindByPrefix(timerCard!, "Timer");
            Assert.IsNotNull(timer, "У пустой карточки нет таймера");
            Assert.IsFalse(
                string.IsNullOrEmpty(timer!.GetComponent<Text>().text),
                "Таймер пустой карточки не заполнен");

            Click("BoardClose");
            yield return null;
            Assert.IsNull(GameObject.Find("QuestBoardScreen"), "Доска должна закрыться");
        }

        /// <summary>Журнал открывается с категориями и показывает взятое задание в «Повторяемых».</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Quest_Journal_ShowsCategoryTabsAndDetails()
        {
            yield return StartGame();

            var screen = FindScreen();
            var session = screen.Session!;
            screen.ShowQuests(session.State.World.CurrentLocation.InstanceId);
            yield return null;
            var board = GameObject.Find("QuestBoardScreen");
            Assert.IsNotNull(board, "Доска объявлений не открылась");
            var card = ActiveChild(BoardContent(board!), "Board_");
            Assert.IsNotNull(card, "На доске нет карточек");
            var take = FindByPrefix(card!.transform, "Take");
            Assert.IsNotNull(take, "Нет кнопки «Принять»");
            take!.GetComponent<Button>().onClick.Invoke();
            yield return null;
            Assert.AreEqual(1, screen.Quests.GetActive(session).Count, "Задание должно быть активным");
            Click("BoardClose");
            yield return null;

            screen.ShowQuests(null);
            yield return null;
            var panel = GameObject.Find("QuestJournalScreen");
            Assert.IsNotNull(panel, "Журнал не открылся");
            Assert.IsNotNull(FindByPrefix(panel!.transform, "StoryTab"), "Нет категории «сюжетные»");

            var repeatable = FindByPrefix(panel.transform, "RepeatableTab");
            Assert.IsNotNull(repeatable, "Нет категории «повторяемые»");
            repeatable!.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var row = FindByPrefix(panel.transform, "QuestRow_");
            Assert.IsNotNull(row, "В журнале нет взятого задания");
            row!.GetComponent<Button>().onClick.Invoke();
            yield return null;

            var title = FindByPrefix(panel.transform, "DetailsTitle");
            Assert.IsNotNull(title, "Нет заголовка описания");
            Assert.IsFalse(
                string.IsNullOrEmpty(title!.GetComponent<Text>().text),
                "Описание выбранного задания пусто");

            Click("JournalClose");
            yield return null;
            Assert.IsNull(GameObject.Find("QuestJournalScreen"), "Журнал должен закрыться");
        }

        private static Transform BoardContent(GameObject panel)
        {
            var content = panel.transform.Find("Content/Window/Slots/Viewport/Content");
            Assert.IsNotNull(content, "В префабе доски нет сетки карточек");
            return content!;
        }

        private static GameObject? ActiveChild(Transform content, string prefix)
        {
            foreach (Transform child in content)
            {
                if (child.gameObject.activeSelf && child.name.StartsWith(prefix, System.StringComparison.Ordinal))
                {
                    return child.gameObject;
                }
            }

            return null;
        }
    }
}
