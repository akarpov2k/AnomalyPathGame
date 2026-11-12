#nullable enable
namespace AnomalyPath.Core.World
{
    /// <summary>Способ перехода между хабами.</summary>
    public enum HubTravelMode
    {
        /// <summary>Идти самому: время и выносливость, возможна встреча с противником.</summary>
        OnFoot,

        /// <summary>Через проводника: время и деньги, встреч нет.</summary>
        Guide
    }
}
