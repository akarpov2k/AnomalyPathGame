#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий редкостей в памяти: строится один раз при загрузке данных.</summary>
    public sealed class RarityRepository : IRarityRepository
    {
        private readonly TemplateStore<RarityTemplate> _store;
        private readonly Dictionary<Rarity, RarityTemplate> _byRarity =
            new Dictionary<Rarity, RarityTemplate>();

        /// <summary>Создаёт репозиторий из готовых записей.</summary>
        /// <param name="templates">Записи редкости.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public RarityRepository(IEnumerable<RarityTemplate> templates)
        {
            _store = new TemplateStore<RarityTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.RarityMissing, id));
            foreach (var template in _store.All)
            {
                _byRarity[template.Rarity] = template;
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<RarityTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out RarityTemplate template) => _store.TryGet(id, out template!);

        /// <inheritdoc />
        public RarityTemplate Get(string id) => _store.Get(id);

        /// <inheritdoc />
        public RarityTemplate Get(Rarity rarity)
        {
            if (_byRarity.TryGetValue(rarity, out var template))
            {
                return template;
            }

            var id = RarityIds.Id(rarity);
            throw new DataException(id, CoreErrors.RarityMissing, id);
        }

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>rarities</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Сущность не соответствует схеме редкости.</exception>
        public static RarityRepository FromLoadedData(LoadedData data) =>
            new RarityRepository(TemplateStore<RarityTemplate>.Load(data, "rarities", RarityTemplate.FromJson));
    }
}
