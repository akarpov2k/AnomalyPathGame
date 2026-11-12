#nullable enable
using System.Collections.Generic;
using AnomalyPath.Core.Spawns;
using AnomalyPath.Core.World;

namespace AnomalyPath.Core.Abstractions
{
    /// <summary>
    /// Спавн объектов локации при входе: подходящие записи её пула бросают свой шанс,
    /// число объектов ограничено шаблоном, противник появляется не больше одного раза.
    /// </summary>
    public interface ILocationObjectService
    {
        /// <summary>
        /// Выполняет спавн объектов локации, если он ещё не выполнялся; повторный вызов
        /// возвращает уже появившиеся объекты и ничего не перебрасывает.
        /// </summary>
        /// <param name="location">Экземпляр локации.</param>
        /// <returns>Объекты, появившиеся на локации; пустой список, если ничего не выпало.</returns>
        IReadOnlyList<LocationObject> Spawn(LocationInstance location);
    }
}
