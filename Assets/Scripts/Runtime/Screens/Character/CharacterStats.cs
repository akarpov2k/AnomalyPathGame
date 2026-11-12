#nullable enable
using System;
using AnomalyPath.Core.Player;
using AnomalyPath.Runtime.Ui.Core;

namespace AnomalyPath.Runtime.Screens.Character
{
    /// <summary>
    /// Расчёт характеристик персонажа для экрана: предел здоровья складывается из базы актёра,
    /// прибавки за уровни, талантов и надетых артефактов; предел выносливости — из мира,
    /// талантов и артефактов.
    /// </summary>
    public static class CharacterStats
    {
        /// <summary>Считает текущий предел здоровья игрока.</summary>
        /// <param name="host">Хост интерфейса: правила, актёры, таланты и артефакты.</param>
        /// <param name="player">Состояние игрока.</param>
        /// <returns>Предел здоровья, не меньше единицы.</returns>
        public static int MaxHealth(IUiHost host, PlayerState player)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var bonus = host.Talents.GetModifiers(player).HealthMax +
                        player.LevelHealthBonus +
                        host.Artifacts.GetEquippedModifiers(player.Equipment).HealthMax;
            try
            {
                var rules = host.Rules.Rules;
                if (rules == null)
                {
                    return Math.Max(1, player.Health + bonus);
                }

                return (host.Actors.Get(rules.PlayerActorId).Combat?.Health ?? Math.Max(1, player.Health)) + bonus;
            }
            catch (Exception)
            {
                return Math.Max(1, player.Health + player.LevelHealthBonus);
            }
        }

        /// <summary>Считает текущий предел выносливости игрока.</summary>
        /// <param name="host">Хост интерфейса: сессия, таланты и артефакты.</param>
        /// <param name="player">Состояние игрока.</param>
        /// <returns>Предел выносливости, не меньше единицы.</returns>
        public static int StaminaMax(IUiHost host, PlayerState player)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            if (player == null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            var world = host.Session?.State.World;
            var artifactBonus = host.Artifacts.GetEquippedModifiers(player.Equipment);
            var talentBonus = host.Talents.GetModifiers(player);
            return Math.Max(1, (world?.StaminaMax ?? 1)
                + Math.Max(0, artifactBonus.Stamina)
                + Math.Max(0, talentBonus.StaminaMax));
        }
    }
}
