#nullable enable
namespace AnomalyPath.Core.Inventory
{
    /// <summary>
    /// Одна характеристика предмета для подсказки: вид, величина и, если нужно,
    /// второй предел и строковый код (калибр или тип урона).
    /// </summary>
    public sealed class ItemFact
    {
        /// <summary>Создаёт характеристику.</summary>
        /// <param name="kind">Что за характеристика.</param>
        /// <param name="value">Величина; может быть отрицательной у модификаторов.</param>
        /// <param name="max">Второй предел, например максимум прочности; <c>null</c> — не нужен.</param>
        /// <param name="code">Строковый код: калибр или тип урона; <c>null</c> — не нужен.</param>
        public ItemFact(ItemFactKind kind, double value, double? max = null, string? code = null)
        {
            Kind = kind;
            Value = value;
            Max = max;
            Code = code;
        }

        /// <summary>Что за характеристика.</summary>
        public ItemFactKind Kind { get; }

        /// <summary>Величина характеристики.</summary>
        public double Value { get; }

        /// <summary>Второй предел, если характеристика парная (прочность).</summary>
        public double? Max { get; }

        /// <summary>Строковый код: калибр или тип урона.</summary>
        public string? Code { get; }
    }
}
