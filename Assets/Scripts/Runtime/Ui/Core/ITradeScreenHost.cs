#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Окно торговли: предметы, актёры, торговля и ремонт.</summary>
    public interface ITradeScreenHost : IItemScreenHost, ICombatServices, ITradeServices
    {
    }
}
