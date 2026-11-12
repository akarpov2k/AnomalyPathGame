#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Core.Combat
{
    /// <summary>Скилл оружия: цена, точность и эффекты.</summary>
    public sealed class SkillTemplate
    {
        /// <summary>Создаёт скилл.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="nameKey">Ключ названия.</param>
        /// <param name="descriptionKey">Ключ описания.</param>
        /// <param name="kind">Назначение скилла.</param>
        /// <param name="apCost">Стоимость в ОД.</param>
        /// <param name="ammoCost">Сколько патронов тратит вызов.</param>
        /// <param name="accuracy">Множитель точности.</param>
        /// <param name="effects">Эффекты скилла.</param>
        /// <param name="injuries">Травмы, которые скилл может нанести; <c>null</c> — не наносит.</param>
        /// <exception cref="ArgumentNullException">Обязательное значение не передано.</exception>
        public SkillTemplate(
            string id,
            string nameKey,
            string descriptionKey,
            SkillKind kind,
            int apCost,
            int ammoCost,
            double accuracy,
            IReadOnlyList<SkillEffect> effects,
            IReadOnlyList<string>? injuries = null)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            NameKey = nameKey ?? throw new ArgumentNullException(nameof(nameKey));
            DescriptionKey = descriptionKey ?? throw new ArgumentNullException(nameof(descriptionKey));
            Kind = kind;
            ApCost = apCost;
            AmmoCost = ammoCost;
            Accuracy = accuracy;
            Effects = effects ?? throw new ArgumentNullException(nameof(effects));
            Injuries = injuries ?? Array.Empty<string>();
        }

        /// <summary>Идентификатор скилла.</summary>
        public string Id { get; }

        /// <summary>Ключ названия в каталоге строк.</summary>
        public string NameKey { get; }

        /// <summary>Ключ описания в каталоге строк.</summary>
        public string DescriptionKey { get; }

        /// <summary>Назначение скилла.</summary>
        public SkillKind Kind { get; }

        /// <summary>Стоимость в ОД.</summary>
        public int ApCost { get; }

        /// <summary>Сколько патронов тратит скилл.</summary>
        public int AmmoCost { get; }

        /// <summary>Множитель точности скилла.</summary>
        public double Accuracy { get; }

        /// <summary>Эффекты скилла в порядке применения.</summary>
        public IReadOnlyList<SkillEffect> Effects { get; }

        /// <summary>Травмы, которые скилл может нанести при попадании; пусто — не наносит.</summary>
        public IReadOnlyList<string> Injuries { get; }

        /// <summary>Разбирает скилл из записи вида <c>skills</c>.</summary>
        /// <param name="value">Запись скилла.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Разобранный скилл.</returns>
        /// <exception cref="DataException">Поля заданы неверно.</exception>
        public static SkillTemplate FromJson(JsonValue value, string sourcePath)
        {
            var id = DataMap.RequireString(value, "id", sourcePath);
            var nameKey = DataMap.RequireString(value, "nameKey", sourcePath);
            var descriptionKey = DataMap.RequireString(value, "descriptionKey", sourcePath);
            var kindCode = DataMap.RequireString(value, "type", sourcePath);
            if (!SchemaEnum.TryParse(kindCode, out SkillKind kind))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownSkillKind, kindCode);
            }

            var apCost = DataMap.RequireInt(value, "apCost", sourcePath);
            if (apCost < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "apCost");
            }

            var ammoCost = DataMap.OptionalInt(value, "ammoCost", 0);
            if (ammoCost < 0)
            {
                throw new DataException(sourcePath, CoreErrors.NonNegative, "ammoCost");
            }

            var accuracy = DataMap.OptionalDouble(value, "accuracy") ?? 1.0;
            if (accuracy <= 0)
            {
                throw new DataException(sourcePath, CoreErrors.PositiveValue, "accuracy");
            }

            var injuries = ReadInjuries(value, sourcePath);
            var effects = ReadEffects(value, id, sourcePath);
            if (kind == SkillKind.Attack)
            {
                var hasDamage = false;
                foreach (var effect in effects)
                {
                    if (effect.Type == SkillEffectType.Damage)
                    {
                        hasDamage = true;
                        break;
                    }
                }

                if (!hasDamage)
                {
                    throw new DataException(sourcePath, CoreErrors.SkillWithoutDamage, id);
                }
            }

            return new SkillTemplate(
                id,
                nameKey,
                descriptionKey,
                kind,
                apCost,
                ammoCost,
                accuracy,
                effects,
                injuries);
        }

        /// <summary>Читает список травм скилла: массив id, дубли запрещены.</summary>
        /// <param name="value">Запись скилла.</param>
        /// <param name="sourcePath">Путь для сообщения об ошибке.</param>
        /// <returns>Id травм в порядке объявления; пусто — скилл травм не наносит.</returns>
        /// <exception cref="DataException">Элемент не строка или id повторяется.</exception>
        internal static IReadOnlyList<string> ReadInjuries(JsonValue value, string sourcePath)
        {
            var injuries = new List<string>();
            if (!value.TryGet("injuries", out var field) || !field.IsArray)
            {
                return injuries;
            }

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in field.Items)
            {
                if (item.Kind != JsonKind.String)
                {
                    throw new DataException(sourcePath, CoreErrors.ArrayItemNotString, "injuries");
                }

                if (!seen.Add(item.Text))
                {
                    throw new DataException(sourcePath, CoreErrors.DuplicateInjuryId, item.Text);
                }

                injuries.Add(item.Text);
            }

            return injuries;
        }

        private static IReadOnlyList<SkillEffect> ReadEffects(JsonValue value, string skillId, string sourcePath)
        {
            if (!value.TryGet("effects", out var field) || !field.IsArray || field.Items.Count == 0)
            {
                throw new DataException(sourcePath, CoreErrors.SkillNoEffects, skillId);
            }

            var effects = new List<SkillEffect>();
            foreach (var item in field.Items)
            {
                if (!item.IsObject)
                {
                    throw new DataException(sourcePath, CoreErrors.SkillUnknownEffect, "?");
                }

                var typeCode = DataMap.RequireString(item, "type", sourcePath);
                if (!SchemaEnum.TryParse(typeCode, out SkillEffectType type))
                {
                    throw new DataException(sourcePath, CoreErrors.SkillUnknownEffect, typeCode);
                }

                switch (type)
                {
                    case SkillEffectType.Damage:
                    {
                        var multiplier = DataMap.OptionalDouble(item, "multiplier") ?? 1.0;
                        if (multiplier <= 0)
                        {
                            throw new DataException(sourcePath, CoreErrors.PositiveValue, "multiplier");
                        }

                        var hits = DataMap.OptionalInt(item, "hits", 1);
                        if (hits < 1)
                        {
                            throw new DataException(sourcePath, CoreErrors.MinimumOne, "hits");
                        }

                        effects.Add(SkillEffect.Damage(
                            multiplier,
                            hits,
                            DataMap.OptionalString(item, "damageTypeId")));
                        break;
                    }

                    case SkillEffectType.Status:
                    {
                        var statusId = DataMap.RequireString(item, "statusId", sourcePath);
                        var target = ReadTarget(item, sourcePath);
                        var accuracy = DataMap.OptionalDouble(item, "accuracy") ?? 0;
                        var actionAp = DataMap.OptionalInt(item, "actionApCost", 0);
                        var damage = DataMap.OptionalDouble(item, "damageMultiplier") ?? 0;
                        var turns = DataMap.OptionalInt(item, "turns", 1);
                        if (turns < 1)
                        {
                            throw new DataException(sourcePath, CoreErrors.MinimumOne, "turns");
                        }

                        if (accuracy == 0 && actionAp == 0 && damage == 0)
                        {
                            throw new DataException(sourcePath, CoreErrors.SkillStatusNoModifier, skillId);
                        }

                        effects.Add(SkillEffect.Status(statusId, target, accuracy, actionAp, damage, turns));
                        break;
                    }

                    case SkillEffectType.RemoveStatus:
                        effects.Add(SkillEffect.RemoveStatus(
                            DataMap.RequireString(item, "statusId", sourcePath),
                            ReadTarget(item, sourcePath)));
                        break;

                    default:
                        throw new DataException(sourcePath, CoreErrors.SkillUnknownEffect, typeCode);
                }
            }

            return effects;
        }

        private static SkillEffectTarget ReadTarget(JsonValue item, string sourcePath)
        {
            var code = DataMap.OptionalString(item, "target") ?? "self";
            if (!SchemaEnum.TryParse(code, out SkillEffectTarget target))
            {
                throw new DataException(sourcePath, CoreErrors.UnknownSkillTarget, code);
            }

            return target;
        }
    }
}
