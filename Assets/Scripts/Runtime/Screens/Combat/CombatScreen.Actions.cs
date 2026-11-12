#nullable enable
using System;
using System.Collections;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime.Screens.Rewards;
using AnomalyPath.Runtime.Ui.Core;
using UnityEngine;
using UnityEngine.UI;

namespace AnomalyPath.Runtime.Screens.Combat
{
    /// <summary>Часть класса <see cref="CombatScreen"/>: атака, переход, конец хода, ход ИИ и журнал.</summary>
    public sealed partial class CombatScreen
    {
        private void Attack()
        {
            CloseMenus();
            HideTooltip();
            var session = Host.Session;
            var combat = session?.Combat;
            var weapon = PlayerWeapon(combat);
            if (session == null || combat == null || weapon == null)
            {
                return;
            }

            var attacked = false;
            var hit = false;
            if (Host.Sessions.TryAttack(session, weapon, out var result))
            {
                attacked = true;
                hit = result.Hit;
                AddLine(result.Hit
                    ? UiText.Format(Host.Texts, UiTextKey.Combat_Log_Hit, result.Damage)
                    : UiText.Get(Host.Texts, UiTextKey.Combat_Log_Miss));
            }

            Refresh();
            if (attacked && playerFigure != null)
            {
                playerFigure.Fire(enemyFigure, hit);
                ShowFloating(enemyFigure, hit, result.Damage);
            }
        }

        /// <summary>
        /// Завершает ход игрока и разыгрывает ход противника по одному действию:
        /// каждое действие — отдельная строка журнала и пауза, чтобы его было видно.
        /// </summary>
        private void EndTurn()
        {
            CloseMenus();
            HideTooltip();
            var session = Host.Session;
            if (session == null || _enemyTurnRunning)
            {
                return;
            }

            _enemyTurnRunning = true;
            Host.RunCoroutine(EnemyTurnRoutine(session));
        }

        /// <summary>Корутина хода противника с паузой между действиями.</summary>
        /// <param name="session">Игровая сессия.</param>
        /// <returns>Корутина теста и игры.</returns>
        private IEnumerator EnemyTurnRoutine(GameSession session)
        {
            _lastEnemyActions.Clear();
            Host.Sessions.EndTurn(session);
            Refresh();

            while (true)
            {
                var combat = session.Combat;
                if (combat == null || combat.IsFinished || !ReferenceEquals(combat.Current, combat.Enemy))
                {
                    break;
                }

                if (!Host.Sessions.TryRunEnemyTurnStep(session, out var action))
                {
                    break;
                }

                _lastEnemyActions.Add(action);
                AddLine(ActionLine(action));
                Refresh();
                if (action.Step.Kind == EnemyTurnActionKind.Skill)
                {
                    if (session.Combat?.EnemyWeapon != null)
                    {
                        enemyFigure?.Fire(playerFigure, action.Hit);
                    }
                    else
                    {
                        enemyFigure?.Lunge(playerFigure, action.Hit);
                    }

                    ShowFloating(playerFigure, action.Hit, action.Damage);
                    yield return new WaitForSeconds(0.18f);
                }

                yield return new WaitForSeconds(CombatMetrics.EnemyActionPause);
            }

            var after = session.Combat;
            if (after != null && !after.IsFinished && ReferenceEquals(after.Current, after.Enemy))
            {
                Host.Sessions.EndTurn(session);
            }

            _enemyTurnRunning = false;
            Refresh();
        }

        /// <summary>Строка журнала для одного действия противника.</summary>
        /// <param name="action">Действие противника.</param>
        /// <returns>Готовая строка журнала.</returns>
        private string ActionLine(EnemyTurnAction action)
        {
            switch (action.Step.Kind)
            {
                case EnemyTurnActionKind.Reload:
                    return UiText.Get(Host.Texts, UiTextKey.Combat_Log_Enemy_Reload);
                case EnemyTurnActionKind.Medkit:
                    return UiText.Format(Host.Texts, UiTextKey.Combat_Log_Enemy_Medkit, action.Healed);
                default:
                    return action.Hit
                        ? UiText.Format(Host.Texts, UiTextKey.Combat_Log_Enemy_Hit, action.Damage)
                        : UiText.Get(Host.Texts, UiTextKey.Combat_Log_Enemy_Miss);
            }
        }

        /// <summary>
        /// Возврат после боя: применяет итог, открывает экран наград, если есть лут,
        /// и сообщает победу или поражение.
        /// </summary>
        private void Leave()
        {
            CloseMenus();
            CloseJournal();
            HideTooltip();
            var session = Host.Session;
            if (session == null)
            {
                return;
            }

            var outcome = Host.Sessions.ApplyCombatOutcome(session);
            Host.ShowGame();
            if (outcome.Loot.Count > 0)
            {
                Host.ShowRewards(outcome.Loot, 0, RewardSource.Victory);
                return;
            }

            if (outcome.Status == CombatOutcomeStatus.Victory)
            {
                Host.Toast(UiText.Get(Host.Texts, UiTextKey.Combat_Result_Victory));
            }
            else if (outcome.Status == CombatOutcomeStatus.Defeat)
            {
                Host.Toast(UiText.Get(Host.Texts, UiTextKey.Combat_Result_Defeat));
            }
        }

        /// <summary>
        /// Показывает над фигурой число урона или слово «Промах». Текст готовый: число
        /// подставляется из результата удара, а слово берётся из каталога строк.
        /// </summary>
        /// <param name="figure">Фигура, по которой пришёлся удар.</param>
        /// <param name="hit">Был ли удар попаданием.</param>
        /// <param name="damage">Урон попадания.</param>
        private void ShowFloating(CombatFigureView? figure, bool hit, double damage)
        {
            if (figure == null)
            {
                return;
            }

            if (hit)
            {
                figure.ShowFloating(
                    UiText.Format(Host.Texts, UiTextKey.Combat_Floating_Damage, Math.Round(damage)),
                    CombatPalette.DamageText);
                return;
            }

            figure.ShowFloating(
                UiText.Get(Host.Texts, UiTextKey.Combat_Floating_Miss),
                CombatPalette.MissText);
        }

        /// <summary>Добавляет строку в журнал, держа последние строки в пределах лимита.</summary>
        /// <param name="line">Готовая строка журнала.</param>
        private void AddLine(string line)
        {
            _lines.Add(line);
            while (_lines.Count > CombatMetrics.LogLines)
            {
                _lines.RemoveAt(0);
            }
        }
    }
}
