#nullable enable
namespace AnomalyPath.Runtime.Ui.Core
{
    /// <summary>Экран локации: предметы, бой, квесты, локации, аномалии и спавн.</summary>
    public interface IPlayScreenHost : IItemScreenHost, ICombatServices, IQuestServices
    {
    }
}
