#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Атака актёра из данных: либо одно оружие (<see cref="WeaponId"/>) или пул оружия
    /// (<see cref="WeaponIds"/>), либо естественное оружие (<see cref="Damage"/> и
    /// <see cref="DamageTypeId"/>); ровно один из двух наборов. Оружие из пула выбирается
    /// детерминированно при старте боя. Дальность не задаётся: позиций и дистанции в бою нет.
    /// </summary>
    public sealed class ActorAttackTemplate
    {
        private readonly IReadOnlyList<string> _weaponIds;

        private ActorAttackTemplate(IReadOnlyList<string> weaponIds, double damage, string? damageTypeId)
        {
            _weaponIds = weaponIds;
            Damage = damage;
            DamageTypeId = damageTypeId;
        }

        /// <summary>Id единственного оружия; <c>null</c> у естественной атаки и пула.</summary>
        public string? WeaponId => _weaponIds.Count == 1 ? _weaponIds[0] : null;

        /// <summary>Пул оружия, из которого выбирается одно при старте боя; пустой у естественной атаки.</summary>
        public IReadOnlyList<string> WeaponIds => _weaponIds;

        /// <summary>База урона естественной атаки; 0 у атаки оружием.</summary>
        public double Damage { get; }

        /// <summary>Тип урона естественной атаки; <c>null</c> у атаки оружием.</summary>
        public string? DamageTypeId { get; }

        /// <summary><c>true</c>, если актёр бьёт без оружия.</summary>
        public bool IsNatural => _weaponIds.Count == 0;

        /// <summary>Читает атаку из блока <c>combat.attack</c>.</summary>
        /// <param name="value">Загруженный объект атаки.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Атака актёра.</returns>
        /// <exception cref="DataException">Поля заданы неверно или смешаны.</exception>
        public static ActorAttackTemplate FromJson(JsonValue value, string sourcePath)
        {
            var weaponId = DataMap.OptionalString(value, "weaponId");
            var hasPool = value.TryGet("weaponIds", out var poolValue);
            var damage = DataMap.OptionalDouble(value, "damage") ?? 0;
            var damageTypeId = DataMap.OptionalString(value, "damageTypeId");
            if (weaponId != null && hasPool)
            {
                throw new DataException(sourcePath, CoreErrors.AttackMixedFields, "attack");
            }

            if (weaponId != null)
            {
                if (damage > 0 || damageTypeId != null)
                {
                    throw new DataException(sourcePath, CoreErrors.AttackMixedFields, "attack");
                }

                return new ActorAttackTemplate(new[] { weaponId }, 0, null);
            }

            if (hasPool)
            {
                if (!poolValue.IsArray || poolValue.Items.Count == 0)
                {
                    throw new DataException(sourcePath, CoreErrors.MissingArrayField, "attack.weaponIds");
                }

                var weaponIds = new List<string>(poolValue.Items.Count);
                foreach (var item in poolValue.Items)
                {
                    if (item.Kind != JsonKind.String || string.IsNullOrWhiteSpace(item.Text))
                    {
                        throw new DataException(sourcePath, CoreErrors.MissingStringField, "attack.weaponIds");
                    }

                    weaponIds.Add(item.Text);
                }

                if (damage > 0 || damageTypeId != null)
                {
                    throw new DataException(sourcePath, CoreErrors.AttackMixedFields, "attack");
                }

                return new ActorAttackTemplate(weaponIds, 0, null);
            }

            if (damage <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.MissingNumberField, "attack.damage");
            }

            if (damageTypeId == null)
            {
                throw new DataException(sourcePath, CoreErrors.MissingStringField, "attack.damageTypeId");
            }

            return new ActorAttackTemplate(Array.Empty<string>(), damage, damageTypeId);
        }
    }
}
