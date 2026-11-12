#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Player;
using AnomalyPath.Core.Save;

namespace AnomalyPath.Core.Session
{
    /// <summary>Игровая сессия: состояние прохождения и текущий бой, если он идёт.</summary>
    public sealed class GameSession
    {
        /// <summary>Создаёт сессию поверх состояния прохождения.</summary>
        /// <param name="state">Состояние мира и игрока.</param>
        /// <param name="combat">Текущий бой или <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Состояние не задано.</exception>
        public GameSession(GameState state, CombatState? combat = null)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Combat = combat;
        }

        /// <summary>Состояние мира и игрока.</summary>
        public GameState State { get; }

        /// <summary>Текущий бой или <c>null</c>.</summary>
        public CombatState? Combat { get; private set; }

        /// <summary>Начатый переход между хабами, ожидающий окончания боя на дороге; иначе <c>null</c>.</summary>
        public PendingHubTravel? PendingHubTravel { get; private set; }

        /// <summary><c>true</c>, если сейчас идёт бой.</summary>
        public bool InCombat => Combat != null;

        /// <summary>Запоминает или сбрасывает ожидающий переход между хабами.</summary>
        /// <param name="travel">Переход или <c>null</c>, чтобы сбросить.</param>
        internal void SetPendingHubTravel(PendingHubTravel? travel)
        {
            PendingHubTravel = travel;
        }

        /// <summary>Начинает или завершает бой.</summary>
        /// <param name="combat">Новый бой или <c>null</c>, чтобы завершить текущий.</param>
        internal void SetCombat(CombatState? combat)
        {
            if (combat == null && Combat != null)
            {
                SyncPlayerInjuries(Combat);
            }

            Combat = combat;
        }

        /// <summary>
        /// Переносит травмы игрока из боя в состояние игрока: травмы врага живут только в бою.
        /// </summary>
        /// <param name="combat">Завершаемый бой.</param>
        private void SyncPlayerInjuries(CombatState combat)
        {
            var player = State.Player;
            if (player == null)
            {
                return;
            }

            var injuries = new List<InjuryState>();
            foreach (var status in combat.Player.Statuses)
            {
                if (!status.IsInjury)
                {
                    // Эффекты медицины (бафы) живут до конца боя и в состояние игрока не попадают.
                    continue;
                }

                injuries.Add(new InjuryState(status.Id, status.Kind, status.Remaining));
            }

            player.SetInjuries(injuries);
        }
    }
}
