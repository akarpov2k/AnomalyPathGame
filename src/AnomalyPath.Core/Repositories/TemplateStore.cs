#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using AnomalyPath.Core.Data;
using AnomalyPath.Core.Json;

namespace AnomalyPath.Core.Repositories
{
    /// <summary>
    /// Общее хранилище шаблонов по строковому id: проверка дублей, поиск и код ошибки
    /// «не найдено». Репозитории передают фабрику исключений, сохраняя свои коды.
    /// </summary>
    /// <typeparam name="T">Тип шаблона.</typeparam>
    public sealed class TemplateStore<T>
        where T : class
    {
        private readonly Dictionary<string, T> _byId;
        private readonly ReadOnlyCollection<T> _all;
        private readonly Func<string, DataException> _notFound;

        /// <summary>Создаёт хранилище из готовых шаблонов.</summary>
        /// <param name="templates">Шаблоны.</param>
        /// <param name="idOf">Как получить id шаблона.</param>
        /// <param name="duplicate">Исключение для повторяющегося id.</param>
        /// <param name="notFound">Исключение для отсутствующего id.</param>
        /// <exception cref="ArgumentNullException">Обязательный аргумент не задан.</exception>
        public TemplateStore(
            IEnumerable<T> templates,
            Func<T, string> idOf,
            Func<string, DataException> duplicate,
            Func<string, DataException> notFound)
        {
            if (templates == null)
            {
                throw new ArgumentNullException(nameof(templates));
            }

            if (idOf == null)
            {
                throw new ArgumentNullException(nameof(idOf));
            }

            if (duplicate == null)
            {
                throw new ArgumentNullException(nameof(duplicate));
            }

            _notFound = notFound ?? throw new ArgumentNullException(nameof(notFound));
            _byId = new Dictionary<string, T>(StringComparer.Ordinal);
            var all = new List<T>();
            foreach (var template in templates)
            {
                var id = idOf(template);
                if (_byId.ContainsKey(id))
                {
                    throw duplicate(id);
                }

                _byId.Add(id, template);
                all.Add(template);
            }

            _all = all.AsReadOnly();
        }

        /// <summary>Все шаблоны в порядке добавления.</summary>
        public IReadOnlyList<T> All => _all;

        /// <summary>Ищет шаблон по id.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <param name="template">Найденный шаблон или <c>null</c>.</param>
        /// <returns><c>true</c>, если шаблон найден.</returns>
        public bool TryGet(string id, out T? template) => _byId.TryGetValue(id, out template);

        /// <summary>Возвращает шаблон по id.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <returns>Шаблон.</returns>
        /// <exception cref="DataException">Шаблон не найден; код задаёт репозиторий.</exception>
        public T Get(string id) =>
            _byId.TryGetValue(id, out var template) ? template : throw _notFound(id);

        /// <summary>Проверяет наличие шаблона.</summary>
        /// <param name="id">Идентификатор.</param>
        /// <returns><c>true</c>, если шаблон есть.</returns>
        public bool Contains(string id) => _byId.ContainsKey(id);

        /// <summary>Читает шаблоны из загруженных сущностей одного вида.</summary>
        /// <param name="data">Результат загрузки.</param>
        /// <param name="kind">Вид сущности (например, <c>items</c>).</param>
        /// <param name="factory">Разбор одной сущности; второй аргумент — путь для ошибки.</param>
        /// <returns>Прочитанные шаблоны в порядке данных.</returns>
        /// <exception cref="ArgumentNullException">Данные или фабрика не заданы.</exception>
        public static IReadOnlyList<T> Load(
            LoadedData data,
            string kind,
            Func<JsonValue, string, T> factory)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (factory == null)
            {
                throw new ArgumentNullException(nameof(factory));
            }

            var result = new List<T>();
            foreach (var entity in data.All(kind))
            {
                var path = entity.TryGet("id", out var id) ? id.Text : kind;
                result.Add(factory(entity, path));
            }

            return result;
        }
    }
}
