#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>Репозиторий предметов в памяти: строится один раз при загрузке данных.</summary>
    public sealed class ItemRepository : IItemRepository
    {
        private readonly TemplateStore<ItemTemplate> _store;

        /// <summary>Создаёт репозиторий из готовых описаний.</summary>
        /// <param name="templates">Описания предметов.</param>
        /// <exception cref="DataException">Дублирующийся id.</exception>
        public ItemRepository(IEnumerable<ItemTemplate> templates)
        {
            _store = new TemplateStore<ItemTemplate>(
                templates,
                template => template.Id,
                id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                id => new DataException(id, CoreErrors.ItemNotFound));
        }

        /// <inheritdoc />
        public IReadOnlyList<ItemTemplate> All => _store.All;

        /// <inheritdoc />
        public bool TryGet(string id, out ItemTemplate template) => _store.TryGet(id, out template!);

        /// <inheritdoc />
        public ItemTemplate Get(string id) => _store.Get(id);

        /// <summary>Строит репозиторий из загруженных сущностей вида <c>items</c>.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <returns>Заполненный репозиторий.</returns>
        /// <exception cref="DataException">Сущность не соответствует схеме предмета.</exception>
        public static ItemRepository FromLoadedData(LoadedData data) =>
            new ItemRepository(TemplateStore<ItemTemplate>.Load(data, "items", ItemTemplate.FromJson));
    }
}
