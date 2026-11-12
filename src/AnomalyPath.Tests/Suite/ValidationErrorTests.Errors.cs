#nullable enable
using AnomalyPath.Core;
using AnomalyPath.Core.Validation;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: тексты ошибок.</summary>
    public sealed partial class ValidationErrorTests
    {
        private void ErrorTests()
        {
            var cases = BuildErrorCasesA();
            cases.AddRange(BuildErrorCasesB());
            foreach (var testCase in cases)
            {
                Check(testCase.Name, () =>
                {
                    WithFixture(testCase.Files, root =>
                    {
                        var error = Throws<ValidationException>(() => Bootstrap.Build(root), testCase.Name);
                        True(
                            error.Message.Contains(testCase.Fragment) || HasAnyError(error.Report, testCase.Fragment),
                            $"причина: {Describe(error.Report)}");
                    });
                });
            }
        }
    }
}
