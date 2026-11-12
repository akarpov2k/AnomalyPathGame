#nullable enable
using System.Collections.Generic;

namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Сумма модификаторов бойца от статусов: травмы, временные эффекты медицины и скиллов.
    /// </summary>
    public sealed class CombatModifiers
    {
        private static readonly IReadOnlyDictionary<string, int> NoProtection =
            new Dictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>Нулевые модификаторы: ничего не меняют.</summary>
        public static CombatModifiers None { get; } = new CombatModifiers(0, 0);

        /// <summary>Создаёт набор модификаторов.</summary>
        /// <param name="accuracyBonus">Прибавка к меткости.</param>
        /// <param name="actionApCostBonus">Прибавка к цене действия в ОД; может быть отрицательной.</param>
        /// <param name="protection">Прибавка к защите по типам урона; <c>null</c> — без прибавки.</param>
        /// <param name="damageMultiplier">Доля прибавки к урону (0.25 = +25%); 0 — без прибавки.</param>
        public CombatModifiers(
            double accuracyBonus,
            int actionApCostBonus,
            IReadOnlyDictionary<string, int>? protection = null,
            double damageMultiplier = 0)
        {
            AccuracyBonus = accuracyBonus;
            ActionApCostBonus = actionApCostBonus;
            Protection = protection ?? NoProtection;
            DamageMultiplier = damageMultiplier;
        }

        /// <summary>Прибавка к меткости.</summary>
        public double AccuracyBonus { get; }

        /// <summary>Прибавка к цене действия в ОД.</summary>
        public int ActionApCostBonus { get; }

        /// <summary>Доля прибавки к урону; 0 — без прибавки.</summary>
        public double DamageMultiplier { get; }

        /// <summary>Прибавка к защите по типам урона.</summary>
        public IReadOnlyDictionary<string, int> Protection { get; }

        /// <summary>Возвращает прибавку к защите от типа урона.</summary>
        /// <param name="damageTypeId">Тип урона.</param>
        /// <returns>Прибавка; 0, если её нет.</returns>
        public int ProtectionOf(string damageTypeId) =>
            damageTypeId != null && Protection.TryGetValue(damageTypeId, out var value) ? value : 0;

        /// <summary>Складывает модификаторы статусов.</summary>
        /// <param name="statuses">Статусы; пустой список даёт <see cref="None"/>.</param>
        /// <returns>Сумма модификаторов.</returns>
        /// <exception cref="System.ArgumentNullException">Список не передан.</exception>
        public static CombatModifiers FromStatuses(IEnumerable<CombatStatus> statuses)
        {
            if (statuses == null)
            {
                throw new System.ArgumentNullException(nameof(statuses));
            }

            double accuracy = 0;
            var actionAp = 0;
            double damage = 0;
            Dictionary<string, int>? protection = null;
            foreach (var status in statuses)
            {
                accuracy += status.Modifiers.AccuracyBonus;
                actionAp += status.Modifiers.ActionApCostBonus;
                damage += status.Modifiers.DamageMultiplier;
                foreach (var pair in status.Modifiers.Protection)
                {
                    protection ??= new Dictionary<string, int>(System.StringComparer.Ordinal);
                    protection.TryGetValue(pair.Key, out var current);
                    protection[pair.Key] = current + pair.Value;
                }
            }

            return new CombatModifiers(accuracy, actionAp, protection, damage);
        }
    }
}
