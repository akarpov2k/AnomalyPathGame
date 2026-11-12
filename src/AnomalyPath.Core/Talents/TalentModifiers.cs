#nullable enable
namespace AnomalyPath.Core.Talents
{
    /// <summary>Собранные бонусы дерева талантов для текущего состояния игрока.</summary>
    public sealed class TalentModifiers
    {
        /// <summary>Пустые бонусы.</summary>
        public static TalentModifiers None { get; } = new TalentModifiers();

        /// <summary>Создаёт набор бонусов.</summary>
        /// <param name="apMax">Прибавка к максимуму ОД.</param>
        /// <param name="healthMax">Прибавка к максимуму здоровья.</param>
        /// <param name="accuracy">Прибавка к меткости.</param>
        /// <param name="staminaMax">Прибавка к максимуму выносливости.</param>
        /// <param name="staminaRecovery">Прибавка к восстановлению выносливости за час отдыха.</param>
        /// <param name="maxFood">Прибавка к максимуму еды.</param>
        /// <param name="maxWater">Прибавка к максимуму воды.</param>
        public TalentModifiers(
            int apMax = 0,
            int healthMax = 0,
            double accuracy = 0,
            int staminaMax = 0,
            int staminaRecovery = 0,
            int maxFood = 0,
            int maxWater = 0)
        {
            ApMax = apMax;
            HealthMax = healthMax;
            Accuracy = accuracy;
            StaminaMax = staminaMax;
            StaminaRecovery = staminaRecovery;
            MaxFood = maxFood;
            MaxWater = maxWater;
        }

        /// <summary>Прибавка к максимуму ОД.</summary>
        public int ApMax { get; }

        /// <summary>Прибавка к максимуму здоровья.</summary>
        public int HealthMax { get; }

        /// <summary>Прибавка к меткости.</summary>
        public double Accuracy { get; }

        /// <summary>Прибавка к максимуму выносливости.</summary>
        public int StaminaMax { get; }

        /// <summary>Прибавка к восстановлению выносливости за час отдыха.</summary>
        public int StaminaRecovery { get; }

        /// <summary>Прибавка к максимуму еды.</summary>
        public int MaxFood { get; }

        /// <summary>Прибавка к максимуму воды.</summary>
        public int MaxWater { get; }
    }
}
