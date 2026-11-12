#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace AnomalyPath.Editor.AutoPlay
{
    /// <summary>
    /// Журнал автопрогона: действия с временем, помощник ассиста и итоговый отчёт.
    /// Пишется построчно в файл, чтобы прогон можно было читать по ходу и после падения.
    /// </summary>
    internal sealed class AutoPlayLog
    {
        private readonly string _path;
        private readonly StringBuilder _lines = new StringBuilder();
        private readonly DateTime _startedAt = DateTime.Now;

        /// <summary>Создаёт журнал и файл прогона.</summary>
        /// <param name="path">Путь текстового журнала.</param>
        public AutoPlayLog(string path)
        {
            _path = path;
            var directory = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            Write("автопрогон начат: " + _startedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        }

        /// <summary>Время с начала прогона в формате <c>чч:мм:сс</c>.</summary>
        public string Elapsed => (DateTime.Now - _startedAt).ToString(@"hh\:mm\:ss");

        /// <summary>Путь журнала.</summary>
        public string Path => _path;

        /// <summary>Записывает шаг прогона.</summary>
        /// <param name="message">Что происходит.</param>
        public void Step(string message) => Write("[шаг] " + message);

        /// <summary>Записывает работу ассиста: лечение, патроны, отдых.</summary>
        /// <param name="message">Что сделал ассист и почему.</param>
        public void Assist(string message) => Write("[ассист] " + message);

        /// <summary>Записывает предупреждение: прогон продолжается.</summary>
        /// <param name="message">Что пошло не так.</param>
        public void Warn(string message) => Write("[внимание] " + message);

        /// <summary>Записывает ошибку: сценарий не может продолжиться.</summary>
        /// <param name="message">Что именно не удалось.</param>
        public void Error(string message) => Write("[ошибка] " + message);

        /// <summary>Записывает итог прогона отдельным файлом рядом с журналом.</summary>
        /// <param name="report">Текст отчёта.</param>
        public void WriteReport(string report)
        {
            var reportPath = System.IO.Path.ChangeExtension(_path, ".report.txt");
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));
            Write("[итог] " + report.Replace("\r\n", " | "));
        }

        /// <summary>Все строки журнала.</summary>
        public IReadOnlyList<string> Lines => _lines.ToString().Split('\n');

        private void Write(string message)
        {
            var line = "[" + Elapsed + "] " + message;
            _lines.AppendLine(line);
            File.AppendAllText(_path, line + Environment.NewLine, new UTF8Encoding(false));
            Debug.Log("[AutoPlay] " + line);
        }
    }
}
