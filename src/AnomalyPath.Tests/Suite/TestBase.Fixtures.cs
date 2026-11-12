#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using AnomalyPath.Core.Abstractions;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Localization;
using AnomalyPath.Core.Repositories;
using AnomalyPath.Core.Templates;
using AnomalyPath.Core.Validation;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="TestBase"/>: фикстуры данных, загрузка и проверки отчёта валидатора.</summary>
    public abstract partial class TestBase
    {
        public static string? DataRootOverride { get; set; }

        protected string ResolveDataRootForTests()
        {
            if (!string.IsNullOrEmpty(DataRootOverride) && Directory.Exists(DataRootOverride))
            {
                return DataRootOverride!;
            }

            var fromCurrent = Path.Combine(Environment.CurrentDirectory, "Assets", "Data");
            if (Directory.Exists(fromCurrent))
            {
                return fromCurrent;
            }

            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "Assets", "Data");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Не найдена папка Assets/Data");
        }

        protected IEnumerable<string> ErrorKeys()
        {
            foreach (var field in typeof(CoreErrors).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static))
            {
                if (field.IsLiteral && field.FieldType == typeof(string))
                {
                    yield return (string)field.GetRawConstantValue()!;
                }
            }
        }

        protected string CollectionWithInclude(string entry, string include)
        {
            return $"{{\"schemaVersion\":1,\"kind\":\"items\",\"collection\":true,\"id\":\"col\",\"entries\":[\"{entry}\"],\"include\":[\"{include}\"]}}";
        }

        protected string CollectionWithIncludeMany(string entriesCsv, string includesCsv)
        {
            var entries = string.Join(",", Array.ConvertAll(entriesCsv.Split(','), e => $"\"{e.Trim()}\""));
            var includes = string.Join(",", Array.ConvertAll(includesCsv.Split(','), i => $"\"{i.Trim()}\""));
            return $"{{\"schemaVersion\":1,\"kind\":\"items\",\"collection\":true,\"id\":\"col\",\"entries\":[{entries}],\"include\":[{includes}]}}";
        }

        protected string Abstract(string body)
        {
            return $"{{\"schemaVersion\":1,\"kind\":\"items\",\"abstract\":true,{body.TrimStart('{').TrimEnd('}')}}}";
        }

        protected Dictionary<string, string> BuildBrokenFixture(string dataRoot)
        {
            var ru = File.ReadAllText(Path.Combine(dataRoot, "Localization", "errors.ru.json"));
            var en = File.ReadAllText(Path.Combine(dataRoot, "Localization", "errors.en.json"));
            var enWithoutOne = en.Replace("\"error.item_not_found\": \"Item not found in repository\",", string.Empty);

            return new Dictionary<string, string>
            {
                ["catalog.json"] = Catalog("items/col.json", "items/col_dup.json", "topologies/missing.json"),
                ["items/col.json"] = CollectionWithIncludeMany(
                    "items/a.json,items/b.json,items/bad.json,items/absent.json",
                    "items/col_cycle.json"),
                ["items/col_dup.json"] = Collection("items/a.json"),
                ["items/col_cycle.json"] = CollectionWithIncludeMany("items/a.json", "items/col_cycle.json"),
                ["items/a.json"] = Weapon("wpn_a"),
                ["items/b.json"] = Weapon("wpn_a"),
                ["items/bad.json"] = "{\"kind\":\"items\",\"id\":\"wpn_bad\",\"category\":\"weapon\",\"footprint\":[1,1],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\"}",
                ["items/broken_syntax.json"] = "{\"kind\":",
                ["items/ext.json"] = Weapon("wpn_ext", extends: "_base/missing.json"),
                ["items/mismatch.json"] = Weapon("wpn_mismatch", extends: "_base/loc.json"),
                ["items/orphan.json"] = Weapon("wpn_orphan"),
                ["_base/loc.json"] = "{\"schemaVersion\":1,\"kind\":\"locations\",\"abstract\":true}",
                ["Localization/errors.ru.json"] = ru,
                ["Localization/errors.en.json"] = enWithoutOne
            };
        }

        protected bool HasIssue(ValidationReport report, string path, string fragment)
        {
            foreach (var issue in report.Errors)
            {
                if (Matches(issue, path, fragment))
                {
                    return true;
                }
            }

            return false;
        }

        protected bool HasWarning(ValidationReport report, string path, string fragment)
        {
            foreach (var issue in report.Warnings)
            {
                if (Matches(issue, path, fragment))
                {
                    return true;
                }
            }

            return false;
        }

        protected bool Matches(ValidationIssue issue, string path, string fragment)
        {
            var pathMatch = path == "items/translation"
                ? issue.Path.StartsWith("Localization/", StringComparison.OrdinalIgnoreCase)
                : issue.Path.Replace('\\', '/').IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0;
            return pathMatch && issue.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected Dictionary<string, string> BuildTranslationFixture(string dataRoot)
        {
            var ru = File.ReadAllText(Path.Combine(dataRoot, "Localization", "errors.ru.json"));
            var en = File.ReadAllText(Path.Combine(dataRoot, "Localization", "errors.en.json"));
            var enWithExtra = en.TrimEnd().TrimEnd('}') + ",\"extra.key\":\"x\"}";

            var files = new Dictionary<string, string>
            {
                ["catalog.json"] = Catalog("items/a.json", "combat.json", "equipment.json", "topology.json", "talents.json", "progression.json", "rarities/col_rarities.json", "formulas/formula_damage_base.json", "formulas/formula_combat_xp.json"),
                ["items/a.json"] = Weapon("wpn_a"),
                ["combat.json"] = CombatJson(),
                ["equipment.json"] = EquipmentJson(),
                ["topology.json"] = TopologyJson(),
                ["talents.json"] = TalentsJson(),
                ["progression.json"] = ProgressionJson(),
                ["Localization/errors.ru.json"] = ru,
                ["Localization/errors.en.json"] = enWithExtra,
                ["Localization/strings.ru.json"] = "{\"n\":\"Название\"}",
                ["Localization/strings.en.json"] = "{\"n\":\"Name\"}"
            };
            AddCombatFormulas(files);
            AddRarities(files);
            return files;
        }

        protected bool HasIssue(ValidationReport report, string path)
        {
            foreach (var issue in report.Errors)
            {
                if (issue.Path.Replace('\\', '/').IndexOf(path, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Проверяет, есть ли в отчёте ошибка с таким фрагментом текста, независимо от пути.</summary>
        /// <param name="report">Отчёт проверки.</param>
        /// <param name="fragment">Фрагмент сообщения.</param>
        /// <returns><c>true</c>, если ошибка найдена.</returns>
        protected bool HasAnyError(ValidationReport report, string fragment)
        {
            foreach (var issue in report.Errors)
            {
                if (issue.Message.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        protected string Describe(ValidationReport report)
        {
            var parts = new List<string>();
            foreach (var issue in report.Errors)
            {
                parts.Add($"{issue.Path}: {issue.Message}");
            }

            return string.Join("; ", parts);
        }

        /// <summary>Добавляет в фикстуру обязательные редкости: коллекцию и шесть записей по порядку.</summary>
        /// <param name="files">Файлы фикстуры; словарь пополняется.</param>
        protected static void AddRarities(Dictionary<string, string> files)
        {
            var ids = new[] { "rar_common", "rar_rare", "rar_special", "rar_epic", "rar_legendary", "rar_unique" };
            var entries = new List<string>();
            for (var i = 0; i < ids.Length; i++)
            {
                var path = "rarities/" + ids[i] + ".json";
                entries.Add("\"" + path + "\"");
                files[path] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"id\":\"" + ids[i] +
                              "\",\"nameKey\":\"n\",\"order\":" + i + "}";
            }

            files["rarities/col_rarities.json"] = "{\"schemaVersion\":1,\"kind\":\"rarities\",\"collection\":true," +
                                                  "\"id\":\"col_rarities\",\"entries\":[" + string.Join(",", entries) + "]}";
        }

        /// <summary>
        /// Редкости для проверок лута: те же веса, что в данных проекта, — обычная 45, редкая 25,
        /// особая 15, эпическая 9, легендарная 4.5, уникальная 1.5.
        /// </summary>
        /// <returns>Репозиторий редкостей с весами выпадения.</returns>
        protected static IRarityRepository LootRarities() =>
            new RarityRepository(new[]
            {
                new RarityTemplate("rar_common", Rarity.Common, "n", 0, 0, 45),
                new RarityTemplate("rar_rare", Rarity.Rare, "n", 1, 0, 25),
                new RarityTemplate("rar_special", Rarity.Special, "n", 2, 0, 15),
                new RarityTemplate("rar_epic", Rarity.Epic, "n", 3, 0, 9),
                new RarityTemplate("rar_legendary", Rarity.Legendary, "n", 4, 0, 4.5),
                new RarityTemplate("rar_unique", Rarity.Unique, "n", 5, 0, 1.5),
            });

        protected string Catalog(params string[] files)
        {
            var list = string.Join(",", Array.ConvertAll(files, f => $"\"{f}\""));
            return $"{{\"schemaVersion\":1,\"id\":\"core\",\"files\":[{list}]}}";
        }

        protected string CombatJson() =>
            "{\"schemaVersion\":1,\"kind\":\"combat\",\"id\":\"combat\",\"damageFormulaId\":\"formula_damage_base\"," +
            "\"xpFormulaId\":\"formula_combat_xp\",\"clampMin\":0,\"apRestore\":\"full\"," +
            "\"reloadApCost\":1,\"weaponSwapApCost\":1,\"aiPlanDepth\":5,\"medkitHpThreshold\":0.3}";

        /// <summary>Формулы, на которые ссылается <see cref="CombatJson"/>: обязательны в полном наборе данных.</summary>
        /// <param name="files">Набор файлов фикстуры, куда добавляются формулы.</param>
        protected static void AddCombatFormulas(Dictionary<string, string> files)
        {
            files["formulas/formula_damage_base.json"] =
                "{\"schemaVersion\":1,\"kind\":\"formulas\",\"id\":\"formula_damage_base\"," +
                "\"expression\":\"max(0, damage - max(0, protection - penetration))\"," +
                "\"variables\":[\"damage\",\"penetration\",\"protection\"],\"result\":\"int\",\"clampMin\":0}";
            files["formulas/formula_combat_xp.json"] =
                "{\"schemaVersion\":1,\"kind\":\"formulas\",\"id\":\"formula_combat_xp\"," +
                "\"expression\":\"round(enemyXp * (1 + locationTier * 5 / 100))\"," +
                "\"variables\":[\"enemyXp\",\"locationTier\"],\"result\":\"int\",\"clampMin\":0}";
        }

        protected string EquipmentJson() =>
            "{\"schemaVersion\":1,\"kind\":\"equipment\",\"id\":\"equipment\",\"quickSlots\":4}";

        protected string TalentsJson() =>
            "{\"schemaVersion\":1,\"kind\":\"talents\",\"id\":\"talents\",\"titleKey\":\"n\"," +
            "\"descriptionKey\":\"n\",\"nodes\":[{\"id\":\"tal_a\",\"titleKey\":\"n\"," +
            "\"descriptionKey\":\"n\",\"row\":0,\"column\":0,\"maxRank\":1,\"requiredPoints\":0," +
            "\"effects\":[{\"stat\":\"accuracy\",\"perRank\":0.01}]}]}";

        protected string ProgressionJson() =>
            "{\"schemaVersion\":1,\"kind\":\"progression\",\"id\":\"progression\",\"levelBaseXp\":100," +
            "\"levelGrowthPercent\":20,\"killXpPerRank\":10,\"rarityBonusPercent\":10," +
            "\"maxRank\":5,\"maxRarity\":5,\"levelRewardStartLevel\":5," +
            "\"levelUpTalentPoints\":1,\"levelUpHealth\":2}";

        protected string TopologyJson() =>
            "{\"schemaVersion\":1,\"kind\":\"topology\",\"id\":\"topology\",\"route\":{\"transitions\":[" +
            "{\"from\":\"hub\",\"to\":\"path\",\"via\":[\"road\",\"path\"]}," +
            "{\"from\":\"path\",\"to\":\"path\",\"via\":[\"road\",\"path\"]}]," +
            "\"lookaheadEdges\":2,\"maxTransitionsPerNode\":3,\"hubReturn\":{\"stamina\":2,\"hours\":0.5}," +
            "\"branches\":{\"oddRange\":[1,2],\"evenGrowth\":1,\"maxChains\":2}},\"costs\":{" +
            "\"road\":{\"stamina\":2,\"hours\":1},\"path\":{\"stamina\":3,\"hours\":1.5}}," +
            "\"travel\":{\"onFoot\":{\"hoursPerStep\":4,\"staminaPerStep\":2,\"encounterChancePerStep\":0.2}," +
            "\"guide\":{\"hoursPerStep\":4,\"moneyPerStep\":200}}," +
            "\"rest\":{\"hubStaminaPercent\":0.2,\"raidStaminaPercent\":0.1,\"raidAttackChancePerHour\":0.1}," +
            "\"initialStamina\":10,\"dayStartHour\":6,\"nightStartHour\":22}";

        protected string Collection(params string[] entries)
        {
            var list = string.Join(",", Array.ConvertAll(entries, e => $"\"{e}\""));
            return $"{{\"schemaVersion\":1,\"kind\":\"items\",\"collection\":true,\"id\":\"col\",\"entries\":[{list}]}}";
        }

        protected string Weapon(string id, string? extends = null, string extra = "")
        {
            var extendsPart = extends == null ? string.Empty : $"\"extends\":\"{extends}\",";
            var extraPart = string.IsNullOrWhiteSpace(extra)
                ? string.Empty
                : extra.Trim().TrimStart(',').TrimEnd(',') + ",";
            return $"{{\"schemaVersion\":1,\"kind\":\"items\",\"id\":\"{id}\",{extendsPart}\"nameKey\":\"n\",\"category\":\"weapon\",\"footprint\":[2,3],\"maxStack\":1,\"weight\":0.1,\"tier\":1,\"icon\":\"i\",{extraPart}\"weapon\":{{\"classId\":\"wclass_rifle\",\"damageTypeId\":\"dmg_bullet\",\"damage\":1,\"accuracy\":0.5,\"magazine\":10,\"ammoCaliber\":\"5.45x39\",\"reloadTime\":2,\"armorPenetration\":0,\"durabilityMax\":10,\"durabilityPerShot\":0.1,\"defaultSkillId\":\"skill_shot\"}}}}";
        }

        protected void WithFixture(Dictionary<string, string> files, Action<string> body)
        {
            var root = Path.Combine(Path.GetTempPath(), "anomalypath-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            try
            {
                foreach (var pair in files)
                {
                    var full = Path.Combine(root, pair.Key.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                    File.WriteAllText(full, pair.Value);
                }

                body(root);
            }
            finally
            {
                try
                {
                    Directory.Delete(root, recursive: true);
                }
                catch (IOException)
                {
                    // временная папка не критична для результата теста
                }
            }
        }

        protected LoadedData Load(string dataRoot) =>
            new DataLoader(new FileSystemDataSource(dataRoot)).Load();

        protected void Check(string name, Action body)
        {
            try
            {
                body();
                _passed++;
                Log($"OK   {name}");
            }
            catch (Exception error)
            {
                _failed++;
                Log($"FAIL {name}: {error.Message}");
                _failures.Add($"{name}: {error.Message}");
            }
        }

        protected void True(bool value, string what)
        {
            if (!value)
            {
                throw new InvalidOperationException($"Ожидалось true: {what}");
            }
        }

        protected void False(bool value, string what)
        {
            if (value)
            {
                throw new InvalidOperationException($"Ожидалось false: {what}");
            }
        }

        protected void Equal<T>(T expected, T actual, string what)
        {
            if (!EqualityComparer<T>.Default.Equals(expected, actual))
            {
                throw new InvalidOperationException($"{what}: ожидалось {expected}, получено {actual}");
            }
        }

        protected void Contains(string text, string fragment, string what)
        {
            if (text.IndexOf(fragment, StringComparison.OrdinalIgnoreCase) < 0)
            {
                throw new InvalidOperationException($"{what}: '{text}' не содержит '{fragment}'");
            }
        }

        protected T Throws<T>(Action body, string what)
            where T : Exception
        {
            try
            {
                body();
            }
            catch (T error)
            {
                return error;
            }
            catch (Exception other)
            {
                throw new InvalidOperationException($"{what}: ожидалось {typeof(T).Name}, получено {other.GetType().Name}: {other.Message}");
            }

            throw new InvalidOperationException($"{what}: исключение не брошено");
        }
    }
}
