#nullable enable
using System;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Minigames;
using AnomalyPath.Core.Quests.Story;
using AnomalyPath.Core.Session;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;
using AnomalyPath.Runtime;
using AnomalyPath.Runtime.Screens.Game;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>Часть класса <see cref="AutoPlayDriver"/>: локация, объекты, переходы и замок тайника.</summary>
    internal sealed partial class AutoPlayDriver
    {
        /// <summary>Действия на экране локации: бой, тайник, лут, NPC и переход.</summary>
        private void HandleLocation(GameScreen screen, GameSession session)
        {
            if (session.InCombat)
            {
                screen.ShowCombat();
                ScheduleNext();
                return;
            }

            var world = session.State.World;
            var location = world.CurrentLocation;
            if (!string.Equals(_locationId, location.InstanceId, StringComparison.Ordinal))
            {
                _locationId = location.InstanceId;
                _handled.Clear();
                _lockIndex = 0;
                _lockLocationId = string.Empty;
                _visited.Add(location.InstanceId);
                _log.Step("локация: " + location.TemplateId + " (" + location.InstanceId + "), " +
                          location.Subtype + ", тир " + location.Tier);
                RestoreNeeds(session);
                var player = session.State.Player;
                if (player != null)
                {
                    var max = screen.Damage.MaxHealth(session.State);
                    if (max > 0 && player.Health < max / 2)
                    {
                        HealOutsideCombat(screen, session);
                    }
                }
            }

            var active = AutoPlayScenario.ActiveStory(session);
            if (active == null)
            {
                if (AutoPlayScenario.IsChapterComplete(session))
                {
                    Succeed();
                    return;
                }

                ReturnToHub(screen, session);
                return;
            }

            var needs = AutoPlayScenario.Needs(screen, session, active);

            if (location.Enemy is EnemyLocationObject enemy && !enemy.Looted && ShouldFight(needs, location, enemy))
            {
                if (PrepareForFight(screen, session))
                {
                    return;
                }

                if (ClickObject(enemy.SpawnId, "бой: " + enemy.ActorId + " на " + location.TemplateId))
                {
                    return;
                }
            }

            if (needs.NeedStash && location.StashPresent && !location.StashOpened &&
                _handled.Add("stash:" + location.InstanceId))
            {
                if (Click(AutoPlayIds.StashButton, "тайник: вскрываю замок"))
                {
                    return;
                }
            }

            if (needs.NeedLoot)
            {
                foreach (var locationObject in location.Objects)
                {
                    if (locationObject is not LootLocationObject loot || !loot.HasLoot || loot.Looted)
                    {
                        continue;
                    }

                    if (!_handled.Add("loot:" + location.InstanceId + ":" + loot.SpawnId))
                    {
                        continue;
                    }

                    if (ClickObject(loot.SpawnId, "осматриваю объект лута " + loot.SpawnId))
                    {
                        return;
                    }
                }
            }

            if (needs.NeedChain)
            {
                foreach (var locationObject in location.Objects)
                {
                    if (locationObject is not NpcLocationObject npc)
                    {
                        continue;
                    }

                    var key = "npc:" + npc.SpawnId;
                    if (!_talked.Add(key))
                    {
                        continue;
                    }

                    _plan = new DialoguePlan(active.QuestId, turnIn: false, npc: true,
                        new[] { "flash", "ok", "bye" });
                    if (ClickObject(npc.SpawnId, "разговор с " + npc.ActorId))
                    {
                        return;
                    }
                }
            }

            Travel(screen, session, needs);
        }

        /// <summary>Нужен ли бой по целям квеста и виду локации.</summary>
        private static bool ShouldFight(QuestNeeds needs, LocationInstance location, EnemyLocationObject enemy)
        {
            if (needs.NeedKills && needs.KillPool.Contains(enemy.ActorId))
            {
                return true;
            }

            // Зачистка локации и цепочки требует убрать противника с небазавой локации.
            return (needs.NeedClear || needs.NeedChain) &&
                   location.Subtype != LocationSubtype.Path &&
                   location.Subtype != LocationSubtype.Hub;
        }

        /// <summary>Нажимает фигуру объекта локации: противника, лут или NPC.</summary>
        private bool ClickObject(string spawnId, string action)
        {
            var name = AutoPlayIds.LocationObjectPrefix + spawnId;
            var target = GameObject.Find(name);
            if (target == null)
            {
                _log.Warn("на локации нет объекта «" + name + "»");
                return false;
            }

            var button = target.GetComponent<Button>();
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

        private int _travelFailures;

        /// <summary>Выбирает переход: цепочка, путь, ветка или возврат в хаб.</summary>
        private void Travel(GameScreen screen, GameSession session, QuestNeeds needs)
        {
            var edges = screen.Sessions.AvailableEdges(session);
            if (edges.Count == 0)
            {
                ReturnToHub(screen, session);
                return;
            }

            var index = ChooseEdge(screen, session, edges, needs);
            if (index < 0)
            {
                ReturnToHub(screen, session);
                return;
            }

            var world = session.State.World;
            if (world.Stamina < 3)
            {
                _log.Assist("сил на переход мало: " + world.Stamina + " → " + world.StaminaMax);
                RestoreStamina(session);
            }

            var before = world.CurrentLocationId;
            if (ClickTransition(edges, index))
            {
                if (!string.Equals(world.CurrentLocationId, before, StringComparison.Ordinal))
                {
                    _travelFailures = 0;
                    return;
                }

                _travelFailures++;
                _log.Warn("переход не состоялся: " + edges[index].EdgeId + " (попытка " + _travelFailures + ")");
                if (_travelFailures >= 2)
                {
                    RestoreStamina(session);
                }

                if (_travelFailures > 8)
                {
                    Fail("переход " + edges[index].EdgeId + " не выполняется");
                }

                ScheduleNext();
                return;
            }

            _log.Warn("переход не найден в сцене, иду сервисом: " + edges[index].EdgeId);
            var result = screen.Sessions.Travel(session, edges[index].EdgeId);
            if (!result.Succeeded)
            {
                _travelFailures++;
                if (_travelFailures >= 2)
                {
                    RestoreStamina(session);
                }

                _log.Warn("переход сервисом не удался: " + result.Status);
                if (_travelFailures > 8)
                {
                    Fail("переход " + edges[index].EdgeId + " не удался: " + result.Status);
                }

                ScheduleNext();
                return;
            }

            _travelFailures = 0;
            _actions++;
            screen.ShowGame(false);
            ScheduleNext();
        }

        /// <summary>Номер перехода в порядке переходов экрана.</summary>
        private int ChooseEdge(
            GameScreen screen,
            GameSession session,
            System.Collections.Generic.IReadOnlyList<WorldEdge> edges,
            QuestNeeds needs)
        {
            var world = session.State.World;

            if (needs.NeedChain && !string.IsNullOrEmpty(needs.ChainId))
            {
                var entry = QuestChainInstanceIds.For(needs.ChainId!, 0);
                for (var index = 0; index < edges.Count; index++)
                {
                    if (string.Equals(edges[index].ToId, entry, StringComparison.Ordinal) &&
                        !_visited.Contains(entry))
                    {
                        return index;
                    }
                }
            }

            // Зачистка, лут и тайники живут на небазавых локациях: сначала ветки.
            if (needs.NeedClear || needs.NeedLoot || needs.NeedStash || needs.NeedChain)
            {
                for (var index = 0; index < edges.Count; index++)
                {
                    var target = world.Map.GetLocation(edges[index].ToId);
                    if (!_visited.Contains(target.InstanceId) &&
                        target.Subtype != LocationSubtype.Hub &&
                        target.Subtype != LocationSubtype.Path)
                    {
                        return index;
                    }
                }
            }

            for (var index = 0; index < edges.Count; index++)
            {
                var target = world.Map.GetLocation(edges[index].ToId);
                if (_visited.Contains(target.InstanceId) || target.Subtype != LocationSubtype.Path)
                {
                    continue;
                }

                return index;
            }

            if (needs.NeedChain)
            {
                for (var index = 0; index < edges.Count; index++)
                {
                    var target = world.Map.GetLocation(edges[index].ToId);
                    if (!_visited.Contains(target.InstanceId) && target.Subtype != LocationSubtype.Hub)
                    {
                        return index;
                    }
                }
            }

            // Цепочка пройдена: выход в хаб.
            for (var index = 0; index < edges.Count; index++)
            {
                if (world.Map.GetLocation(edges[index].ToId).Subtype == LocationSubtype.Hub)
                {
                    return index;
                }
            }

            // Нечего исследовать: с дороги возвращаемся в хаб, с ветки — на дорогу.
            if (world.CurrentLocation.Subtype == LocationSubtype.Path)
            {
                return -1;
            }

            for (var index = 0; index < edges.Count; index++)
            {
                if (world.Map.GetLocation(edges[index].ToId).Subtype == LocationSubtype.Path)
                {
                    return index;
                }
            }

            return edges.Count > 0 ? 0 : -1;
        }

        /// <summary>Нажимает объект-переход сцены по номеру перехода.</summary>
        private bool ClickTransition(System.Collections.Generic.IReadOnlyList<WorldEdge> edges, int index)
        {
            var forward = -1;
            var world = UnityEngine.Object.FindAnyObjectByType<GameScreen>()?.Session?.State.World;
            if (world == null)
            {
                return false;
            }

            for (var edgeIndex = 0; edgeIndex < edges.Count; edgeIndex++)
            {
                if (forward < 0 &&
                    world.Map.GetLocation(edges[edgeIndex].ToId).Subtype == LocationSubtype.Path)
                {
                    forward = edgeIndex;
                }
            }

            var layout = PlaySceneSlots.Layout(edges.Count, forward);
            if (index < 0 || index >= layout.Count)
            {
                return false;
            }

            var slotName = layout[index] switch
            {
                PlaySceneSlot.Center => AutoPlayIds.TransitionPrefix + "Center",
                PlaySceneSlot.RightLow => AutoPlayIds.TransitionPrefix + "Right",
                PlaySceneSlot.LeftHigh => AutoPlayIds.TransitionPrefix + "Left",
                _ => string.Empty
            };

            if (slotName.Length == 0)
            {
                return false;
            }

            var root = GameObject.Find(slotName);
            if (root == null)
            {
                return false;
            }

            var art = root.transform.Find(AutoPlayIds.TransitionArt);
            var button = art != null ? art.GetComponent<Button>() : null;
            if (button == null)
            {
                return false;
            }

            _log.Step("переход: " + edges[index].EdgeId + " → " +
                      world.Map.GetLocation(edges[index].ToId).TemplateId);
            button.onClick.Invoke();
            _actions++;
            ScheduleNext();
            return true;
        }

        /// <summary>Возврат в хаб: кнопка «В хаб» или переход на дорогу/в хаб.</summary>
        private void ReturnToHub(GameScreen screen, GameSession session)
        {
            if (WorldStaminaLow(session))
            {
                RestoreStamina(session);
            }

            if (TryClick(AutoPlayIds.ReturnToHubButton, "возвращаюсь в хаб"))
            {
                return;
            }

            var edges = screen.Sessions.AvailableEdges(session);
            var world = session.State.World;
            for (var index = 0; index < edges.Count; index++)
            {
                var target = world.Map.GetLocation(edges[index].ToId);
                if (target.Subtype == LocationSubtype.Path || target.Subtype == LocationSubtype.Hub)
                {
                    if (ClickTransition(edges, index))
                    {
                        return;
                    }
                }
            }

            _log.Warn("с локации " + world.CurrentLocationId + " нет пути в хаб");
            ScheduleNext();
        }

        /// <summary>Окно взлома: вводит код, посчитанный по seed локации.</summary>
        private void HandleLock(GameScreen screen, GameSession session)
        {
            var location = session.State.World.CurrentLocation;
            if (!string.Equals(_lockLocationId, location.InstanceId, StringComparison.Ordinal))
            {
                _lockLocationId = location.InstanceId;
                _lockIndex = 0;
            }

            var state = screen.Sessions.LockState(session);
            if (state == null)
            {
                _log.Warn("замок тайника не настроен: закрываю окно");
                screen.HandleEscape();
                ScheduleNext();
                return;
            }

            if (_lockIndex == 0)
            {
                var seed = StableHash.Combine(location.Seed, AutoPlayIds.StashLockSalt + location.InstanceId);
                var puzzle = new LockPuzzle(seed, state.CodeLength, state.AlphabetSize);
                var code = puzzle.CodeText();
                _log.Step("замок: код " + code + " (" + state.AttemptsLeft + " попыток)");
                for (var index = 0; index < code.Length; index++)
                {
                    var symbol = AutoPlayIds.LockSymbolPrefix + (code[index] - '1');
                    if (!TryClick(symbol, "замок: символ " + code[index]))
                    {
                        Fail("в окне замка нет кнопки " + symbol);
                        return;
                    }
                }
            }

            _lockIndex = 0;
            if (!TryClick(AutoPlayIds.LockSubmitButton, "замок: проверяю код"))
            {
                Fail("в окне замка нет кнопки проверки");
            }
        }
    }
}
