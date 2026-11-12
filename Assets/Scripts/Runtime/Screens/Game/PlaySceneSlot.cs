#nullable enable
namespace AnomalyPath.Runtime.Screens.Game
{
    /// <summary>Место объекта-перехода на сцене локации.</summary>
    public enum PlaySceneSlot
    {
        /// <summary>Место не назначено.</summary>
        None = 0,

        /// <summary>По центру дороги: продолжение пути.</summary>
        Center = 1,

        /// <summary>Справа ниже центра: ближняя ветка.</summary>
        RightLow = 2,

        /// <summary>Слева выше центра: дальняя ветка на другой стороне дороги.</summary>
        LeftHigh = 3
    }
}
