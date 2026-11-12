#nullable enable
using System.Collections;
using System.Collections.Generic;
using AnomalyPath.Core.Dialog;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Ui.Dialogs;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Проверяет диалог торговца в комнате: меню открывается само, «Поговорить»
    /// показывает ответ и фразы, завершающая фраза возвращает в меню, «Поторгуем?»
    /// открывает окно торговли с возвратом, «Прощай» уводит в большой хаб.
    /// </summary>
    public sealed class TraderDialogPlayModeTests : PlayModeTestBase
    {
        private const string TraderId = "trd_hub_smuggler";

        /// <summary>При входе в комнату меню диалога появляется само.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_Room_OpensMenuAutomatically()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom(TraderId);
            yield return null;

            Assert.IsNotNull(GameObject.Find("TraderDialog"), "Диалог торговца не открылся");
            Assert.IsNotNull(GameObject.Find("DialogMenu"), "Меню диалога не показано");
            Assert.IsNull(GameObject.Find("DialogTalkWindow"), "Окно разговора не должно быть открыто");
            Assert.AreEqual("Поговорить", Label("DialogTalk"), "Первая полоса меню");
            Assert.AreEqual("Поторгуем?", Label("DialogTrade"), "Полоса торговли");
            Assert.AreEqual("Прощай", Label("DialogFarewell"), "Полоса прощания");
        }

        /// <summary>Диалог стоит в нижней части комнаты, а выбор торговца — левее окна.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_Menu_SitsLowerAndTradersLeft()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom(TraderId);
            yield return null;

            Assert.IsNotNull(GameObject.Find("Merchant"), "Модель торговца должна остаться на экране");
            var stage = WorldRect((RectTransform)GameObject.Find("Hub")!.transform.Find("Content/Stage"));
            var menu = WorldRect((RectTransform)GameObject.Find("DialogMenu")!.transform);
            var traders = WorldRect((RectTransform)GameObject.Find("Traders")!.transform);

            Assert.Less(menu.center.y, stage.center.y, "Окно диалога должно быть в нижней части комнаты");
            Assert.Less(traders.xMax, menu.xMin, "Выбор торговца должен стоять левее окна диалога");

            Click("Traders");
            yield return null;
            var dropdown = WorldRect((RectTransform)GameObject.Find("TraderMenu")!.transform);
            Assert.Less(dropdown.xMax, menu.xMin, "Выпадашка выбора торговцев должна открываться левее окна");
        }

        /// <summary>«Поговорить» показывает ответ торговца сверху и фразы игрока снизу.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_Talk_ShowsTraderReplyAndPhrases()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowTraderRoom(TraderId);
            yield return null;

            Click("DialogTalk");
            yield return null;

            Assert.IsNotNull(GameObject.Find("DialogTalkWindow"), "Окно разговора не открылось");
            Assert.IsNull(GameObject.Find("DialogMenu"), "Меню должно скрыться");
            Assert.AreEqual("Контрабандист", Label("DialogSpeaker"), "Имя торговца");
            Assert.AreEqual(
                screen.Texts.Get("dialog.dlg_trd_hub_smuggler.root"),
                Label("DialogReply"),
                "Ответ торговца взят из диалога");
            Assert.IsNotNull(GameObject.Find("DialogPhrase_bye"), "Нет прощальной фразы");
        }

        /// <summary>Если фраз больше, чем вмещает окно, появляется полоса прокрутки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_ManyPhrases_ShowsScrollbar()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom(TraderId);
            yield return null;

            var room = GameObject.Find("Hub")!.GetComponent<HubScreen>()!;
            var view = room.Dialog!.View;
            var options = new List<DialogOptionView>();
            for (var index = 0; index < 8; index++)
            {
                options.Add(new DialogOptionView("phrase_" + index, "dialog.common.ok", true));
            }

            view.ShowTalk("Контрабандист", "Длинный ответ торговца.", options);
            Canvas.ForceUpdateCanvases();
            yield return null;

            var expectedHeight = (DialogMetrics.VisiblePhrases * DialogMetrics.PhraseHeight) +
                                 ((DialogMetrics.VisiblePhrases - 1) * DialogMetrics.PhraseGap) +
                                 (2f * DialogMetrics.PhrasePadding);
            Assert.AreEqual(
                expectedHeight,
                view.PhraseScroll.ViewportHeight,
                0.5f,
                "Окно должно вмещать ровно три фразы");
            Assert.Greater(
                view.PhraseScroll.Content.rect.height,
                view.PhraseScroll.ViewportHeight,
                "Фраз больше, чем вмещает окно, — контент должен быть выше области");
            Assert.IsTrue(
                view.PhraseScroll.Bar.gameObject.activeSelf,
                "При переполнении должна появиться полоса прокрутки");
        }

        /// <summary>Завершающая фраза возвращает в меню разделов, а не закрывает диалог.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_FinalPhrase_ReturnsToMenu()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom(TraderId);
            yield return null;

            Click("DialogTalk");
            yield return null;
            Click("DialogPhrase_bye");
            yield return null;

            Assert.IsNotNull(GameObject.Find("TraderDialog"), "Диалог не должен закрываться");
            Assert.IsNotNull(GameObject.Find("DialogMenu"), "Нужно вернуться в меню");
            Assert.IsNull(GameObject.Find("DialogTalkWindow"), "Окно разговора должно скрыться");
        }

        /// <summary>«Поторгуем?» открывает окно торговли, закрытие возвращает в меню.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_Trade_OpensTradeAndReturnsToMenu()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom(TraderId);
            yield return null;

            Click("DialogTrade");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Trade"), "Окно торговли не открылось");

            Click("TradeClose");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Hub"), "После торговли должна открыться комната");
            Assert.IsNotNull(GameObject.Find("TraderDialog"), "Диалог должен снова открыться");
            Assert.IsNotNull(GameObject.Find("DialogMenu"), "После торговли нужно вернуться в меню");
        }

        /// <summary>«Прощай» уводит в большой хаб, как дверь.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_Farewell_ReturnsToBigHub()
        {
            yield return StartGame();
            FindScreen().ShowTraderRoom(TraderId);
            yield return null;

            Click("DialogFarewell");
            yield return null;

            Assert.IsNotNull(GameObject.Find("BigHub"), "Прощание не вернуло в большой хаб");
            Assert.IsNull(GameObject.Find("Hub"), "Комната торговца осталась открытой");
        }

        /// <summary>Esc из разговора возвращает в меню, повторный Esc — в большой хаб.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Dialog_Escape_FromTalk_StepsBackToMenuThenHub()
        {
            yield return StartGame();
            var screen = FindScreen();
            screen.ShowTraderRoom(TraderId);
            yield return null;

            Click("DialogTalk");
            yield return null;

            Assert.IsTrue(screen.HandleEscape(), "Первый Esc должен обработать окно разговора");
            yield return null;
            Assert.IsNotNull(GameObject.Find("DialogMenu"), "Первый Esc должен вернуть в меню");
            Assert.IsNull(GameObject.Find("BigHub"), "Из разговора нельзя сразу уходить в хаб");

            Assert.IsTrue(screen.HandleEscape(), "Второй Esc должен выйти из комнаты");
            yield return null;
            Assert.IsNotNull(GameObject.Find("BigHub"), "Второй Esc не вернул в большой хаб");
        }

        private static string Label(string name)
        {
            var target = GameObject.Find(name);
            Assert.IsNotNull(target, $"Нет объекта «{name}»");
            var label = target!.GetComponentInChildren<Text>();
            Assert.IsNotNull(label, $"У «{name}» нет подписи");
            return label!.text;
        }
    }
}
