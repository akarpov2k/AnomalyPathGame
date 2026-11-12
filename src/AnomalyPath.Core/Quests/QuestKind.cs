#nullable enable
namespace AnomalyPath.Core.Quests
{
    /// <summary>Вид квеста: сюжетный или повторяемый.</summary>
    public enum QuestKind
    {
        /// <summary>Сюжетный квест, проходится один раз.</summary>
        Story,

        /// <summary>Повторяемый квест, выдаётся доской объявлений хаба.</summary>
        Repeatable
    }
}
