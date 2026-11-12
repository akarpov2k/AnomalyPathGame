#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core;

namespace AnomalyPath.Tests
{
    /// <summary>Часть класса <see cref="ValidationErrorTests"/>: ошибки диалогов.</summary>
    public sealed partial class ValidationErrorTests
    {
        private void DialogErrorTests()
        {
            var cases = new (string Name, string Dialog, string? ExtraFile, string? ExtraJson, string Fragment)[]
            {
                (
                    "ошибка: начальный узел диалога не найден",
                    Dialog("\"startNodeId\":\"n9\",\"nodes\":[" + Node("n1", OptionEnd()) + "]"),
                    null,
                    null,
                    "Начальный узел"),
                (
                    "ошибка: у узла диалога нет вариантов",
                    Dialog("\"startNodeId\":\"n1\",\"nodes\":[{\"id\":\"n1\",\"textKey\":\"t\"," +
                           "\"options\":[]}]"),
                    null,
                    null,
                    "нет вариантов ответа"),
                (
                    "ошибка: следующий узел варианта не найден",
                    Dialog("\"startNodeId\":\"n1\",\"nodes\":[" +
                           Node("n1", Option("o1", "\"nextNodeId\":\"n9\"")) + "]"),
                    null,
                    null,
                    "Следующий узел"),
                (
                    "ошибка: говорящий в диалоге не найден",
                    Dialog("\"startNodeId\":\"n1\",\"nodes\":[" +
                           "{\"id\":\"n1\",\"speakerActorId\":\"npc_missing\",\"textKey\":\"t\"," +
                           "\"options\":[" + OptionEnd() + "]}]"),
                    null,
                    null,
                    "Говорящий"),
                (
                    "ошибка: последствие ссылается на неизвестный квест",
                    Dialog("\"startNodeId\":\"n1\",\"nodes\":[" +
                           Node("n1", Option(
                               "o1",
                               "\"effects\":[{\"type\":\"quest\",\"action\":\"give\"," +
                               "\"questId\":\"qst_missing\"}],\"end\":true")) + "]"),
                    null,
                    null,
                    "неизвестный сюжетный квест"),
                (
                    "ошибка: узел диалога недостижим",
                    Dialog("\"startNodeId\":\"n1\",\"nodes\":[" +
                           Node("n1", OptionEnd()) + "," + Node("n2", OptionEnd()) + "]"),
                    null,
                    null,
                    "недостижим"),
                (
                    "ошибка: диалог у актёра не найден",
                    Dialog("\"startNodeId\":\"n1\",\"nodes\":[" + Node("n1", OptionEnd()) + "]"),
                    "actors/npc.json",
                    "{\"schemaVersion\":1,\"kind\":\"actors\",\"id\":\"npc_x\",\"type\":\"npc\"," +
                    "\"nameKey\":\"n\",\"dialogId\":\"dlg_missing\"}",
                    "Диалог не найден")
            };

            foreach (var testCase in cases)
            {
                Check(testCase.Name, () =>
                {
                    var files = new Dictionary<string, string>
                    {
                        ["catalog.json"] = testCase.ExtraFile == null
                            ? Catalog("dialogs/dlg_x.json")
                            : Catalog("dialogs/dlg_x.json", testCase.ExtraFile),
                        ["dialogs/dlg_x.json"] = testCase.Dialog
                    };
                    if (testCase.ExtraFile != null && testCase.ExtraJson != null)
                    {
                        files[testCase.ExtraFile] = testCase.ExtraJson;
                    }

                    WithFixture(
                        files,
                        root =>
                        {
                            var report = Preflight.Run(root).Report;
                            True(
                                HasAnyError(report, testCase.Fragment),
                                $"нет ошибки: {testCase.Fragment}; {Describe(report)}");
                        });
                });
            }
        }

        private static string Dialog(string body)
        {
            return "{\"schemaVersion\":1,\"kind\":\"dialogs\",\"id\":\"dlg_x\"," + body + "}";
        }

        private static string Node(string id, string option)
        {
            return "{\"id\":\"" + id + "\",\"textKey\":\"t\",\"options\":[" + option + "]}";
        }

        private static string Option(string id, string body)
        {
            return "{\"id\":\"" + id + "\",\"textKey\":\"t\"," + body + "}";
        }

        private static string OptionEnd() => Option("o1", "\"end\":true");
    }
}
