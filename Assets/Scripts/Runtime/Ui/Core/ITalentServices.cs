#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Прокачка: дерево талантов, очки и ранги.</summary>
    public interface ITalentServices
    {
        /// <summary>Дерево талантов: изучение, очки и ранги.</summary>
        ITalentService Talents { get; }
    }
}
