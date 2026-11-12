#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Combat;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: травмы и ссылки на них из скиллов.</summary>
    public sealed partial class DataValidator
    {
        private static void ValidateInjuries(LoadedData data, List<ValidationIssue> issues)
        {
            IInjuryRepository? injuries = null;
            try
            {
                injuries = InjuryRepository.FromLoadedData(data);
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
            }

            IReadOnlyList<SkillTemplate> skills;
            try
            {
                skills = SkillRepository.FromLoadedData(data).All;
            }
            catch (DataException)
            {
                // Ошибки схемы скиллов уже собраны в ValidateEntities.
                return;
            }

            if (injuries == null)
            {
                return;
            }

            foreach (var injury in injuries.All)
            {
                ValidateInjury(injury, issues);
            }

            foreach (var skill in skills)
            {
                foreach (var injuryId in skill.Injuries)
                {
                    if (injuries.Contains(injuryId))
                    {
                        continue;
                    }

                    issues.Add(new ValidationIssue(
                        skill.Id,
                        CoreErrors.InjuryNotFound,
                        CoreText.Format(CoreErrors.InjuryNotFound, injuryId)));
                }
            }
        }

        private static void ValidateInjury(InjuryTemplate injury, List<ValidationIssue> issues)
        {
            if (!InjuryDurationCodec.IsTimed(injury.DurationKind) && injury.DurationAmount != 0)
            {
                issues.Add(new ValidationIssue(
                    injury.Id,
                    CoreErrors.InjuryDurationAmount,
                    CoreText.Format(CoreErrors.InjuryDurationAmount, injury.Id)));
            }

            if (InjuryDurationCodec.IsTimed(injury.DurationKind) && injury.DurationAmount < 1)
            {
                issues.Add(new ValidationIssue(
                    injury.Id,
                    CoreErrors.InjuryDurationAmount,
                    CoreText.Format(CoreErrors.InjuryDurationAmount, injury.Id)));
            }

            var modifiers = injury.Modifiers;
            if (modifiers.AccuracyBonus == 0 &&
                modifiers.ActionApCostBonus == 0)
            {
                // Травма без эффекта — это ошибка данных, а не «пустая» травма: непонятно, зачем она.
                issues.Add(new ValidationIssue(
                    injury.Id,
                    CoreErrors.InjuryWithoutEffect,
                    CoreText.Format(CoreErrors.InjuryWithoutEffect, injury.Id)));
            }
        }
    }
}
