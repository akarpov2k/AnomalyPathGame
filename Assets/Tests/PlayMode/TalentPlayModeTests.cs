#nullable enable
using System.Collections;
using AnomalyPath.Runtime.Screens.Talents;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>Проверяет дерево талантов: вход, карточки, подсказку и отложенное вложение очков.</summary>
    public sealed class TalentPlayModeTests : PlayModeTestBase
    {
        /// <summary>С экрана персонажа открывается дерево, показываются карточки, связи и очки.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Talent_Character_OpensTree_AndReturns()
        {
            yield return StartGame();

            Click("Backpack");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Inventory"), "Окно инвентаря не открылось");
            Assert.IsNotNull(GameObject.Find("InventoryTalents"), "Нет кнопки талантов");

            Click("InventoryTalents");
            yield return null;
            var panel = GameObject.Find("Talents");
            Assert.IsNotNull(panel, "Дерево талантов не открылось");
            Assert.IsNotNull(panel!.transform.Find("ScreenBackdrop"), "У экрана талантов нет задника");
            Assert.IsNotNull(GameObject.Find("TalentNode_tal_steady_hand"), "Нет стартового таланта");
            Assert.IsNotNull(GameObject.Find("TalentNode_tal_toughness"), "Нет правой ветки");
            Assert.IsNotNull(GameObject.Find("TalentLink"), "Нет связей между талантами");
            Assert.IsNotNull(GameObject.Find("TalentSave"), "Нет кнопки «Сохранить»");
            Assert.IsNotNull(GameObject.Find("TalentBack"), "Нет кнопки «Назад»");
            var points = GameObject.Find("TalentPoints");
            Assert.IsNotNull(points, "Нет подписи очков");
            Assert.IsFalse(string.IsNullOrWhiteSpace(points!.GetComponent<Text>().text), "Очки не показаны");
            var level = GameObject.Find("TalentLevel");
            Assert.IsNotNull(level, "Нет подписи уровня");
            Assert.IsFalse(string.IsNullOrWhiteSpace(level!.GetComponent<Text>().text), "Уровень не показан");

            Click("TalentBack");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Inventory"), "Возврат в окно инвентаря не сработал");
        }

        /// <summary>Подсказка появляется через две секунды удержания и скрывается при уходе.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Talent_Hover_ShowsTooltipAfterTwoSeconds_AndHidesOnExit()
        {
            yield return StartGame();

            Click("Backpack");
            yield return null;
            Click("InventoryTalents");
            yield return null;

            var node = GameObject.Find("TalentNode_tal_steady_hand");
            Assert.IsNotNull(node, "Нет стартового таланта");
            Assert.IsNotNull(node!.GetComponent<TalentHover>(), "У карточки нет наведения");
            var data = new PointerEventData(EventSystem.current)
            {
                position = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f)
            };
            ExecuteEvents.Execute(node, data, ExecuteEvents.pointerEnterHandler);

            yield return new WaitForSecondsRealtime(1.0f);
            Assert.IsNull(GameObject.Find("TalentTooltip"), "Подсказка появилась раньше двух секунд");

            yield return new WaitForSecondsRealtime(1.1f);
            var tooltip = GameObject.Find("TalentTooltip");
            Assert.IsNotNull(tooltip, "Подсказка не появилась через две секунды");
            Assert.IsTrue(tooltip!.activeSelf, "Подсказка не активна");

            ExecuteEvents.Execute(node, data, ExecuteEvents.pointerExitHandler);
            yield return null;
            Assert.IsFalse(tooltip.activeSelf, "Подсказка не скрылась");
        }

        /// <summary>Клик копит очко на экране, «Сохранить» применяет его к игроку.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Talent_ClickStagesPoint_AndSaveAppliesIt()
        {
            yield return StartGame();

            Click("Backpack");
            yield return null;
            Click("InventoryTalents");
            yield return null;

            var host = FindScreen();
            var player = host.Session!.State.Player!;
            var talents = host.Talents;
            talents.GrantPoints(player, 2);
            var beforeRank = talents.RankOf(player, "tal_steady_hand");
            var beforePoints = talents.AvailablePoints(player);
            Assert.Greater(beforePoints, 0, "Для теста нужны свободные очки");

            Click("TalentNode_tal_steady_hand");
            yield return null;
            var screen = GameObject.Find("Talents")!.GetComponent<TalentScreen>();
            Assert.AreEqual(1, screen.PendingPoints, "Очко не накопилось на экране");
            Assert.AreEqual(beforeRank, talents.RankOf(player, "tal_steady_hand"), "Ранг применился без «Сохранить»");
            Assert.AreEqual(beforePoints, talents.AvailablePoints(player), "Очки списались без «Сохранить»");

            Click("TalentSave");
            yield return null;
            Assert.AreEqual(0, screen.PendingPoints, "Очки не сброшены после сохранения");
            Assert.AreEqual(beforeRank + 1, talents.RankOf(player, "tal_steady_hand"), "Ранг не применился");
            Assert.AreEqual(beforePoints - 1, talents.AvailablePoints(player), "Очки не списались");
        }

        /// <summary>«Назад» с несохранёнными очками спрашивает подтверждение и отбрасывает их.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Talent_Back_WithPendingPoints_AsksConfirmation()
        {
            yield return StartGame();

            Click("Backpack");
            yield return null;
            Click("InventoryTalents");
            yield return null;

            var host = FindScreen();
            var player = host.Session!.State.Player!;
            host.Talents.GrantPoints(player, 1);
            var beforeRank = host.Talents.RankOf(player, "tal_steady_hand");
            Click("TalentNode_tal_steady_hand");
            yield return null;

            Click("TalentBack");
            yield return null;
            var panel = GameObject.Find("Talents");
            Assert.IsNotNull(panel, "Экран талантов закрылся без подтверждения");
            var dialog = panel!.transform.Find("ConfirmDialog");
            Assert.IsNotNull(dialog, "Нет диалога подтверждения");
            Assert.IsTrue(dialog!.gameObject.activeSelf, "Диалог не открылся");
            Assert.IsNotNull(GameObject.Find("Talents"), "Экран закрылся без подтверждения");

            Click("Выйти");
            yield return null;
            Assert.IsNotNull(GameObject.Find("Inventory"), "Возврат в окно инвентаря не сработал");
            Assert.AreEqual(beforeRank, host.Talents.RankOf(player, "tal_steady_hand"), "Несохранённые очки применились");
        }
    }
}
