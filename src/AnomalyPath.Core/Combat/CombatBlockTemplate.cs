#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Боевые характеристики актёра из данных.</summary>
    public sealed class CombatBlockTemplate
    {
        /// <summary>Создаёт боевой блок.</summary>
        /// <param name="tier">Тир.</param>
        /// <param name="level">Уровень актёра.</param>
        /// <param name="health">Здоровье.</param>
        /// <param name="ap">Очки действия.</param>
        /// <param name="protection">Защита по типам урона.</param>
        /// <param name="accuracy">Базовая меткость 0..1.</param>
        /// <param name="accuracyBonus">Бонус меткости от талантов.</param>
        /// <param name="xp">Опыт за победу над актёром; у врага обязателен и больше нуля.</param>
        /// <param name="attack">Чем актёр атакует; <c>null</c> — атаки нет.</param>
        /// <param name="skills">Скиллы без оружия, только у мутанта; <c>null</c> — пустой список.</param>
        /// <param name="defaultSkillId">Скилл стандартной атаки; только у мутанта, <c>null</c> — нет.</param>
        /// <param name="medkitItemId">Аптечка противника; <c>null</c> — аптечки нет.</param>
        /// <param name="medkitCharges">Сколько раз противник может применить аптечку; 0 — не может.</param>
        public CombatBlockTemplate(
            int tier,
            int level,
            int health,
            int ap,
            IReadOnlyDictionary<string, int> protection,
            double accuracy,
            double accuracyBonus,
            int xp = 0,
            ActorAttackTemplate? attack = null,
            IReadOnlyList<string>? skills = null,
            string? defaultSkillId = null,
            string? medkitItemId = null,
            int medkitCharges = 0)
        {
            Tier = tier;
            Level = level;
            Health = health;
            Ap = ap;
            Protection = protection;
            Accuracy = accuracy;
            AccuracyBonus = accuracyBonus;
            Xp = xp;
            Attack = attack;
            Skills = skills ?? Array.Empty<string>();
            DefaultSkillId = defaultSkillId;
            MedkitItemId = medkitItemId;
            MedkitCharges = medkitCharges;
        }

        /// <summary>Опыт за победу над актёром.</summary>
        public int Xp { get; }

        /// <summary>Чем актёр атакует; <c>null</c> — атаки нет.</summary>
        public ActorAttackTemplate? Attack { get; }

        /// <summary>Скиллы без оружия; пустой список у актёров с оружием.</summary>
        public IReadOnlyList<string> Skills { get; }

        /// <summary>Скилл стандартной атаки; <c>null</c> у актёров с оружием.</summary>
        public string? DefaultSkillId { get; }

        /// <summary>Аптечка противника; <c>null</c> — пользоваться медициной он не может.</summary>
        public string? MedkitItemId { get; }

        /// <summary>Сколько раз за бой противник может применить аптечку.</summary>
        public int MedkitCharges { get; }

        /// <summary>Базовая меткость актёра, 0..1.</summary>
        public double Accuracy { get; }

        /// <summary>Бонус меткости от талантов, прибавляется к базовой.</summary>
        public double AccuracyBonus { get; }

        /// <summary>Тир.</summary>
        public int Tier { get; }

        /// <summary>Уровень актёра.</summary>
        public int Level { get; }

        /// <summary>Здоровье.</summary>
        public int Health { get; }

        /// <summary>Очки действия.</summary>
        public int Ap { get; }

        /// <summary>Базовая защита персонажа по типам урона (характеристика актёра).</summary>
        public IReadOnlyDictionary<string, int> Protection { get; }
    }
}
