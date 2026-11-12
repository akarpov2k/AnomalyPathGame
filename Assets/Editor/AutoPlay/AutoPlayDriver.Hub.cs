#nullable enable
using System;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>Часть класса <see cref="AutoPlayDriver"/>: хаб, карта мира, диалоги и награды.</summary>
    internal sealed partial class AutoPlayDriver
    {
        /// <summary>Решение в большом хабе: сдать, взять квест, поехать в Депо или выйти на маршрут.</summary>
        private void HandleHub(GameScreen screen, GameSession session)
        {
            if (session.InCombat)
            {
                screen.ShowCombat();
                ScheduleNext();
                return;
            }

            if (_plan != null)
            {
                OpenPlan();
                return;
            }

            var active = AutoPlayScenario.ActiveStory(session);
            if (active != null)
            {
                if (screen.StoryQuests.IsReady(session, active))
                {
                    if (AutoPlayScenario.IsGvozdQuest(active.QuestId) &&
                        !string.Equals(
                            AutoPlayScenario.CurrentTemplateId(session),
                            AutoPlayIds.DepotTemplateId,
                            StringComparison.Ordinal))
                    {
                        OpenMap(screen);
                        return;
                    }

                    StartPlan(AutoPlayScenario.PlanFor(active.QuestId, turnIn: true));
                    OpenPlan();
                    return;
                }

                if (AutoPlayScenario.IsGvozdQuest(active.QuestId))
                {
                    if (string.Equals(
                            AutoPlayScenario.CurrentTemplateId(session),
                            AutoPlayIds.DepotTemplateId,
                            StringComparison.Ordinal))
                    {
                        TalkToGvozd(active);
                        return;
                    }

                    OpenMap(screen);
                    return;
                }

                PrepareForExpedition(screen, session);
                Click(AutoPlayIds.LeaveHubButton, "хаб: выхожу на маршрут, квест " +
                                                  AutoPlayScenario.StoryNumber(active.QuestId));
                return;
            }

            if (AutoPlayScenario.TryGetNextAvailable(screen, session, out var next))
            {
                StartPlan(AutoPlayScenario.PlanFor(next, turnIn: false));
                OpenPlan();
                return;
            }

            if (AutoPlayScenario.IsChapterComplete(session))
            {
                Succeed();
                return;
            }

            Fail("в хабе нет доступного сюжетного квеста");
        }

        /// <summary>Гвоздь в Депо: разговор, затем сдача готового S5.</summary>
        private void TalkToGvozd(StoryQuestState active)
        {
            if (_gvozdTries > 10)
            {
                Fail("S5 не стал готов к сдаче у Гвоздя");
                return;
            }

            _gvozdTries++;
            var npcKey = "npc:" + AutoPlayIds.GvozdSpawnId;
            if (_talked.Contains(npcKey))
            {
                // Разговор уже засчитан; если квест не готов, ждём смены состояния.
                ScheduleNext();
                return;
            }

            _plan = new DialoguePlan(active.QuestId, turnIn: false, npc: true, new[] { "bye" });
            OpenPlan();
        }

        /// <summary>Открывает диалог по плану: комната контрабандиста или фигура Гвоздя.</summary>
        private void OpenPlan()
        {
            if (_plan == null)
            {
                return;
            }

            if (_plan.Npc)
            {
                var npc = GameObject.Find(AutoPlayIds.LocationObjectPrefix + AutoPlayIds.GvozdSpawnId);
                if (npc == null)
                {
                    Fail("на большом хабе Депо нет фигуры Гвоздя");
                    return;
                }

                var button = npc.GetComponent<Button>();
                if (button == null || !button.interactable)
                {
                    Fail("фигура Гвоздя недоступна");
                    return;
                }

                _log.Step("хаб: говорю с Гвоздём (" + (_plan.TurnIn ? "сдача S5" : "разговор") + ")");
                _talked.Add("npc:" + AutoPlayIds.GvozdSpawnId);
                button.onClick.Invoke();
                _actions++;
                ScheduleNext();
                return;
            }

            if (IsActive(AutoPlayIds.TraderMenuPanel))
            {
                if (Click(
                        "TraderCard_" + AutoPlayIds.SmugglerActorId,
                        "хаб: выбираю контрабандиста"))
                {
                    return;
                }

                Fail("в выпадашке нет карточки контрабандиста");
                return;
            }

            Click(AutoPlayIds.TradersButton, "хаб: открываю торговцев");
        }

        /// <summary>Комната торговца: меню диалога и фразы плана.</summary>
        private void HandleTraderRoom(GameScreen screen, GameSession session)
        {
            if (_plan == null || _plan.Npc)
            {
                Click(AutoPlayIds.LeaveHubButton, "комната торговца: выхожу в большой хаб");
                return;
            }

            if (IsActive(AutoPlayIds.DialogTalkWindow))
            {
                NextPlanOption();
                return;
            }

            if (IsActive(AutoPlayIds.DialogMenuPanel))
            {
                Click(AutoPlayIds.DialogTalkButton, "диалог: «Поговорить»");
                return;
            }

            ScheduleNext();
        }

        /// <summary>Диалог NPC поверх локации или хаба.</summary>
        private void HandleDialog(GameScreen screen, GameSession session)
        {
            if (_plan == null || !_plan.Npc)
            {
                // Незапланированный диалог: закрываем, чтобы не зависнуть.
                if (IsActive(AutoPlayIds.DialogTalkWindow) || IsActive(AutoPlayIds.DialogMenuPanel))
                {
                    screen.HandleEscape();
                    ScheduleNext();
                }

                return;
            }

            if (IsActive(AutoPlayIds.DialogTalkWindow))
            {
                NextPlanOption();
                return;
            }

            if (IsActive(AutoPlayIds.DialogMenuPanel))
            {
                Click(AutoPlayIds.DialogTalkButton, "диалог: «Поговорить»");
                return;
            }

            ScheduleNext();
        }

        /// <summary>Нажимает следующую фразу плана диалога.</summary>
        private void NextPlanOption()
        {
            if (_plan == null)
            {
                return;
            }

            if (_plan.Index >= _plan.Options.Length)
            {
                _log.Step("диалог: план выполнен (" + _plan.QuestId + ")");
                _plan = null;
                ScheduleNext();
                return;
            }

            var optionId = _plan.Options[_plan.Index];
            var name = AutoPlayIds.DialogPhrasePrefix + optionId;
            if (TryClick(name, "диалог: фраза «" + optionId + "» (" + _plan.QuestId + ")"))
            {
                _plan.Index++;
                return;
            }

            _log.Error("диалог: нет фразы «" + optionId + "» — план квеста " + _plan.QuestId + " сброшен");
            _plan = null;
            ScheduleNext();
        }

        /// <summary>Начинает план диалога.</summary>
        private void StartPlan(DialoguePlan plan)
        {
            _plan = plan;
            _log.Step("план диалога: " + (plan.TurnIn ? "сдать " : "взять ") + plan.QuestId +
                      " (" + string.Join(" → ", plan.Options) + ")");
        }

        /// <summary>Открывает карту мира из хаба.</summary>
        private void OpenMap(GameScreen screen)
        {
            _mapSpotClicked = false;
            _mapTries = 0;
            Click(AutoPlayIds.MapButton, "хаб: открываю карту мира");
        }

        /// <summary>Карта мира: выбрать Депо и поехать проводником или пешком.</summary>
        private void HandleMap(GameScreen screen, GameSession session)
        {
            var spotName = AutoPlayIds.MapSpotPrefix + AutoPlayIds.DepotTemplateId;
            var spot = FindButton(spotName);
            if (spot != null && !_mapSpotClicked)
            {
                _mapSpotClicked = true;
                _log.Step("карта: выбираю Депо");
                spot.onClick.Invoke();
                _actions++;
                ScheduleNext();
                return;
            }

            if (TryClick(AutoPlayIds.MapGuideButton, "карта: в Депо с проводником"))
            {
                _mapSpotClicked = false;
                return;
            }

            if (TryClick(AutoPlayIds.MapWalkButton, "карта: в Депо пешком"))
            {
                _mapSpotClicked = false;
                return;
            }

            if (_mapTries++ < 2)
            {
                _log.Assist("карта: не хватает сил или денег — восстанавливаю");
                RestoreStamina(session);
                ScheduleNext();
                return;
            }

            _log.Warn("карта: Депо недоступно, закрываю карту");
            Click(AutoPlayIds.MapCloseButton, "карта: закрываю");
            _mapSpotClicked = false;
        }

        /// <summary>Экран наград: забрать всё, при переполнении — закрыть с подтверждением.</summary>
        private void HandleRewards(GameScreen screen)
        {
            if (_rewardTries < 2 &&
                TryClick(AutoPlayIds.RewardTakeAllButton, "награды: «Взять всё»"))
            {
                _rewardTries++;
                return;
            }

            if (TryClick(AutoPlayIds.RewardCloseButton, "награды: закрываю"))
            {
                return;
            }

            ScheduleNext();
        }

        /// <summary>Подтверждение: нажимает кнопку действия, а не «Отмена».</summary>
        private void ConfirmPrompt(GameScreen screen, ConfirmDialog dialog)
        {
            var cancelLabel = AnomalyPath.Runtime.Ui.Core.UiText.Get(
                screen.Texts,
                AnomalyPath.Runtime.Ui.Core.UiTextKey.Common_Cancel);

            foreach (var button in dialog.GetComponentsInChildren<Button>(true))
            {
                if (!button.gameObject.activeInHierarchy)
                {
                    continue;
                }

                var label = button.GetComponentInChildren<Text>(true);
                if (label == null || string.Equals(label.text, cancelLabel, StringComparison.Ordinal))
                {
                    continue;
                }

                _log.Step("подтверждение: «" + label.text + "»");
                button.onClick.Invoke();
                _actions++;
                ScheduleNext();
                return;
            }

            ScheduleNext();
        }
    }
}
