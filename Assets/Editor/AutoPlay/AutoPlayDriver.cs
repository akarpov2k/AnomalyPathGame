#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Character;
using AnomalyPath.Runtime.Screens.Game;
using AnomalyPath.Runtime.Screens.Game.Hub;
using AnomalyPath.Runtime.Screens.Map;
using AnomalyPath.Runtime.Screens.Menu;
using AnomalyPath.Runtime.Screens.Minigames;
using AnomalyPath.Runtime.Screens.Quests;
using AnomalyPath.Runtime.Ui.Core;
using AnomalyPath.Runtime.Ui.Dialogs;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Автопилот главы 1: читает состояние реальной игры, ходит по маршруту, воюет,
    /// лутает, взламывает тайники и проходит диалоги, нажимая настоящие кнопки экранов
    /// с паузой 1–2 секунды между действиями. При опасности (мало здоровья, патронов,
    /// еды или сил на возврат) включает ассист и пишет об этом в журнал.
    /// </summary>
    internal sealed partial class AutoPlayDriver
    {
        private const double ActionPauseMin = 1.0;
        private const double ActionPauseMax = 2.0;
        private const double StuckWarnSeconds = 180;
        private const double StuckFailSeconds = 600;
        private const double RunLimitSeconds = 7200;

        private readonly AutoPlayLog _log;
        private readonly Action<string> _finish;
        private readonly double _startedAt;
        private readonly HashSet<string> _visited = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _handled = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _talked = new HashSet<string>(StringComparer.Ordinal);
        private double _nextActionAt;
        private double _lastProgressAt;
        private double _lastStuckWarnAt;
        private int _progressFingerprint;
        private int _actions;
        private bool _finished;
        private string? _failReason;
        private string _locationId = string.Empty;
        private DialoguePlan? _plan;
        private int _rewardTries;
        private bool _combatReloadPending;
        private int _attackTries;
        private int _lockIndex;
        private string _lockLocationId = string.Empty;
        private bool _mapSpotClicked;
        private int _mapTries;
        private int _gvozdTries;

        /// <summary>Создаёт автопилот.</summary>
        /// <param name="log">Журнал прогона.</param>
        /// <param name="finish">Что вызвать после завершения или провала.</param>
        public AutoPlayDriver(AutoPlayLog log, Action<string> finish)
        {
            _log = log;
            _finish = finish;
            _startedAt = EditorApplication.timeSinceStartup;
            _lastProgressAt = _startedAt;
            _progressFingerprint = 0;
        }

        /// <summary>Прогон завершён (успешно или с провалом).</summary>
        public bool IsFinished => _finished;

        /// <summary>Причина завершения; <c>null</c> — глава пройдена.</summary>
        public string? FailReason => _failReason;

        /// <summary>Сколько действий выполнено.</summary>
        public int Actions => _actions;

        /// <summary>Секунды с начала прогона.</summary>
        public double Seconds => EditorApplication.timeSinceStartup - _startedAt;

        /// <summary>Один шаг автопилота: выбрать экран и сделать одно действие.</summary>
        public void Tick()
        {
            if (_finished)
            {
                return;
            }

            var now = EditorApplication.timeSinceStartup;
            if (now - _startedAt > RunLimitSeconds)
            {
                Fail("истёк предел времени прогона");
                return;
            }

            if (now < _nextActionAt)
            {
                CheckStuck(now);
                return;
            }

            var screen = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            if (screen == null)
            {
                return;
            }

            if (screen.Session == null)
            {
                HandleStart(screen);
                return;
            }

            var session = screen.Session;
            CheckStuck(now);
            if (!screen.Rewards.Panel.activeInHierarchy)
            {
                _rewardTries = 0;
            }

            if (screen.Rewards.Panel.activeInHierarchy)
            {
                HandleRewards(screen);
                return;
            }

            var confirm = UnityEngine.Object.FindAnyObjectByType<ConfirmDialog>();
            if (confirm != null && confirm.IsOpen)
            {
                ConfirmPrompt(screen, confirm);
                return;
            }

            if (Visible<LockMinigameScreen>())
            {
                HandleLock(screen, session);
                return;
            }

            if (Visible<MapScreen>())
            {
                HandleMap(screen, session);
                return;
            }

            if (IsActive(AutoPlayIds.NpcDialogPanel))
            {
                HandleDialog(screen, session);
                return;
            }

            if (Visible<HubScreen>())
            {
                HandleTraderRoom(screen, session);
                return;
            }

            if (screen.Combat.Panel.activeInHierarchy)
            {
                HandleCombat(screen, session);
                return;
            }

            if (Visible<BigHubScreen>())
            {
                HandleHub(screen, session);
                return;
            }

            if (Visible<PlayScreen>())
            {
                HandleLocation(screen, session);
                return;
            }

            if (Visible<GameMenuScreen>())
            {
                Click(AutoPlayIds.GameMenuContinueButton, "меню игры: продолжаю");
                return;
            }

            if (Visible<InventoryScreen>())
            {
                Click(AutoPlayIds.InventoryCloseButton, "инвентарь: закрываю");
                return;
            }

            if (Visible<QuestBoardScreen>() || Visible<QuestJournalScreen>())
            {
                screen.HandleEscape();
                ScheduleNext();
                return;
            }

            _log.Warn("неизвестный экран: нет ни игры, ни меню");
            ScheduleNext();
        }

        /// <summary>Принудительно завершает прогон с ошибкой.</summary>
        /// <param name="reason">Почему сценарий не может продолжаться.</param>
        public void Fail(string reason)
        {
            if (_finished)
            {
                return;
            }

            _failReason = reason;
            _log.Error(reason);
            FinishRun();
        }

        /// <summary>Завершает прогон и пишет отчёт.</summary>
        private void FinishRun()
        {
            _finished = true;
            _log.WriteReport(BuildReport());
            _finish(_failReason ?? "глава 1 пройдена");
        }

        /// <summary>Стартовое меню: новая игра и запуск.</summary>
        private void HandleStart(GameScreen screen)
        {
            _log.Step("старт: MainMenu=" + IsActive(AutoPlayIds.MainMenuPanel) +
                      ", NewGame=" + IsActive(AutoPlayIds.NewGamePanel) +
                      ", visibleNewGame=" + Visible<NewGameScreen>() +
                      ", visibleMainMenu=" + Visible<MainMenuScreen>());
            if (Visible<NewGameScreen>())
            {
                Click(AutoPlayIds.StartButton, "новая игра: «Начать»");
                return;
            }

            if (Visible<MainMenuScreen>())
            {
                if (Click(AutoPlayIds.NewGameButton, "стартовое меню: «Новая игра»"))
                {
                    _log.Step("после клика: NewGame=" + IsActive(AutoPlayIds.NewGamePanel) +
                              ", MainMenu=" + IsActive(AutoPlayIds.MainMenuPanel) +
                              ", router=" + (screen.Router.Current == null ? "null" : screen.Router.Current.name));
                    if (!IsActive(AutoPlayIds.NewGamePanel))
                    {
                        var all = UnityEngine.Object.FindObjectsByType<GameScreen>(FindObjectsInactive.Include);
                        var ng = UnityEngine.Object.FindAnyObjectByType<NewGameScreen>(FindObjectsInactive.Include);
                        var hostProperty = ng == null ? null : ng.GetType().GetProperty(
                            "Host",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        var ngHost = ng == null || hostProperty == null ? null : hostProperty.GetValue(ng);
                        _log.Warn("клик не открыл NewGame; GameScreen=" + all.Length +
                                  ", этот=" + screen.name +
                                  ", rewards=" + (screen.Rewards == null ? "null" : "ok") +
                                  ", nicknames=" + (screen.Nicknames == null ? "null" : "ok") +
                                  ", newGameHost=" + (ngHost == null ? "null" : ngHost.GetType().Name) +
                                  ", ngPanel=" + (ng == null ? "null" : ng.Panel.activeSelf.ToString()));
                        screen.ShowNewGame();
                        _log.Step("после ShowNewGame: NewGame=" + IsActive(AutoPlayIds.NewGamePanel) +
                                  ", router=" + (screen.Router.Current == null ? "null" : screen.Router.Current.name));
                    }
                }

                return;
            }

            _log.Warn("нет ни стартового меню, ни новой игры");
            ScheduleNext();
        }

        /// <summary>Нажимает кнопку по имени, если она есть и доступна.</summary>
        private bool Click(string name, string action)
        {
            var button = FindButton(name);
            if (button == null)
            {
                _log.Warn("не найдена кнопка «" + name + "» для действия: " + action);
                return false;
            }

            if (!button.interactable)
            {
                return false;
            }

            _log.Step(action);
            button.onClick.Invoke();
            _actions++;
            ScheduleNext();
            return true;
        }

        /// <summary>Нажимает кнопку, если она есть и доступна, без предупреждения.</summary>
        private bool TryClick(string name, string action)
        {
            var button = FindButton(name);
            if (button == null || !button.interactable)
            {
                return false;
            }

            _log.Step(action);
            button.onClick.Invoke();
            _actions++;
            ScheduleNext();
            return true;
        }

        private void ScheduleNext()
        {
            _nextActionAt = EditorApplication.timeSinceStartup +
                            UnityEngine.Random.Range((float)ActionPauseMin, (float)ActionPauseMax);
        }

        private static Button? FindButton(string name)
        {
            var target = GameObject.Find(name);
            if (target == null)
            {
                return null;
            }

            var button = target.GetComponent<Button>();
            if (button != null)
            {
                return button;
            }

            return target.GetComponentInChildren<Button>(true);
        }

        private static bool IsActive(string name) => GameObject.Find(name) != null;

        private static bool Visible<T>()
            where T : MonoBehaviour, IScreen
        {
            var screen = UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
            return screen != null && screen.Panel.activeInHierarchy;
        }

        /// <summary>Завершает прогон успехом: глава 1 пройдена.</summary>
        private void Succeed()
        {
            if (_finished)
            {
                return;
            }

            _log.Step("глава 1 пройдена: S1–S5 сданы");
            FinishRun();
        }

        /// <summary>Отслеживает зависание: нет прогресса — предупреждение, затем провал.</summary>
        private void CheckStuck(double now)
        {
            var fingerprint = Fingerprint();
            if (fingerprint != _progressFingerprint)
            {
                _progressFingerprint = fingerprint;
                _lastProgressAt = now;
                return;
            }

            if (now - _lastProgressAt > StuckFailSeconds)
            {
                Fail("нет прогресса " + Mathf.RoundToInt((float)(now - _lastProgressAt)) + " с");
                return;
            }

            if (now - _lastStuckWarnAt > StuckWarnSeconds &&
                now - _lastProgressAt > StuckWarnSeconds)
            {
                _lastStuckWarnAt = now;
                _log.Warn("нет прогресса " + Mathf.RoundToInt((float)(now - _lastProgressAt)) + " с");
            }
        }

        /// <summary>Отпечаток прогресса: квесты, деньги, локация, смерти, здоровье.</summary>
        private int Fingerprint()
        {
            var screen = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            var session = screen == null ? null : screen.Session;
            if (session == null)
            {
                return 0;
            }

            var hash = 17;
            var player = session.State.Player;
            var world = session.State.World;
            hash = hash * 31 + world.CurrentLocationId.GetHashCode();
            hash = hash * 31 + world.Clock.AbsoluteHours.GetHashCode();
            if (player == null)
            {
                return hash;
            }

            hash = hash * 31 + player.Health;
            hash = hash * 31 + player.Money;
            hash = hash * 31 + player.Deaths;
            foreach (var quest in player.StoryQuests)
            {
                hash = hash * 31 + quest.QuestId.GetHashCode();
                hash = hash * 31 + (int)quest.Status;
                foreach (var objective in quest.Progress)
                {
                    hash = hash * 31 + objective.Current;
                }
            }

            return hash;
        }

        /// <summary>Текст отчёта о прогоне для итогового файла и журнала.</summary>
        private string BuildReport()
        {
            var screen = UnityEngine.Object.FindAnyObjectByType<GameScreen>();
            var session = screen == null ? null : screen.Session;
            var report = new StringBuilder();
            report.AppendLine("Автопрогон сюжета главы 1");
            report.AppendLine("Результат: " + (_failReason == null ? "глава пройдена" : "провал: " + _failReason));
            report.AppendLine("Длительность: " + TimeSpan.FromSeconds(Seconds).ToString(@"hh\:mm\:ss"));
            report.AppendLine("Действий: " + _actions);
            if (session == null)
            {
                return report.ToString();
            }

            var player = session.State.Player;
            var world = session.State.World;
            report.AppendLine("Локация: " + world.CurrentLocation.TemplateId + " (" + world.CurrentLocationId + ")");
            report.AppendLine("Часы мира: " + world.Clock.AbsoluteHours.ToString("0.#", CultureInfo.InvariantCulture));
            if (player != null)
            {
                report.AppendLine("Игрок: уровень " + player.Level + ", здоровье " + player.Health +
                                  ", деньги " + player.Money + ", смертей " + player.Deaths);
                foreach (var questId in AutoPlayIds.StoryQuestOrder)
                {
                    var status = player.HasCompletedQuest(questId) ? "сдан" : "не сдан";
                    report.AppendLine("  " + questId + ": " + status + ProgressLine(player, questId));
                }
            }

            return report.ToString();
        }

        private static string ProgressLine(AnomalyPath.Core.Player.PlayerState player, string questId)
        {
            foreach (var quest in player.StoryQuests)
            {
                if (!string.Equals(quest.QuestId, questId, System.StringComparison.Ordinal))
                {
                    continue;
                }

                var text = new StringBuilder(" (");
                for (var index = 0; index < quest.Progress.Count; index++)
                {
                    if (index > 0)
                    {
                        text.Append(", ");
                    }

                    text.Append(quest.Progress[index].Type).Append(' ').Append(quest.Progress[index].Current);
                }

                return text.Append(')').ToString();
            }

            return string.Empty;
        }
    }
}
