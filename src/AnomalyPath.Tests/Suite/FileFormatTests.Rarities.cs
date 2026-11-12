#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Data;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="FileFormatTests"/>: редкости.</summary>
    public sealed partial class FileFormatTests
    {
        private void RarityFormatTests()
        {
            Check("формат: редкость грузится как сущность rarities", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/rar_common.json"),
                        ["rarities/rar_common.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\"," +
                                                       "\"id\":\"rar_common\",\"nameKey\":\"n\",\"order\":0,\"accuracyBonus\":0.02}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(1, data.All("rarities").Count, "rarities");
                        True(data.TryGet("rarities", "rar_common", out var record), "TryGet");
                        Equal(0.0, record!.Get("order").Number, "order");
                    });
            });

            Check("формат: коллекция редкостей", () =>
            {
                WithFixture(
                    new Dictionary<string, string>
                    {
                        ["catalog.json"] = Catalog("rarities/col_rarities.json"),
                        ["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                         "\"id\":\"col_rarities\",\"entries\":[\"rarities/a.json\",\"rarities/b.json\"]}",
                        ["rarities/a.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_common\"," +
                                              "\"nameKey\":\"n\",\"order\":0}",
                        ["rarities/b.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"rar_rare\"," +
                                              "\"nameKey\":\"n\",\"order\":1,\"accuracyBonus\":0.02}"
                    },
                    root =>
                    {
                        var data = new DataLoader(new FileSystemDataSource(root)).Load();
                        Equal(2, data.All("rarities").Count, "rarities");
                        True(data.OfKind("rarities")["rar_rare"].Get("accuracyBonus").Number > 0, "бонус");
                    });
            });
        }
    }
}
