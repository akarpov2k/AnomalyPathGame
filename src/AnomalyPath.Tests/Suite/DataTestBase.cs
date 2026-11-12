#nullable enable
using AnomalyPath.Core;
using AnomalyPath.Core.Di;

namespace AnomalyPath.Tests
{
    /// <summary>Категория тестов, которой нужна папка данных проекта.</summary>
    public abstract class DataTestBase : TestBase
    {
        /// <summary>Корень данных, переданный раннером.</summary>
        protected readonly string _dataRoot;

        /// <summary>Создаёт категорию для папки данных.</summary>
        /// <param name="dataRoot">Корень данных проекта (в редакторе — <c>Assets/Data</c>).</param>
        protected DataTestBase(string dataRoot)
        {
            _dataRoot = dataRoot;
        }

        /// <summary>Собирает новый контейнер сервисов из данных проекта.</summary>
        /// <returns>Контейнер с репозиториями и сервисами.</returns>
        /// <exception cref="AnomalyPath.Core.Validation.ValidationException">Данные не прошли проверку.</exception>
        protected ServiceContainer BuildServices() => Bootstrap.Build(_dataRoot);
    }
}
