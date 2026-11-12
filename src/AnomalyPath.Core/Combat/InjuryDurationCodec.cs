#nullable enable
namespace AnomalyPath.Core.Combat
{
    /// <summary>
    /// Код вида длительности статуса в JSON. Общее соглашение <see cref="Data.SchemaEnum"/>
    /// склеивает слова без разделителя, поэтому «до лечения» получает собственный код <c>until_healed</c>.
    /// </summary>
    public static class InjuryDurationCodec
    {
        /// <summary>Код вида длительности для JSON.</summary>
        /// <param name="kind">Вид длительности.</param>
        /// <returns>Строковый код.</returns>
        public static string ToCode(InjuryDurationKind kind) => kind switch
        {
            InjuryDurationKind.Hours => "hours",
            InjuryDurationKind.Turns => "turns",
            _ => "until_healed"
        };

        /// <summary>Разбирает код вида длительности.</summary>
        /// <param name="code">Текст из JSON.</param>
        /// <param name="kind">Разобранный вид.</param>
        /// <returns><c>true</c>, если код канонический и известен.</returns>
        public static bool TryParse(string code, out InjuryDurationKind kind)
        {
            switch (code)
            {
                case "hours":
                    kind = InjuryDurationKind.Hours;
                    return true;
                case "turns":
                    kind = InjuryDurationKind.Turns;
                    return true;
                case "until_healed":
                    kind = InjuryDurationKind.UntilHealed;
                    return true;
                default:
                    kind = InjuryDurationKind.UntilHealed;
                    return false;
            }
        }

        /// <summary>Проверяет, истекает ли вид длительности сам по себе.</summary>
        /// <param name="kind">Вид длительности.</param>
        /// <returns><c>true</c> для часов и ходов.</returns>
        public static bool IsTimed(InjuryDurationKind kind) =>
            kind == InjuryDurationKind.Hours || kind == InjuryDurationKind.Turns;
    }
}
