#nullable enable
using System;
using System.Collections.Generic;
using AnomalyPath.Core;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;

namespace AnomalyPath.Tests
{
    /// <summary>Пакет данных: упаковка папки, разбор и чтение файлов из памяти (путь Android-сборки).</summary>
    public sealed class DataPackTests : DataTestBase
    {
        /// <summary>Создаёт категорию для папки данных.</summary>
        /// <param name="dataRoot">Корень данных проекта.</param>
        public DataPackTests(string dataRoot)
            : base(dataRoot)
        {
        }

        /// <inheritdoc />
        public override void Run()
        {
            RoundTripOfRealData();
            PreflightOverPack();
            DeterministicText();
            Escaping();
            MemorySourceReads();
            InvalidPacks();
        }

        private void RoundTripOfRealData()
        {
            Check("пакет: реальные данные упаковываются и разбираются без потерь", () =>
            {
                var folder = new FileSystemDataSource(_dataRoot);
                var files = new List<string>();
                foreach (var file in folder.EnumerateFiles())
                {
                    if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    files.Add(file);
                }

                var parsed = DataPack.Parse(DataPack.Serialize(folder));

                True(files.Count > 0, "файлы данных найдены");
                Equal(files.Count, parsed.Count, "число файлов");
                foreach (var file in files)
                {
                    Equal(folder.ReadAllText(file), parsed[Relative(file)], file);
                }
            });

            Check("пакет: файлы .meta в пакет не попадают", () =>
            {
                var packed = DataPack.Serialize(new MemoryDataSource(new Dictionary<string, string>
                {
                    ["items/a.json"] = "{}",
                    ["items/a.json.meta"] = "guid: 1",
                    ["meta.meta"] = "guid: 2"
                }));

                var parsed = DataPack.Parse(packed);
                Equal(1, parsed.Count, "число файлов");
                True(parsed.ContainsKey("items/a.json"), "данные на месте");
            });
        }

        private void PreflightOverPack()
        {
            Check("пакет: preflight на пакете проходит так же, как на папке", () =>
            {
                var source = DataPack.ToSource(DataPack.Serialize(new FileSystemDataSource(_dataRoot)));
                var result = Preflight.Run(source);

                True(result.Report.IsValid, "отчёт без ошибок");
                True(result.CanStart, "сервисы собраны");
            });
        }

        private void DeterministicText()
        {
            Check("пакет: две упаковки одного набора дают одинаковый текст", () =>
            {
                var source = new FileSystemDataSource(_dataRoot);
                Equal(DataPack.Serialize(source), DataPack.Serialize(source), "текст пакета");
            });

            Check("пакет: файлы перечисляются в алфавитном порядке", () =>
            {
                var source = DataPack.ToSource(Pack(
                    ("b/two.json", "{}"),
                    ("a/one.json", "{}"),
                    ("c/three.json", "{}")));
                var files = source.EnumerateFiles();

                Equal(3, files.Count, "число файлов");
                Equal("a/one.json", files[0], "первый файл");
                Equal("b/two.json", files[1], "второй файл");
                Equal("c/three.json", files[2], "третий файл");
            });
        }

        private void Escaping()
        {
            Check("пакет: кавычки, переводы строк и кириллица переживают упаковку", () =>
            {
                var text = "строка \"в кавычках\"\nвторая\\строкa\tтаб\u0001\u0002";
                var parsed = DataPack.Parse(DataPack.Serialize(new Dictionary<string, string>
                {
                    ["Data/one.json"] = text
                }));

                Equal(text, parsed["Data/one.json"], "текст файла");
            });

            Check("пакет: обратные слэши в пути приводятся к прямым", () =>
            {
                var parsed = DataPack.Parse(DataPack.Serialize(new Dictionary<string, string>
                {
                    ["Data\\sub\\file.json"] = "{}"
                }));

                True(parsed.ContainsKey("Data/sub/file.json"), "путь с прямыми слэшами");
            });
        }

        private void MemorySourceReads()
        {
            Check("источник в памяти: читает файл и сообщает о наличии", () =>
            {
                var source = new MemoryDataSource(new Dictionary<string, string>
                {
                    ["items/a.json"] = "{\"id\":\"a\"}"
                });

                True(source.Exists("items/a.json"), "файл есть");
                False(source.Exists("items/b.json"), "файла нет");
                False(source.Exists(string.Empty), "пустой путь не существует");
                Equal("{\"id\":\"a\"}", source.ReadAllText("items/a.json"), "текст");
                Equal(1, source.Count, "число файлов");
            });

            Check("источник в памяти: путь с обратными слэшами находит файл", () =>
            {
                var source = new MemoryDataSource(new Dictionary<string, string>
                {
                    ["items/sub/a.json"] = "x"
                });

                Equal("x", source.ReadAllText("items\\sub\\a.json"), "текст");
            });

            Check("источник в памяти: отсутствующий файл даёт ошибку file_not_found", () =>
            {
                var source = new MemoryDataSource(new Dictionary<string, string>());
                var error = Throws<DataException>(() => source.ReadAllText("missing.json"), "чтение");

                Equal(CoreErrors.FileNotFound, error.Code, "код ошибки");
            });

            Check("источник в памяти: пустой путь даёт ошибку empty_path", () =>
            {
                var source = new MemoryDataSource(new Dictionary<string, string>());
                var error = Throws<DataException>(() => source.ReadAllText("  "), "чтение");

                Equal(CoreErrors.EmptyPath, error.Code, "код ошибки");
            });

            Check("источник в памяти: пустой набор отвергается", () =>
            {
                Throws<ArgumentNullException>(() => new MemoryDataSource(null!), "создание");
            });
        }

        private void InvalidPacks()
        {
            var invalid = new (string Name, string Text)[]
            {
                ("пустой текст", string.Empty),
                ("только пробелы", "   "),
                ("не объект", "[1,2]"),
                ("нет версии", "{\"files\":{}}"),
                ("версия не число", "{\"version\":\"1\",\"files\":{}}"),
                ("нет списка файлов", "{\"version\":1}"),
                ("список файлов не объект", "{\"version\":1,\"files\":[]}"),
                ("значение файла не строка", "{\"version\":1,\"files\":{\"a.json\":1}}"),
                ("битый JSON", "{\"version\":1,")
            };

            foreach (var testCase in invalid)
            {
                Check($"пакет: {testCase.Name} — ошибка data_pack_invalid", () =>
                {
                    var error = Throws<DataException>(() => DataPack.Parse(testCase.Text), "разбор");
                    Equal(CoreErrors.DataPackInvalid, error.Code, "код ошибки");
                });
            }

            Check("пакет: чужая версия формата — ошибка data_pack_version", () =>
            {
                var error = Throws<DataException>(
                    () => DataPack.Parse("{\"version\":2,\"files\":{}}"),
                    "разбор");
                Equal(CoreErrors.DataPackVersion, error.Code, "код ошибки");
            });
        }

        private static string Pack(params (string Path, string Text)[] files)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var file in files)
            {
                map[file.Path] = file.Text;
            }

            return DataPack.Serialize(map);
        }

        private static string Relative(string path) => path.Replace('\\', '/').TrimStart('/');
    }
}
