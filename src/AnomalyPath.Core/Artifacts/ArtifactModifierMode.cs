#nullable enable
namespace AnomalyPath.Core.Artifacts
{
    /// <summary>Как складывается повтор того же модификатора.</summary>
    public enum ArtifactModifierMode
    {
        /// <summary>Полная величина без затухания.</summary>
        Add,

        /// <summary>Первый — полный, каждый следующий умножается на коэффициент затухания.</summary>
        Diminishing
    }
}
