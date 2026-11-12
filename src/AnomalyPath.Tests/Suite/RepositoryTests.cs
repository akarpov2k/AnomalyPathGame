#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Di;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    public sealed class RepositoryTests : DataTestBase
    {
        public RepositoryTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            RepositoryOperations(); TemplateStoreTests(); DiTests();
        }

        private void RepositoryOperations()
        {
            var container = BuildServices();
            var repository = container.Resolve<IItemRepository>();
            var catalog = container.Resolve<IItemCatalogService>();

            Check("репозиторий: все предметы", () =>
            {
                Equal(39, repository.All.Count, "All");
            });

            Check("репозиторий: скиллы", () =>
            {
                var skills = container.Resolve<ISkillRepository>();
                Equal(15, skills.All.Count, "скиллы");
                True(skills.TryGet("skill_stock", out var stock), "TryGet");
                Equal("skill_stock", stock!.Id, "id");
                Throws<DataException>(() => skills.Get("skill_unknown"), "Get unknown");
            });

            Check("репозиторий: редкости", () =>
            {
                var rarities = container.Resolve<IRarityRepository>();
                Equal(6, rarities.All.Count, "редкости");
                Equal("rar_rare", rarities.Get(Rarity.Rare).Id, "по ступени");
                Throws<DataException>(() => rarities.Get("rar_mythic"), "Get unknown");
            });

            Check("репозиторий: поиск по id", () =>
            {
                True(repository.TryGet("wpn_ak74", out var ak74), "TryGet ak74");
                Equal("wpn_ak74", ak74.Id, "id");
                Throws<DataException>(() => repository.Get("wpn_unknown"), "Get unknown");
            });

            Check("сервис: выборка по категории", () =>
            {
                Equal(6, catalog.ByCategory(ItemCategory.Weapon).Count, "weapon");
                Equal(3, catalog.ByCategory(ItemCategory.Armor).Count, "armor");
            });

            Check("сервис: выборка по тиру", () =>
            {
                var tier2 = catalog.ByTier(2);
                Equal(13, tier2.Count, "tier 2");
                Equal("wpn_ak74", tier2[0].Id, "tier 2 id");
            });

            Check("распределение: id уникальны и разрешаются", () =>
            {
                var ids = new HashSet<string>();
                foreach (var item in repository.All)
                {
                    True(ids.Add(item.Id), $"дубль {item.Id}");
                    Equal(item.Id, repository.Get(item.Id).Id, "Get by id");
                }
            });
        }

        private void TemplateStoreTests()
        {
            Check("хранилище шаблонов: поиск, порядок и Contains", () =>
            {
                var store = new TemplateStore<StoreEntry>(
                    new[] { new StoreEntry("b"), new StoreEntry("a") },
                    entry => entry.Id,
                    id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                    id => new DataException(id, CoreErrors.ItemNotFound));
                Equal(2, store.All.Count, "размер");
                Equal("b", store.All[0].Id, "порядок");
                True(store.TryGet("a", out var found), "TryGet");
                Equal("a", found!.Id, "id");
                True(store.Contains("b"), "Contains");
                Throws<DataException>(() => store.Get("missing"), "Get missing");
            });

            Check("хранилище шаблонов: дубль id — ошибка", () =>
            {
                Throws<DataException>(
                    () => new TemplateStore<StoreEntry>(
                        new[] { new StoreEntry("a"), new StoreEntry("a") },
                        entry => entry.Id,
                        id => new DataException(id, CoreErrors.DuplicateRepositoryId),
                        id => new DataException(id, CoreErrors.ItemNotFound)),
                    "дубль");
            });
        }

        private sealed class StoreEntry
        {
            public StoreEntry(string id)
            {
                Id = id;
            }

            public string Id { get; }
        }

        private void DiTests()
        {
            Check("DI: репозиторий и сервис разрешаются", () =>
            {
                var container = BuildServices();
                True(container.IsRegistered<IItemRepository>(), "репозиторий зарегистрирован");
                True(container.IsRegistered<IItemCatalogService>(), "сервис зарегистрирован");
                Equal(39, container.Resolve<IItemCatalogService>().All().Count, "All через сервис");
            });

            Check("DI: повторная регистрация — ошибка", () =>
            {
                var container = new ServiceContainer();
                var repository = new ItemRepository(Array.Empty<ItemTemplate>());
                container.RegisterSingleton<IItemRepository>(repository);
                Throws<InvalidOperationException>(
                    () => container.RegisterSingleton<IItemRepository>(repository),
                    "дубль регистрации");
            });

            Check("DI: незарегистрированный сервис — ошибка", () =>
            {
                Throws<InvalidOperationException>(
                    () => new ServiceContainer().Resolve<IItemCatalogService>(),
                    "resolve missing");
            });
        }
    }
}
