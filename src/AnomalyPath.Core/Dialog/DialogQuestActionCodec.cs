#nullable enable
using System;

namespace AnomalyPath.Core.Dialog
{
    /// <summary>Код действия с квестом в JSON диалога.</summary>
    public static class DialogQuestActionCodec
    {
        /// <summary>Возвращает код действия.</summary>
        /// <param name="action">Действие.</param>
        /// <returns>Код.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Действие неизвестно.</exception>
        public static string ToCode(DialogQuestAction action) => action switch
        {
            DialogQuestAction.Give => "give",
            DialogQuestAction.TurnIn => "turnIn",
            _ => throw new ArgumentOutOfRangeException(nameof(action))
        };

        /// <summary>Разбирает код действия.</summary>
        /// <param name="code">Код из JSON.</param>
        /// <param name="action">Разобранное действие.</param>
        /// <returns><c>true</c>, если код известен.</returns>
        public static bool TryParse(string? code, out DialogQuestAction action)
        {
            switch (code)
            {
                case "give":
                    action = DialogQuestAction.Give;
                    return true;
                case "turnIn":
                    action = DialogQuestAction.TurnIn;
                    return true;
                default:
                    action = default;
                    return false;
            }
        }
    }
}
