#nullable enable
using System.Collections;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime.Ui.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace AnomalyPath.Tests.PlayMode
{
    /// <summary>
    /// Сквозной срез сюжетного квеста: разговор с Контрабандистом, взятие S1,
    /// журнал, прогресс зачистки и сдача в диалоге.
    /// </summary>
    public sealed class StoryQuestPlayModeTests : PlayModeTestBase
    {
        /// <summary>Диалог выдаёт S1, журнал показывает квест, сдача возвращает награду.</summary>
        /// <returns>Корутина теста.</returns>
        [UnityTest]
        public IEnumerator Story_Smuggler_AcceptProgressTurnIn()
        {
            yield return StartGame();

            var screen = FindScreen();
            screen.ShowDialog("trd_hub_smuggler", "dlg_trd_hub_smuggler");
            yield return null;
            Assert.IsNotNull(GameObject.Find("NpcDialog"), "Диалог с NPC не открылся");

            Click("DialogTalk");
            yield return null;
            Click("DialogPhrase_s1_std");
            yield return null;
            Click("DialogPhrase_ok");
            yield return null;

            var session = screen.Session!;
            Assert.AreEqual(
                1,
                screen.StoryQuests.GetActive(session).Count,
                "S1 должен быть активным");
            Assert.IsNull(GameObject.Find("NpcDialog"), "Диалог должен закрыться");

            screen.ShowQuests(null);
            yield return null;
            Assert.IsNotNull(
                FindByPrefix(GameObject.Find("QuestJournalScreen")!.transform, "QuestRow_qst_story_01_cordon"),
                "В журнале нет сюжетного квеста");
            Click("JournalClose");
            yield return null;

            for (var index = 0; index < 4; index++)
            {
                screen.StoryQuests.NotifyLocationCleared(
                    session,
                    "playmode_loc_" + index,
                    "loc_building_industrial",
                    LocationSubtype.Building);
            }

            Assert.IsTrue(
                screen.StoryQuests.TryGetActive(session, "qst_story_01_cordon", out var active),
                "S1 потерялся");
            Assert.IsTrue(screen.StoryQuests.IsReady(session, active), "S1 должен быть готов");

            screen.ShowDialog("trd_hub_smuggler", "dlg_trd_hub_smuggler");
            yield return null;
            Click("DialogTalk");
            yield return null;
            Click("DialogPhrase_s1_ready");
            yield return null;
            Click("DialogPhrase_std");
            yield return null;
            Click("DialogPhrase_thanks");
            yield return null;

            var rewards = GameObject.Find("Rewards");
            Assert.IsNotNull(rewards, "После сдачи в диалоге должен открыться экран наград");
            Assert.AreEqual(
                UiText.Get(screen.Texts, UiTextKey.Rewards_Source_Quest),
                Label(rewards!, "RewardSource"),
                "Слева — награда за задание");
            Assert.IsNotNull(FindByPrefix(rewards!.transform, "RewardItem_"), "В награде нет предметов");
            Click("RewardTakeAll");
            yield return null;
            Assert.IsNull(GameObject.Find("Rewards"), "После «Взять всё» окно наград закрывается");

            Assert.IsTrue(
                session.State.Player!.HasCompletedQuest("qst_story_01_cordon"),
                "S1 должен быть сдан");
            Assert.IsNull(GameObject.Find("NpcDialog"), "Диалог должен закрыться");

            screen.ShowDialog("trd_hub_smuggler", "dlg_trd_hub_smuggler");
            yield return null;
            Click("DialogTalk");
            yield return null;
            Assert.IsNotNull(
                GameObject.Find("DialogPhrase_s2"),
                "После S1 должен предлагаться S2");
        }

        private static string Label(GameObject panel, string name)
        {
            var target = FindDeep(panel.transform, name);
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
