#nullable enable
namespace AnomalyPath.Core.Loot
{
    /// <summary>Чем закончилась попытка вскрыть тайник.</summary>
    public enum StashOpenStatus
    {
        /// <summary>На локации нет тайника.</summary>
        NoStash = 0,

        /// <summary>Тайник уже вскрыт.</summary>
        AlreadyOpened = 1,

        /// <summary>Тайник вскрыт, лут выдан.</summary>
        Opened = 2,

        /// <summary>Замок ещё не вскрыт верным кодом.</summary>
        Locked = 3,

        /// <summary>Попытки взлома кончились, тайник потерян.</summary>
        LockFailed = 4
    }
}
