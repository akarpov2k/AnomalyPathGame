#nullable enable
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Inventory;
using AnomalyPath.Core.Session;
using AnomalyPath.Runtime;
using UnityEngine;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>Часть класса <see cref="AutoPlayDriver"/>: бой.</summary>
    internal sealed partial class AutoPlayDriver
    {
        /// <summary>Боевой экран: атака, перезарядка, медицина и конец хода.</summary>
        private void HandleCombat(GameScreen screen, GameSession session)
        {
            var combat = session.Combat;
            if (combat == null)
            {
                screen.ShowGame(false);
                ScheduleNext();
                return;
            }

            if (combat.IsFinished)
            {
                _attackTries = 0;
                TryClick(AutoPlayIds.CombatBackButton, "бой: возвращаюсь на локацию");
                return;
            }

            if (!ReferenceEquals(combat.Current, combat.Player))
            {
                // Ход противника: ждём, не нажимая кнопок.
                return;
            }

            if (TryUseCombatMedicine(screen, session, combat))
            {
                return;
            }

            var weapon = combat.Player.Equipment?.Get(combat.PlayerWeaponSlot);
            if (weapon == null)
            {
                TryClick(AutoPlayIds.CombatEndTurnButton, "бой: оружия нет, конец хода");
                return;
            }

            if (weapon.MagazineRounds <= 0 || _combatReloadPending)
            {
                if (_combatReloadPending && TryClick(AutoPlayIds.CombatReloadButton, "бой: перезарядка"))
                {
                    _combatReloadPending = false;
                    return;
                }

                if (EnsureAmmo(screen, session, weapon))
                {
                    _combatReloadPending = true;
                    if (TryClick(AutoPlayIds.CombatSkillsButton, "бой: открываю скиллы для перезарядки"))
                    {
                        return;
                    }
                }

                TryClick(AutoPlayIds.CombatEndTurnButton, "бой: патронов нет, конец хода");
                return;
            }

            if (combat.Player.Ap <= 0)
            {
                _attackTries = 0;
                TryClick(AutoPlayIds.CombatEndTurnButton, "бой: ОД кончились, конец хода");
                return;
            }

            var apBefore = combat.Player.Ap;
            if (TryClick(
                    AutoPlayIds.CombatAttackButton,
                    "бой: атака (ОД " + combat.Player.Ap + ", магазин " + weapon.MagazineRounds + ")"))
            {
                if (combat.Player.Ap == apBefore)
                {
                    _attackTries++;
                    if (_attackTries >= 3)
                    {
                        _attackTries = 0;
                        TryClick(AutoPlayIds.CombatEndTurnButton, "бой: атака не срабатывает, конец хода");
                    }
                }
                else
                {
                    _attackTries = 0;
                }

                return;
            }

            TryClick(AutoPlayIds.CombatEndTurnButton, "бой: атака недоступна, конец хода");
        }

        /// <summary>Лечение в бою: аптечка из быстрого слота, когда здоровье ниже 45%.</summary>
        private bool TryUseCombatMedicine(GameScreen screen, GameSession session, CombatState combat)
        {
            if (combat.Player.MaxHealth <= 0 ||
                combat.Player.Health / combat.Player.MaxHealth > 0.45)
            {
                return false;
            }

            var equipment = combat.Player.Equipment;
            if (equipment == null)
            {
                return false;
            }

            for (var index = 0; index < equipment.QuickSlotCount; index++)
            {
                var item = equipment.GetQuick(index);
                if (item == null || !screen.Sessions.CanUseMedicine(session, item))
                {
                    continue;
                }

                if (screen.Sessions.TryUseMedicine(session, item, out var result))
                {
                    _log.Assist("бой: аптечка +" + result.HealthRestored + " HP (было " +
                                Mathf.RoundToInt((float)combat.Player.Health) + ")");
                    _actions++;
                    ScheduleNext();
                    return true;
                }
            }

            // Аптечки в быстром слоте нет: берём боевую из инвентаря и кладём в слот.
            if (TryPutMedkitInQuickSlot(screen, session))
            {
                return true;
            }

            return false;
        }

        /// <summary>Проверяет, что бой идёт, и вопрос «чей ход» нужен тестам прогона.</summary>
        /// <param name="combat">Состояние боя.</param>
        /// <returns><c>true</c>, если сейчас ход игрока.</returns>
        public static bool IsPlayerTurn(CombatState? combat) =>
            combat != null && !combat.IsFinished && ReferenceEquals(combat.Current, combat.Player);
    }
}
