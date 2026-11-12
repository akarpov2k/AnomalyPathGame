#nullable enable
using System;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;

namespace AnomalyPath.Tests
{
    /// <summary>Редкости: кодек, разбор записи, репозиторий и данные проекта.</summary>
    public sealed class RarityTests : DataTestBase
    {
        public RarityTests(string dataRoot)
            : base(dataRoot)
        {
        }

        public override void Run()
        {
            CodecTests();
            TemplateTests();
            RepositoryTests();
            ProjectDataTests();
        }

        private void CodecTests()
        {
            Check("редкость: id и ступень согласованы", () =>
            {
                Equal("rar_common", RarityIds.Id(Rarity.Common), "id обычной");
                True(RarityIds.TryParse("rar_unique", out var unique), "разбор unique");
                Equal(Rarity.Unique, unique, "ступень");
                False(RarityIds.TryParse("common", out _), "без префикса");
                False(RarityIds.TryParse("rar_mythic", out _), "неизвестный код");
                Equal(6, RarityIds.Count, "ступеней шесть");
            });
        }

        private void TemplateTests()
        {
            Check("редкость: разбор записи", () =>
            {
                var template = Parse("{\"id\":\"rar_rare\",\"nameKey\":\"n\",\"order\":1,\"accuracyBonus\":0.02}");
                Equal(Rarity.Rare, template.Rarity, "ступень");
                Equal("n", template.NameKey, "ключ названия");
                Equal(1, template.Order, "порядок");
                True(Math.Abs(template.AccuracyBonus - 0.02) < 1e-9, "бонус меткости");
            });

            Check("редкость: бонус меткости по умолчанию 0", () =>
            {
                var template = Parse("{\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0}");
                True(Math.Abs(template.AccuracyBonus) < 1e-9, "бонус");
            });

            Check("редкость: вес выпадения читается и по умолчанию 0", () =>
            {
                var withWeight = Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3,\"dropWeight\":9}");
                True(Math.Abs(withWeight.DropWeight - 9) < 1e-9, "вес");
                var without = Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}");
                True(Math.Abs(without.DropWeight) < 1e-9, "по умолчанию ноль");
            });

            Check("редкость: отрицательный вес выпадения — ошибка", () =>
            {
                Equal(
                    CoreErrors.NonNegative,
                    Throws<DataException>(
                        () => Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3,\"dropWeight\":-1}"),
                        "dropWeight").Code,
                    "код");
            });

            Check("редкость: прибавка к статам оружия читается и по умолчанию 0", () =>
            {
                var withBonus = Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3,\"weaponStatBonus\":0.3}");
                True(Math.Abs(withBonus.WeaponStatBonus - 0.3) < 1e-9, "прибавка");
                var without = Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}");
                True(Math.Abs(without.WeaponStatBonus) < 1e-9, "по умолчанию ноль");
            });

            Check("редкость: отрицательная прибавка к статам оружия — ошибка", () =>
            {
                Equal(
                    CoreErrors.NonNegative,
                    Throws<DataException>(
                        () => Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3,\"weaponStatBonus\":-0.1}"),
                        "weaponStatBonus").Code,
                    "код");
            });

            Check("редкость: прибавка к цене продажи читается и по умолчанию 0", () =>
            {
                var withBonus = Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3,\"sellPriceBonus\":0.3}");
                True(Math.Abs(withBonus.SellPriceBonus - 0.3) < 1e-9, "прибавка");
                var without = Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3}");
                True(Math.Abs(without.SellPriceBonus) < 1e-9, "по умолчанию ноль");
            });

            Check("редкость: отрицательная прибавка к цене продажи — ошибка", () =>
            {
                Equal(
                    CoreErrors.NonNegative,
                    Throws<DataException>(
                        () => Parse("{\"id\":\"rar_epic\",\"nameKey\":\"n\",\"order\":3,\"sellPriceBonus\":-0.1}"),
                        "sellPriceBonus").Code,
                    "код");
            });

            Check("редкость: неизвестный id — ошибка", () =>
            {
                Equal(
                    CoreErrors.UnknownRarity,
                    Throws<DataException>(
                        () => Parse("{\"id\":\"rar_mythic\",\"nameKey\":\"n\",\"order\":0}"),
                        "id").Code,
                    "код");
            });

            Check("редкость: порядок вне диапазона — ошибка", () =>
            {
                Equal(
                    CoreErrors.RarityOrderRange,
                    Throws<DataException>(
                        () => Parse("{\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":6}"),
                        "order").Code,
                    "код");
            });

            Check("редкость: отрицательный бонус — ошибка", () =>
            {
                Equal(
                    CoreErrors.NonNegative,
                    Throws<DataException>(
                        () => Parse("{\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0,\"accuracyBonus\":-0.1}"),
                        "accuracyBonus").Code,
                    "код");
            });

            Check("редкость: без ключа названия — ошибка", () =>
            {
                Throws<DataException>(() => Parse("{\"id\":\"rar_common\",\"order\":0}"), "nameKey");
            });
        }

        private void RepositoryTests()
        {
            Check("редкость: репозиторий по id и ступени", () =>
            {
                var repository = new RarityRepository(new[]
                {
                    Make("rar_common", 0),
                    Make("rar_rare", 1, 0.02)
                });
                Equal(2, repository.All.Count, "All");
                Equal(Rarity.Rare, repository.Get("rar_rare").Rarity, "по id");
                True(Math.Abs(repository.Get(Rarity.Rare).AccuracyBonus - 0.02) < 1e-9, "бонус");
                True(repository.TryGet("rar_common", out _), "TryGet");
                False(repository.TryGet("rar_epic", out _), "нет записи");
                Equal(
                    CoreErrors.RarityMissing,
                    Throws<DataException>(() => repository.Get(Rarity.Epic), "Get по ступени").Code,
                    "код");
            });

            Check("редкость: дубль id — ошибка", () =>
            {
                Throws<DataException>(
                    () => new RarityRepository(new[] { Make("rar_common", 0), Make("rar_common", 0) }),
                    "дубль");
            });
        }

        private void ProjectDataTests()
        {
            var services = BuildServices();
            var repository = services.Resolve<IRarityRepository>();

            Check("редкость: данные проекта — шесть ступеней по порядку", () =>
            {
                Equal(6, repository.All.Count, "записей");
                foreach (var rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
                {
                    var template = repository.Get(rarity);
                    Equal(RarityIds.Id(rarity), template.Id, "id");
                    Equal((int)rarity - 1, template.Order, "порядок");
                    True(template.AccuracyBonus >= 0, "бонус не отрицательный");
                }

                True(Math.Abs(repository.Get(Rarity.Common).AccuracyBonus) < 1e-9, "обычная без бонуса");
            });

            Check("редкость: ступень актёра есть в данных", () =>
            {
                var actors = services.Resolve<IActorRepository>();
                var merc = actors.Get("enemy_merc");
                Equal(merc.Rarity, repository.Get(merc.Rarity).Rarity, "ступень наёмника");
            });

            Check("редкость: данные проекта задают веса выпадения 45/25/15/9/4.5/1.5", () =>
            {
                var expected = new[] { 45.0, 25.0, 15.0, 9.0, 4.5, 1.5 };
                var total = 0.0;
                var index = 0;
                foreach (var rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
                {
                    var template = repository.Get(rarity);
                    True(Math.Abs(template.DropWeight - expected[index]) < 1e-9, "вес " + template.Id);
                    total += template.DropWeight;
                    index++;
                }

                True(Math.Abs(total - 100.0) < 1e-9, "сумма весов");
            });

            Check("редкость: данные проекта дают +10% к статам оружия за ступень", () =>
            {
                var expected = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5 };
                var index = 0;
                foreach (var rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
                {
                    var template = repository.Get(rarity);
                    True(Math.Abs(template.WeaponStatBonus - expected[index]) < 1e-9, "прибавка " + template.Id);
                    index++;
                }
            });

            Check("редкость: данные проекта дают +10% к цене продажи за ступень", () =>
            {
                var expected = new[] { 0.0, 0.1, 0.2, 0.3, 0.4, 0.5 };
                var index = 0;
                foreach (var rarity in (Rarity[])Enum.GetValues(typeof(Rarity)))
                {
                    var template = repository.Get(rarity);
                    True(Math.Abs(template.SellPriceBonus - expected[index]) < 1e-9, "прибавка " + template.Id);
                    index++;
                }
            });
        }

        private static RarityTemplate Parse(string json) =>
            RarityTemplate.FromJson(JsonParser.Parse(json, "test"), "test");

        private static RarityTemplate Make(string id, int order, double accuracyBonus = 0)
        {
            RarityIds.TryParse(id, out var rarity);
            return new RarityTemplate(id, rarity, "n", order, accuracyBonus);
        }
    }
}
