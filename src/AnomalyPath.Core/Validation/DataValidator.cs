#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;

namespace AnomalyPath.Core.Validation
{
    /// <summary>
    /// Проверка данных до старта игры на уже разобранных файлах. Собирает все ошибки
    /// за один проход: синтаксис, наследование, дубли id, коллекции, каталог и переводы.
    /// </summary>
    public sealed partial class DataValidator : IDataValidator
    {
        private readonly IDataSource _source;
        private readonly IReadOnlyDictionary<string, JsonValue> _parsed;
        private readonly IReadOnlyList<ValidationIssue> _syntaxIssues;
        private readonly IReadOnlyList<Action<LoadedData, List<ValidationIssue>>> _passes;

        /// <summary>Создаёт валидатор поверх источника и разобранных файлов.</summary>
        /// <param name="source">Источник данных.</param>
        /// <param name="parsed">Кэш «путь → разобранный JSON».</param>
        /// <param name="syntaxIssues">Ошибки разбора, найденные при чтении файлов.</param>
        public DataValidator(
            IDataSource source,
            IReadOnlyDictionary<string, JsonValue> parsed,
            IReadOnlyList<ValidationIssue> syntaxIssues)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _parsed = parsed ?? throw new ArgumentNullException(nameof(parsed));
            _syntaxIssues = syntaxIssues ?? throw new ArgumentNullException(nameof(syntaxIssues));
            _passes = new Action<LoadedData, List<ValidationIssue>>[]
            {
                (data, issues) => Try(() => ItemRepository.FromLoadedData(data), issues),
                (data, issues) => Try(() => DamageTypeRepository.FromLoadedData(data), issues),
                (data, issues) => ValidateProtectionKeys(data, issues),
                (data, issues) => ValidateRules(data, issues),
                (data, issues) => ValidateLoadouts(data, issues),
                (data, issues) => ValidateSettings(data, issues),
                (data, issues) => ValidateFormulas(data, issues),
                (data, issues) => ValidateCombatFormulas(data, issues),
                (data, issues) => ValidateTraders(data, issues),
                (data, issues) => ValidateQuests(data, issues),
                (data, issues) => ValidateStoryQuests(data, issues),
                (data, issues) => ValidateDialogs(data, issues),
                (data, issues) => ValidateChains(data, issues),
                (data, issues) => ValidateTalents(data, issues),
                (data, issues) => ValidateLoot(data, issues),
                (data, issues) => ValidateSpawns(data, issues),
                (data, issues) => ValidateAnomalies(data, issues),
                (data, issues) => AddWorldIssues(data, issues),
                (data, issues) => ValidateActorAttacks(data, issues),
                (data, issues) => ValidateSkills(data, issues),
                (data, issues) => ValidateRarities(data, issues),
                (data, issues) => ValidateInjuries(data, issues),
                (data, issues) => ValidateItems(data, issues),
            };
        }

        /// <inheritdoc />
        public ValidationReport Validate() => Validate(out _);

        /// <summary>Проверяет данные и отдаёт загруженные сущности, чтобы не грузить их повторно.</summary>
        /// <param name="data">Загруженные данные или <c>null</c>, если есть ошибки.</param>
        /// <returns>Отчёт со всеми найденными ошибками и предупреждениями.</returns>
        public ValidationReport Validate(out LoadedData? data)
        {
            var issues = new List<ValidationIssue>(_syntaxIssues);
            var loader = new DataLoader(_source, _parsed);
            var entityFiles = ValidateEntities(loader, issues);
            var reachable = ValidateCatalogAndReachability(issues);

            if (reachable != null)
            {
                foreach (var file in entityFiles)
                {
                    if (!reachable.Contains(file))
                    {
                        issues.Add(new ValidationIssue(
                            file,
                            CoreErrors.UnreachableFile,
                            CoreText.Get(CoreErrors.UnreachableFile),
                            isWarning: true));
                    }
                }
            }

            data = null;
            try
            {
                data = loader.Load();
                var loaded = data;
                foreach (var pass in _passes)
                {
                    pass(loaded, issues);
                }
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
                data = null;
            }

            ValidateTextSets(issues);
            ValidateGameTexts(issues);

            var report = new ValidationReport(Distinct(issues));
            if (!report.IsValid)
            {
                data = null;
            }

            return report;
        }
    }
}
