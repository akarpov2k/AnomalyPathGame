#nullable enable
namespace AnomalyPath.Core.Spawns
{
    /// <summary>Вид объекта локации: код в данных — имя члена в нижнем регистре.</summary>
    public enum LocationObjectKind
    {
        /// <summary>Объект лута.</summary>
        Loot,

        /// <summary>Тайник.</summary>
        Stash,

        /// <summary>NPC.</summary>
        Npc,

        /// <summary>Противник: на локации не больше одного.</summary>
        Enemy,

        /// <summary>Аномалия: источник артефактов с фиксированным уроном за попытку.</summary>
        Anomaly
    }
}
