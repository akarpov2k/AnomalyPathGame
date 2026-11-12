#nullable enable
using System;
using System.Collections.Generic;

namespace AnomalyPath.Core.Artifacts
{
    /// <summary>
    /// Собранные модификаторы надетых артефактов. Одинаковые модификаторы складываются
    /// с убывающей отдачей: первый даёт полную величину, каждый следующий — с коэффициентом.
    /// </summary>
    public sealed class ArtifactModifiers
    {
        private readonly Dictionary<string, double> _protection;

        /// <summary>Пустой набор: артефактов нет или все они сломаны.</summary>
        public static ArtifactModifiers None { get; } = new ArtifactModifiers(Array.Empty<ArtifactModifier>(), 1);

        /// <summary>Собирает модификаторы в один набор.</summary>
        /// <param name="parts">Модификаторы в порядке надевания.</param>
        /// <param name="falloff">Коэффициент убывающей отдачи, больше нуля и не больше единицы.</param>
        /// <exception cref="ArgumentNullException">Список модификаторов не задан.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Коэффициент вне диапазона.</exception>
        public ArtifactModifiers(IEnumerable<ArtifactModifier> parts, double falloff = 1)
        {
            if (parts == null)
            {
                throw new ArgumentNullException(nameof(parts));
            }

            if (falloff <= 0 || falloff > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(falloff));
            }

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var health = 0.0;
            var accuracy = 0.0;
            var stamina = 0.0;
            _protection = new Dictionary<string, double>(StringComparer.Ordinal);

            foreach (var part in parts)
            {
                if (part == null)
                {
                    continue;
                }

                counts.TryGetValue(part.Key, out var seen);
                counts[part.Key] = seen + 1;
                var factor = part.Mode == ArtifactModifierMode.Diminishing ? Math.Pow(falloff, seen) : 1;
                var value = part.Value * factor;

                switch (part.Stat)
                {
                    case ArtifactStat.HealthMax:
                        health += value;
                        break;
                    case ArtifactStat.Accuracy:
                        accuracy += value;
                        break;
                    case ArtifactStat.Stamina:
                        stamina += value;
                        break;
                    case ArtifactStat.Protection:
                        var damageType = part.DamageTypeId!;
                        _protection.TryGetValue(damageType, out var current);
                        _protection[damageType] = current + value;
                        break;
                }
            }

            HealthMax = ToInt(health);
            Accuracy = accuracy;
            Stamina = ToInt(stamina);
        }

        /// <summary>Прибавка к максимуму здоровья.</summary>
        public int HealthMax { get; }

        /// <summary>Прибавка к меткости.</summary>
        public double Accuracy { get; }

        /// <summary>Прибавка к максимуму выносливости.</summary>
        public int Stamina { get; }

        /// <summary>Прибавка к защите от указанного типа урона; неизвестный тип — 0.</summary>
        /// <param name="damageTypeId">Тип урона.</param>
        /// <returns>Целая прибавка к защите.</returns>
        public int ProtectionOf(string? damageTypeId) =>
            damageTypeId != null && _protection.TryGetValue(damageTypeId, out var value) ? ToInt(value) : 0;

        private static int ToInt(double value) =>
            (int)Math.Round(value, MidpointRounding.AwayFromZero);
    }
}
