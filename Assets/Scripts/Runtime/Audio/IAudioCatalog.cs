#nullable enable
using UnityEngine;

namespace AnomalyPath.Runtime.Audio
{
    /// <summary>
    /// Каталог звуков по строковому id из данных: эмбиенты локаций и музыка.
    /// Заполняется редакторным инструментом <c>Tools/AnomalyPath/Собрать каталог звуков</c>,
    /// ссылка на ассет лежит на <c>GameScreen</c>.
    /// </summary>
    public interface IAudioCatalog
    {
        /// <summary>Ищет клип по id из данных.</summary>
        /// <param name="id">Id из данных: <c>amb-atp</c>, <c>mus-menu-theme</c>.</param>
        /// <param name="clip">Найденный клип или <c>null</c>.</param>
        /// <returns><c>true</c>, если клип есть в каталоге.</returns>
        bool TryGet(string id, out AudioClip? clip);
    }
}
