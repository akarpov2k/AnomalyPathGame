#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Quests.Story;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий квестовых цепочек в памяти.</summary>
    public sealed class QuestChainRepository : IQuestChainRepository
    {
        private readonly TemplateStore<QuestChainTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых цепочек.</summary>
        /// <param name="chains">Цепочки.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public QuestChainRepository(IEnumerable<QuestChainTemplate> chains)
        {
            _store = new TemplateStore<QuestChainTemplate>(
                chains,
                chain => chain.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.ChainNotFound, id));
        }

        /// <inheritdoc />
        public IReadOnlyList<QuestChainTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out QuestChainTemplate chain) => _store.TryGet(id, out chain!);

        /// <inheritdoc />
        public bool Contains(string id) => _store.Contains(id);

        /// <inheritdoc />
        public QuestChainTemplate Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>chains</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        public static QuestChainRepository FromLoadedData(LoadedData data) =>
            new QuestChainRepository(TemplateStore<QuestChainTemplate>.Load(
                data,
                "chains",
                QuestChainTemplate.FromJson));
    }
}
