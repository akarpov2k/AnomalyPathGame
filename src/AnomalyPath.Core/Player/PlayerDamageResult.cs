#nullable enable
using AnomalyPath.Core.Death;

namespace AnomalyPath.Core.Player
{
    /// <summary>Результат урона вне боя: сколько прошло, сколько осталось и что стало со смертью.</summary>
    public sealed class PlayerDamageResult
    {
        /// <summary>Создаёт результат урона.</summary>
        /// <param name="rawDamage">Заявленный урон до защиты.</param>
        /// <param name="protection">Защита персонажа от типа урона.</param>
        /// <param name="damage">Урон, прошедший по здоровью.</param>
        /// <param name="healthBefore">Здоровье до удара.</param>
        /// <param name="healthAfter">Здоровье после удара и возможного респавна.</param>
        /// <param name="died">Здоровье опустилось до нуля.</param>
        /// <param name="permadeath">Режим постоянной смерти: респавна нет.</param>
        /// <param name="death">Потеря вещей и опыта; <c>null</c>, если смерти не было.</param>
        /// <param name="respawnHubId">Хаб, куда вернулся игрок; <c>null</c> — респавна не было.</param>
        public PlayerDamageResult(
            int rawDamage,
            int protection,
            int damage,
            int healthBefore,
            int healthAfter,
            bool died,
            bool permadeath = false,
            DeathResult? death = null,
            string? respawnHubId = null)
        {
            RawDamage = rawDamage;
            Protection = protection;
            Damage = damage;
            HealthBefore = healthBefore;
            HealthAfter = healthAfter;
            Died = died;
            Permadeath = permadeath;
            Death = death;
            RespawnHubId = respawnHubId;
        }

        /// <summary>Заявленный урон до защиты.</summary>
        public int RawDamage { get; }

        /// <summary>Защита персонажа от типа урона.</summary>
        public int Protection { get; }

        /// <summary>Урон, прошедший по здоровью.</summary>
        public int Damage { get; }

        /// <summary>Здоровье до удара.</summary>
        public int HealthBefore { get; }

        /// <summary>Здоровье после удара и возможного респавна.</summary>
        public int HealthAfter { get; }

        /// <summary>Здоровье опустилось до нуля.</summary>
        public bool Died { get; }

        /// <summary>Режим постоянной смерти: респавна нет.</summary>
        public bool Permadeath { get; }

        /// <summary>Потеря вещей и опыта; <c>null</c>, если смерти не было.</summary>
        public DeathResult? Death { get; }

        /// <summary>Хаб, куда вернулся игрок после смерти; <c>null</c> — респавна не было.</summary>
        public string? RespawnHubId { get; }
    }
}
