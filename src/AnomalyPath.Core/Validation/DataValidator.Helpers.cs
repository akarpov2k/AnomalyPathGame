#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;

namespace AnomalyPath.Core.Validation
{
    /// <summary>Часть класса <see cref="DataValidator"/>: служебные помощники.</summary>
    public sealed partial class DataValidator
    {
        private static void Try(Action body, List<ValidationIssue> issues)
        {
            try
            {
                body();
            }
            catch (DataException error)
            {
                issues.Add(new ValidationIssue(error.Path, error.Code, error.Reason));
            }
        }

        private static bool IsFlag(JsonValue value, string field) =>
            value.TryGet(field, out var flag) && flag.Kind == JsonKind.Bool && flag.Bool;

        private static IEnumerable<ValidationIssue> Distinct(List<ValidationIssue> issues)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<ValidationIssue>();
            foreach (var issue in issues)
            {
                if (seen.Add(issue.Path + "|" + issue.Message))
                {
                    result.Add(issue);
                }
            }

            return result;
        }
    }
}
