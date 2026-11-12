#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Хаб: актёры и прогрессия, локации и диалоги, торговля и ремонт.</summary>
    public interface IHubScreenHost : IScreenHost, ICombatServices, IQuestServices, ITradeServices
    {
    }
}
