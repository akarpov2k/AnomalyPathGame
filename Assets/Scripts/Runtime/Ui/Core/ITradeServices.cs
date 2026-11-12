#nullable enable
using AnomalyPath.Core.Abstractions;

namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Торговля и ремонт: ассортимент, сделки и станок механика.</summary>
    public interface ITradeServices
    {
        /// <summary>Торговля: доступность торговцев, ассортимент и сделки.</summary>
        ITradeService Trade { get; }

        /// <summary>Ремонт снаряжения у торговца-механика.</summary>
        IRepairService Repair { get; }
    }
}
