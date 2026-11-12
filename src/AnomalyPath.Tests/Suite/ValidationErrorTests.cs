#nullable enable
namespace AnomalyPath.Tests
{
    public sealed partial class ValidationErrorTests : DataTestBase
    {
        public ValidationErrorTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            ErrorTests(); ErrorBehaviorTests(); RouteErrorTests(); TraderErrorTests(); QuestErrorTests(); QuestBoardErrorTests(); DialogErrorTests(); TalentErrorTests(); LootErrorTests(); SpawnErrorTests(); AnomalyErrorTests();
        }
    }
}
